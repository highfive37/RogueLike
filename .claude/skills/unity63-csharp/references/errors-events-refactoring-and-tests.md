# Errors, Events, Refactoring Safety, and Testable C#

## Scope

Provides C# review rules for exceptions, logging, assertions, event lifetime, serialized refactors, and choosing EditMode versus PlayMode tests.

Read this reference when the request involves:

- handling exceptions or diagnostics
- reviewing event subscription leaks
- refactoring serialized or Unity-callback code safely
- choosing EditMode or PlayMode tests

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Exceptions and diagnostics**
- **events**
- **safe refactoring**
- **Unity Test Framework**
- **Debug**
- **Assertions**
- **UnityEvent**
- **LogAssert**
- **FormerlySerializedAs**

## Verified Unity 6.3 facts

- Unity Console and logging APIs record messages, warnings, errors, exceptions, and stack traces for Editor and Player diagnostics. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/debugging-and-diagnostics.html))
- `Debug.LogException` logs an Exception and can associate the message with a Unity object context. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Debug.LogException.html))
- Unity assertions can express development-time invariants and are controlled by assertion compilation settings. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Assertions.Assert.html))
- C# delegates and events retain references to subscribed targets until unsubscribed or the publisher becomes unreachable. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/performance-managed-memory.html))
- UnityEvent supports persistent Inspector listeners and runtime listeners; runtime listeners use AddListener/RemoveListener. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Events.UnityEvent.html))
- Unity Test Framework supports tests in Edit mode and Play mode with different engine/runtime contexts. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html))
- `LogAssert` enables tests to expect and validate Unity log output. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.LogAssert.html))
- FormerlySerializedAs can preserve existing serialized field data across a name change. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Serialization.FormerlySerializedAsAttribute.html))
- Assembly definition test settings isolate test code and can keep test dependencies out of Player assemblies. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Catch an exception only where the code can add context, recover, translate to a domain result, or guarantee cleanup. Otherwise let the owning boundary observe it.
- **Interpretation:** Include stable identifiers and operation context in logs, but avoid per-frame log flooding and sensitive data.
- **Interpretation:** Pair subscriptions with the exact intended lifetime and test disable/re-enable, scene unload, object destruction, and repeated initialization paths.
- **Interpretation:** Before refactoring a serialized field or callback, inventory prefab/scene/assets, preserve names where possible, and add tests for data and message behavior.
- **Interpretation:** Use EditMode tests for pure rules, serialization helpers, migrations, and editor-independent code. Use PlayMode tests for GameObject lifecycle, coroutines, scenes, physics, UI, and frame timing.
- **Interpretation:** Prefer behavior tests over implementation tests. A refactor should be able to change private decomposition while external outcomes remain verified.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Programmer invariant during development | Assert with clear condition/context | Not a substitute for validating untrusted runtime input. |
| Recoverable external failure | Typed result or caught exception at boundary | Caller receives an actionable state. |
| Pure deterministic rule | EditMode test | Fast and independent of scene/frame lifecycle. |
| Unity lifecycle/physics/coroutine integration | PlayMode test | Runs in the required engine context. |

## Risks and review checks

- Do not catch `Exception` and return a default value without recording or exposing failure.
- Do not use assertions as production input validation or recovery.
- Do not subscribe in OnEnable and unsubscribe only in OnDestroy when disabled objects should stop receiving events.
- Do not rename serialized fields or Unity message methods as an ordinary symbol-only refactor.
- Do not write tests that call private callbacks by reflection instead of exercising the observable lifecycle.
- Do not leave unexpected error logs in passing tests; explicitly expect them or fix the source.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Debug.LogException`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Debug.LogException.html) | `public static void LogException(Exception exception, Object context = null)` | Runtime | Logs an exception, optionally linked to a Unity object. |
| [`UnityEngine.Assertions.Assert.IsTrue`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Assertions.Assert.IsTrue.html) | `public static void IsTrue(bool condition, string message = null)` | Runtime | Development assertion for a true invariant. |
| [`UnityEngine.Events.UnityEvent.AddListener`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Events.UnityEvent.AddListener.html) | `public void AddListener(UnityAction call)` | Runtime | Adds a runtime listener. |
| [`UnityEngine.Events.UnityEvent.RemoveListener`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Events.UnityEvent.RemoveListener.html) | `public void RemoveListener(UnityAction call)` | Runtime | Removes a matching runtime listener. |
| [`UnityEngine.TestTools.LogAssert.Expect`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.LogAssert.Expect.html) | `public static void Expect(LogType type, string message)` | Test assembly | Declares an expected Unity log in a test; regex overloads are also available. |
| [`UnityEngine.Serialization.FormerlySerializedAsAttribute`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Serialization.FormerlySerializedAsAttribute.html) | `[FormerlySerializedAs(string oldName)]` | Runtime | Preserves existing serialized field data after rename. |

## Official sources

- Debugging And Diagnostics — https://docs.unity3d.com/6000.3/Documentation/Manual/debugging-and-diagnostics.html
- Test Framework Introduction — https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html
- Performance Managed Memory — https://docs.unity3d.com/6000.3/Documentation/Manual/performance-managed-memory.html
- Assembly Definition Files — https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html
- Debug.Log Exception — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Debug.LogException.html
- Assertions.Assert — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Assertions.Assert.html
- Assertions.Assert.Is True — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Assertions.Assert.IsTrue.html
- Events.Unity Event — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Events.UnityEvent.html
- Events.Unity Event.Add Listener — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Events.UnityEvent.AddListener.html
- Events.Unity Event.Remove Listener — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Events.UnityEvent.RemoveListener.html
- Test Tools.Log Assert — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.LogAssert.html
- Test Tools.Log Assert.Expect — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.LogAssert.Expect.html
- Serialization.Formerly Serialized As Attribute — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Serialization.FormerlySerializedAsAttribute.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
