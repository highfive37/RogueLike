# Performance and Quality Code Review and Triage

## Scope

Converts Unity 6.3 official behavior into issue-classification, evidence, review, regression, and anti-optimization checklists for architecture and code reviews.

Read this reference when the request involves:

- reviewing a pull request for Unity performance or quality risk
- triaging a regression with incomplete evidence
- deciding whether an optimization is justified and how to verify it

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Quality triage**
- **code review**
- **performance budgets**
- **regression verification**
- **ProfilerMarker**
- **ProfilerRecorder**
- **BuildReport**
- **LogAssert**

## Verified Unity 6.3 facts

- Unity provides separate profiling modules and tools for CPU, GPU/rendering, memory, audio, physics, UI, loading, and platform-specific analysis. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-profiling-tools.html))
- Editor and Player behavior/timing differ, so target Player evidence is required for runtime performance conclusions. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-profiling-tools.html))
- Managed allocation, native memory, GPU resources, assets, and object lifetime are distinct diagnostic domains. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-managed-memory.html))
- Update, fixed update, rendering, coroutines, async continuations, and destruction execute in documented lifecycle/PlayerLoop phases. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- Build profiles/backends/platforms can change compilation, stripping, assets, graphics, APIs, and runtime behavior. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/build-profiles.html))
- Unity Test Framework, Code Coverage, Performance Testing API, Profiler tools, and Project Auditor provide complementary—not interchangeable—quality evidence. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- Project Auditor 2.0 can report static project/code/settings issues, while runtime tools are still required to prove dynamic cost and correctness. ([official source](https://docs.unity3d.com/Packages/com.unity.project-auditor@2.0/manual/index.html))
- Profiler markers/recorders and BuildReport provide machine-readable evidence that can be used by tests and CI gates. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerMarker.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Classify first: correctness/lifetime, CPU, GPU, memory, GC, loading/IO, physics, UI, network, build/backend, platform, or content. Each class needs different evidence.
- **Interpretation:** Require a baseline and acceptance criterion before approving an optimization: target device/build, reproducible scenario, metric, sample window, expected gain, and visual/behavioral equivalence.
- **Interpretation:** Review ownership before micro-performance: leaked events/handles/scenes/pools and duplicate systems are both correctness and performance defects.
- **Interpretation:** Inspect hot-path code for repeated lookup, allocation, logging, reflection, string work, asset load, Instantiate/Destroy, material instantiation, UI invalidation, and unbounded collections—but only prioritize measured impact.
- **Interpretation:** Every performance change needs a regression guard proportional to risk: test, counter, capture recipe, budget, validation script, or documented manual check.
- **Interpretation:** Reject “optimizations” that obscure behavior, broaden global state, reduce quality, or disable safety without quantified benefit and rollback criteria.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Correctness/lifetime issue | Reproduction + ownership/lifecycle trace + test | Performance tools cannot replace a lifetime contract. |
| CPU/GPU regression | Equivalent before/after Player captures | Compare distributions and dominant samples/passes. |
| Memory growth | Repeatable cycle + snapshot diff/retaining path | Distinguish leak, cache, fragmentation, and peak overlap. |
| Build/platform-only issue | Exact profile/backend/device artifact + logs/smoke test | Editor evidence is insufficient. |

## Risks and review checks

- Do not approve “zero-allocation” rewrites without a measured hot path and readability/ownership review.
- Do not trade deterministic correctness for frame-rate claims measured in a different scenario.
- Do not accept screenshots of FPS without build/device/settings and profiler evidence.
- Do not add global caches, singletons, pools, or static events without lifetime, capacity, reset, and test contracts.
- Do not mark a defect fixed until the original reproduction and relevant regression tests pass on the affected target.
- Do not merge a build-setting/profile change without recording its effect on all supported targets and CI.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`Unity.Profiling.ProfilerMarker`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerMarker.html) | `public struct ProfilerMarker` | Runtime | Semantic custom timing evidence. |
| [`Unity.Profiling.ProfilerRecorder`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerRecorder.html) | `public struct ProfilerRecorder : IDisposable` | Runtime | Runtime counter/sample collection for diagnostics or tests. |
| [`UnityEditor.Build.Reporting.BuildReport`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Build.Reporting.BuildReport.html) | `public class BuildReport : Object` | Editor | Structured build result, files, steps, stripping, and summary data. |
| [`UnityEngine.TestTools.LogAssert.Expect`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.LogAssert.Expect.html) | `public static void Expect(LogType type, string message)` | Test assembly | Makes an expected diagnostic part of a test contract. |

### Package prerequisites

- `com.unity.project-auditor@2.0` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.project-auditor@2.0/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.test-framework.performance@3.5` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@3.5/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.testtools.codecoverage@1.3` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.performance.profile-analyzer@1.4` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.performance.profile-analyzer@1.4/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.memoryprofiler@1.1` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.memoryprofiler@1.1/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Performance Profiling Tools — https://docs.unity3d.com/6000.3/Documentation/Manual/performance-profiling-tools.html
- Performance Managed Memory — https://docs.unity3d.com/6000.3/Documentation/Manual/performance-managed-memory.html
- Execution Order — https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html
- Build Profiles — https://docs.unity3d.com/6000.3/Documentation/Manual/build-profiles.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Profiling.Profiler Marker — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerMarker.html
- Profiling.Profiler Recorder — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerRecorder.html
- Build.Reporting.Build Report — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Build.Reporting.BuildReport.html
- Test Tools.Log Assert.Expect — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.LogAssert.Expect.html
- Com.Unity.Project Auditor@2.0 — https://docs.unity3d.com/Packages/com.unity.project-auditor@2.0/manual/index.html
- Com.Unity.Test Framework.Performance@3.5 — https://docs.unity3d.com/Packages/com.unity.test-framework.performance@3.5/manual/index.html
- Com.Unity.Testtools.Codecoverage@1.3 — https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3/manual/index.html
- Com.Unity.Performance.Profile Analyzer@1.4 — https://docs.unity3d.com/Packages/com.unity.performance.profile-analyzer@1.4/manual/index.html
- Com.Unity.Memoryprofiler@1.1 — https://docs.unity3d.com/Packages/com.unity.memoryprofiler@1.1/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
