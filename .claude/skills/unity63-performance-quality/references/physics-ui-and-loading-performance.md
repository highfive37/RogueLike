# Physics, UI, Scene, and Asset Loading Performance

## Scope

Covers fixed-step catch-up, physics queries/contacts, UI layout and rebuilds, scene activation, asynchronous asset loading, Addressables ownership, and transition-budget design.

Read this reference when the request involves:

- physics, UI, scene transition, or asset loading causes frame spikes
- optimizing FixedUpdate, Canvas/UI Toolkit layout, additive scenes, or Addressables
- designing loading screens and cancellation/release behavior

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Physics performance**
- **UI performance**
- **scene loading**
- **asset loading**
- **Physics**
- **Canvas**
- **VisualElement**
- **SceneManager**
- **AsyncOperation**

## Verified Unity 6.3 facts

- Physics cost is affected by active bodies, colliders, contacts, solver settings, fixed timestep/catch-up, broadphase, queries, and synchronization. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/physics-optimization.html))
- FixedUpdate can run multiple times in a rendered frame when simulation catches up, multiplying script and physics cost in that frame. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- NonAlloc physics queries write into caller buffers but require capacity/truncation handling. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.RaycastNonAlloc.html))
- Unity UI Canvas changes can trigger layout, graphic, geometry, batching, and raycast work; hierarchy and update frequency affect rebuild scope. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/UIToolkits.html))
- UI Toolkit uses retained visual trees and can incur style, layout, binding, geometry, and repaint work when properties/hierarchy change. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/UIToolkits.html))
- SceneManager asynchronous load/unload returns AsyncOperation; activation and completion have explicit states and can still perform main-thread integration work. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html))
- Asset import/load/decompression/upload and first use can produce CPU, IO, memory, and GPU work even when the initiating API is asynchronous. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html))
- Addressables 3.1 supports async content loading and reference-counted release through handles, but grouping/dependencies and activation remain project design concerns. ([official source](https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/index.html))
- Unity 6.3 includes profiling and adaptive performance updates documented in the release changes. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** For physics spikes, count fixed steps first; then inspect bodies/contacts/queries/solver and scripts. A timestep backlog can make an otherwise acceptable step miss the frame budget.
- **Interpretation:** Partition uGUI Canvases by independent update frequency and avoid dirtying large hierarchies for small dynamic elements. For UI Toolkit, batch model-to-view changes and virtualize large collections.
- **Interpretation:** Prewarm fonts, shaders, pools, animation controllers, audio, and frequently used assets only when the startup/retained-memory tradeoff fits the budget.
- **Interpretation:** Design transitions as staged work: begin IO, report progress, load dependencies, gate activation, initialize, switch ownership, unload old content, then release temporary memory.
- **Interpretation:** Keep AsyncOperation/Addressables handles with the owner and cancel/ignore superseded requests. Completion after a scene change must not mutate stale UI.
- **Interpretation:** Profile loading from cold and warm caches on target storage; Editor and desktop SSD behavior can conceal mobile/console IO and decompression costs.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| FixedUpdate spike | Inspect steps per frame, then physics/script samples | Separate catch-up multiplier from per-step cost. |
| uGUI frequently changing small widget | Separate smaller Canvas when justified | Limits rebuild scope at additional batch/Canvas cost. |
| Large scrolling list | Virtualized UI Toolkit ListView or equivalent strategy | Only visible/recycled elements perform work. |
| Large content transition | Async staged load + explicit activation/release | Controls frame and memory overlap. |

## Risks and review checks

- Do not reduce physics quality globally before identifying catch-up, contacts, or expensive queries.
- Do not place every UI element on one constantly dirty Canvas or split every element into its own Canvas.
- Do not rebuild visual trees/lists each frame when data binding/recycling can update existing elements.
- Do not interpret async load as “zero main-thread work.”
- Do not load a new world while retaining the old world and transition assets beyond the memory budget.
- Do not lose the Addressables/AsyncOperation owner or forget release/unload on failure and cancellation paths.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Time.maximumDeltaTime`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time-maximumDeltaTime.html) | `public static float maximumDeltaTime { get; set; }` | Runtime | Caps reported/processed time behavior and influences fixed-step catch-up limits; tune only with simulation consequences understood. |
| [`UnityEngine.Physics.autoSyncTransforms`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics-autoSyncTransforms.html) | `public static bool autoSyncTransforms { get; set; }` | Runtime | Controls automatic transform-to-physics synchronization; manual policy requires explicit SyncTransforms where needed. |
| [`UnityEngine.Canvas.ForceUpdateCanvases`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Canvas.ForceUpdateCanvases.html) | `public static void ForceUpdateCanvases()` | Runtime | Forces Canvas layout/content updates; can be expensive and should be rare. |
| [`UnityEngine.UIElements.VisualElement.MarkDirtyRepaint`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.VisualElement.MarkDirtyRepaint.html) | `public void MarkDirtyRepaint()` | Runtime | Requests repaint for a VisualElement; avoid unnecessary repeated invalidation. |
| [`UnityEngine.SceneManagement.SceneManager.LoadSceneAsync`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html) | `public static AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)` | Runtime | Begins asynchronous scene load. |
| [`UnityEngine.AsyncOperation.allowSceneActivation`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AsyncOperation-allowSceneActivation.html) | `public bool allowSceneActivation { get; set; }` | Runtime | Controls scene activation for load operations with documented progress/completion behavior. |

### Package prerequisites

- `com.unity.addressables@3.1` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Physics Optimization — https://docs.unity3d.com/6000.3/Documentation/Manual/physics-optimization.html
- Execution Order — https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html
- Uitoolkits — https://docs.unity3d.com/6000.3/Documentation/Manual/UIToolkits.html
- Asset Workflow — https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html
- Whats New Unity63 — https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html
- Physics.Raycast Non Alloc — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.RaycastNonAlloc.html
- Time Maximum Delta Time — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time-maximumDeltaTime.html
- Physics Auto Sync Transforms — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics-autoSyncTransforms.html
- Canvas.Force Update Canvases — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Canvas.ForceUpdateCanvases.html
- Uielements.Visual Element.Mark Dirty Repaint — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.VisualElement.MarkDirtyRepaint.html
- Scene Management.Scene Manager.Load Scene Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html
- Async Operation Allow Scene Activation — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AsyncOperation-allowSceneActivation.html
- Com.Unity.Addressables@3.1 — https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
