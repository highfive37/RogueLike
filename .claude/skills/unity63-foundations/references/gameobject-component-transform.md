# GameObject, Component, Transform, and Hierarchy

## Scope

Defines Unity’s core scene object composition model and the exact distinctions among GameObject identity, Component behavior, Transform hierarchy, activation, lookup, and destruction.

Read this reference when the request involves:

- GameObject versus Component responsibilities
- Transform parent/child behavior or local versus world space
- component lookup, activation, creation, destruction, or composition

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **GameObjects**
- **Components**
- **Transforms**
- **Hierarchy**
- **Activation**
- **UnityEngine.GameObject**
- **UnityEngine.Component**
- **UnityEngine.Transform**
- **UnityEngine.Object**

## Verified Unity 6.3 facts

- A GameObject is a container whose functionality is defined by the Components attached to it. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/gameobject-fundamentals.html))
- Every GameObject has a Transform component; the Transform stores position, rotation, scale, and hierarchy relationships. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/gameobject-fundamentals.html))
- Components cannot exist independently of a GameObject and expose their owning `gameObject` and `transform` references through the Component base class. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Component.html))
- A GameObject can be active in itself while inactive in the hierarchy because an inactive ancestor suppresses its descendants. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject-activeInHierarchy.html))
- Calling `SetActive` changes the GameObject local activation state and can trigger `OnEnable` or `OnDisable` when the effective hierarchy state changes. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.SetActive.html))
- Transform child positions, rotations, and scales are interpreted relative to their parent, while world-space properties expose the resolved transform. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-Transform.html))
- Non-uniform parent scale can produce skew-like effects in descendants, so hierarchy and scaling choices affect spatial behavior. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-Transform.html))
- `GetComponent<T>` returns the first matching component on the same GameObject; other APIs cover children, parents, and multiple matches. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.GetComponent.html))
- `AddComponent<T>` adds a component type to a GameObject; components with dependency attributes can cause required components to be added. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.AddComponent.html))
- `Object.Destroy` schedules removal and actual destruction occurs after the current Update loop but before rendering. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Destroy.html))
- GameObject tags, layers, names, activation, and component composition are independent concerns; none should be used as a substitute for explicit ownership. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Make a GameObject responsible for spatial identity and composition, not for being a monolithic domain object. Put each runtime responsibility in a focused component or plain C# collaborator.
- **Interpretation:** Cache stable required component references rather than repeating broad lookups in hot paths. Keep discovery at composition or initialization boundaries.
- **Interpretation:** Use parent-child relationships when lifetime and transform inheritance are genuinely shared; avoid hierarchy as a general-purpose service locator.
- **Interpretation:** Treat activation as lifecycle control: disabling an object can suspend enabled components and callbacks but does not erase its serialized state.
- **Interpretation:** Prefer typed component references over name/tag searches for core dependencies, because typed references make contracts reviewable and refactorable.
- **Interpretation:** When destroying objects, assume the managed wrapper may remain until collection and use Unity object truth/equality semantics rather than ordinary CLR assumptions.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Behavior needs scene presence and Unity callbacks | MonoBehaviour component | Attach the behavior to a GameObject and keep dependencies explicit. |
| Behavior has no scene identity or Unity callback requirement | Plain C# object | Avoid unnecessary Component lifetime and serialization coupling. |
| Objects share both transform and destruction lifetime | Parent-child hierarchy | The hierarchy communicates ownership and inherited transform. |
| Objects only need logical coordination | Reference or event relationship | Do not create transform coupling solely to find another object. |

## Risks and review checks

- Do not call `new` for GameObject components; create a GameObject or call `AddComponent`.
- Do not assume `activeSelf` means callbacks run; inspect `activeInHierarchy` and component `enabled` state.
- Do not combine non-uniform scale and rotated descendants without testing the resulting transforms and physics behavior.
- Do not rely on `GetComponent` order when multiple components of the same type exist unless the design makes the choice unambiguous.
- Do not call `DestroyImmediate` in normal runtime code; the ScriptReference limits it to Edit mode use with caution.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.GameObject.GameObject`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject-ctor.html) | `public GameObject(string name); public GameObject(string name, params Type[] components)` | Runtime | Creates a GameObject and optionally adds component types. |
| [`UnityEngine.GameObject.AddComponent<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.AddComponent.html) | `public T AddComponent<T>() where T : Component` | Runtime | Adds T and returns the new component. |
| [`UnityEngine.GameObject.GetComponent<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.GetComponent.html) | `public T GetComponent<T>()` | Runtime | Returns the first matching component on the same GameObject or null when absent. |
| [`UnityEngine.GameObject.TryGetComponent<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.TryGetComponent.html) | `public bool TryGetComponent<T>(out T component)` | Runtime | Returns success and writes the matching component without requiring a separate null test. |
| [`UnityEngine.GameObject.SetActive`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.SetActive.html) | `public void SetActive(bool value)` | Runtime | Changes `activeSelf`; effective activation also depends on ancestors. |
| [`UnityEngine.Transform.SetParent`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Transform.SetParent.html) | `public void SetParent(Transform parent, bool worldPositionStays)` | Runtime | Changes hierarchy; the Boolean controls whether the world transform is preserved. |
| [`UnityEngine.Object.Destroy`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Destroy.html) | `public static void Destroy(Object obj); public static void Destroy(Object obj, float t)` | Runtime | Schedules destruction; use the delayed overload only when delayed teardown is intended. |
| [`UnityEngine.RequireComponent`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RequireComponent.html) | `[RequireComponent(typeof(T))]` | Runtime | Declares component prerequisites when the script is added; it does not retroactively repair all existing objects. |

## Official sources

- Working With Gameobjects — https://docs.unity3d.com/6000.3/Documentation/Manual/working-with-gameobjects.html
- Gameobject Fundamentals — https://docs.unity3d.com/6000.3/Documentation/Manual/gameobject-fundamentals.html
- Components — https://docs.unity3d.com/6000.3/Documentation/Manual/Components.html
- Class Transform — https://docs.unity3d.com/6000.3/Documentation/Manual/class-Transform.html
- Game Object — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.html
- Component — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Component.html
- Transform — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Transform.html
- Game Object Active In Hierarchy — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject-activeInHierarchy.html
- Game Object.Set Active — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.SetActive.html
- Game Object.Get Component — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.GetComponent.html
- Game Object.Add Component — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.AddComponent.html
- Game Object.Try Get Component — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.TryGetComponent.html
- Transform.Set Parent — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Transform.SetParent.html
- Object.Destroy — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Destroy.html
- Require Component — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RequireComponent.html
- Game Object Ctor — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject-ctor.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
