# GPU, Rendering, Shader, Texture, Mesh, and Asset Performance

## Scope

Provides a quality workflow for GPU-bound frames, render passes, overdraw, batches, shaders/variants, texture and mesh import, LOD, asset loading, and visual-equivalence checks.

Read this reference when the request involves:

- a build is GPU-bound or has rendering spikes
- reducing draw calls, SetPasses, overdraw, shadows, shader variants, texture or mesh memory
- reviewing asset import settings, LOD, Addressables, or pipeline quality

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **GPU profiling**
- **rendering**
- **shader variants**
- **textures and meshes**
- **asset loading**
- **FrameTimingManager**
- **QualitySettings**
- **Shader**
- **Texture**
- **Mesh**
- **Addressables**

## Verified Unity 6.3 facts

- Unity Profiler and supported GPU profiling tools attribute GPU time and rendering workload; platform GPU capture tools can provide deeper pipeline analysis. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-profiling-tools.html))
- Frame Debugger steps through render events and shows draw/clear/blit/state information for diagnosing how a frame is constructed. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/debugging-and-diagnostics.html))
- Rendering cost depends on render pipeline, passes, materials/shaders, lights/shadows, geometry, transparency/overdraw, resolution, post-processing, and target GPU. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/render-pipelines.html))
- Shader variants are generated from keywords and platform/pipeline combinations and can affect build size, memory, warm-up, and runtime availability. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Shaders.html))
- Texture import controls resolution, compression, mipmaps, platform overrides, read/write state, streaming, and format choices that affect memory/quality/bandwidth. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html))
- Mesh import/readability, vertex attributes, compression, index format, skinning, blend shapes, and LOD affect CPU/GPU memory and rendering cost. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html))
- LODGroup selects renderer sets according to screen-relative size and can cull at its final threshold. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LODGroup.html))
- Addressables 3.1 is a released Unity 6.3 package for asynchronous, reference-counted content location/load/release workflows. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- FrameTimingManager can capture recent CPU/GPU frame timing where platform support exists. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/FrameTimingManager.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** First prove GPU-bound behavior with frame timing/profiler data at target resolution and thermal state. CPU draw submission and GPU execution can bottleneck differently.
- **Interpretation:** Use Frame Debugger to identify unexpected passes, cameras, clears, shadows, transparent order, and material breaks; use a platform GPU capture for shader/bandwidth/occupancy detail.
- **Interpretation:** Optimize visual features by controlled A/B changes while preserving a reference screenshot/video and measurable acceptance threshold.
- **Interpretation:** Apply platform-specific texture formats/max sizes and remove Read/Write when CPU access is not required. Verify quality on device, including mip/streaming behavior.
- **Interpretation:** Group Addressable assets by load/unload lifetime and release every acquired handle. Bundle layout is both a loading and memory architecture decision.
- **Interpretation:** Audit shader variants and warm-up from actual content; avoid globally enabling keywords/features that multiply variants for little visible value.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| GPU time dominates frame | GPU module + platform frame capture | Locate pass/shader/bandwidth/overdraw bottleneck. |
| Unexpected draw/pass count | Frame Debugger | Inspect frame construction and state changes. |
| Large texture memory | Per-platform import/streaming policy | Balance visible resolution, format, mips, and residency. |
| Content loaded by lifetime/remote catalog | Addressables 3.1 | Use exact installed package and balanced acquire/release. |

## Risks and review checks

- Do not claim draw-call reduction improves a GPU-bound shader/overdraw bottleneck without measurement.
- Do not compare GPU performance at different resolution, render scale, MSAA, shadows, or post-processing.
- Do not disable mipmaps globally for 3D textures viewed at distance.
- Do not leave textures/meshes readable when no CPU API needs their data.
- Do not release an Addressables handle while consumers still use the asset or forget to release it at all.
- Do not strip a shader variant that runtime content later needs.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.FrameTimingManager.CaptureFrameTimings`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/FrameTimingManager.CaptureFrameTimings.html) | `public static void CaptureFrameTimings()` | Runtime | Requests timing capture for subsequent frames on supported platforms. |
| [`UnityEngine.FrameTimingManager.GetLatestTimings`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/FrameTimingManager.GetLatestTimings.html) | `public static uint GetLatestTimings(uint numFrames, FrameTiming[] timings)` | Runtime | Copies recent captured frame timings into caller storage. |
| [`UnityEngine.QualitySettings.SetQualityLevel`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/QualitySettings.SetQualityLevel.html) | `public static void SetQualityLevel(int index, bool applyExpensiveChanges = true)` | Runtime | Changes quality level; costly changes and pipeline assets require deliberate transition handling. |
| [`UnityEngine.LODGroup.SetLODs`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LODGroup.SetLODs.html) | `public void SetLODs(LOD[] lods)` | Runtime | Replaces LOD levels and renderer sets. |
| [`UnityEngine.Texture2D.Apply`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Texture2D.Apply.html) | `public void Apply(bool updateMipmaps = true, bool makeNoLongerReadable = false)` | Runtime | Uploads changed CPU texture data; can regenerate mips and discard CPU readability. |
| [`UnityEngine.Mesh.UploadMeshData`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Mesh.UploadMeshData.html) | `public void UploadMeshData(bool markNoLongerReadable)` | Runtime | Uploads mesh changes and can release CPU-side readable data. |

### Package prerequisites

- `com.unity.addressables@3.1` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Performance Profiling Tools — https://docs.unity3d.com/6000.3/Documentation/Manual/performance-profiling-tools.html
- Debugging And Diagnostics — https://docs.unity3d.com/6000.3/Documentation/Manual/debugging-and-diagnostics.html
- Render Pipelines — https://docs.unity3d.com/6000.3/Documentation/Manual/render-pipelines.html
- Shaders — https://docs.unity3d.com/6000.3/Documentation/Manual/Shaders.html
- Asset Workflow — https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Frame Timing Manager — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/FrameTimingManager.html
- Frame Timing Manager.Capture Frame Timings — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/FrameTimingManager.CaptureFrameTimings.html
- Frame Timing Manager.Get Latest Timings — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/FrameTimingManager.GetLatestTimings.html
- Quality Settings.Set Quality Level — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/QualitySettings.SetQualityLevel.html
- Lodgroup — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LODGroup.html
- Lodgroup.Set Lods — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LODGroup.SetLODs.html
- Texture2D.Apply — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Texture2D.Apply.html
- Mesh.Upload Mesh Data — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Mesh.UploadMeshData.html
- Com.Unity.Addressables@3.1 — https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
