# Profiling CPU, Frames, and the PlayerLoop

## Scope

Provides a measurement-first workflow for CPU frame time, main/render/job threads, PlayerLoop phases, scripts, custom markers, call stacks, comparisons, and representative Player captures.

Read this reference when the request involves:

- a frame-rate, CPU, or main-thread problem is reported
- using Profiler, ProfilerRecorder, Profile Analyzer, or custom markers
- reviewing Update/FixedUpdate cost or intermittent spikes

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Unity Profiler**
- **CPU Usage module**
- **PlayerLoop**
- **custom instrumentation**
- **capture comparison**
- **ProfilerMarker**
- **ProfilerRecorder**
- **Profiler**
- **PlayerLoop**

## Verified Unity 6.3 facts

- Unity Profiler records performance data in modules such as CPU, GPU, Rendering, Memory, Audio, Physics, UI, and others depending on target/support. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Profiler.html))
- The CPU Usage Profiler module presents time by PlayerLoop hierarchy, call stacks/samples, threads, categories, and timeline/hierarchy views. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/ProfilerWindow.html))
- Profiling the Editor includes Editor overhead; Unity recommends profiling a representative Player on target hardware for runtime decisions. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-profiling-tools.html))
- Deep Profiling adds instrumentation and significant overhead, so its timings are not a normal performance baseline. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-profiling-tools.html))
- Development builds and Autoconnect Profiler enable Player capture workflows, with overhead that must be considered. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-profiling-tools.html))
- ProfilerMarker adds named custom samples and its Auto scope can delimit a block safely. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerMarker.html))
- ProfilerRecorder reads selected profiler counters/samples from code and can be configured with capacity and options. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerRecorder.html))
- Profile Analyzer 1.4 is a released Unity 6.3 package for comparing and aggregating captured frames. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- The PlayerLoop exposes the engine’s ordered update systems; script callbacks appear within these phases. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LowLevel.PlayerLoop.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Define a target metric, hardware/build configuration, scene, camera path, input script, warm-up, sample window, and percentile before changing code.
- **Interpretation:** Start with frame-level CPU/GPU attribution, then descend only into the dominant subsystem/thread/sample. Do not optimize the most visible method if it is not material to the frame budget.
- **Interpretation:** Add markers around feature-level operations that Unity samples cannot distinguish, and remove or retain them according to diagnostic value and overhead.
- **Interpretation:** Use medians/percentiles and compare captures rather than one frame. For spikes, correlate the exact frame with GC, loading, physics catch-up, shader compilation, logging, or operating-system events.
- **Interpretation:** Measure Player builds with matching scripting backend, stripping, graphics API, quality, resolution, and content. Editor results are for navigation, not final acceptance.
- **Interpretation:** When FixedUpdate dominates, inspect fixed-step count and catch-up before optimizing individual physics callbacks.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Unknown frame-rate regression | Target Player Profiler capture | First locate CPU/GPU/thread/subsystem ownership. |
| Feature has opaque nested work | ProfilerMarker around semantic operation | Correlates system intent with engine samples. |
| Compare before/after or two branches | Profile Analyzer 1.4 | Aggregates equivalent frame ranges and distributions. |
| Runtime telemetry counter | ProfilerRecorder | Reads supported counters without opening Profiler UI. |

## Risks and review checks

- Do not optimize from Editor Game view FPS alone.
- Do not compare captures with different quality, resolution, thermal state, warm-up, camera path, or build backend.
- Do not leave Deep Profiling enabled when reporting production frame costs.
- Do not infer causation from one tall sample without inspecting child samples, thread overlap, and frame context.
- Do not average away rare but user-visible spikes; inspect percentiles and worst frames.
- Do not rewrite PlayerLoop order as a first response to expensive feature code.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`Unity.Profiling.ProfilerMarker`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerMarker-ctor.html) | `public ProfilerMarker(string name)` | Runtime | Creates a custom profiler marker. |
| [`Unity.Profiling.ProfilerMarker.Auto`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerMarker.Auto.html) | `public AutoScope Auto()` | Runtime | Begins a marker and returns a disposable scope. |
| [`Unity.Profiling.ProfilerRecorder.StartNew`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerRecorder.StartNew.html) | `public static ProfilerRecorder StartNew(ProfilerCategory category, string statName, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)` | Runtime | Starts recording a named profiler statistic. |
| [`UnityEngine.Profiling.Profiler.BeginSample`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.Profiler.BeginSample.html) | `public static void BeginSample(string name, Object targetObject = null)` | Runtime | Begins a custom sample; pair with EndSample on all paths. |
| [`UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LowLevel.PlayerLoop.GetCurrentPlayerLoop.html) | `public static PlayerLoopSystem GetCurrentPlayerLoop()` | Runtime | Returns current loop configuration for inspection. |

### Package prerequisites

- `com.unity.performance.profile-analyzer@1.4` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.performance.profile-analyzer@1.4/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Profiler — https://docs.unity3d.com/6000.3/Documentation/Manual/Profiler.html
- Profiler Window — https://docs.unity3d.com/6000.3/Documentation/Manual/ProfilerWindow.html
- Performance Profiling Tools — https://docs.unity3d.com/6000.3/Documentation/Manual/performance-profiling-tools.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Profiling.Profiler Marker — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerMarker.html
- Profiling.Profiler Marker Ctor — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerMarker-ctor.html
- Profiling.Profiler Marker.Auto — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerMarker.Auto.html
- Profiling.Profiler Recorder — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerRecorder.html
- Profiling.Profiler Recorder.Start New — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerRecorder.StartNew.html
- Profiling.Profiler.Begin Sample — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.Profiler.BeginSample.html
- Low Level.Player Loop — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LowLevel.PlayerLoop.html
- Low Level.Player Loop.Get Current Player Loop — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LowLevel.PlayerLoop.GetCurrentPlayerLoop.html
- Com.Unity.Performance.Profile Analyzer@1.4 — https://docs.unity3d.com/Packages/com.unity.performance.profile-analyzer@1.4/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
