# Lighting, Materials, Shaders, and Per-Renderer Data

## Scope

Explains light sources, environment lighting, baked/realtime/mixed workflows, probes, materials, shader properties/variants, and per-renderer overrides across Unity 6.3 pipelines.

Read this reference when the request involves:

- authoring or debugging lights, shadows, lightmaps, probes, materials, or shaders
- setting shader properties from C# without duplicating materials
- controlling shader variants, keywords, or per-renderer values

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Lighting**
- **Materials**
- **Shaders**
- **lightmapping and probes**
- **shader variants**
- **Light**
- **RenderSettings**
- **Material**
- **Shader**
- **MaterialPropertyBlock**

## Verified Unity 6.3 facts

- Unity lighting combines Light components, environment/sky lighting, shadows, emissive materials, baked data, light probes, reflection probes, and render-pipeline-specific features. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/LightingOverview.html))
- Realtime, baked, and mixed lighting modes have different runtime flexibility, build/bake data, performance, and platform implications. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/LightingOverview.html))
- Materials select a Shader and store values for that shader’s exposed properties; render-pipeline shader compatibility must be respected. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Materials.html))
- Shaders define GPU programs and properties, and Unity compiles shader variants for keyword/platform/pipeline combinations that remain after build processing. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Shaders.html))
- Shader.PropertyToID converts a property name to an integer identifier for repeated property access during one run; the identifier is not a persistence value. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Shader.PropertyToID.html))
- MaterialPropertyBlock supplies per-renderer property overrides without requiring a unique Material asset for every renderer. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MaterialPropertyBlock.html))
- Accessing `Renderer.material` returns an instantiated material for that renderer when needed, while `sharedMaterial` references shared material data. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Renderer-material.html))
- Unity 6.3 adds shader and per-renderer data improvements documented in the 6.3 changes, including user per-renderer values and shader variant controls. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html))
- Global and local shader keywords have different scopes and affect variant selection/compilation. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Shaders.html))
- Shader Graph 17.3 is a Unity 6.3 core package; its package documentation describes a node-based visual shader-authoring workflow and notes its availability with URP or HDRP. ([official source](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.3/manual/index.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Choose the lighting workflow per target hardware, time-of-day needs, scene mutability, iteration budget, and memory/build constraints; do not default every light to realtime shadows.
- **Interpretation:** Treat lighting data, probes, and reflection captures as scene/content dependencies that must be rebuilt and validated after geometry or pipeline changes.
- **Interpretation:** Use shared Material assets for common visual identity and MaterialPropertyBlock for compatible per-renderer variation; instantiate materials only when persistent independent material state is required.
- **Interpretation:** Cache shader property IDs in static readonly fields for repeated calls, but never serialize the integer across runs.
- **Interpretation:** Control keywords at the narrowest correct scope and audit variants in builds. Runtime keyword combinations that were stripped cannot be recovered by C# alone.
- **Interpretation:** Keep shader/pipeline-specific rendering code behind a rendering adapter if the project supports more than one pipeline.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Static environment on constrained target | Baked lighting/probes after visual validation | Moves work from runtime at the cost of bake data and reduced dynamism. |
| Dynamic key light or time-of-day | Selective realtime/mixed lighting | Budget shadows, affected objects, and pipeline features. |
| Same material with per-object numeric/color values | MaterialPropertyBlock | Avoids unnecessary material instances when batching/pipeline rules permit. |
| Independent material keyword/state per renderer | Intentional material instance | Accept memory and batching implications. |

## Risks and review checks

- Do not access `renderer.material` repeatedly in initialization loops without understanding that it may instantiate material state.
- Do not change a shared Material asset at runtime when only one renderer should change.
- Do not serialize Shader.PropertyToID results.
- Do not enable arbitrary keyword combinations without build-time variant and memory analysis.
- Do not assume a shader written for one render pipeline renders correctly in another.
- Do not raise shadow distance/resolution globally before profiling GPU, CPU culling, memory, and visual need.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Light`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Light.html) | `public sealed class Light : Behaviour` | Runtime | Light type, color/intensity, range, shadows, culling, baking, and pipeline-facing settings. |
| [`UnityEngine.RenderSettings`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RenderSettings.html) | `public sealed class RenderSettings : Object` | Runtime | Scene environment and fog/ambient/reflection settings. |
| [`UnityEngine.Shader.PropertyToID`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Shader.PropertyToID.html) | `public static int PropertyToID(string name)` | Runtime | Returns a run-local property identifier. |
| [`UnityEngine.Material.SetFloat`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Material.SetFloat.html) | `public void SetFloat(int nameID, float value)` | Runtime | Sets a float material property; string overload also exists. |
| [`UnityEngine.Renderer.SetPropertyBlock`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Renderer.SetPropertyBlock.html) | `public void SetPropertyBlock(MaterialPropertyBlock properties)` | Runtime | Applies per-renderer or per-material-index overrides. |
| [`UnityEngine.MaterialPropertyBlock.SetColor`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MaterialPropertyBlock.SetColor.html) | `public void SetColor(int nameID, Color value)` | Runtime | Sets a color override in the property block. |
| [`UnityEngine.Shader.EnableKeyword`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Shader.EnableKeyword.html) | `public static void EnableKeyword(string keyword)` | Runtime | Enables a global shader keyword; use local-keyword APIs for local scope. |

### Package prerequisites

- `com.unity.shadergraph@17.3` — documented as a Unity 6.3 core package distributed with the target Editor. [Package documentation](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.3/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-core.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Lighting Overview — https://docs.unity3d.com/6000.3/Documentation/Manual/LightingOverview.html
- Materials — https://docs.unity3d.com/6000.3/Documentation/Manual/Materials.html
- Shaders — https://docs.unity3d.com/6000.3/Documentation/Manual/Shaders.html
- Whats New Unity63 — https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html
- Light — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Light.html
- Render Settings — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RenderSettings.html
- Material — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Material.html
- Renderer Material — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Renderer-material.html
- Shader.Property To Id — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Shader.PropertyToID.html
- Material.Set Float — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Material.SetFloat.html
- Material Property Block — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MaterialPropertyBlock.html
- Renderer.Set Property Block — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Renderer.SetPropertyBlock.html
- Material Property Block.Set Color — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MaterialPropertyBlock.SetColor.html
- Shader.Enable Keyword — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Shader.EnableKeyword.html
- Com.Unity.Shadergraph@17.3 — https://docs.unity3d.com/Packages/com.unity.shadergraph@17.3/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
