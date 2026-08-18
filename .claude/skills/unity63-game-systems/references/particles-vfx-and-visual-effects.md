# Particle Systems, VFX Graph, Lines, Trails, and Visual Effects

## Scope

Covers Shuriken ParticleSystem, VFX Graph, line/trail renderers, effect lifetime, simulation spaces, pooling, bounds, events, and CPU/GPU performance choices.

Read this reference when the request involves:

- creating particles, GPU effects, trails, lines, decals, or transient effects
- choosing Particle System versus VFX Graph
- pooling effects or diagnosing VFX performance/culling

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Particle System**
- **Visual Effect Graph**
- **LineRenderer**
- **TrailRenderer**
- **effect lifecycle**
- **ParticleSystem**
- **VisualEffect**
- **LineRenderer**
- **TrailRenderer**

## Verified Unity 6.3 facts

- Unity visual-effects documentation includes the built-in Particle System, Visual Effect Graph, line/trail renderers, lens flares, halos, and full-screen/pipeline effects. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/visual-effects.html))
- ParticleSystem is a component-based CPU-side particle system with modular emission, shape, lifetime, velocity, color, size, collision, triggers, sub-emitters, and renderer configuration. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/ParticleSystems.html))
- ParticleSystem simulation can use local, world, or custom space, affecting how particles respond when the emitter transforms. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.MainModule-simulationSpace.html))
- ParticleSystem.Stop and Clear behavior is controlled by stop behavior and child options; effect pooling needs an explicit reset sequence. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.Stop.html))
- Visual Effect Graph targets large-scale GPU-simulated effects with graph-authored data/contexts and render-pipeline support constraints. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/visual-effects.html))
- Visual Effect Graph 17.3 is a Unity 6.3 core package; its package documentation describes node-based effects stored in reusable Visual Effect Assets and attached through Visual Effect components. ([official source](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.3/manual/index.html))
- LineRenderer draws a line from positions and TrailRenderer creates a time-based trail behind a moving object. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LineRenderer.html))
- Renderer bounds determine visibility culling; effects whose computed bounds do not cover generated content can disappear unexpectedly. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Renderer-bounds.html))
- ParticleSystem callbacks and trigger/collision modules can transfer event data to scripts but add CPU/data-transfer work. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnParticleCollision.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Use ParticleSystem for moderate effect counts, straightforward authoring, broad platform reach, and CPU interaction; use VFX Graph when GPU scale and supported pipeline/platform features justify it.
- **Interpretation:** Give every effect an owner and completion policy: one-shot completion, loop cancellation, pooled reset, or scene teardown. Visual effects must not self-leak indefinitely.
- **Interpretation:** When pooling, stop emission, wait or clear particles as intended, reset transform/properties, remove listeners, and return only after the effect is truly reusable.
- **Interpretation:** Select simulation space intentionally. World-space particles preserve emitted positions while local-space particles follow the emitter transform.
- **Interpretation:** Keep gameplay authority outside cosmetic particle collisions unless latency, determinism, and platform behavior are explicitly acceptable.
- **Interpretation:** Profile overdraw, particle count, vertex count, sorting, shadows, texture bandwidth, bounds/culling, and CPU modules from representative camera views.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Moderate portable effect with script interaction | ParticleSystem | Component modules and CPU-accessible particle data. |
| Very high particle count on supported GPU/pipeline | VFX Graph | GPU simulation/graph workflow after compatibility validation. |
| Path or beam with explicit points | LineRenderer | Direct point list and width/color control. |
| Motion history ribbon | TrailRenderer | Time-based trail generation. |

## Risks and review checks

- Do not return an effect GameObject to a pool while live particles or delayed sub-emitters remain unless reset handles them.
- Do not let every particle cast shadows or use expensive transparent shaders without measuring overdraw/GPU cost.
- Do not use world/local simulation space accidentally; emitter movement will expose the mistake.
- Do not rely on cosmetic particle collisions for authoritative multiplayer gameplay.
- Do not set bounds so small that GPU-generated effects are culled or so large that culling becomes ineffective.
- Do not assume VFX Graph support/behavior is identical across pipelines and target platforms.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.ParticleSystem.Play`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.Play.html) | `public void Play(bool withChildren = true)` | Runtime | Starts particle emission/simulation, optionally including child systems. |
| [`UnityEngine.ParticleSystem.Stop`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.Stop.html) | `public void Stop(bool withChildren = true, ParticleSystemStopBehavior stopBehavior = ParticleSystemStopBehavior.StopEmitting)` | Runtime | Stops emission with selected clear behavior. |
| [`UnityEngine.ParticleSystem.Clear`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.Clear.html) | `public void Clear(bool withChildren = true)` | Runtime | Removes particles from the system. |
| [`UnityEngine.ParticleSystem.GetParticles`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.GetParticles.html) | `public int GetParticles(ParticleSystem.Particle[] particles, int size = -1, int offset = 0)` | Runtime | Copies live particles to caller storage; overloads include list/native forms where documented. |
| [`UnityEngine.LineRenderer.SetPositions`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LineRenderer.SetPositions.html) | `public void SetPositions(Vector3[] positions)` | Runtime | Sets line points from an array. |
| [`UnityEngine.TrailRenderer.Clear`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TrailRenderer.Clear.html) | `public void Clear()` | Runtime | Clears all trail points for reuse. |
| [`UnityEngine.VFX.VisualEffect`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/VFX.VisualEffect.html) | `public class VisualEffect : Behaviour` | VFX runtime | Runtime component for a Visual Effect Graph asset; availability depends on the VFX Graph/render pipeline setup. |

### Package prerequisites

- `com.unity.visualeffectgraph@17.3` — documented as a Unity 6.3 core package distributed with the target Editor. [Package documentation](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.3/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-core.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Visual Effects — https://docs.unity3d.com/6000.3/Documentation/Manual/visual-effects.html
- Particle Systems — https://docs.unity3d.com/6000.3/Documentation/Manual/ParticleSystems.html
- Particle System — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.html
- Particle System.Main Module Simulation Space — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.MainModule-simulationSpace.html
- Particle System.Play — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.Play.html
- Particle System.Stop — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.Stop.html
- Particle System.Clear — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.Clear.html
- Particle System.Get Particles — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.GetParticles.html
- Line Renderer — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LineRenderer.html
- Line Renderer.Set Positions — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LineRenderer.SetPositions.html
- Trail Renderer — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TrailRenderer.html
- Trail Renderer.Clear — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TrailRenderer.Clear.html
- Renderer Bounds — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Renderer-bounds.html
- Mono Behaviour.On Particle Collision — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnParticleCollision.html
- Vfx.Visual Effect — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/VFX.VisualEffect.html
- Com.Unity.Visualeffectgraph@17.3 — https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.3/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
