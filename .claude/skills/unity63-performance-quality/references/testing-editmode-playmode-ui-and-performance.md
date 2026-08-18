# EditMode, PlayMode, UI, Coverage, and Performance Testing

## Scope

Defines a layered Unity 6.3 test strategy using EditMode and PlayMode tests, UnityTest frame flows, logs, scenes, UI Test Framework additions, code coverage, and performance measurements.

Read this reference when the request involves:

- creating a Unity test strategy
- choosing EditMode versus PlayMode tests
- testing coroutines, scenes, physics, UI, performance, or coverage

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Unity Test Framework**
- **EditMode tests**
- **PlayMode tests**
- **UI tests**
- **performance tests**
- **UnityTest**
- **LogAssert**
- **SceneManager**
- **Performance testing API**

## Verified Unity 6.3 facts

- Unity Test Framework supports EditMode and PlayMode test execution and integrates tests through test assemblies. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html))
- EditMode tests run in the Editor test environment and are appropriate for fast code/Editor tests that do not require Player frame behavior. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html))
- PlayMode tests run with PlayerLoop behavior and can exercise GameObjects, scenes, coroutines, physics, rendering, and runtime lifecycle. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html))
- A UnityTest can return IEnumerator so a test can yield across frames and Unity yield instructions. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.UnityTestAttribute.html))
- LogAssert can declare expected logs and fail tests on unexpected error logs according to test settings. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.LogAssert.html))
- Code Coverage 1.3 is a released Unity 6.3 package for collecting managed code coverage. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- Performance Testing API 3.5 is a released Unity 6.3 package for repeatable measurements and metadata. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- Unity 6.3 adds UI Test Framework 1.0 capabilities documented in the release changes; exact package/setup requirements must be verified before its APIs are assumed. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html))
- Test results are sensitive to mode, platform, backend, frame timing, scene state, graphics, and hardware, so target-device tests remain necessary. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Use a pyramid: many pure/EditMode rule tests, focused PlayMode component/scene integration tests, and fewer target-device end-to-end/performance tests.
- **Interpretation:** Create a fresh fixture owner for every test and tear down scenes, GameObjects, static state, event subscriptions, temporary assets, and settings even when the assertion fails.
- **Interpretation:** For frame tests, wait on observable conditions with a timeout rather than an arbitrary number of frames when timing is not the behavior under test.
- **Interpretation:** Use deterministic seeds, fixed inputs, controlled scenes, and explicit quality/time settings. Record backend/platform metadata with performance results.
- **Interpretation:** Set performance budgets using distributions/warm-up/measurement counts and compare like-for-like builds. A performance test is not a one-shot stopwatch.
- **Interpretation:** Use coverage to find untested risk, not as proof of behavioral quality. Prioritize state transitions, migrations, failure paths, and ownership cleanup.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Pure state/rule/serializer | EditMode test | Fast, deterministic, no frame/scene dependency. |
| MonoBehaviour lifecycle/coroutine/scene/physics | PlayMode UnityTest | Requires PlayerLoop and Unity object lifecycle. |
| UI interaction/focus/accessibility flow | PlayMode/UI Test Framework after exact setup verification | Exercise events, focus, rendering, and accessible semantics. |
| Frame time/allocation regression | Performance Testing API in representative Player | Warm-up, sample, metadata, and thresholds are explicit. |

## Risks and review checks

- Do not let tests depend on execution order, previous test scene, static singleton residue, or editor selection.
- Do not use long arbitrary waits where a condition and timeout are possible.
- Do not ignore unexpected error logs in a “passing” test.
- Do not benchmark Editor-only timings and label them target performance.
- Do not chase 100% coverage while failure paths and integration boundaries remain untested.
- Do not assume UI Test Framework APIs/package are present merely from the 6.3 feature announcement.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.TestTools.UnityTestAttribute`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.UnityTestAttribute.html) | `[UnityTest]` | Test assembly | Marks an IEnumerator test that can yield through Unity frames. |
| [`UnityEngine.TestTools.LogAssert.Expect`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.LogAssert.Expect.html) | `public static void Expect(LogType type, string message)` | Test assembly | Declares expected log output; regex overloads exist. |
| [`UnityEngine.TestTools.LogAssert.NoUnexpectedReceived`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.LogAssert.NoUnexpectedReceived.html) | `public static void NoUnexpectedReceived()` | Test assembly | Asserts no unhandled unexpected logs were received. |
| [`UnityEngine.SceneManagement.SceneManager.LoadScene`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.LoadScene.html) | `public static void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)` | PlayMode test/runtime | Loads test scenes synchronously when that behavior is acceptable; async is preferable for production transitions. |

### Package prerequisites

- `com.unity.testtools.codecoverage@1.3` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.test-framework.performance@3.5` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@3.5/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Test Framework Introduction — https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Whats New Unity63 — https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html
- Test Tools.Unity Test Attribute — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.UnityTestAttribute.html
- Test Tools.Log Assert — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.LogAssert.html
- Test Tools.Log Assert.Expect — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.LogAssert.Expect.html
- Test Tools.Log Assert.No Unexpected Received — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TestTools.LogAssert.NoUnexpectedReceived.html
- Scene Management.Scene Manager.Load Scene — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.LoadScene.html
- Com.Unity.Testtools.Codecoverage@1.3 — https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3/manual/index.html
- Com.Unity.Test Framework.Performance@3.5 — https://docs.unity3d.com/Packages/com.unity.test-framework.performance@3.5/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
