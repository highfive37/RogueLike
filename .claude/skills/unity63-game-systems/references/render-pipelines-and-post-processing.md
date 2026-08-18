# Render Pipelines, Graphics Settings, and Post-Processing

## Scope

Defines the Built-in Render Pipeline, URP/HDRP selection boundary, Scriptable Render Pipeline assets, camera/pipeline interaction, render callbacks, and post-processing ownership.

Read this reference when the request involves:

- choosing or migrating a render pipeline
- configuring pipeline assets or quality tiers
- implementing custom render callbacks/features or post-processing

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Built-in Render Pipeline**
- **Universal Render Pipeline**
- **High Definition Render Pipeline**
- **post-processing**
- **GraphicsSettings**
- **RenderPipelineAsset**
- **RenderPipelineManager**
- **CommandBuffer**

## Verified Unity 6.3 facts

- Unity 6.3 documents three primary render pipeline choices: the Built-in Render Pipeline, Universal Render Pipeline, and High Definition Render Pipeline. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/render-pipelines.html))
- URP and HDRP are Scriptable Render Pipelines with pipeline assets/settings and distinct feature, shader, lighting, camera, and platform support. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/render-pipelines.html))
- A ScriptableRenderPipelineAsset creates the active ScriptableRenderPipeline implementation and is selected through Graphics/Quality settings. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.RenderPipelineAsset.html))
- GraphicsSettings exposes project graphics configuration and the current/default render pipeline references. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.GraphicsSettings.html))
- RenderPipelineManager exposes SRP rendering events; Built-in pipeline camera callbacks are not interchangeable with every SRP hook. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.RenderPipelineManager.html))
- CommandBuffer records rendering commands for execution by supported camera/pipeline integration points. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.CommandBuffer.html))
- Post-processing implementation depends on the render pipeline; the Post Processing 3.5 package is released for Unity 6.3 but is not a blanket replacement for pipeline-integrated volume systems. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- Pipeline changes can require material/shader conversion and can change lighting, renderer features, camera stacking, post-processing, and performance behavior. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/render-pipelines.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Select the render pipeline at project inception when possible using target platforms, visual requirements, team expertise, shader ecosystem, and performance budgets.
- **Interpretation:** Treat pipeline migration as a content and code migration: shaders/materials, lighting, custom passes, post effects, camera settings, screenshots, and performance baselines all need validation.
- **Interpretation:** Keep pipeline-specific render features in isolated assemblies/folders and expose narrow data contracts to gameplay.
- **Interpretation:** Use quality-level pipeline assets only with a documented runtime switch policy; asset differences can affect shader variants and visual consistency.
- **Interpretation:** Prefer pipeline-supported extension points over legacy callbacks when using SRP. Unsubscribe static render events on domain/lifetime teardown.
- **Interpretation:** Own post-processing volumes/profiles like other authored assets and avoid runtime mutation of shared profiles when only one camera/instance should change.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Broad platform range including mobile/standalone | URP after target feature verification | Scalable SRP with project-selected feature set. |
| High-end fidelity with HDRP-supported targets | HDRP | Advanced lighting/rendering with stricter platform scope. |
| Legacy project deeply tied to built-in shaders/callbacks | Built-in until migration case is justified | Migration cost and compatibility are real constraints. |
| Custom render pass | Pipeline-native extension point | Avoid assuming Built-in and SRP hooks match. |

## Risks and review checks

- Do not install or switch pipelines casually in a mature project without source control and a conversion/test plan.
- Do not combine shader/material assets from different pipelines and treat magenta output as a runtime-only bug.
- Do not subscribe static RenderPipelineManager events without unsubscribing during reload/teardown.
- Do not mutate a shared volume/post-processing profile for one transient camera effect unless sharing is intentional.
- Do not assume a custom CommandBuffer injection point exists or executes in the same order across pipelines.
- Do not compare pipeline performance using different visual features, resolutions, quality assets, or warm-up states.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.GraphicsSettings-defaultRenderPipeline.html) | `public static RenderPipelineAsset defaultRenderPipeline { get; set; }` | Runtime | Project default SRP asset; quality settings can override pipeline selection. |
| [`UnityEngine.Rendering.RenderPipelineAsset`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.RenderPipelineAsset.html) | `public abstract class RenderPipelineAsset : ScriptableObject` | Runtime | Asset that creates/configures an SRP instance. |
| [`UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.RenderPipelineManager-beginCameraRendering.html) | `public static event Action<ScriptableRenderContext, Camera> beginCameraRendering` | Runtime | SRP camera render event; unsubscribe under matching lifetime. |
| [`UnityEngine.Rendering.CommandBuffer`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.CommandBuffer.html) | `public class CommandBuffer : IDisposable` | Runtime | Records rendering commands and owns native resources that should be released/disposed. |
| [`UnityEngine.Camera.AddCommandBuffer`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera.AddCommandBuffer.html) | `public void AddCommandBuffer(CameraEvent evt, CommandBuffer buffer)` | Built-in pipeline | Built-in-pipeline camera command buffer integration at a CameraEvent. |

### Package prerequisites

- `com.unity.postprocessing@3.5` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.postprocessing@3.5/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Render Pipelines — https://docs.unity3d.com/6000.3/Documentation/Manual/render-pipelines.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Materials — https://docs.unity3d.com/6000.3/Documentation/Manual/Materials.html
- Shaders — https://docs.unity3d.com/6000.3/Documentation/Manual/Shaders.html
- Rendering.Graphics Settings — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.GraphicsSettings.html
- Rendering.Graphics Settings Default Render Pipeline — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.GraphicsSettings-defaultRenderPipeline.html
- Rendering.Render Pipeline Asset — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.RenderPipelineAsset.html
- Rendering.Render Pipeline Manager — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.RenderPipelineManager.html
- Rendering.Render Pipeline Manager Begin Camera Rendering — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.RenderPipelineManager-beginCameraRendering.html
- Rendering.Command Buffer — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.CommandBuffer.html
- Camera.Add Command Buffer — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera.AddCommandBuffer.html
- Com.Unity.Postprocessing@3.5 — https://docs.unity3d.com/Packages/com.unity.postprocessing@3.5/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
