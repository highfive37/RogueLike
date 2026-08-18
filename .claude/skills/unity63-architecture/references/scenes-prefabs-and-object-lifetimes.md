# Scenes, Prefabs, and Object Lifetime Boundaries

## Scope

Defines responsibility and lifetime choices for scenes, additive scene composition, prefab assets and instances, persistent objects, and asynchronous scene transitions.

Read this reference when the request involves:

- splitting gameplay across scenes or loading additively
- choosing between a prefab, scene object, bootstrap object, or persistent service
- designing scene transitions and teardown without duplicates or leaked references

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Scenes**
- **multi-scene editing**
- **Prefab assets and instances**
- **object lifetime**
- **SceneManager**
- **AsyncOperation**
- **DontDestroyOnLoad**
- **PrefabUtility**

## Verified Unity 6.3 facts

- Scenes contain GameObjects and represent content such as levels, menus, and other parts of an application. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/CreatingScenes.html))
- Unity supports opening and editing multiple scenes and designating an active scene; the active scene affects where newly created GameObjects are placed and which scene settings are used. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/MultiSceneEditing.html))
- Prefab assets are reusable templates and prefab instances maintain a connection to their source while allowing documented overrides. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/prefabs-introduction.html))
- Nested prefabs and prefab variants express reusable composition and inheritance-like relationships with distinct override behavior. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/NestedPrefabs.html))
- SceneManager provides synchronous and asynchronous scene loading, unloading, active-scene selection, and scene events. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.html))
- LoadSceneAsync returns an AsyncOperation that represents progress and completion of a scene load. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html))
- Objects in a loaded scene are normally destroyed when that scene unloads; `DontDestroyOnLoad` moves a root object hierarchy so it survives scene loading. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.DontDestroyOnLoad.html))
- PrefabUtility is an Editor-only API for prefab asset/instance operations and is not available in Player runtime code. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PrefabUtility.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Give each scene a declared responsibility: composition root, persistent shell, level chunk, UI layer, lighting set, or transient transition content.
- **Interpretation:** Create persistent systems once from an explicit bootstrap path and make duplicate prevention deterministic; do not let every gameplay scene opportunistically create the same singleton.
- **Interpretation:** Use prefabs for repeatable object composition and scenes for spatial/lifetime grouping. A prefab should not secretly assume a particular scene singleton without an explicit contract.
- **Interpretation:** For additive loading, retain a scene-scoped owner that tracks registrations and cancels work before unloading the scene.
- **Interpretation:** Keep cross-scene references either in a persistent composition layer or re-resolve them through explicit registration after load. Avoid stale direct references after unload.
- **Interpretation:** Model transitions as a state machine: request, suspend input, load, activate, initialize, hand over, unload old content, and restore interaction.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Reusable object composition | Prefab | Instances can share authored structure and controlled overrides. |
| Spatial level or lifetime group | Scene | Scene load/unload gives a clear content lifetime boundary. |
| System that must survive level changes | Single persistent bootstrap-owned object | Creation and teardown remain centralized. |
| Large world or separated content layers | Additive scenes | Load, activate, and unload independently with explicit ownership. |

## Risks and review checks

- Do not mark every manager `DontDestroyOnLoad`; persistent objects accumulate and can retain unloaded content.
- Do not depend on scene load order to establish hidden singleton availability without a documented bootstrap contract.
- Do not retain delegates, tasks, or static references to objects in an unloaded scene.
- Do not put per-level state into the persistent shell unless ownership and reset are explicit.
- Do not use Editor-only PrefabUtility operations as runtime prefab APIs.
- Do not synchronously load large scenes on an interactive frame without measuring and accepting the stall.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.SceneManagement.SceneManager.LoadSceneAsync`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html) | `public static AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)` | Runtime | Begins an asynchronous load; index and parameter overloads also exist. |
| [`UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.UnloadSceneAsync.html) | `public static AsyncOperation UnloadSceneAsync(string sceneName)` | Runtime | Begins unloading a loaded scene; overloads accept Scene or build index. |
| [`UnityEngine.SceneManagement.SceneManager.SetActiveScene`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html) | `public static bool SetActiveScene(Scene scene)` | Runtime | Selects the active loaded scene when valid. |
| [`UnityEngine.SceneManagement.SceneManager.sceneLoaded`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager-sceneLoaded.html) | `public static event UnityAction<Scene, LoadSceneMode> sceneLoaded` | Runtime | Notifies after a scene load. |
| [`UnityEngine.Object.DontDestroyOnLoad`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.DontDestroyOnLoad.html) | `public static void DontDestroyOnLoad(Object target)` | Runtime | Preserves a root object hierarchy across scene loads. |
| [`UnityEditor.PrefabUtility`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PrefabUtility.html) | `public static class PrefabUtility` | Editor | Editor-only prefab asset and instance operations. |

## Official sources

- Creating Scenes — https://docs.unity3d.com/6000.3/Documentation/Manual/CreatingScenes.html
- Multi Scene Editing — https://docs.unity3d.com/6000.3/Documentation/Manual/MultiSceneEditing.html
- Prefabs — https://docs.unity3d.com/6000.3/Documentation/Manual/Prefabs.html
- Prefabs Introduction — https://docs.unity3d.com/6000.3/Documentation/Manual/prefabs-introduction.html
- Nested Prefabs — https://docs.unity3d.com/6000.3/Documentation/Manual/NestedPrefabs.html
- Prefab Variants — https://docs.unity3d.com/6000.3/Documentation/Manual/PrefabVariants.html
- Prefabs Override — https://docs.unity3d.com/6000.3/Documentation/Manual/prefabs-override.html
- Scene Management.Scene Manager — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.html
- Scene Management.Scene Manager.Load Scene Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html
- Scene Management.Scene Manager.Unload Scene Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.UnloadSceneAsync.html
- Scene Management.Scene Manager.Set Active Scene — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html
- Scene Management.Scene Manager Scene Loaded — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager-sceneLoaded.html
- Object.Dont Destroy On Load — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.DontDestroyOnLoad.html
- Prefab Utility — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PrefabUtility.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
