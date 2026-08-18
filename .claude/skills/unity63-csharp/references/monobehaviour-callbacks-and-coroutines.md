# MonoBehaviour Messages, Callback Contracts, and Coroutines

## Scope

Documents message signatures, activation requirements, callback ordering limits, coroutine ownership, yield instructions, and implementation review rules.

Read this reference when the request involves:

- implementing or reviewing Unity messages
- a callback does not execute
- starting, stopping, nesting, or cancelling coroutines

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **MonoBehaviour lifecycle**
- **Unity messages**
- **coroutines**
- **activation**
- **Awake**
- **OnEnable**
- **Start**
- **Update**
- **OnDisable**
- **OnDestroy**
- **StartCoroutine**

## Verified Unity 6.3 facts

- Unity invokes MonoBehaviour message methods by documented name/signature; they are not ordinary virtual overrides of MonoBehaviour. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-MonoBehaviour.html))
- Awake is called when the script instance is loaded, OnEnable when enabled and active, Start before the first frame update if enabled, and Update once per frame while enabled. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- FixedUpdate is scheduled on fixed time, LateUpdate follows Update, and rendering/GUI/destruction callbacks occupy later documented phases. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- Ordering of the same event across different objects is not a general dependency guarantee; execution order settings can adjust relative order. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-MonoManager.html))
- A coroutine method returns IEnumerator and yields instructions or values that determine when it resumes. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Coroutines.html))
- StartCoroutine returns a Coroutine handle and supports an IEnumerator overload plus a string-based overload with higher overhead and limited parameter shape. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.StartCoroutine.html))
- StopCoroutine must use a compatible identity form with how the coroutine was started; StopAllCoroutines affects only coroutines on that MonoBehaviour. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.StopCoroutine.html))
- WaitForSeconds uses scaled time and begins waiting at the end of the current frame, while WaitForSecondsRealtime uses unscaled time. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/WaitForSeconds.html))
- Coroutines do not run on a worker thread; work before and after yields executes on Unity’s main thread unless another mechanism explicitly moves it. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Coroutines.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Keep message methods thin and delegate to explicit methods/classes. This makes behavior callable and testable without pretending Unity messages are a normal inheritance API.
- **Interpretation:** Pair enabled-lifetime resource acquisition/subscription in OnEnable/OnDisable and destruction-lifetime release in an explicit owner/OnDestroy path.
- **Interpretation:** Store Coroutine handles when cancellation matters. Avoid string-based startup because it weakens refactoring and type safety.
- **Interpretation:** Prefer one coroutine owner for a sequence. Nested routines should propagate cancellation/stop semantics clearly rather than starting detached child routines accidentally.
- **Interpretation:** Select scaled or unscaled waits according to the feature’s pause policy, not convenience.
- **Interpretation:** Use explicit guards for re-entry and disable/destroy transitions so multiple starts cannot race or leak state.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Local invariant construction | Awake | No cross-object Start readiness assumptions. |
| Active subscription/resource | OnEnable / OnDisable | Follows activation. |
| One-time enabled startup | Start | Runs before first Update when enabled. |
| Cancellable frame sequence | IEnumerator + stored Coroutine handle | Owner can stop the exact sequence. |

## Risks and review checks

- Do not add `override` to Unity message methods unless the base class in your own hierarchy declares a virtual method and the message dispatch design is intentional.
- Do not misspell or change a message parameter signature; Unity will not call an unrecognized method.
- Do not call StopCoroutine with a new IEnumerator instance and expect it to identify an existing run.
- Do not use WaitForSeconds for pause-independent behavior.
- Do not perform long blocking work between yields; a coroutine still occupies the main thread.
- Do not assume disabling one component automatically stops every async flow not owned as its coroutine.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.MonoBehaviour.Awake`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Awake.html) | `private void Awake()` | Runtime | Load-time message for local initialization. |
| [`UnityEngine.MonoBehaviour.OnEnable`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnEnable.html) | `private void OnEnable()` | Runtime | Message when enabled and active. |
| [`UnityEngine.MonoBehaviour.Start`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Start.html) | `private void Start(); private IEnumerator Start()` | Runtime | One-time enabled startup; IEnumerator form is a coroutine. |
| [`UnityEngine.MonoBehaviour.StartCoroutine`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.StartCoroutine.html) | `public Coroutine StartCoroutine(IEnumerator routine)` | Runtime | Starts and returns a handle for an IEnumerator routine. |
| [`UnityEngine.MonoBehaviour.StopCoroutine`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.StopCoroutine.html) | `public void StopCoroutine(Coroutine routine)` | Runtime | Stops the exact coroutine handle. |
| [`UnityEngine.WaitForSeconds`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/WaitForSeconds.html) | `public WaitForSeconds(float seconds)` | Runtime | Scaled-time coroutine yield instruction. |
| [`UnityEngine.WaitForSecondsRealtime`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/WaitForSecondsRealtime.html) | `public WaitForSecondsRealtime(float time)` | Runtime | Unscaled-time yield instruction. |
| [`UnityEngine.WaitUntil`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/WaitUntil.html) | `public WaitUntil(Func<bool> predicate)` | Runtime | Resumes after the predicate becomes true in its documented PlayerLoop position. |

## Official sources

- Class Mono Behaviour — https://docs.unity3d.com/6000.3/Documentation/Manual/class-MonoBehaviour.html
- Execution Order — https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html
- Class Mono Manager — https://docs.unity3d.com/6000.3/Documentation/Manual/class-MonoManager.html
- Coroutines — https://docs.unity3d.com/6000.3/Documentation/Manual/Coroutines.html
- Mono Behaviour.Awake — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Awake.html
- Mono Behaviour.On Enable — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnEnable.html
- Mono Behaviour.Start — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Start.html
- Mono Behaviour.Start Coroutine — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.StartCoroutine.html
- Mono Behaviour.Stop Coroutine — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.StopCoroutine.html
- Wait For Seconds — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/WaitForSeconds.html
- Wait For Seconds Realtime — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/WaitForSecondsRealtime.html
- Wait Until — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/WaitUntil.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
