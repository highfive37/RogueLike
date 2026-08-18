# Cameras, Cinemachine, and World Building

## Scope

Covers Camera projection and rendering responsibility, Cinemachine 3.1 coordination, Terrain and geometry authoring, LOD/occlusion, and scene-scale world composition.

Read this reference when the request involves:

- designing cameras or camera transitions
- using Cinemachine 3.1
- building Terrain, ProBuilder, spline, LOD, or large-world scene content

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Cameras**
- **Cinemachine**
- **World building**
- **Terrain**
- **LOD and occlusion**
- **Camera**
- **CullingGroup**
- **LODGroup**
- **Terrain**
- **CinemachineCamera**

## Verified Unity 6.3 facts

- A Camera renders a view of the scene according to projection, clipping, culling, viewport, target, and pipeline settings. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/CamerasOverview.html))
- Multiple Cameras can render in a defined order or to different targets/viewports; their cost and compositing behavior depend on the active render pipeline. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/CamerasOverview.html))
- Camera world-to-screen, screen-to-world, and ray creation APIs translate between coordinate spaces for interaction and presentation. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera.html))
- Cinemachine 3.1 is a released package for Unity 6.3 and provides camera-control components that drive an output Unity Camera through a Cinemachine Brain. ([official source](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html))
- Unity world-building documentation groups Terrain, trees/vegetation, ProBuilder, Polybrush, splines, and related tools as complementary authoring systems. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/CreatingEnvironments.html))
- Terrain stores heightmap and terrain-layer data and renders large outdoor surfaces through the Terrain component and TerrainData asset. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-Terrain.html))
- LODGroup selects renderer levels based on screen-relative size and provides a culling threshold. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LODGroup.html))
- Occlusion culling can avoid rendering objects hidden behind other geometry, but it requires appropriate baking/runtime setup and is distinct from frustum culling. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/CamerasOverview.html))
- ProBuilder 6.1, Terrain Tools 5.3, and Splines 2.9 are released Unity 6.3 packages. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Separate camera intent from camera output: gameplay selects targets/modes, a camera rig computes framing, and the render Camera remains the pipeline output.
- **Interpretation:** Use Cinemachine only after confirming the package is installed; isolate package types in a camera integration assembly if the project can run without it.
- **Interpretation:** Make camera update timing agree with target motion: physics targets may require interpolation, while frame-driven targets typically settle before LateUpdate camera evaluation.
- **Interpretation:** Partition a large world by loading/lifetime and authoring responsibility rather than one enormous scene. Terrain tiles, lighting scenes, and gameplay chunks can have distinct owners.
- **Interpretation:** Use LOD, culling distances, impostors, and occlusion only after capturing representative camera paths; incorrect thresholds cause popping or excess draw cost.
- **Interpretation:** Keep authoring tools out of runtime assemblies. Runtime consumes built meshes, terrain data, splines, and prefabs rather than Editor tool APIs.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Single gameplay view | One output Camera + explicit rig | Minimizes duplicate render cost and centralizes projection. |
| Complex follow/blend/framing requirements | Cinemachine 3.1 | Use the released package and retain gameplay-independent camera intent. |
| Large outdoor heightfield | Terrain/TerrainData | Purpose-built authoring and rendering pipeline. |
| Modular architecture/greybox | ProBuilder-authored meshes/prefabs | Bake/export runtime content and keep Editor APIs isolated. |

## Risks and review checks

- Do not add multiple full-screen Cameras without measuring extra culling, render passes, shadows, and post-processing.
- Do not mix world-space, viewport, and screen-space coordinates without using the matching Camera API and depth semantics.
- Do not make gameplay state depend directly on a transient Cinemachine blend state unless that dependency is intentional.
- Do not assume occlusion culling helps open terrain or rapidly changing occluders; profile the actual world.
- Do not edit TerrainData shared by multiple Terrain instances at runtime without understanding shared asset mutation.
- Do not ship ProBuilder/Terrain editor tools as runtime dependencies merely because they authored content.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Camera.ScreenPointToRay`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera.ScreenPointToRay.html) | `public Ray ScreenPointToRay(Vector3 pos, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono)` | Runtime | Creates a world ray through a screen pixel. |
| [`UnityEngine.Camera.WorldToScreenPoint`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera.WorldToScreenPoint.html) | `public Vector3 WorldToScreenPoint(Vector3 position, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono)` | Runtime | Projects world position to pixel coordinates. |
| [`UnityEngine.Camera.cullingMask`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera-cullingMask.html) | `public int cullingMask { get; set; }` | Runtime | Layer mask used for Camera culling. |
| [`UnityEngine.LODGroup`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LODGroup.html) | `public class LODGroup : Component` | Runtime | Controls renderer LOD transitions and culling. |
| [`UnityEngine.Terrain.terrainData`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Terrain-terrainData.html) | `public TerrainData terrainData { get; set; }` | Runtime | Terrain data asset used by a Terrain component. |
| [`Unity.Cinemachine.CinemachineCamera`](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/api/Unity.Cinemachine.CinemachineCamera.html) | `public sealed class CinemachineCamera : CinemachineVirtualCameraBase` | Package runtime | Cinemachine 3.1 camera component; exact package installation required. |

### Package prerequisites

- `com.unity.cinemachine@3.1` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.probuilder@6.1` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.probuilder@6.1/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.terrain-tools@5.3` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.terrain-tools@5.3/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.splines@2.9` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.splines@2.9/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Cameras Overview — https://docs.unity3d.com/6000.3/Documentation/Manual/CamerasOverview.html
- World Building Feature — https://docs.unity3d.com/6000.3/Documentation/Manual/WorldBuildingFeature.html
- Creating Environments — https://docs.unity3d.com/6000.3/Documentation/Manual/CreatingEnvironments.html
- Script Terrain — https://docs.unity3d.com/6000.3/Documentation/Manual/script-Terrain.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Camera — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera.html
- Camera.Screen Point To Ray — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera.ScreenPointToRay.html
- Camera.World To Screen Point — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera.WorldToScreenPoint.html
- Camera Culling Mask — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera-cullingMask.html
- Lodgroup — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LODGroup.html
- Terrain — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Terrain.html
- Terrain Terrain Data — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Terrain-terrainData.html
- Com.Unity.Cinemachine@3.1 — https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html
- Com.Unity.Probuilder@6.1 — https://docs.unity3d.com/Packages/com.unity.probuilder@6.1/manual/index.html
- Com.Unity.Terrain Tools@5.3 — https://docs.unity3d.com/Packages/com.unity.terrain-tools@5.3/manual/index.html
- Com.Unity.Splines@2.9 — https://docs.unity3d.com/Packages/com.unity.splines@2.9/manual/index.html
- Unity.Cinemachine.Cinemachine Camera — https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/api/Unity.Cinemachine.CinemachineCamera.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
