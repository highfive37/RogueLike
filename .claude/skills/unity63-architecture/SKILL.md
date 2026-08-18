---
name: unity63-architecture
description: Use when designing or reviewing a maintainable Unity 6.3 project, including modules, dependencies, data ownership, scene and object lifetime, initialization, state, events, update timing, async flow, persistence, platform isolation, or testability.
---

# Unity 6.3 Architecture

## Knowledge scope

Fact-grounded architecture choices and trade-offs for maintainable Unity projects.

This skill is a knowledge reference. It does not automate or control the Unity Editor.

## Target-version policy

- Treat **Unity 6.3 LTS / documentation 6000.3 / supported range 6000.3.x** as the only built-in engine target.
- Use only the exact 6000.3 Manual and Scripting API URLs recorded in the selected reference.
- Use a package API only when the reference records an exact Unity 6.3 released or core package version and the current project confirms that the dependency is available and configured.
- Do not import facts from another Unity version, an unversioned page, memory, a blog, forum, sample repository, or third-party framework.
- When the target official documentation does not establish a behavior or API, state that it is unverified and exclude it rather than guessing.

## Evidence and answer rules

1. Load only the reference or references that match the request.
2. Separate **verified Unity 6.3 official facts** from **design or implementation interpretation derived from those facts**.
3. Preserve runtime versus Editor-only, built-in versus package, and available/configured versus merely released-or-core distinctions.
4. For code, verify namespace, type, member, signature/overload, lifetime, thread, platform, backend, and package prerequisites from the recorded official source.
5. Answer in the user’s language and include the exact official URLs supporting material claims.

## Reference selection

- [Project Modules and Dependency Boundaries](references/project-modules-and-dependency-boundaries.md) — Turns Unity 6.3 project, package, folder, namespace, and assembly behavior into decision criteria for modular projects with explicit dependency direction. Load when: designing project folders, namespaces, asmdefs, feature modules, or package boundaries; reducing compilation scope or preventing cyclic dependencies; separating runtime, editor, tests, platform adapters, or optional package integrations.
- [Component Composition and Plain C# Boundaries](references/component-composition-and-plain-csharp.md) — Provides criteria for assigning scene identity, Unity callbacks, data, and domain behavior among GameObjects, MonoBehaviours, ScriptableObjects, and ordinary C# objects. Load when: deciding whether a class should be a MonoBehaviour, ScriptableObject, component, or plain C# type; reducing large MonoBehaviours or making gameplay logic testable; expressing required components and runtime composition.
- [Data Ownership: Authored, Runtime, and Saved Data](references/data-ownership-and-scriptableobject.md) — Distinguishes project-authored assets, scene/prefab serialized state, mutable runtime state, and persistent save data, with focused use of ScriptableObject and Unity serialization. Load when: choosing where configuration, runtime state, or save data should live; using ScriptableObject without accidental shared-state coupling; designing cloning, reset, serialization, or persistence boundaries.
- [Scenes, Prefabs, and Object Lifetime Boundaries](references/scenes-prefabs-and-object-lifetimes.md) — Defines responsibility and lifetime choices for scenes, additive scene composition, prefab assets and instances, persistent objects, and asynchronous scene transitions. Load when: splitting gameplay across scenes or loading additively; choosing between a prefab, scene object, bootstrap object, or persistent service; designing scene transitions and teardown without duplicates or leaked references.
- [Initialization, State, Events, and Dependency Coordination](references/initialization-state-events-and-dependencies.md) — Combines Unity lifecycle guarantees with explicit initialization, state-machine, event, and dependency practices that avoid hidden callback ordering. Load when: designing bootstrap or feature initialization; coordinating events and subscriptions; building state machines or resolving lifecycle race conditions.
- [Update, FixedUpdate, LateUpdate, and Time Ownership](references/update-fixedupdate-and-time.md) — Defines which loop owns input, simulation, physics interaction, presentation, and camera work, and how Unity time values should enter testable code. Load when: choosing Update, FixedUpdate, or LateUpdate; mixing input and physics; building pause, slow motion, timers, or deterministic simulation boundaries.
- [Coroutines, Awaitable, Async Flow, and Cancellation](references/coroutines-awaitable-and-cancellation.md) — Explains Unity 6.3 coroutine and Awaitable behavior, continuation threads, ownership, cancellation, and when to use each asynchronous flow. Load when: choosing a coroutine, Awaitable, Task, or asynchronous scene/asset operation; cancelling work when a component, scene, or application exits; avoiding main-thread violations or pooled Awaitable misuse.
- [Persistence, Platform Isolation, and Testable Architecture](references/persistence-platform-isolation-and-testability.md) — Defines ports and adapters for save/load, filesystem, platform APIs, build variants, and tests without coupling domain rules to static Unity state. Load when: architecting save/load or configuration persistence; supporting multiple platforms or scripting backends; making code testable in EditMode and validating Player-only behavior.

## Cross-skill boundary

- Use `unity63-foundations` for object/project/scene/prefab/asset fundamentals.
- Use `unity63-architecture` for ownership, dependency, lifetime, state, and structural choices.
- Use `unity63-csharp` for implementation language, serialization, async, memory, backend, and refactoring details.
- Use `unity63-game-systems` for a concrete Unity production system.
- Use `unity63-performance-quality` for measurement, testing, debugging, builds, and review gates.
