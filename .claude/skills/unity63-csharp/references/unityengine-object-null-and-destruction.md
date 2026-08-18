# UnityEngine.Object Lifetime, Equality, Null, and Destruction

## Scope

Explains the managed/native dual lifetime of UnityEngine.Object, overloaded equality and Boolean checks, fake-null behavior, destruction timing, and safe reference use.

Read this reference when the request involves:

- a Unity object compares equal to null unexpectedly
- MissingReferenceException or destroyed-object access occurs
- reviewing `?.`, `??`, caching, destruction, or reference lifetime

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **UnityEngine.Object**
- **managed wrapper and native object**
- **custom null semantics**
- **destruction**
- **Object.Destroy**
- **Object.Instantiate**
- **Object.operator ==**
- **Object.FindObjectsByType**

## Verified Unity 6.3 facts

- UnityEngine.Object is the base class for objects managed by Unity’s native engine and exposed through managed C# wrappers. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-Object.html))
- A UnityEngine.Object wrapper can remain as a managed object after its native engine object has been destroyed. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.html))
- Unity overloads equality and implicit Boolean conversion so a destroyed Unity object can compare equal to null even when the managed wrapper reference is not CLR-null. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object-operator_eq.html))
- The null-conditional and null-coalescing operators do not invoke UnityEngine.Object’s custom equality operators and are not supported as destroyed-object checks. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.html))
- `Object.Destroy` schedules destruction after the current Update loop and before rendering; an optional delay uses scaled time. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Destroy.html))
- `Object.DestroyImmediate` destroys immediately and the ScriptReference warns that it should normally be used only in Editor code. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.DestroyImmediate.html))
- `Object.Instantiate` clones a Unity object and returns a distinct UnityEngine.Object instance. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Instantiate.html))
- Unity object instance IDs identify an object instance during its lifetime but are not documented as stable persistence identifiers. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.GetInstanceID.html))
- Find APIs have scope and sorting choices; broad searches are not a substitute for explicit dependency references. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.FindObjectsByType.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Use `if (unityObject)` or `unityObject != null` when the requirement is “the Unity native object is still alive.” Use ReferenceEquals only when deliberately inspecting the managed wrapper itself.
- **Interpretation:** After requesting destruction, stop publishing the object to new consumers and clear owner references at a deterministic teardown boundary.
- **Interpretation:** Treat every cached Unity object reference as bounded by the lifetime of its scene, prefab instance, asset load handle, or owning system.
- **Interpretation:** Never persist instance IDs as save identifiers. Use authored GUIDs/logical IDs whose resolution is explicitly controlled.
- **Interpretation:** Avoid `?.` and `??` for Unity object lifetime checks; make the Unity-null test explicit before member access.
- **Interpretation:** If an async continuation captures a Unity object, couple the operation to destruction cancellation and re-check liveness after suspension.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Need to know whether native Unity object remains valid | `obj != null` or `if (obj)` | Uses UnityEngine.Object lifetime semantics. |
| Need to inspect whether managed reference itself is CLR-null | `ReferenceEquals(obj, null)` | Does not detect a destroyed native object. |
| Normal runtime teardown | `Object.Destroy` | Respects Unity’s delayed destruction lifecycle. |
| Editor asset manipulation with immediate removal required | `DestroyImmediate` with documented safeguards | Never make it the default runtime path. |

## Risks and review checks

- Do not replace `obj != null` with `obj?.Member` on a UnityEngine.Object during a generic C# cleanup.
- Do not catch MissingReferenceException and continue as if state were valid; fix ownership or cancellation.
- Do not call Destroy repeatedly from multiple owners; designate one owner for lifetime.
- Do not assume a destroyed component’s C# fields imply its GameObject/native object remains usable.
- Do not use instance IDs, names, or hierarchy paths as durable save keys without a separately managed identity policy.
- Do not run broad find calls in hot paths or use them to hide missing serialized references.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Object.operator ==`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object-operator_eq.html) | `public static bool operator ==(Object x, Object y)` | Runtime | Unity-aware equality including destroyed native objects. |
| [`UnityEngine.Object.implicit operator bool`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object-operator_Object.html) | `public static implicit operator bool(Object exists)` | Runtime | True when the Unity object exists natively. |
| [`UnityEngine.Object.Destroy`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Destroy.html) | `public static void Destroy(Object obj, float t = 0.0F)` | Runtime | Schedules native object destruction; delayed time is scaled. |
| [`UnityEngine.Object.DestroyImmediate`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.DestroyImmediate.html) | `public static void DestroyImmediate(Object obj, bool allowDestroyingAssets = false)` | Editor-focused | Immediate destruction intended primarily for Editor use; asset deletion risk. |
| [`UnityEngine.Object.Instantiate<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Instantiate.html) | `public static T Instantiate<T>(T original) where T : Object` | Runtime | Clones a Unity object; placement/parent overloads exist. |
| [`UnityEngine.Object.GetInstanceID`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.GetInstanceID.html) | `public int GetInstanceID()` | Runtime | Returns the current object instance ID, not a persistence contract. |
| [`UnityEngine.Object.FindObjectsByType<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.FindObjectsByType.html) | `public static T[] FindObjectsByType<T>(FindObjectsInactive includeInactive, FindObjectsSortMode sortMode)` | Runtime | Finds loaded objects of type T with explicit inactive/sort behavior. |

## Official sources

- Class Object — https://docs.unity3d.com/6000.3/Documentation/Manual/class-Object.html
- Object — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.html
- Object Operator Eq — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object-operator_eq.html
- Object Operator Object — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object-operator_Object.html
- Object.Destroy — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Destroy.html
- Object.Destroy Immediate — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.DestroyImmediate.html
- Object.Instantiate — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Instantiate.html
- Object.Get Instance Id — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.GetInstanceID.html
- Object.Find Objects By Type — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.FindObjectsByType.html
- Mono Behaviour Destroy Cancellation Token — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour-destroyCancellationToken.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
