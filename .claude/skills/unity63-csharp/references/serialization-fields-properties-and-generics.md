# Serialization: Fields, References, Containers, and Refactoring

## Scope

Provides exact Unity 6.3 serialization boundaries for fields, Unity object references, inline values, managed references, containers, callbacks, and safe field renaming.

Read this reference when the request involves:

- a field is missing from the Inspector or fails to persist
- designing serializable data, polymorphism, references, collections, or field renames
- reviewing domain reload, hot reload, prefab, scene, or ScriptableObject data

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Unity serialization rules**
- **inline and managed references**
- **field attributes**
- **safe data evolution**
- **SerializeField**
- **SerializeReference**
- **ISerializationCallbackReceiver**
- **FormerlySerializedAs**

## Verified Unity 6.3 facts

- Unity serializes fields that meet its visibility, attribute, mutability, type, and container rules; ordinary properties are not the primary serialized members. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- Public fields are eligible unless excluded and private/protected fields can be included with `SerializeField` when their types are supported. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- Static, const, readonly, and unsupported field shapes are not serialized under the documented field rules. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- Unity supports a documented set of primitive, enum, Unity built-in, UnityEngine.Object, serializable custom, and one-dimensional array/List field types. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- Nested container shapes such as multidimensional arrays, jagged arrays, dictionaries, and nested generic containers require a supported wrapper or custom serialization approach. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- Custom serializable classes are normally serialized inline by value; repeated references can become separate serialized copies. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- `SerializeReference` enables managed-reference serialization for supported non-UnityEngine.Object references, preserving reference identity and polymorphic types with added overhead and constraints. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- UnityEngine.Object-derived fields are serialized as references to Unity objects rather than inline managed values. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- ISerializationCallbackReceiver provides callbacks before serialization and after deserialization for transforming unsupported data, with restrictions on Unity API use and callback complexity. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ISerializationCallbackReceiver.html))
- FormerlySerializedAs records a previous field name so existing serialized data can migrate during a rename. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Serialization.FormerlySerializedAsAttribute.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Keep serialized storage fields private and expose behavior through methods/properties when invariants matter; Inspector visibility does not require public mutability.
- **Interpretation:** Use inline serialization for small owner-private value data. Use UnityEngine.Object references for asset/scene identity and SerializeReference only when reference graphs or polymorphism justify its complexity.
- **Interpretation:** Refactor serialized data in deliberate migration steps: preserve old names, open representative assets/scenes, resave, and test builds before removing migration attributes.
- **Interpretation:** Wrap unsupported containers in serializable entry lists and rebuild runtime dictionaries in callbacks or initialization, keeping callbacks lightweight.
- **Interpretation:** Do not place runtime caches in serialized fields unless persistence is intentional. Rebuild derived state after deserialize/enable.
- **Interpretation:** Version external save schemas separately from Unity scene/prefab serialization; they have different compatibility and migration requirements.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Inspector-authored private field | `[SerializeField] private` | Retains encapsulation while participating in Unity serialization. |
| Reference to asset/component/GameObject | UnityEngine.Object field | Unity records object identity rather than inline data. |
| Small owned value with no shared identity | Serializable inline class/struct | Simple and efficient owner-local data. |
| Polymorphic non-Unity object graph requiring identity | `[SerializeReference]` | Use only after accepting overhead, type metadata, and migration complexity. |

## Risks and review checks

- Do not make fields public solely to show them in the Inspector.
- Do not assume auto-properties, dictionaries, nested lists, or arbitrary generic types serialize because C# can represent them.
- Do not use SerializeReference for UnityEngine.Object-derived types.
- Do not rename or change serialized field types without a migration and representative asset/prefab/scene tests.
- Do not call broad Unity APIs or mutate object graphs unpredictably inside serialization callbacks.
- Do not confuse `[NonSerialized]` with Unity object lifetime; it only excludes a field from serialization.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.SerializeField`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeField.html) | `[SerializeField]` | Runtime | Includes an otherwise eligible non-public field in Unity serialization. |
| [`UnityEngine.SerializeReference`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeReference.html) | `[SerializeReference]` | Runtime | Stores supported managed references with identity/polymorphism. |
| [`UnityEngine.ISerializationCallbackReceiver`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ISerializationCallbackReceiver.html) | `void OnBeforeSerialize(); void OnAfterDeserialize()` | Runtime | Transforms serialized representation around Unity serialization. |
| [`UnityEngine.Serialization.FormerlySerializedAsAttribute`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Serialization.FormerlySerializedAsAttribute.html) | `[FormerlySerializedAs(string oldName)]` | Runtime | Preserves data across a serialized field rename. |

## Official sources

- Script Serialization — https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization.html
- Script Serialization Rules — https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html
- Serialize Field — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeField.html
- Serialize Reference — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeReference.html
- Iserialization Callback Receiver — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ISerializationCallbackReceiver.html
- Serialization.Formerly Serialized As Attribute — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Serialization.FormerlySerializedAsAttribute.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
