# Update, FixedUpdate, LateUpdate, and Time Ownership

## Scope

Defines which loop owns input, simulation, physics interaction, presentation, and camera work, and how Unity time values should enter testable code.

Read this reference when the request involves:

- choosing Update, FixedUpdate, or LateUpdate
- mixing input and physics
- building pause, slow motion, timers, or deterministic simulation boundaries

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **PlayerLoop**
- **frame time**
- **fixed timestep**
- **physics synchronization**
- **Time**
- **MonoBehaviour.Update**
- **FixedUpdate**
- **LateUpdate**
- **Rigidbody movement**

## Verified Unity 6.3 facts

- Update is called once per rendered frame on enabled active MonoBehaviours, while FixedUpdate is scheduled according to the fixed timestep and can run zero, one, or multiple times per rendered frame. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- LateUpdate runs after Update callbacks and is commonly used for work that depends on completed frame motion such as camera following. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- `Time.deltaTime` reports frame-scaled elapsed time for frame callbacks; `Time.fixedDeltaTime` defines the fixed-step interval and is affected by time scale according to the documented behavior. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time.html))
- `Time.unscaledDeltaTime` and related unscaled values provide elapsed time independent of `timeScale` for systems such as pause menus. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time-unscaledDeltaTime.html))
- Physics simulation uses fixed timesteps by default and Unity documents performance/accuracy tradeoffs for the fixed timestep and maximum allowed timestep. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/physics-optimization.html))
- Rigidbody physics state should normally be changed through Rigidbody forces, velocities, or movement APIs rather than writing Transform every frame. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/PhysicsSection.html))
- Input System events and polling can be associated with dynamic, fixed, or manual update processing, so action timing must be aligned with the consumer. ([official source](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html))
- The PlayerLoop defines ordered engine subsystems and script update phases; custom loop changes affect engine-wide scheduling. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LowLevel.PlayerLoop.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Sample user intent in the input update domain, store an intent snapshot, and consume that snapshot in fixed simulation when driving Rigidbody physics.
- **Interpretation:** Advance visual-only interpolation, UI, and non-physics presentation in frame time; keep authoritative physics changes in the fixed domain.
- **Interpretation:** Inject elapsed time into plain C# state machines rather than reading static Time throughout domain code. Tests can then advance exact durations.
- **Interpretation:** Use LateUpdate for followers that need the final target pose after Update, but explicitly coordinate physics interpolation when the target moves in FixedUpdate.
- **Interpretation:** Treat pause as multiple policies: scaled simulation, unscaled UI, audio, network, and asynchronous operations may each need different behavior.
- **Interpretation:** Change fixed timestep only after measuring CPU cost, tunneling/accuracy, and visible interpolation across representative hardware.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Read controls and update UI | Update / Input System dynamic update | Matches rendered-frame responsiveness. |
| Apply forces or Rigidbody movement | FixedUpdate | Aligns commands with fixed physics simulation. |
| Follow a target after its frame motion | LateUpdate | Consumes the final frame pose. |
| Pause-independent menu animation | Unscaled time | Not halted by `Time.timeScale = 0`. |

## Risks and review checks

- Do not multiply Rigidbody force modes by delta time without checking the documented force-mode semantics.
- Do not read a transient button edge only in FixedUpdate; fixed steps can miss or duplicate rendered-frame edges without buffering.
- Do not move a dynamic Rigidbody by Transform each Update and expect stable collision response.
- Do not assume FixedUpdate executes exactly once per frame.
- Do not use one global timeScale as the complete pause architecture.
- Do not customize PlayerLoop ordering without documenting restoration, package interactions, and domain reload behavior.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.MonoBehaviour.Update`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Update.html) | `private void Update()` | Runtime | Rendered-frame callback. |
| [`UnityEngine.MonoBehaviour.FixedUpdate`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.FixedUpdate.html) | `private void FixedUpdate()` | Runtime | Fixed-timestep callback. |
| [`UnityEngine.MonoBehaviour.LateUpdate`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.LateUpdate.html) | `private void LateUpdate()` | Runtime | Runs after Update callbacks in the frame. |
| [`UnityEngine.Time.deltaTime`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time-deltaTime.html) | `public static float deltaTime { get; }` | Runtime | Scaled interval associated with the current frame or fixed step context. |
| [`UnityEngine.Time.fixedDeltaTime`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time-fixedDeltaTime.html) | `public static float fixedDeltaTime { get; set; }` | Runtime | Fixed physics/update interval in seconds. |
| [`UnityEngine.Time.unscaledDeltaTime`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time-unscaledDeltaTime.html) | `public static float unscaledDeltaTime { get; }` | Runtime | Frame interval unaffected by timeScale. |
| [`UnityEngine.Rigidbody.MovePosition`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.MovePosition.html) | `public void MovePosition(Vector3 position)` | Runtime | Moves a kinematic Rigidbody toward a position with interpolation behavior governed by Rigidbody settings. |

### Package prerequisites

- `com.unity.inputsystem@1.20` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Execution Order — https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html
- Physics Section — https://docs.unity3d.com/6000.3/Documentation/Manual/PhysicsSection.html
- Physics Optimization — https://docs.unity3d.com/6000.3/Documentation/Manual/physics-optimization.html
- Com.Unity.Inputsystem@1.20 — https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html
- Time — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time.html
- Time Delta Time — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time-deltaTime.html
- Time Fixed Delta Time — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time-fixedDeltaTime.html
- Time Unscaled Delta Time — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time-unscaledDeltaTime.html
- Mono Behaviour.Update — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Update.html
- Mono Behaviour.Fixed Update — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.FixedUpdate.html
- Mono Behaviour.Late Update — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.LateUpdate.html
- Rigidbody.Move Position — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.MovePosition.html
- Low Level.Player Loop — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LowLevel.PlayerLoop.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
