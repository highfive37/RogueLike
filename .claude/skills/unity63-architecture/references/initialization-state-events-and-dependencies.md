# Initialization, State, Events, and Dependency Coordination

## Scope

Combines Unity lifecycle guarantees with explicit initialization, state-machine, event, and dependency practices that avoid hidden callback ordering.

Read this reference when the request involves:

- designing bootstrap or feature initialization
- coordinating events and subscriptions
- building state machines or resolving lifecycle race conditions

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Script lifecycle**
- **execution order**
- **state ownership**
- **event subscription**
- **RuntimeInitializeOnLoadMethod**
- **UnityEvent**
- **SceneManager events**
- **CancellationToken**

## Verified Unity 6.3 facts

- Unity invokes documented lifecycle messages through the PlayerLoop, but ordering between the same callback on different objects is not generally a dependency contract unless execution order is configured. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- Awake establishes an object during loading, OnEnable runs when it becomes enabled and active, Start runs before the first frame update for enabled instances, and OnDisable/OnDestroy provide teardown phases. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- Script Execution Order and `DefaultExecutionOrder` can adjust relative MonoBehaviour event order but do not replace explicit data readiness protocols. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-MonoManager.html))
- RuntimeInitializeOnLoadMethod marks static methods for documented load phases independent of a scene component callback. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html))
- UnityEvent is a serializable event type with persistent Inspector-configured calls and runtime listeners. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Events.UnityEvent.html))
- Runtime listeners added with AddListener can be removed with RemoveListener; persistent listeners are managed differently. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Events.UnityEvent.RemoveListener.html))
- SceneManager exposes sceneLoaded, sceneUnloaded, and activeSceneChanged events for scene-level coordination. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.html))
- MonoBehaviour exposes a destruction cancellation token that is cancelled when the object is destroyed. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour-destroyCancellationToken.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Separate construction from readiness: local fields can be validated in Awake, active subscriptions belong in OnEnable/OnDisable, and cross-system readiness should use an explicit initializer or state transition.
- **Interpretation:** Give each state machine one owner and a closed set of transitions. Events announce completed facts; commands request changes through the owner.
- **Interpretation:** Pair every subscription with an unsubscription under the same lifetime. For enabled-only participation, use OnEnable/OnDisable; for object lifetime, use a deliberate create/dispose boundary.
- **Interpretation:** Use execution-order configuration only for narrow engine integration constraints. Hidden project-wide ordering becomes fragile as scenes and prefabs evolve.
- **Interpretation:** Make initialization idempotent or reject duplicate calls clearly, especially when additive scenes and domain reload settings can produce repeated entry paths.
- **Interpretation:** Propagate cancellation from the owning lifetime into asynchronous operations so late continuations cannot mutate destroyed or replaced state.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Local component invariant | Awake | Does not depend on another object being Start-ready. |
| Participation while enabled | OnEnable + OnDisable | Subscription lifetime follows effective activation. |
| Cross-feature startup sequence | Explicit bootstrap/initializer state | Readiness and failure are observable rather than callback-order dependent. |
| Inspector-authored UI response | UnityEvent where persistent wiring is useful | Retain clear ownership and account for persistent versus runtime listeners. |

## Risks and review checks

- Do not use Start order across unrelated objects as an undocumented service initialization protocol.
- Do not fire an “initialized” event before the owner has committed a fully valid state.
- Do not subscribe anonymous delegates that cannot later be removed under the intended lifetime.
- Do not let multiple objects write the same state directly; route transitions through one authority.
- Do not continue an async transition after a newer transition supersedes it; cancel or compare a generation/token.
- Do not hide initialization failure behind a null check that leaves the feature partially active.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.RuntimeInitializeOnLoadMethodAttribute`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html) | `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType loadType)]` | Runtime | Invokes a static method at a selected documented runtime load phase. |
| [`UnityEngine.Events.UnityEvent.AddListener`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Events.UnityEvent.AddListener.html) | `public void AddListener(UnityAction call)` | Runtime | Adds a non-persistent runtime listener. |
| [`UnityEngine.Events.UnityEvent.RemoveListener`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Events.UnityEvent.RemoveListener.html) | `public void RemoveListener(UnityAction call)` | Runtime | Removes a matching non-persistent listener. |
| [`UnityEngine.MonoBehaviour.destroyCancellationToken`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour-destroyCancellationToken.html) | `public CancellationToken destroyCancellationToken { get; }` | Runtime | Cancelled when the MonoBehaviour is destroyed. |
| [`UnityEngine.SceneManagement.SceneManager.sceneUnloaded`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager-sceneUnloaded.html) | `public static event UnityAction<Scene> sceneUnloaded` | Runtime | Notifies after scene unload. |

## Official sources

- Execution Order — https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html
- Class Mono Manager — https://docs.unity3d.com/6000.3/Documentation/Manual/class-MonoManager.html
- Runtime Initialize On Load Method Attribute — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html
- Default Execution Order — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/DefaultExecutionOrder.html
- Events.Unity Event — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Events.UnityEvent.html
- Events.Unity Event.Add Listener — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Events.UnityEvent.AddListener.html
- Events.Unity Event.Remove Listener — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Events.UnityEvent.RemoveListener.html
- Scene Management.Scene Manager — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.html
- Scene Management.Scene Manager Scene Unloaded — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager-sceneUnloaded.html
- Mono Behaviour Destroy Cancellation Token — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour-destroyCancellationToken.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
