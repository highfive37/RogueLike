# Coroutines, Awaitable, Async Flow, and Cancellation

## Scope

Explains Unity 6.3 coroutine and Awaitable behavior, continuation threads, ownership, cancellation, and when to use each asynchronous flow.

Read this reference when the request involves:

- choosing a coroutine, Awaitable, Task, or asynchronous scene/asset operation
- cancelling work when a component, scene, or application exits
- avoiding main-thread violations or pooled Awaitable misuse

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Coroutines**
- **Unity Awaitable**
- **main-thread continuation**
- **cancellation**
- **StartCoroutine**
- **Awaitable**
- **destroyCancellationToken**
- **AsyncOperation**

## Verified Unity 6.3 facts

- A coroutine is an IEnumerator-based flow that can suspend with `yield` and resume on later PlayerLoop phases without creating a separate thread. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Coroutines.html))
- Stopping or disabling behavior has documented coroutine effects; coroutine ownership remains with the MonoBehaviour that started it. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Coroutines.html))
- Unity 6.3 documents `Awaitable` as the Unity-specific awaitable type for asynchronous engine operations and timing helpers. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html))
- Awaitable instances are pooled; an Awaitable must not be awaited more than once. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html))
- Unity Awaitable continuations run synchronously when completion is triggered, and most Unity-provided Awaitables complete on the main thread unless a thread-switch API is used. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html))
- Unity API access is generally restricted to the main thread; BackgroundThreadAsync and MainThreadAsync explicitly switch continuation context. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html))
- MonoBehaviour.destroyCancellationToken is cancelled when its owning MonoBehaviour is destroyed, and Application.exitCancellationToken is cancelled on application exit. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour-destroyCancellationToken.html))
- AsyncOperation-derived engine operations can be awaited through Unity-supported async integration where documented, while their activation/completion behavior remains operation-specific. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Use coroutines for simple frame-sequenced behavior tightly owned by one MonoBehaviour and naturally expressed through yields.
- **Interpretation:** Use Awaitable for composable async methods, explicit return values, exception propagation, and Unity timing or thread-switch operations.
- **Interpretation:** Create one cancellation source at the owning lifetime and link or pass its token through every nested operation. Cancellation is part of the API contract, not an afterthought.
- **Interpretation:** After BackgroundThreadAsync, switch back with MainThreadAsync before reading or writing Unity objects unless the called API explicitly supports background use.
- **Interpretation:** Never cache and re-await a Unity Awaitable. Cache the resulting data or create a fresh operation instead.
- **Interpretation:** Guard against superseded work as well as destruction: search, scene transition, and UI request flows often need cancellation when a newer request starts.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Frame-by-frame sequence owned by one component | Coroutine | Simple lifecycle and yield instructions are sufficient. |
| Composable operation with result/error/cancellation | Awaitable-returning async method | Callers can await and propagate failure. |
| CPU-only work safe off main thread | BackgroundThreadAsync then MainThreadAsync | Explicitly brackets thread-sensitive Unity access. |
| Scene or object lifetime | Pass destruction/scene cancellation token | Late continuations cannot outlive their owner. |

## Risks and review checks

- Do not await the same Awaitable instance twice; pooling makes this invalid.
- Do not call UnityEngine object APIs after switching to a background thread.
- Do not use `async void` except for an event/message boundary whose exceptions are intentionally handled; prefer Awaitable or Awaitable<T> return values.
- Do not assume disabling a component cancels every asynchronous method it started.
- Do not swallow OperationCanceledException together with real faults unless cancellation is the intended outcome.
- Do not retain a Coroutine handle or task without a clear owner and stop/cancel path.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.MonoBehaviour.StartCoroutine`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.StartCoroutine.html) | `public Coroutine StartCoroutine(IEnumerator routine)` | Runtime | Starts an IEnumerator coroutine owned by the MonoBehaviour. |
| [`UnityEngine.MonoBehaviour.StopCoroutine`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.StopCoroutine.html) | `public void StopCoroutine(Coroutine routine)` | Runtime | Stops the specific started coroutine represented by the handle. |
| [`UnityEngine.Awaitable.NextFrameAsync`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.NextFrameAsync.html) | `public static Awaitable NextFrameAsync(CancellationToken cancellationToken = default)` | Runtime | Completes on a later frame and supports cancellation. |
| [`UnityEngine.Awaitable.FixedUpdateAsync`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.FixedUpdateAsync.html) | `public static Awaitable FixedUpdateAsync(CancellationToken cancellationToken = default)` | Runtime | Continues at the fixed-update phase. |
| [`UnityEngine.Awaitable.WaitForSecondsAsync`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.WaitForSecondsAsync.html) | `public static Awaitable WaitForSecondsAsync(float seconds, CancellationToken cancellationToken = default)` | Runtime | Waits using Unity time and supports cancellation. |
| [`UnityEngine.Awaitable.BackgroundThreadAsync`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.BackgroundThreadAsync.html) | `public static Awaitable BackgroundThreadAsync()` | Runtime | Switches continuation to a background thread. |
| [`UnityEngine.Awaitable.MainThreadAsync`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.MainThreadAsync.html) | `public static Awaitable MainThreadAsync()` | Runtime | Switches continuation to Unity main thread. |
| [`UnityEngine.MonoBehaviour.destroyCancellationToken`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour-destroyCancellationToken.html) | `public CancellationToken destroyCancellationToken { get; }` | Runtime | Cancelled on owner destruction. |
| [`UnityEngine.Application.exitCancellationToken`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-exitCancellationToken.html) | `public static CancellationToken exitCancellationToken { get; }` | Runtime | Cancelled when the application exits. |

## Official sources

- Coroutines — https://docs.unity3d.com/6000.3/Documentation/Manual/Coroutines.html
- Async Await Support — https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html
- Mono Behaviour.Start Coroutine — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.StartCoroutine.html
- Mono Behaviour.Stop Coroutine — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.StopCoroutine.html
- Awaitable — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.html
- Awaitable.Next Frame Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.NextFrameAsync.html
- Awaitable.Fixed Update Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.FixedUpdateAsync.html
- Awaitable.Wait For Seconds Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.WaitForSecondsAsync.html
- Awaitable.Background Thread Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.BackgroundThreadAsync.html
- Awaitable.Main Thread Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Awaitable.MainThreadAsync.html
- Mono Behaviour Destroy Cancellation Token — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour-destroyCancellationToken.html
- Application Exit Cancellation Token — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-exitCancellationToken.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
