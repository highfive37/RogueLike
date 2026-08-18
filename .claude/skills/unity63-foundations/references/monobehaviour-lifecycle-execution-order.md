# MonoBehaviour Lifecycle, PlayerLoop, and Script Execution Order

## Scope

Maps MonoBehaviour event functions to creation, activation, frame, physics, rendering, and destruction phases, including the limits of cross-object ordering and Unity 6.3 async continuation behavior.

Read this reference when the request involves:

- Awake, OnEnable, Start, Update, FixedUpdate, LateUpdate, OnDisable, or OnDestroy ordering
- initialization races, event subscription lifetime, scene-load callbacks, or PlayerLoop timing
- script execution order settings or DefaultExecutionOrder are being considered

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **MonoBehaviour**
- **Event function execution order**
- **Script Execution Order**
- **PlayerLoop**
- **MonoBehaviour callbacks**
- **PlayerLoop**
- **RuntimeInitializeOnLoadMethod**
- **DefaultExecutionOrder**

## Verified Unity 6.3 facts

- MonoBehaviour is the framework for attaching script components to GameObjects and receiving Unity event functions such as Start and Update. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-MonoBehaviour.html))
- Unity invokes event functions through the PlayerLoop; the execution-order Manual describes the relative phases rather than guaranteeing arbitrary same-callback ordering between different objects. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- `Awake` is used when the script instance is being loaded, and `OnEnable` is called when the object becomes enabled and active. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Awake.html))
- `Start` is called before the first frame update when the script is enabled, after all scene-object `Awake` calls have completed. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Start.html))
- The sceneLoaded event occurs after `OnEnable` and before `Start` for objects from the newly loaded Scene. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- `FixedUpdate` can run zero, one, or multiple times before a rendered frame because it follows a fixed timestep rather than display frames. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- `Update` runs once per rendered frame while the enabled component is active, and `LateUpdate` follows Update-phase work. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Update.html))
- `OnDisable` is called when the behaviour becomes disabled or inactive and also during script reload; `OnDestroy` is called when the MonoBehaviour is destroyed after previously being active. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnDisable.html))
- Coroutine resumptions and Awaitable/Task continuations are integrated into documented PlayerLoop points; their exact resumption phase depends on what they await. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- Script Execution Order settings can order selected MonoBehaviour event functions by script class, and `DefaultExecutionOrder` can provide a default relative order in code. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-MonoManager.html))
- Runtime initialization methods marked with RuntimeInitializeOnLoadMethod can run at specified load phases without requiring a scene component. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html))

### Processing or lifecycle flow

1. Native/managed objects and serialized fields are loaded.
2. `Awake` establishes local invariants.
3. `OnEnable` establishes active subscriptions and resources.
4. Scene load notification can run before Start.
5. `Start` performs enabled one-time startup.
6. Fixed, frame, late, rendering, and coroutine/await continuation phases execute through the PlayerLoop.
7. `OnDisable` suspends active participation.
8. `OnDestroy` releases destruction-owned resources.

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Use `Awake` for local invariant setup that does not depend on another object having completed `Start`; use an explicit bootstrap protocol for cross-object initialization.
- **Interpretation:** Use `OnEnable`/`OnDisable` as a paired subscription boundary when an inactive component must not receive events.
- **Interpretation:** Keep `Update` thin: gather frame input, advance lightweight frame logic, or delegate to scheduled systems. Move deterministic physics interaction to fixed-step work.
- **Interpretation:** Treat global Script Execution Order as a last-mile compatibility mechanism, not a substitute for explicit dependencies and readiness.
- **Interpretation:** Make shutdown idempotent because disable, scene unload, application quit, domain reload, and destruction paths can overlap in development workflows.
- **Interpretation:** When async work captures a MonoBehaviour, validate cancellation and Unity object lifetime again after every suspension point.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Local field/reference setup | `Awake` | Runs during loading and should not require another object’s Start. |
| Subscribe while active | `OnEnable` / `OnDisable` pair | Activation naturally owns the subscription. |
| One-time dependency-ready start | Explicit initializer or `Start` with clear prerequisites | Avoid relying on unspecified same-callback ordering. |
| Physics force/motion interaction | `FixedUpdate` | Aligns with fixed simulation steps. |
| Camera follow after motion | `LateUpdate` | Runs after Update work in the frame. |

## Risks and review checks

- Do not assume `Awake` order across different GameObjects unless explicitly configured.
- Do not subscribe in `Start` and forget to unsubscribe when the object is disabled.
- Do not perform high-allocation searches or string formatting every Update without measurement.
- Do not run physics-affecting logic solely in Update when fixed-step consistency is required.
- Do not put broad project ordering into dozens of Script Execution Order entries; it creates hidden global coupling.
- Do not resume asynchronous work against a destroyed component without a cancellation or lifetime check.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.MonoBehaviour.Awake`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Awake.html) | `private void Awake()` | Runtime | Unity message; called when the script instance is loaded. |
| [`UnityEngine.MonoBehaviour.OnEnable`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnEnable.html) | `private void OnEnable()` | Runtime | Unity message; called when enabled and active. |
| [`UnityEngine.MonoBehaviour.Start`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Start.html) | `private void Start(); private IEnumerator Start()` | Runtime | Called before the first Update while enabled; IEnumerator form acts as a coroutine. |
| [`UnityEngine.MonoBehaviour.Update`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Update.html) | `private void Update()` | Runtime | Per-frame callback for enabled active behaviours. |
| [`UnityEngine.MonoBehaviour.FixedUpdate`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.FixedUpdate.html) | `private void FixedUpdate()` | Runtime | Fixed-timestep callback associated with physics updates. |
| [`UnityEngine.MonoBehaviour.OnDisable`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnDisable.html) | `private void OnDisable()` | Runtime | Called when the behaviour becomes disabled or inactive. |
| [`UnityEngine.MonoBehaviour.OnDestroy`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnDestroy.html) | `private void OnDestroy()` | Runtime | Called when a previously active MonoBehaviour is destroyed. |
| [`UnityEngine.RuntimeInitializeOnLoadMethodAttribute`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html) | `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType loadType)]` | Runtime | Marks a static method for a documented runtime-load phase. |
| [`UnityEngine.DefaultExecutionOrder`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/DefaultExecutionOrder.html) | `[DefaultExecutionOrder(int order)]` | Runtime | Supplies a default execution-order offset for a MonoBehaviour class. |

## Official sources

- Class Mono Behaviour — https://docs.unity3d.com/6000.3/Documentation/Manual/class-MonoBehaviour.html
- Execution Order — https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html
- Class Mono Manager — https://docs.unity3d.com/6000.3/Documentation/Manual/class-MonoManager.html
- Mono Behaviour — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.html
- Mono Behaviour.Awake — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Awake.html
- Mono Behaviour.On Enable — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnEnable.html
- Mono Behaviour.Start — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Start.html
- Mono Behaviour.Update — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Update.html
- Mono Behaviour.Fixed Update — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.FixedUpdate.html
- Mono Behaviour.On Disable — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnDisable.html
- Mono Behaviour.On Destroy — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnDestroy.html
- Runtime Initialize On Load Method Attribute — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html
- Default Execution Order — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/DefaultExecutionOrder.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
