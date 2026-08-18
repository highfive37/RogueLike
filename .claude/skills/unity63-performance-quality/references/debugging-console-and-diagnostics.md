# Debugging, Console, Logs, Assertions, and Diagnostics

## Scope

Provides a reproducible diagnostic workflow using Console/logging, stack traces, Development Builds, debugger attachment, crash/player logs, Frame Debugger, Profiler, and targeted assertions.

Read this reference when the request involves:

- investigating an exception, crash, hang, rendering error, or device-only defect
- using Console, logs, stack traces, debugger, Development Build, Frame Debugger, or diagnostics
- designing actionable logging and failure reports

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Console and logging**
- **debugger and development builds**
- **crash diagnostics**
- **Frame Debugger**
- **Debug**
- **Logger**
- **StackTraceUtility**
- **Application.logMessageReceived**

## Verified Unity 6.3 facts

- Unity debugging and diagnostics documentation covers Console messages, script debugging, stack traces, Player logs, crash reports, Profiler, Frame Debugger, and platform tools. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/debugging-and-diagnostics.html))
- Debug.Log, LogWarning, LogError, and LogException can include a Unity object context that links diagnostics to an object. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Debug.html))
- Application.logMessageReceived and logMessageReceivedThreaded expose log callbacks with different thread constraints. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-logMessageReceived.html))
- StackTraceLogType settings control stack trace detail per log type. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application.SetStackTraceLogType.html))
- Development Build and Script Debugging alter build diagnostics and overhead/security characteristics and should not automatically ship as production settings. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/build-profiles.html))
- The Frame Debugger exposes ordered rendering events and state for a captured frame. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/debugging-and-diagnostics.html))
- Assertions express development invariants and can be conditionally compiled/configured; they do not replace runtime validation of untrusted input. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Assertions.Assert.html))
- Project Auditor 2.0 is a released Unity 6.3 package for static project/code/settings analysis and issue reporting. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Make every bug report reproducible: exact Unity/editor revision, build commit, platform/device, backend, graphics API, quality/settings, scene, steps, expected/actual result, logs, and capture files.
- **Interpretation:** Start from the first causal error, not the flood of follow-on NullReference or missing-object messages. Preserve full stack traces and context IDs.
- **Interpretation:** Use structured categories and stable operation/session/content identifiers. Throttle repeated logs and exclude secrets, tokens, personal data, and excessive payloads.
- **Interpretation:** Choose the diagnostic tool by symptom: debugger for state/control flow, Profiler for timing/allocation, Frame Debugger for render event construction, platform crash tools for native failure.
- **Interpretation:** Reproduce in a Development Player that matches release backend/settings closely, then confirm the fix in a non-development release candidate.
- **Interpretation:** Turn recurring defects into assertions, validation, tests, or Project Auditor rules where the check is static/repeatable.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Managed exception with stack | Console/Player log + debugger | Inspect first fault and object/state context. |
| Frame spike/hang | Profiler timeline and target capture | Debug stepping changes timing and can hide the issue. |
| Wrong/missing draw | Frame Debugger then GPU capture | Inspect render event, pass, shader, resources, and state. |
| Native crash/device-only issue | Platform crash dump/logs + symbols + matching build | Managed Console alone may have no causal data. |

## Risks and review checks

- Do not log every Update value to diagnose a timing issue; logging can become the timing issue.
- Do not catch and discard exceptions to make Console output clean.
- Do not ship Script Debugging or verbose sensitive logs by accident.
- Do not diagnose a Player-only bug solely in Editor Play mode.
- Do not assume the last log line caused a native crash; correlate timestamps, threads, stack, and dump.
- Do not use assertions for recoverable user/network/file input failures.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Debug.Log`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Debug.Log.html) | `public static void Log(object message, Object context = null)` | Runtime | Logs an informational message, optionally associated with a Unity object. |
| [`UnityEngine.Debug.LogException`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Debug.LogException.html) | `public static void LogException(Exception exception, Object context = null)` | Runtime | Logs an exception and stack/context. |
| [`UnityEngine.Application.logMessageReceived`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-logMessageReceived.html) | `public static event LogCallback logMessageReceived` | Runtime | Main-thread log callback; callback logging can recurse. |
| [`UnityEngine.Application.logMessageReceivedThreaded`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-logMessageReceivedThreaded.html) | `public static event LogCallback logMessageReceivedThreaded` | Runtime | Can be invoked from any thread; handler must be thread-safe. |
| [`UnityEngine.Application.SetStackTraceLogType`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application.SetStackTraceLogType.html) | `public static void SetStackTraceLogType(LogType logType, StackTraceLogType stackTraceType)` | Runtime | Sets stack trace output policy for a log type. |
| [`UnityEngine.Assertions.Assert.IsNotNull`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Assertions.Assert.IsNotNull.html) | `public static void IsNotNull<T>(T value, string message = null) where T : class` | Runtime | Development assertion for a non-null reference. |

### Package prerequisites

- `com.unity.project-auditor@2.0` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.project-auditor@2.0/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Debugging And Diagnostics — https://docs.unity3d.com/6000.3/Documentation/Manual/debugging-and-diagnostics.html
- Build Profiles — https://docs.unity3d.com/6000.3/Documentation/Manual/build-profiles.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Debug — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Debug.html
- Debug.Log — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Debug.Log.html
- Debug.Log Exception — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Debug.LogException.html
- Application Log Message Received — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-logMessageReceived.html
- Application Log Message Received Threaded — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-logMessageReceivedThreaded.html
- Application.Set Stack Trace Log Type — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application.SetStackTraceLogType.html
- Assertions.Assert — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Assertions.Assert.html
- Assertions.Assert.Is Not Null — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Assertions.Assert.IsNotNull.html
- Com.Unity.Project Auditor@2.0 — https://docs.unity3d.com/Packages/com.unity.project-auditor@2.0/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
