# Managed Allocations, Garbage Collection, and Object Pooling

## Scope

Connects C# allocation sources, Unity incremental GC, collection reuse, non-alloc APIs, and UnityEngine.Pool to code-level review and measurement.

Read this reference when the request involves:

- reducing GC.Alloc or frame spikes
- reviewing LINQ, closures, boxing, strings, arrays, or per-frame collections
- designing a pool for managed objects or GameObjects

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Managed memory**
- **garbage collection**
- **incremental GC**
- **allocation-free hot paths**
- **pooling**
- **UnityEngine.Pool**
- **GC**
- **ProfilerMarker**
- **non-alloc physics APIs**

## Verified Unity 6.3 facts

- Unity uses managed memory for C# objects and garbage collection reclaims managed allocations that are no longer reachable. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-managed-memory.html))
- Incremental garbage collection spreads collection work over multiple frames to reduce long pauses, but it does not eliminate allocation cost or total GC work. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-incremental-garbage-collection.html))
- Unity’s garbage-collection guidance identifies temporary allocations, repeated string concatenation, boxing, closures, array-returning APIs, and collection churn as common sources to inspect. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-garbage-collection-best-practices.html))
- Repeatedly growing collections can allocate new backing storage; collections can be cleared and reused when ownership makes reuse safe. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-garbage-collection-best-practices.html))
- Unity provides `UnityEngine.Pool.ObjectPool<T>` and related collection pools for reusable objects and containers. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1.html))
- CollectionPool<T0,T1> and ListPool<T> expose Get/Release patterns and pooled-object helpers. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.CollectionPool_2.html))
- Physics exposes NonAlloc query variants that write results into caller-provided arrays for reuse. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.RaycastNonAlloc.html))
- ProfilerMarker can instrument custom code and its Auto scope disposes to end a sample. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerMarker.html))
- Collections 2.6 is a released Unity 6.3 package whose documentation describes unmanaged data structures for jobs and Burst-compiled code; it is an optional package dependency, not a replacement for managed collections in ordinary code. ([official source](https://docs.unity3d.com/Packages/com.unity.collections@2.6/manual/index.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Measure a representative Player build before optimizing. Use allocation call stacks and markers to identify the exact path, frequency, and lifetime.
- **Interpretation:** Remove recurring allocations at high-frequency boundaries first: Update, FixedUpdate, animation/UI rebuilds, physics queries, and network serialization loops.
- **Interpretation:** Reuse buffers under a clear owner and document whether methods may retain them. Shared mutable buffers without ownership can corrupt nested or concurrent calls.
- **Interpretation:** Pool only objects with significant creation/destruction cost or high churn. Reset every externally visible field and unsubscribe listeners before release.
- **Interpretation:** Set realistic initial capacities for hot collections when upper bounds are known, but avoid retaining extremely large capacity after rare spikes without a memory policy.
- **Interpretation:** Prefer direct loops and cached delegates in hot paths when profiling proves LINQ/closure allocations matter; readability remains the default outside measured paths.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Short-lived managed object in infrequent setup | Allocate normally | Pooling can add complexity and retention for no material gain. |
| Per-frame reusable result buffer | Owner-held array/list cleared and reused | Avoids repeated backing allocations. |
| Frequently spawned complex GameObject | Measured object pool | Avoid repeated Instantiate/Destroy and initialization cost. |
| Physics query with known bounded results | NonAlloc overload + reusable array | Caller controls storage and handles truncation. |

## Risks and review checks

- Do not assume incremental GC makes per-frame allocation harmless.
- Do not pool without a reset contract; stale transforms, subscriptions, coroutines, and references are functional bugs.
- Do not retain pooled objects after Release or release an object twice.
- Do not replace every clear expression with manual low-level code before profiling.
- Do not ignore result truncation when a NonAlloc buffer is full.
- Do not keep unlimited pool size or collection capacity without measuring retained memory.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Pool.ObjectPool<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1-ctor.html) | `public ObjectPool(Func<T> createFunc, Action<T> actionOnGet = null, Action<T> actionOnRelease = null, Action<T> actionOnDestroy = null, bool collectionCheck = true, int defaultCapacity = 10, int maxSize = 10000)` | Runtime | Stack-based reusable object pool with lifecycle callbacks and maximum retained size. |
| [`UnityEngine.Pool.IObjectPool<T>.Get`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.IObjectPool_1.Get.html) | `T Get()` | Runtime | Obtains an instance; pooled-object overloads can provide an IDisposable release scope. |
| [`UnityEngine.Pool.IObjectPool<T>.Release`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.IObjectPool_1.Release.html) | `void Release(T element)` | Runtime | Returns an instance under the pool reset contract. |
| [`UnityEngine.Pool.ListPool<T>.Get`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ListPool_1.Get.html) | `public static List<T> Get()` | Runtime | Gets a pooled List<T>. |
| [`UnityEngine.Pool.ListPool<T>.Release`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ListPool_1.Release.html) | `public static void Release(List<T> toRelease)` | Runtime | Clears/returns a list to the collection pool. |
| [`UnityEngine.Physics.RaycastNonAlloc`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.RaycastNonAlloc.html) | `public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)` | Runtime | Writes hits into caller storage and returns count. |
| [`Unity.Profiling.ProfilerMarker.Auto`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerMarker.Auto.html) | `public AutoScope Auto()` | Runtime | Creates a disposable profiling sample scope. |

### Package prerequisites

- `com.unity.collections@2.6` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.collections@2.6/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Performance Managed Memory — https://docs.unity3d.com/6000.3/Documentation/Manual/performance-managed-memory.html
- Performance Incremental Garbage Collection — https://docs.unity3d.com/6000.3/Documentation/Manual/performance-incremental-garbage-collection.html
- Performance Garbage Collection Best Practices — https://docs.unity3d.com/6000.3/Documentation/Manual/performance-garbage-collection-best-practices.html
- Pool.Object Pool 1 — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1.html
- Pool.Object Pool 1 Ctor — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1-ctor.html
- Pool.Iobject Pool 1.Get — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.IObjectPool_1.Get.html
- Pool.Iobject Pool 1.Release — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.IObjectPool_1.Release.html
- Pool.Collection Pool 2 — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.CollectionPool_2.html
- Pool.List Pool 1.Get — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ListPool_1.Get.html
- Pool.List Pool 1.Release — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ListPool_1.Release.html
- Physics.Raycast Non Alloc — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.RaycastNonAlloc.html
- Profiling.Profiler Marker — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerMarker.html
- Profiling.Profiler Marker.Auto — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Profiling.ProfilerMarker.Auto.html
- Com.Unity.Collections@2.6 — https://docs.unity3d.com/Packages/com.unity.collections@2.6/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
