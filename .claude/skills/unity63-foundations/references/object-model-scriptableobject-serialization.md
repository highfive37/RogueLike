# Unity Object Model, ScriptableObject, and Serialization Fundamentals

## Scope

Explains UnityEngine.Object native/managed identity, Unity null behavior, ScriptableObject assets, field serialization, managed references, unsupported shapes, and the distinction between authored data and mutable runtime state.

Read this reference when the request involves:

- UnityEngine.Object null behavior or destroyed-object checks
- ScriptableObject usage, asset-backed configuration, or shared data
- Inspector serialization, field eligibility, polymorphism, dictionaries, properties, records, or hot reload

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
- **ScriptableObject**
- **Unity serialization**
- **Managed references**
- **Asset-backed data**
- **UnityEngine.Object**
- **UnityEngine.ScriptableObject**
- **SerializeField**
- **SerializeReference**
- **ISerializationCallbackReceiver**

## Verified Unity 6.3 facts

- `UnityEngine.Object` is the base class for objects Unity can reference, and each instance is linked to a native counterpart. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.html))
- When the native counterpart is destroyed, the managed wrapper can remain detached and Unity equality compares it as null even though `ReferenceEquals` is false. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.html))
- The null-conditional and null-coalescing operators do not use Unity’s overloaded object null semantics and are unsafe for references that might be detached. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.html))
- ScriptableObject derives from UnityEngine.Object, is not attached to a GameObject, and is commonly saved as an asset for shared data. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html))
- A ScriptableObject can reduce duplicated serialized data when many prefab instances reference the same asset. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html))
- In a deployed build, ScriptableObject assets provide authored data but are not a general mechanism for persisting arbitrary runtime changes back to project assets. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html))
- Unity serializes eligible fields rather than ordinary C# properties; public fields serialize by default unless excluded, and private fields can opt in with `SerializeField`. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- A field must be non-static, non-const, non-readonly, and of a supported serializable type to participate in Unity field serialization. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- Custom non-Unity classes are serialized inline by value by default, which duplicates shared references and does not preserve polymorphic identity. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- `SerializeReference` opts eligible managed-reference fields into reference-based polymorphic serialization with additional overhead and restrictions. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeReference.html))
- Unity does not directly support multilevel containers such as nested arrays or dictionaries through field serialization; wrappers or serialization callbacks are required. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- Unity 6.3 uses C# 9 but Unity serialization does not support records as serialized data shapes. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html))
- Serialization callbacks can transform unsupported runtime structures to supported serialized fields, but Unity places restrictions on what may be called from callback methods. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ISerializationCallbackReceiver.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Use ScriptableObject assets for authored, shared, inspectable configuration or catalogs. Create separate runtime copies or state objects when mutation must be isolated.
- **Interpretation:** Keep serialized fields as a stable data contract. Rename fields with an explicit migration mechanism and avoid changing type shape without considering existing assets and scenes.
- **Interpretation:** Prefer ordinary inline serialization for small value-like data. Use `SerializeReference` only when preserved identity, null, cycles, or polymorphism is required and tested.
- **Interpretation:** Hide fields with private access plus `SerializeField` when external mutation is not part of the runtime API; Inspector visibility is not a reason to make a field public.
- **Interpretation:** Avoid doing domain work inside serialization callbacks. Restrict them to deterministic data conversion that does not depend on Unity object graphs being fully restored.
- **Interpretation:** Use Unity object equality (`if (obj)` or `obj == null`) for UnityEngine.Object lifetime checks and reserve `ReferenceEquals` for diagnosing managed-wrapper state.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Shared authored configuration | ScriptableObject asset | One asset reference avoids copying the same serialized payload into every component. |
| Per-instance mutable state | Plain runtime object or component state | Avoid mutating a shared asset accidentally. |
| Value-like nested custom data | Inline serializable class/struct | Simple and compact when identity is irrelevant. |
| Polymorphic managed graph | `SerializeReference` with known types | Preserves managed identity but increases complexity and migration risk. |

## Risks and review checks

- Do not use `?.` or `??` as the only destroyed-object check for UnityEngine.Object references.
- Do not assume a ScriptableObject reference is an immutable singleton; it is an asset object and can be mutated unless the design prevents it.
- Do not expose serialized implementation fields as public merely for Inspector access.
- Do not expect dictionaries, jagged arrays, nested lists, delegates, or arbitrary interfaces to serialize without a supported representation.
- Do not reorder or rename managed-reference types without migration tests for existing serialized data.
- Do not call broad Unity APIs from `OnBeforeSerialize` or `OnAfterDeserialize`; callback timing and thread constraints require narrow conversion logic.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Object.operator ==`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object-operator_eq.html) | `public static bool operator ==(Object x, Object y)` | Runtime | Compares Unity object identity and treats detached/destroyed objects as null. |
| [`UnityEngine.Object.bool`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object-operator_Object.html) | `public static implicit operator bool(Object exists)` | Runtime | Returns whether the Unity object exists from Unity’s native-object perspective. |
| [`UnityEngine.ScriptableObject.CreateInstance<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ScriptableObject.CreateInstance.html) | `public static T CreateInstance<T>() where T : ScriptableObject` | Runtime | Creates a runtime ScriptableObject instance; creating an asset additionally requires Editor AssetDatabase APIs. |
| [`UnityEngine.CreateAssetMenuAttribute`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CreateAssetMenuAttribute.html) | `[CreateAssetMenu(fileName = "...", menuName = "...", order = ...)]` | Runtime | Adds a ScriptableObject type to the Editor Create menu. |
| [`UnityEngine.SerializeField`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeField.html) | `[SerializeField]` | Runtime | Makes an eligible nonpublic field part of Unity serialization. |
| [`UnityEngine.SerializeReference`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeReference.html) | `[SerializeReference]` | Runtime | Serializes an eligible managed object by reference rather than inline value. |
| [`UnityEngine.ISerializationCallbackReceiver`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ISerializationCallbackReceiver.html) | `void OnBeforeSerialize(); void OnAfterDeserialize()` | Runtime | Custom serialization conversion hooks; keep calls thread-safe and data-focused. |
| [`UnityEditor.EditorUtility.SetDirty`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorUtility.SetDirty.html) | `public static void SetDirty(Object target)` | Editor | Marks a non-scene asset object dirty after Editor-side modification when the serialized workflow does not do so automatically. |

## Official sources

- Class Object — https://docs.unity3d.com/6000.3/Documentation/Manual/class-Object.html
- Class Scriptable Object — https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html
- Script Serialization — https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization.html
- Script Serialization Rules — https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html
- Csharp Compiler — https://docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html
- Object — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.html
- Object Operator Eq — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object-operator_eq.html
- Object Operator Object — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object-operator_Object.html
- Scriptable Object — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ScriptableObject.html
- Scriptable Object.Create Instance — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ScriptableObject.CreateInstance.html
- Create Asset Menu Attribute — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CreateAssetMenuAttribute.html
- Serialize Field — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeField.html
- Serialize Reference — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeReference.html
- Iserialization Callback Receiver — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ISerializationCallbackReceiver.html
- Editor Utility.Set Dirty — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorUtility.SetDirty.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
