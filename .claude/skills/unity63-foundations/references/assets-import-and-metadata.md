# Assets, Import Pipeline, Metadata, GUIDs, and Runtime Loading

## Scope

Explains the distinction among source files, imported Unity objects, `.meta` identity, Asset Database state, sub-assets, import settings, and runtime-loading systems.

Read this reference when the request involves:

- asset import, GUIDs, `.meta` files, importer settings, sub-assets, or broken references
- runtime asset loading must choose direct references, Resources, AssetBundles, or Addressables
- Editor scripts create, move, query, or reimport assets

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Assets and media**
- **Importing assets**
- **Asset metadata**
- **Asset Database**
- **Runtime asset management**
- **UnityEditor.AssetDatabase**
- **UnityEditor.AssetImporter**
- **UnityEngine.Resources**
- **UnityEngine.AssetBundle**

## Verified Unity 6.3 facts

- Unity assets can originate outside the Editor or be created inside Unity, and Unity imports supported source files into project-usable objects. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html))
- Files placed under the project `Assets` folder are imported; the imported result rather than the raw source representation is what Unity uses internally and at runtime. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/ImportingAssets.html))
- Unity stores asset metadata in `.meta` files, including the GUID used to maintain references independently of a file name or location. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetMetadata.html))
- A missing or regenerated `.meta` file changes identity and can break serialized references to that asset. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetMetadata.html))
- The Asset Database tracks the relationship between source assets, imported artifacts, dependencies, and project state. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetDatabase.html))
- Importer settings are part of the asset import result and can change runtime size, quality, platform variants, and generated sub-assets. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/ImportingAssets.html))
- The Asset Database API is Editor-only and can create, import, refresh, move, delete, and query project assets. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.html))
- Unity warns that cloud-synchronized folders are an unsupported way to store a project because synchronization can corrupt project state; use version control. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html))
- The Manual identifies Addressables as the recommended system for organizing runtime asset loading, while the Resources system can negatively affect performance. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html))
- Addressables 3.1 is listed as a released package for Unity 6.3, but it remains an optional package dependency. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- `Resources.Load` only finds assets placed in a Resources folder and resolves paths relative to that folder without file extensions. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Resources.Load.html))
- `AssetBundle.Unload` distinguishes unloading the bundle container from unloading all loaded objects that came from the bundle. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetBundle.Unload.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Version `.meta` files with their source assets and move assets from inside Unity or with the source and metadata together to preserve GUID identity.
- **Interpretation:** Use direct serialized references for bounded, always-present dependencies; use Addressables when content needs asynchronous loading, grouping, catalogs, remote delivery, or lifetime handles.
- **Interpretation:** Treat import settings as build inputs. Review platform overrides and importer changes with the same care as source code because they alter memory and rendering behavior.
- **Interpretation:** Centralize runtime-load ownership: the component that acquires an Addressables handle or AssetBundle object should define who releases it and when.
- **Interpretation:** Avoid broad Asset Database refreshes in Editor tools. Batch edits and use asset editing scopes where the official API permits.
- **Interpretation:** Model asset identity by GUID or typed reference, not by a fragile display name or arbitrary absolute path.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Small fixed dependency known at authoring time | Serialized direct reference | Simple, type-safe, and included through dependency analysis. |
| Dynamic or remotely delivered content | Addressables 3.1 | Provides address-based asynchronous loading and explicit handle lifetime. |
| Legacy fixed Resources content | Resources only with measured scope | Easy access but weaker organization and loading control. |
| Editor import or migration tooling | AssetDatabase / AssetImporter | Editor-only APIs understand Unity asset identity and import state. |

## Risks and review checks

- Do not generate new `.meta` files for moved tracked assets.
- Do not call AssetDatabase APIs from runtime assemblies or Player code.
- Do not use an absolute operating-system path as a persistent Unity asset identifier.
- Do not release a runtime asset while instantiated objects or consumers still depend on it.
- Do not choose Resources for convenience without checking build inclusion, memory, load spikes, and unload behavior.
- Do not assume importer defaults are appropriate for every target platform.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEditor.AssetDatabase.ImportAsset`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.ImportAsset.html) | `public static void ImportAsset(string path, ImportAssetOptions options = ImportAssetOptions.Default)` | Editor | Imports or reimports the project-relative asset path. |
| [`UnityEditor.AssetDatabase.AssetPathToGUID`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.AssetPathToGUID.html) | `public static string AssetPathToGUID(string path)` | Editor | Returns the GUID for a project-relative asset path. |
| [`UnityEditor.AssetDatabase.GUIDToAssetPath`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.GUIDToAssetPath.html) | `public static string GUIDToAssetPath(string guid)` | Editor | Resolves a GUID to the current project-relative path. |
| [`UnityEditor.AssetImporter.GetAtPath`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetImporter.GetAtPath.html) | `public static AssetImporter GetAtPath(string path)` | Editor | Returns the importer for an asset path so Editor code can inspect or change import settings. |
| [`UnityEngine.Resources.Load<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Resources.Load.html) | `public static T Load<T>(string path) where T : Object` | Runtime | Loads a typed asset from a Resources folder; returns null if no matching asset exists. |
| [`UnityEngine.AssetBundle.LoadAssetAsync<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetBundle.LoadAssetAsync.html) | `public AssetBundleRequest LoadAssetAsync<T>(string name) where T : Object` | Runtime | Begins loading a named object from a loaded bundle. |
| [`UnityEngine.AssetBundle.Unload`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetBundle.Unload.html) | `public void Unload(bool unloadAllLoadedObjects)` | Runtime | Boolean controls whether loaded objects are also unloaded. |

### Package prerequisites

- `com.unity.addressables@3.1` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Asset Workflow — https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html
- Importing Assets — https://docs.unity3d.com/6000.3/Documentation/Manual/ImportingAssets.html
- Asset Metadata — https://docs.unity3d.com/6000.3/Documentation/Manual/AssetMetadata.html
- Asset Database — https://docs.unity3d.com/6000.3/Documentation/Manual/AssetDatabase.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Com.Unity.Addressables@3.1 — https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/index.html
- Asset Database — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.html
- Asset Database.Import Asset — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.ImportAsset.html
- Asset Database.Asset Path To Guid — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.AssetPathToGUID.html
- Asset Database.Guidto Asset Path — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.GUIDToAssetPath.html
- Asset Importer.Get At Path — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetImporter.GetAtPath.html
- Resources.Load — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Resources.Load.html
- Asset Bundle.Load Asset Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetBundle.LoadAssetAsync.html
- Asset Bundle.Unload — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetBundle.Unload.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
