# Component Composition and Plain C# Boundaries

## Scope

Provides criteria for assigning scene identity, Unity callbacks, data, and domain behavior among GameObjects, MonoBehaviours, ScriptableObjects, and ordinary C# objects.

Read this reference when the request involves:

- deciding whether a class should be a MonoBehaviour, ScriptableObject, component, or plain C# type
- reducing large MonoBehaviours or making gameplay logic testable
- expressing required components and runtime composition

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **GameObject composition**
- **MonoBehaviour**
- **ScriptableObject**
- **testable domain logic**
- **GameObject.AddComponent**
- **Component lookup**
- **RequireComponent**
- **CreateInstance**

## Verified Unity 6.3 facts

- A GameObject obtains behavior from attached Components and every Component has an owning GameObject. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Components.html))
- MonoBehaviour is the base class from which Unity scripts commonly derive to attach behavior to GameObjects and receive Unity messages. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-MonoBehaviour.html))
- MonoBehaviour instances must not be created with the C# `new` operator; they are created as components on GameObjects. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.html))
- ScriptableObject is a UnityEngine.Object used as an asset-backed or runtime-created data object and is not attached to a GameObject. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html))
- Plain serializable classes can be serialized inline as fields of a Unity object when they satisfy Unity serialization rules. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))
- `RequireComponent` causes required component types to be added when the annotated component is added, but it does not repair every previously existing instance retroactively. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RequireComponent.html))
- Component lookup APIs operate on the current object, parents, or children and have distinct inclusion and multiplicity behavior. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Component.html))
- Unity Test Framework supports Edit mode and Play mode tests, allowing pure code and Unity-integrated behavior to be tested at different boundaries. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Keep MonoBehaviour as the Unity-facing adapter: serialized references, lifecycle messages, scene registration, and translation between engine events and domain operations.
- **Interpretation:** Move deterministic rules, calculations, state transitions, and validation into plain C# objects when they do not require Unity object identity or messages.
- **Interpretation:** Use ScriptableObject for authored shared definitions or assets, not as a default global mutable runtime state container.
- **Interpretation:** Represent required local engine collaborators with serialized component references or `RequireComponent`; represent wider dependencies through explicit constructors or initialization methods in plain C# collaborators.
- **Interpretation:** Compose behavior from several narrow components when responsibilities have different lifetimes or can be reused independently; do not split only to reduce line count.
- **Interpretation:** A domain object can receive immutable values or narrow interfaces extracted from Unity state, making it testable without fabricating scene hierarchies.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Needs Transform, scene identity, Unity messages, or Inspector wiring | MonoBehaviour component | Unity owns construction and lifecycle. |
| Authored reusable definition shared by many objects | ScriptableObject asset | Stable project asset identity and Inspector authoring are valuable. |
| Deterministic rule or state machine without engine identity | Plain C# class/struct | Construction, lifetime, and testing remain explicit. |
| Small immutable value copied into an owner | Serializable value type/class | No independent asset identity is required. |

## Risks and review checks

- Do not call `new SomeMonoBehaviour()` or `new ScriptableObjectSubclass()`; use component creation or `ScriptableObject.CreateInstance` as documented.
- Do not make a plain C# object reach into `Find` APIs or global singletons; pass its dependencies and inputs.
- Do not turn every configuration into a ScriptableObject when inline serialized values are private to one owner.
- Do not assume a serialized interface field works like a concrete UnityEngine.Object field; verify supported serialization strategy.
- Do not hide missing dependencies by lazily searching every frame.
- Do not unit-test engine callbacks by manually invoking private messages as the primary strategy; test extracted behavior and use PlayMode tests for integration.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.GameObject.AddComponent<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.AddComponent.html) | `public T AddComponent<T>() where T : Component` | Runtime | Creates a component under Unity ownership. |
| [`UnityEngine.GameObject.TryGetComponent<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.TryGetComponent.html) | `public bool TryGetComponent<T>(out T component)` | Runtime | Queries a local component with explicit success. |
| [`UnityEngine.RequireComponent`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RequireComponent.html) | `[RequireComponent(typeof(T))]` | Runtime | Declares a component prerequisite at add time. |
| [`UnityEngine.ScriptableObject.CreateInstance<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ScriptableObject.CreateInstance.html) | `public static T CreateInstance<T>() where T : ScriptableObject` | Runtime | Creates a non-persistent ScriptableObject instance unless saved as an asset by Editor code. |
| [`UnityEngine.SerializeReference`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeReference.html) | `[SerializeReference]` | Runtime | Requests managed-reference serialization for supported non-UnityEngine.Object reference graphs. |

## Official sources

- Gameobject Fundamentals — https://docs.unity3d.com/6000.3/Documentation/Manual/gameobject-fundamentals.html
- Components — https://docs.unity3d.com/6000.3/Documentation/Manual/Components.html
- Class Mono Behaviour — https://docs.unity3d.com/6000.3/Documentation/Manual/class-MonoBehaviour.html
- Class Scriptable Object — https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html
- Script Serialization Rules — https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html
- Test Framework Introduction — https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html
- Mono Behaviour — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.html
- Game Object.Add Component — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.AddComponent.html
- Game Object.Try Get Component — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.TryGetComponent.html
- Require Component — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RequireComponent.html
- Scriptable Object.Create Instance — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ScriptableObject.CreateInstance.html
- Serialize Reference — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeReference.html
- Component — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Component.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
