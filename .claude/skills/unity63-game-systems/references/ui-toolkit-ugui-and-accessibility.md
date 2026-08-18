# UI Toolkit, Unity UI, IMGUI, and Accessibility

## Scope

Compares Unity’s UI systems and integrates layout, events, focus, data binding, rendering, native screen readers, and accessible interaction requirements in Unity 6.3.

Read this reference when the request involves:

- choosing UI Toolkit, Unity UI (uGUI), or IMGUI
- building runtime/editor UI, focus/navigation, localization, or responsive layout
- implementing screen reader and accessibility support in Unity 6.3

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **UI Toolkit**
- **Unity UI (uGUI)**
- **IMGUI**
- **Accessibility module**
- **UIDocument**
- **VisualElement**
- **Canvas**
- **EventSystem**
- **AccessibilityHierarchy**

## Verified Unity 6.3 facts

- Unity documents three UI systems—UI Toolkit, Unity UI (uGUI), and IMGUI—with different supported use cases and workflows. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/UI-system-compare.html))
- UI Toolkit uses UXML, USS, VisualElement trees, retained-mode layout/rendering, events, focus, and data binding for Editor and runtime UI. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/UIToolkits.html))
- Unity UI uses GameObjects and RectTransforms under a Canvas with Graphic, layout, selectable, EventSystem, and raycaster components. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/UI-system-compare.html))
- IMGUI is immediate-mode and remains primarily associated with custom Editor tooling and OnGUI-based interfaces. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/UI-system-compare.html))
- Unity 6.3 adds UI Toolkit capabilities including SVG background image support, filters, and Shader Graph integration documented in the release changes. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html))
- Unity 6.3 adds native screen reader support for desktop platforms in addition to the Accessibility module’s supported platform behavior documented for the target release. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html))
- An AccessibilityHierarchy and AccessibilityNode model exposes content to supported native screen readers through AssistiveSupport. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/accessibility/module-intro.html))
- Accessibility changes require appropriate hierarchy activation and notifications so assistive technology receives layout or screen updates. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Accessibility.AssistiveSupport.html))
- Localization 1.5 is a released Unity 6.3 package and must be installed before package-specific localized string/table APIs are assumed. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Choose the UI system per screen and platform constraints, then avoid mixing systems within one interaction surface unless there is a clear integration boundary.
- **Interpretation:** Separate view state from domain state. UI callbacks issue commands; a presenter/view-model translates domain state into visual and accessibility properties.
- **Interpretation:** Build keyboard/gamepad focus order and screen-reader hierarchy together. Visual order alone is not an accessibility contract.
- **Interpretation:** Every interactive control needs a meaningful label, role/state, focus behavior, target size, contrast, and non-pointer path appropriate to the supported platforms.
- **Interpretation:** Virtualize large lists, minimize hierarchy/layout invalidation, and batch state changes; both UI Toolkit and uGUI can incur rebuild costs.
- **Interpretation:** Treat localization as layout input: text expansion, font/fallback, bidirectionality, and dynamic updates must be tested with the exact package installed.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| New data-heavy runtime or Editor UI | UI Toolkit after feature/platform verification | Retained-mode styling, events, binding, and shared Editor/runtime concepts. |
| Existing GameObject/Canvas UI or world-space UI requirement | Unity UI (uGUI) | Mature component/EventSystem workflow. |
| Small custom Editor diagnostic control | IMGUI where appropriate | Immediate-mode simplicity can fit tool-only UI. |
| Native screen reader support | AccessibilityHierarchy + platform testing | Visual UI is not automatically a complete accessibility tree. |

## Risks and review checks

- Do not assume UI Toolkit and uGUI components/events are interchangeable.
- Do not rebuild an entire visual tree or Canvas hierarchy for a small state change without profiling.
- Do not make color the only carrier of status or selection.
- Do not expose decorative nodes or omit actionable controls from the accessibility hierarchy.
- Do not send accessibility notifications for every insignificant mutation; send the semantically correct layout/screen/focus change.
- Do not assume desktop, mobile, console, and XR input/focus behavior is identical.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.UIElements.UIDocument`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.UIDocument.html) | `public sealed class UIDocument : MonoBehaviour` | Runtime | Connects a runtime GameObject to a VisualTreeAsset/PanelSettings UI Toolkit document. |
| [`UnityEngine.UIElements.VisualElement`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.VisualElement.html) | `public class VisualElement` | Runtime | Base retained-mode UI Toolkit element with hierarchy, style, layout, events, and focus. |
| [`UnityEngine.Canvas`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Canvas.html) | `public sealed class Canvas : Behaviour` | Runtime | Root render space for Unity UI graphics. |
| [`UnityEngine.EventSystems.EventSystem`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EventSystems.EventSystem.html) | `public class EventSystem : UIBehaviour` | Runtime | Coordinates selected object, input modules, and UI event routing. |
| [`UnityEngine.Accessibility.AccessibilityHierarchy`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Accessibility.AccessibilityHierarchy.html) | `public class AccessibilityHierarchy` | Runtime | Screen-reader hierarchy model. |
| [`UnityEngine.Accessibility.AssistiveSupport.activeHierarchy`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Accessibility.AssistiveSupport-activeHierarchy.html) | `public static AccessibilityHierarchy activeHierarchy { get; set; }` | Runtime | Sets the hierarchy exposed to supported assistive technology. |
| [`UnityEngine.Accessibility.AccessibilityNode`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Accessibility.AccessibilityNode.html) | `public class AccessibilityNode` | Runtime | Accessible element with label, role/state, frame, and actions. |

### Package prerequisites

- `com.unity.localization@1.5` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Uitoolkits — https://docs.unity3d.com/6000.3/Documentation/Manual/UIToolkits.html
- Ui System Compare — https://docs.unity3d.com/6000.3/Documentation/Manual/UI-system-compare.html
- Module Intro — https://docs.unity3d.com/6000.3/Documentation/Manual/accessibility/module-intro.html
- Com.Unity.Modules.Accessibility — https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.modules.accessibility.html
- Whats New Unity63 — https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Uielements.Uidocument — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.UIDocument.html
- Uielements.Visual Element — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.VisualElement.html
- Canvas — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Canvas.html
- Event Systems.Event System — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EventSystems.EventSystem.html
- Accessibility.Accessibility Hierarchy — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Accessibility.AccessibilityHierarchy.html
- Accessibility.Assistive Support — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Accessibility.AssistiveSupport.html
- Accessibility.Assistive Support Active Hierarchy — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Accessibility.AssistiveSupport-activeHierarchy.html
- Accessibility.Accessibility Node — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Accessibility.AccessibilityNode.html
- Com.Unity.Localization@1.5 — https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
