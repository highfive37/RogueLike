# Awaitable, Task Interop, Exceptions, and Main-Thread Safety

## Scope

Provides implementation-level rules for Unity Awaitable and Task-returning code, pooled awaitables, continuation scheduling, exceptions, cancellation, and Unity main-thread access.

Read this reference when the request involves:

- writing async methods in Unity 6.3
- interoperating with Task-based libraries
- debugging async exceptions, thread affinity, cancellation, or duplicate awaits

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Unity Awaitable**
- **Task interop**
- **continuation scheduling**
- **exception and cancellation flow**
- **Awaitable<T>**
- **AwaitableCompletionSource**
- **MainThreadAsync**
- **destroyCancellationToken**

## Verified Unity 6.3 facts

- Unity 6.3 officially supports async/await and provides `Awaitable`/`Awaitable<T>` as Unity-specific awaitable types. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html))
- Awaitable objects are pooled and must not be awaited more than once. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html))
- Awaitable continuations run synchronously when completion is raised, unlike Task continuations whose scheduling behavior is different. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html))
- Unity-provided Awaitables normally complete on the main thread unless the operation explicitly switches threads. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html))
- BackgroundThreadAsync moves continuation to a background thread and MainThreadAsync moves it to Unity’s main thread. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html))
- Awaitable can represent a result with Awaitable<T>, and AwaitableCompletionSource/its generic form can adapt callback completion into awaitable flow. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AwaitableCompletionSource.html))
- CancellationToken parameters are supported by Unity timing Awaitables, and destruction/application-exit tokens provide Unity lifetime cancellation sources. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.NextFrameAsync.html))
- Exceptions from async operations are observed through awaiting; fire-and-forget async code needs an explicit observation/logging policy. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Return Awaitable or Awaitable<T> from Unity-facing async APIs so callers can await completion, observe faults, and pass cancellation.
- **Interpretation:** When adapting a callback, complete exactly once and decide whether success, exception, or cancellation wins; synchronous continuation means completion can immediately re-enter caller code.
- **Interpretation:** At a Task-library boundary, await the Task according to its documented scheduler/thread behavior, then explicitly return to the main thread before Unity object access when needed.
- **Interpretation:** Name and document thread context at methods that can return on a background thread. Thread context is part of the method contract.
- **Interpretation:** Use `try/finally` for resource/state restoration and catch cancellation separately from actual failure.
- **Interpretation:** For intentionally detached operations, use one supervised launcher that catches/logs faults and owns cancellation rather than scattering `async void` calls.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Unity frame/timing operation | Unity Awaitable API | Integrates with PlayerLoop and Unity cancellation. |
| External library returns Task | Await Task at adapter boundary | Do not rewrite the dependency; restore Unity thread before engine access. |
| Callback/event must become awaitable | AwaitableCompletionSource | Complete once and account for synchronous continuation. |
| No caller can await by framework contract | Supervised `async void` boundary only | Catch faults and retain lifetime ownership. |

## Risks and review checks

- Do not cache and await a completed Awaitable a second time.
- Do not assume a continuation is deferred; Awaitable completion can invoke it synchronously.
- Do not touch UnityEngine.Object from a worker thread unless the specific API explicitly permits it.
- Do not omit cancellation from operations owned by a component, scene, or application lifetime.
- Do not use `async void` as a general method return type.
- Do not convert every Awaitable to Task without understanding allocation and scheduling consequences.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Awaitable`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.html) | `public class Awaitable` | Runtime | Pooled Unity awaitable; one await per instance. |
| [`UnityEngine.Awaitable<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable_1.html) | `public class Awaitable<T>` | Runtime | Unity awaitable that produces a result. |
| [`UnityEngine.AwaitableCompletionSource`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AwaitableCompletionSource.html) | `public class AwaitableCompletionSource` | Runtime | Produces an Awaitable controlled by explicit completion, cancellation, or exception. |
| [`UnityEngine.AwaitableCompletionSource<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AwaitableCompletionSource_1.html) | `public class AwaitableCompletionSource<T>` | Runtime | Generic completion source for a result-bearing Awaitable. |
| [`UnityEngine.Awaitable.MainThreadAsync`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.MainThreadAsync.html) | `public static Awaitable MainThreadAsync()` | Runtime | Switches continuation to main thread. |
| [`UnityEngine.Awaitable.BackgroundThreadAsync`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.BackgroundThreadAsync.html) | `public static Awaitable BackgroundThreadAsync()` | Runtime | Switches continuation to a background thread. |
| [`UnityEngine.MonoBehaviour.destroyCancellationToken`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour-destroyCancellationToken.html) | `public CancellationToken destroyCancellationToken { get; }` | Runtime | Object-destruction cancellation source. |

## Official sources

- Async Await Support — https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html
- Awaitable — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.html
- Awaitable 1 — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable_1.html
- Awaitable Completion Source — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AwaitableCompletionSource.html
- Awaitable Completion Source 1 — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AwaitableCompletionSource_1.html
- Awaitable.Next Frame Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.NextFrameAsync.html
- Awaitable.Main Thread Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.MainThreadAsync.html
- Awaitable.Background Thread Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.BackgroundThreadAsync.html
- Mono Behaviour Destroy Cancellation Token — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour-destroyCancellationToken.html
- Application Exit Cancellation Token — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-exitCancellationToken.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
