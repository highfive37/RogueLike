# Memory, GC, Snapshots, and Pooling Quality

## Scope

Covers managed/native/GPU memory categories, allocation tracing, incremental GC, snapshots, retained references, asset lifetime, pools, and memory-budget verification.

Read this reference when the request involves:

- memory grows, GC spikes, or out-of-memory occurs
- using Memory Profiler 1.1 or managed allocation call stacks
- reviewing pools, caches, asset unloading, or retained references

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Managed and native memory**
- **garbage collection**
- **Memory Profiler**
- **asset lifetime**
- **pooling**
- **ProfilerRecorder**
- **Resources.UnloadUnusedAssets**
- **ObjectPool**
- **GC**

## Verified Unity 6.3 facts

- Unity applications use multiple memory domains including managed heap, native engine allocations, graphics resources, executable/code data, audio, and platform allocations. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-managed-memory.html))
- Managed allocation and garbage collection address only managed memory; a low GC.Alloc value does not prove total memory is low. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-managed-memory.html))
- Incremental GC distributes collection work but requires sufficient per-frame opportunity and does not reclaim referenced objects. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-incremental-garbage-collection.html))
- Unity’s GC best-practices documentation identifies reusable collections, closures, boxing, strings, arrays, and temporary object creation as common allocation concerns. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-garbage-collection-best-practices.html))
- Memory Profiler 1.1 is a released package for Unity 6.3 that captures and compares memory snapshots and object/reference data. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- UnityEngine.Object assets can remain loaded while referenced by scenes, objects, static fields, caches, or other assets. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html))
- `Resources.UnloadUnusedAssets` performs an operation to unload eligible unused assets and can be expensive; it is not a per-frame memory-management API. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Resources.UnloadUnusedAssets.html))
- ObjectPool<T> retains objects up to its maximum and therefore exchanges creation churn for retained memory and reset complexity. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1.html))
- Destroying a UnityEngine.Object releases its native object according to Unity lifetime rules, while the managed wrapper is later reclaimed by GC when unreachable. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Set per-platform budgets for total resident memory and major categories, then capture the same content checkpoints from a Player build on target hardware.
- **Interpretation:** For a leak, compare snapshots before and after a repeatable load/use/unload cycle and follow retaining references to the actual owner.
- **Interpretation:** Separate high-water caches from leaks: both retain memory, but a cache has a bounded eviction policy and measurable hit/value justification.
- **Interpretation:** Use pools only when creation/destruction churn is measured and cap retained size. A pool that never shrinks can convert a spike into permanent memory pressure.
- **Interpretation:** Release Addressables/asset handles, event subscriptions, static caches, render textures, native collections, and scene references at the same lifetime boundary that acquired them.
- **Interpretation:** Verify memory after several cycles and after a stabilization/collection point; a single snapshot at startup cannot demonstrate leak freedom.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Recurring managed allocation spike | Allocation call stacks + code inspection | Find source before changing collection/algorithm. |
| Resident memory grows across scene cycles | Memory snapshot diff and retaining paths | Distinguish assets, native objects, managed roots, and caches. |
| High spawn/despawn churn | Bounded pool after measurement | Trade CPU/GC for controlled retained memory. |
| One-time transition memory peak | Explicit load/unload sequencing | Avoid overlapping old/new content beyond budget. |

## Risks and review checks

- Do not call GC.Collect or UnloadUnusedAssets every frame to hide ownership bugs.
- Do not call every memory increase a managed leak; inspect native, GPU, asset, and platform categories.
- Do not keep static event subscribers or cached Unity objects past scene unload.
- Do not make pools unbounded or skip reset/release validation.
- Do not compare snapshots from different scenes, quality levels, platforms, or warm-up states as if they were equivalent.
- Do not dispose native resources only in a finalizer; use deterministic owner cleanup.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Resources.UnloadUnusedAssets`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Resources.UnloadUnusedAssets.html) | `public static AsyncOperation UnloadUnusedAssets()` | Runtime | Unloads eligible unused assets asynchronously and can trigger expensive work. |
| [`UnityEngine.Object.Destroy`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Destroy.html) | `public static void Destroy(Object obj)` | Runtime | Schedules Unity object destruction. |
| [`UnityEngine.Pool.ObjectPool<T>.Clear`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1.Clear.html) | `public void Clear()` | Runtime | Destroys/removes retained pool items through configured callbacks. |
| [`Unity.Profiling.ProfilerRecorder`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerRecorder.html) | `public struct ProfilerRecorder : IDisposable` | Runtime | Records supported memory counters and must be disposed. |
| [`UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.Profiler.GetTotalAllocatedMemoryLong.html) | `public static long GetTotalAllocatedMemoryLong()` | Runtime | Returns total allocated memory tracked by Unity; use profiler counters/snapshots for complete diagnosis. |

### Package prerequisites

- `com.unity.memoryprofiler@1.1` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.memoryprofiler@1.1/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Performance Managed Memory — https://docs.unity3d.com/6000.3/Documentation/Manual/performance-managed-memory.html
- Performance Incremental Garbage Collection — https://docs.unity3d.com/6000.3/Documentation/Manual/performance-incremental-garbage-collection.html
- Performance Garbage Collection Best Practices — https://docs.unity3d.com/6000.3/Documentation/Manual/performance-garbage-collection-best-practices.html
- Asset Workflow — https://docs.unity3d.com/6000.3/Documentation/Manual/AssetWorkflow.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Object — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.html
- Object.Destroy — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Destroy.html
- Resources.Unload Unused Assets — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Resources.UnloadUnusedAssets.html
- Pool.Object Pool 1 — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1.html
- Pool.Object Pool 1.Clear — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1.Clear.html
- Profiling.Profiler Recorder — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerRecorder.html
- Profiling.Profiler.Get Total Allocated Memory Long — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.Profiler.GetTotalAllocatedMemoryLong.html
- Com.Unity.Memoryprofiler@1.1 — https://docs.unity3d.com/Packages/com.unity.memoryprofiler@1.1/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
