# Editor, Project, Packages, and Version Context

## Scope

Explains the Unity 6.3 Editor-facing project model: how the Project window exposes project files, how assets and packages enter a project, how feature sets relate to packages, and how to keep every answer pinned to 6000.3.

Read this reference when the request involves:

- the Unity Editor, Project window, project files, package management, feature sets, or version identity
- a question might accidentally mix Unity 6.3 with another Unity documentation version
- a package API is proposed and its installation or compatibility must be checked

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Unity 6.3 User Manual**
- **Unity Editor interface**
- **Project window**
- **Packages and package management**
- **Feature sets**
- **UnityEditor.AssetDatabase**
- **UnityEngine.Application project paths**

## Verified Unity 6.3 facts

- The target Manual identifies the editor line as Unity 6.3 LTS with documentation version 6000.3. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/UnityManual.html))
- The target Manual links to a Scripting API under the same 6000.3 documentation root; version-matched Manual and API pages are the required pairing for this repository. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/ManualVersions.html))
- The Project window is the primary Editor view for navigating assets and other project files and displays the project folder hierarchy. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/ProjectView.html))
- The Project window can expose package content and provides a control for showing or hiding packages. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/ProjectView.html))
- Unity assets include imported visual and audio files as well as abstract authored data such as text, gradients, masks, and numeric data. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html))
- Unity imports source files through the Asset Database and maintains the relationship between source files and the imported objects used by the Editor and Player. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html))
- Unity packages can add assets and extend Editor functionality; package availability and lifecycle are managed through Unity package management. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/PackagesList.html))
- Unity defines released packages as packages tested with a specific Editor version and the other released packages for that version, with complete documentation and supported production use. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- Unity 6.3 core packages are distributed alongside that Editor version and cannot be independently version-selected through Package Manager; the target core-package page records Shader Graph 17.3 and Visual Effect Graph 17.3. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-core.html))
- Feature sets group packages for a development use case, while each included package remains a separately managed package dependency. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/FeatureSets.html))
- Unity 6.3 documents distinct feature sets such as 2D, 3D Characters and Animation, 3D World Building, ECS, Engineering, Gameplay and Storytelling, Mobile, and deprecated AR/VR feature sets. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/FeatureSets.html))
- Unity 6.3 has version-specific additions and changes; the New in Unity 6.3 page is the authoritative starting point for identifying those differences. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Treat `Assets/` as authored project content, `Packages/` as dependency content, and project settings as configuration concerns; establish ownership before moving or generating files.
- **Interpretation:** Do not infer that a package is installed merely because it is released for 6000.3. First inspect the project package manifest or ask for its package list.
- **Interpretation:** Use feature sets as discovery aids, not as architecture boundaries. Runtime code should depend on the exact package assemblies it uses.
- **Interpretation:** When reviewing a copied snippet, reject unversioned API claims until the corresponding 6000.3 Manual, ScriptReference, or verified package page is found.
- **Interpretation:** Keep source assets and generated/imported artifacts conceptually separate; version the source and metadata that establish stable identity, not the Library cache.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| A built-in engine concept | 6000.3 Manual and ScriptReference | These pages are bound to the target Editor line. |
| A package feature | Exact released or core package version | Confirm its exact target-version lifecycle evidence and whether the project exposes/configures it. |
| A broad workflow bundle | Feature set for discovery | Then resolve each concrete dependency to an exact package. |
| A version migration question | New in 6.3 and Upgrade to 6.3 | Do not substitute current or 6000.0 documentation. |

## Risks and review checks

- Do not cite `latest` package URLs as stable evidence when an exact released version is available.
- Do not edit or version-control the `Library` directory as authored project data.
- Do not put runtime code under an Editor-only assembly or Editor folder merely because it is created from an Editor workflow.
- Check whether a file belongs to the project, a package, or generated state before assigning ownership or proposing refactors.
- Check the version banner on every Manual and ScriptReference page; a familiar API name is not proof of 6000.3 behavior.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Application.dataPath`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-dataPath.html) | `public static string dataPath { get; }` | Runtime | Absolute path to the project Assets folder in the Editor and the application data path in a built Player; platform behavior differs. |
| [`UnityEngine.Application.persistentDataPath`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html) | `public static string persistentDataPath { get; }` | Runtime | Platform-specific persistent data directory intended for data retained between runs. |
| [`UnityEditor.AssetDatabase`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.html) | `public static class AssetDatabase` | Editor | Editor-only API for importing, creating, querying, moving, and deleting project assets. Never compile it into Player assemblies. |
| [`UnityEditor.AssetDatabase.GUIDToAssetPath`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.GUIDToAssetPath.html) | `public static string GUIDToAssetPath(string guid)` | Editor | Resolves an asset GUID to a project-relative path in Editor tooling. |
| [`UnityEditor.AssetDatabase.LoadAssetAtPath<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.LoadAssetAtPath.html) | `public static T LoadAssetAtPath<T>(string assetPath)` | Editor | Loads an asset of type T from a project-relative path in Editor code. |

## Official sources

- Unity Manual — https://docs.unity3d.com/6000.3/Documentation/Manual/UnityManual.html
- Manual Versions — https://docs.unity3d.com/6000.3/Documentation/Manual/ManualVersions.html
- Whats New Unity63 — https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html
- Project View — https://docs.unity3d.com/6000.3/Documentation/Manual/ProjectView.html
- Asset Workflow — https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html
- Packages List — https://docs.unity3d.com/6000.3/Documentation/Manual/PackagesList.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Feature Sets — https://docs.unity3d.com/6000.3/Documentation/Manual/FeatureSets.html
- Application Data Path — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-dataPath.html
- Application Persistent Data Path — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html
- Asset Database — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.html
- Asset Database.Guidto Asset Path — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.GUIDToAssetPath.html
- Asset Database.Load Asset At Path — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.LoadAssetAtPath.html
- Pack Core — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-core.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
