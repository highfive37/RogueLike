# Scenes, Additive Loading, and Hierarchy Boundaries

## Scope

Explains Scene assets, loaded Scene state, additive composition, root GameObjects, active Scene semantics, asynchronous loading, and responsibility boundaries for multi-scene projects.

Read this reference when the request involves:

- scene composition, level flow, bootstrap scenes, additive loading, or persistent systems
- objects move between scenes or a request depends on active Scene semantics
- a design must choose between one large Scene and multiple responsibility-oriented Scenes

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
- **Multi-Scene editing**
- **Scene loading**
- **Scene roots**
- **Active Scene**
- **UnityEngine.SceneManagement.Scene**
- **UnityEngine.SceneManagement.SceneManager**
- **AsyncOperation**

## Verified Unity 6.3 facts

- A Scene is an asset that contains environments, menus, and other GameObjects used to build an application. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/CreatingScenes.html))
- A project can use multiple Scenes, and the Editor supports opening and editing multiple Scenes together. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/CreatingScenes.html))
- Multi-Scene editing allows several Scene assets to be loaded into the Hierarchy at once while preserving separate Scene ownership. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/MultiSceneEditing.html))
- At runtime, `SceneManager` exposes loaded Scene count, active Scene selection, loading, unloading, and scene events. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.html))
- The active Scene is the target for newly instantiated GameObjects when no explicit Scene transfer is performed and influences lighting settings used by the Editor/runtime context. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html))
- A Scene struct is a handle to Scene data and exposes properties such as name, path, build index, loaded state, and root GameObject access. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.Scene.html))
- LoadSceneMode.Single closes currently loaded Scenes before loading the target, while Additive adds the target without unloading existing Scenes. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.LoadSceneMode.html))
- `LoadSceneAsync` returns an AsyncOperation so code can observe progress and completion without blocking the main thread with synchronous loading. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html))
- `sceneLoaded` is raised after a Scene is loaded; the documented execution sequence places it after `OnEnable` and before `Start` for scene objects. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- `DontDestroyOnLoad` preserves the target object and its transform children when a new Scene is loaded in Single mode. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.DontDestroyOnLoad.html))
- `MoveGameObjectToScene` moves a root GameObject to an already loaded Scene. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.MoveGameObjectToScene.html))

### Processing or lifecycle flow

1. Select the target Scene asset and load mode.
2. Start asynchronous loading and expose progress/cancellation state to the coordinator.
3. Resolve loaded Scene handles and set the active Scene when creation ownership requires it.
4. Run explicit initialization for systems that depend on newly loaded objects.
5. Enable gameplay only after readiness criteria pass.
6. On transition, cancel Scene-owned work, unsubscribe, release references, then unload.

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Use additive Scenes to separate content ownership that must load, unload, or be authored independently, such as bootstrap, persistent services, world segments, lighting sets, and UI shells.
- **Interpretation:** Make one component responsible for a load transaction: request, progress, activation, dependency readiness, and rollback. Do not scatter scene calls across gameplay components.
- **Interpretation:** Treat scene load completion as only an asset/object availability boundary. Explicitly establish when dependent systems are initialized and ready for interaction.
- **Interpretation:** Avoid duplicate persistent managers by making creation idempotent or by using one authoritative bootstrap Scene.
- **Interpretation:** Keep cross-scene references deliberate. Runtime discovery may be necessary after additive load, but it should resolve into stable typed references rather than repeated searches.
- **Interpretation:** Design Scene unload as a cancellation and teardown boundary for tasks, event subscriptions, native resources, and pooled objects owned by that Scene.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| One self-contained level with no independent streaming | Single Scene load | Simpler ownership and reset behavior. |
| Persistent shell plus replaceable gameplay content | Bootstrap Scene + additive content Scene | Separates application lifetime from level lifetime. |
| Large world or independent authoring teams | Multiple additive content Scenes | Independent loading and ownership justify the coordination cost. |
| Object must survive a Single load | Explicit persistent owner or `DontDestroyOnLoad` | Use sparingly and prevent duplicates. |

## Risks and review checks

- Do not assume a Scene is valid merely because a stored Scene struct is non-default; check `IsValid` and `isLoaded` where relevant.
- Do not subscribe to `sceneLoaded` without an equally clear unsubscribe path.
- Do not unload a Scene while tasks, callbacks, or references owned by it can still execute.
- Do not put every global system under `DontDestroyOnLoad`; persistent hidden state makes Play Mode and tests harder to reset.
- Do not rely on a particular ordering among same-type callbacks on different GameObjects unless you configure or orchestrate it explicitly.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.SceneManagement.SceneManager.LoadSceneAsync`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html) | `public static AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)` | Runtime | Loads by name asynchronously; overloads also accept build index and LoadSceneParameters. |
| [`UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.UnloadSceneAsync.html) | `public static AsyncOperation UnloadSceneAsync(Scene scene)` | Runtime | Destroys Scene GameObjects and removes the Scene; assets can require separate unloading policy. |
| [`UnityEngine.SceneManagement.SceneManager.SetActiveScene`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html) | `public static bool SetActiveScene(Scene scene)` | Runtime | The Scene must be loaded; returns whether the operation succeeds. |
| [`UnityEngine.SceneManagement.SceneManager.GetSceneAt`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.GetSceneAt.html) | `public static Scene GetSceneAt(int index)` | Runtime | Returns a loaded Scene by index from `0` to `sceneCount - 1`. |
| [`UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.MoveGameObjectToScene.html) | `public static void MoveGameObjectToScene(GameObject go, Scene scene)` | Runtime | The GameObject must be a root object and the destination Scene loaded. |
| [`UnityEngine.SceneManagement.Scene.GetRootGameObjects`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.Scene.GetRootGameObjects.html) | `public GameObject[] GetRootGameObjects(); public void GetRootGameObjects(List<GameObject> rootGameObjects)` | Runtime | The list overload lets callers reuse storage to reduce allocations. |
| [`UnityEngine.Object.DontDestroyOnLoad`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.DontDestroyOnLoad.html) | `public static void DontDestroyOnLoad(Object target)` | Runtime | Preserves a target object across Single Scene loads. |

## Official sources

- Creating Scenes — https://docs.unity3d.com/6000.3/Documentation/Manual/CreatingScenes.html
- Multi Scene Editing — https://docs.unity3d.com/6000.3/Documentation/Manual/MultiSceneEditing.html
- Execution Order — https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html
- Scene Management.Scene — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.Scene.html
- Scene Management.Scene Manager — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.html
- Scene Management.Scene Manager.Load Scene Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html
- Scene Management.Scene Manager.Unload Scene Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.UnloadSceneAsync.html
- Scene Management.Load Scene Mode — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.LoadSceneMode.html
- Scene Management.Scene Manager.Set Active Scene — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html
- Scene Management.Scene Manager.Get Scene At — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.GetSceneAt.html
- Scene Management.Scene Manager.Move Game Object To Scene — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.MoveGameObjectToScene.html
- Scene Management.Scene.Get Root Game Objects — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.Scene.GetRootGameObjects.html
- Object.Dont Destroy On Load — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.DontDestroyOnLoad.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
