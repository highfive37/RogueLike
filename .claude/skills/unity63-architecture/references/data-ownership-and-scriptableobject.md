# Data Ownership: Authored, Runtime, and Saved Data

## Scope

Distinguishes project-authored assets, scene/prefab serialized state, mutable runtime state, and persistent save data, with focused use of ScriptableObject and Unity serialization.

Read this reference when the request involves:

- choosing where configuration, runtime state, or save data should live
- using ScriptableObject without accidental shared-state coupling
- designing cloning, reset, serialization, or persistence boundaries

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Asset data**
- **ScriptableObject**
- **scene and prefab serialization**
- **runtime state**
- **save data**
- **ScriptableObject.CreateInstance**
- **Object.Instantiate**
- **JsonUtility**
- **Application.persistentDataPath**

## Verified Unity 6.3 facts

- Assets are project files imported and tracked by the Asset Database; scenes and prefabs serialize object references and field data into authored project content. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html))
- ScriptableObject can store data independently of GameObjects and can be saved as an asset in the Editor. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html))
- Unity serializes fields rather than arbitrary properties and supports a documented set of field types and container shapes. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- UnityEngine.Object references are serialized by reference to Unity objects, while supported custom serializable classes are normally serialized inline by value unless SerializeReference is used. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- Unity serialization is used by scenes, prefabs, assets, Inspector editing, hot reload, and other Editor/runtime systems; it is not a general-purpose versioned save-file format by itself. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization.html))
- `Object.Instantiate` clones UnityEngine.Object instances, including ScriptableObjects, and produces an independent Unity object instance. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Instantiate.html))
- `JsonUtility` serializes supported structured fields using Unity serialization rules and does not provide a general arbitrary JSON object model. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.html))
- `Application.persistentDataPath` supplies a platform-specific location intended for retained application data. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Treat authored ScriptableObject assets as definitions or defaults. Copy their values into a session-owned runtime model when gameplay mutates them and reset semantics matter.
- **Interpretation:** Assign one clear owner for every mutable state object. Other systems should observe or request changes through an API rather than mutate shared fields opportunistically.
- **Interpretation:** Create an explicit save DTO/schema separated from scene objects. Include a schema version and migration path instead of serializing live MonoBehaviours.
- **Interpretation:** Use stable logical identifiers in saved data and resolve them to current assets at load time; do not treat transient instance IDs as persistence keys.
- **Interpretation:** Choose inline serialized classes for owner-private configuration and ScriptableObject assets for independently reusable or addressable definitions.
- **Interpretation:** Document whether runtime clones inherit defaults once, track live asset changes in Editor, or are recreated on every session.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Reusable authored immutable definition | ScriptableObject asset | Independent asset identity and references are useful. |
| Mutable state for one play session | Runtime model owned by a session/system | Prevents asset mutation and makes reset/lifetime explicit. |
| Small configuration owned by one prefab | Inline serialized class/struct | Avoids unnecessary asset proliferation. |
| Data retained across application runs | Versioned save DTO under persistentDataPath | Persistence format and migration are explicit. |

## Risks and review checks

- Do not write player progress directly into an authored ScriptableObject asset as the production save strategy.
- Do not serialize scene object references into external save files and expect them to remain stable between builds or content revisions.
- Do not assume properties, dictionaries, multidimensional arrays, or nested containers serialize unless the 6000.3 rules explicitly support the shape.
- Do not use `JsonUtility` as evidence that arbitrary JSON or polymorphism is supported.
- Do not mutate a shared definition asset from multiple systems without an explicit ownership and reset policy.
- Do not overwrite save files in place without considering atomic replacement, corruption recovery, and migration failure behavior.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.ScriptableObject.CreateInstance<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ScriptableObject.CreateInstance.html) | `public static T CreateInstance<T>() where T : ScriptableObject` | Runtime | Creates a runtime ScriptableObject instance. |
| [`UnityEngine.Object.Instantiate<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Instantiate.html) | `public static T Instantiate<T>(T original) where T : Object` | Runtime | Clones a Unity object; overloads add parent or transform placement for scene objects. |
| [`UnityEngine.JsonUtility.ToJson`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.ToJson.html) | `public static string ToJson(object obj, bool prettyPrint = false)` | Runtime | Serializes supported fields to JSON. |
| [`UnityEngine.JsonUtility.FromJson<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.FromJson.html) | `public static T FromJson<T>(string json)` | Runtime | Creates and populates a supported non-UnityEngine.Object value from JSON. |
| [`UnityEngine.JsonUtility.FromJsonOverwrite`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.FromJsonOverwrite.html) | `public static void FromJsonOverwrite(string json, object objectToOverwrite)` | Runtime | Overwrites supported serialized fields on an existing object. |
| [`UnityEngine.Application.persistentDataPath`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html) | `public static string persistentDataPath { get; }` | Runtime | Platform-specific persistent storage root. |

## Official sources

- Asset Workflow — https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html
- Class Scriptable Object — https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html
- Script Serialization — https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization.html
- Script Serialization Rules — https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html
- Object.Instantiate — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Instantiate.html
- Json Utility — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.html
- Json Utility.To Json — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.ToJson.html
- Json Utility.From Json — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.FromJson.html
- Json Utility.From Json Overwrite — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.FromJsonOverwrite.html
- Application Persistent Data Path — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html
- Scriptable Object.Create Instance — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ScriptableObject.CreateInstance.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
