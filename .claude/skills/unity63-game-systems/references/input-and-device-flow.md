# Input Systems, Actions, Devices, and Update Flow

## Scope

Distinguishes the Input System 1.20 package from the legacy Input Manager and defines action maps, devices, control schemes, update timing, UI integration, and rebinding boundaries.

Read this reference when the request involves:

- implementing keyboard, mouse, gamepad, touch, sensor, or device input
- choosing legacy Input Manager versus Input System 1.20
- connecting input to physics, UI, local multiplayer, or rebinding

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Input System package**
- **legacy Input Manager**
- **actions and bindings**
- **devices and update modes**
- **InputAction**
- **InputActionMap**
- **PlayerInput**
- **InputSystemUIInputModule**

## Verified Unity 6.3 facts

- Unity 6.3 documents two input approaches: the newer Input System package and the legacy built-in Input Manager. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Input.html))
- Input System 1.20 is a released package for Unity 6.3 and must be present in the project before its namespaces/types are assumed. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- The Input System models devices and controls, bindings, actions, action maps, control schemes, and interactions/processors. ([official source](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html))
- InputAction can be read by polling or through started/performed/canceled callbacks depending on action type and interaction. ([official source](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputAction.html))
- Actions and maps must be enabled to process input and should be disabled when their owning context is inactive. ([official source](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputActionMap.html))
- PlayerInput can coordinate an actions asset, notification behavior, control schemes, device pairing, and per-player setup. ([official source](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.PlayerInput.html))
- InputSystemUIInputModule connects Input System actions to Unity UI event processing. ([official source](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html))
- Legacy `UnityEngine.Input` polling depends on configured axes/buttons and remains a separate API with different capabilities and project settings. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/InputLegacy.html))
- Input System update processing can be dynamic, fixed, or manual; event timing must be aligned with simulation consumers. ([official source](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Model gameplay intent as actions such as Move, Submit, or Interact rather than hard-coding physical controls into domain behavior.
- **Interpretation:** Enable only the action maps valid for the current interaction state. A state transition should atomically disable the old map and enable the new map.
- **Interpretation:** Buffer button edges or latest intent from input processing to fixed simulation; do not depend on FixedUpdate seeing every device event directly.
- **Interpretation:** Keep rebinding, persistence of overrides, and device presentation in an input adapter layer. Gameplay consumes normalized action values/events.
- **Interpretation:** For local multiplayer, make player/device ownership explicit through PlayerInput/manager configuration and do not rely on global action singletons.
- **Interpretation:** Choose one primary input backend per feature. Parallel legacy/new reads can double-trigger behavior and complicate testing.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| New project or advanced device/rebinding/local multiplayer needs | Input System 1.20 | Released package with action/device model. |
| Maintaining a legacy project with stable mappings | Legacy Input Manager until planned migration | Avoid partial dual-stack behavior. |
| Physics-driven movement | Read/buffer input then consume in FixedUpdate | Preserves short input edges and fixed simulation timing. |
| UI navigation with Input System | InputSystemUIInputModule + dedicated UI actions | Aligns EventSystem input with the selected package. |

## Risks and review checks

- Do not assume Input System types exist when the package is absent.
- Do not leave multiple action maps enabled when their actions conflict.
- Do not read transient `WasPressedThisFrame`-style edges only in a fixed step without understanding the package update mode.
- Do not bind gameplay code directly to keyboard key names when remapping or alternate devices are requirements.
- Do not mix PlayerInput callback notification modes accidentally; each has different reflection/message/event coupling.
- Do not serialize binding overrides without validating asset/action IDs and migration behavior.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.InputSystem.InputAction.Enable`](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputAction.Enable.html) | `public void Enable()` | Package runtime | Enables action monitoring and phase processing. |
| [`UnityEngine.InputSystem.InputAction.Disable`](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputAction.Disable.html) | `public void Disable()` | Package runtime | Disables an action and cancels it if in progress. |
| [`UnityEngine.InputSystem.InputAction.ReadValue<TValue>`](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputAction.ReadValue.html) | `public TValue ReadValue<TValue>() where TValue : struct` | Package runtime | Reads the current processed action value of TValue. |
| [`UnityEngine.InputSystem.InputAction.performed`](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputAction.performed.html) | `public event Action<CallbackContext> performed` | Package runtime | Action callback at performed phase. |
| [`UnityEngine.InputSystem.PlayerInput`](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.PlayerInput.html) | `public class PlayerInput : MonoBehaviour` | Package runtime | Actions/device pairing and notification component. |
| [`UnityEngine.Input.GetAxis`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Input.GetAxis.html) | `public static float GetAxis(string axisName)` | Legacy runtime | Legacy virtual-axis polling. |

### Package prerequisites

- `com.unity.inputsystem@1.20` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Input — https://docs.unity3d.com/6000.3/Documentation/Manual/Input.html
- Input Legacy — https://docs.unity3d.com/6000.3/Documentation/Manual/InputLegacy.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Com.Unity.Inputsystem@1.20 — https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html
- Unity Engine.Input System.Input Action — https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputAction.html
- Unity Engine.Input System.Input Action Map — https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputActionMap.html
- Unity Engine.Input System.Player Input — https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.PlayerInput.html
- Unity Engine.Input System.Ui.Input System Uiinput Module — https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html
- Unity Engine.Input System.Input Action.Enable — https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputAction.Enable.html
- Unity Engine.Input System.Input Action.Disable — https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputAction.Disable.html
- Unity Engine.Input System.Input Action.Read Value — https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputAction.ReadValue.html
- Unity Engine.Input System.Input Action.Performed — https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputAction.performed.html
- Input.Get Axis — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Input.GetAxis.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
