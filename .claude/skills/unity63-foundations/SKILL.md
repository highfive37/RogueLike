---
name: unity63-foundations
description: Use when a Unity 6.3 question concerns the Editor/project model, GameObjects and Components, Transforms, Scenes, Prefabs, assets/import, MonoBehaviour, ScriptableObject, serialization, lifecycle, or assembly boundaries.
---

# Unity 6.3 Foundations

## Knowledge scope

Core Unity object, project, scene, prefab, asset, serialization, lifecycle, and compilation concepts.

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

- [Editor, Project, Packages, and Version Context](references/editor-project-packages.md) — Explains the Unity 6.3 Editor-facing project model: how the Project window exposes project files, how assets and packages enter a project, how feature sets relate to packages, and how to keep every answer pinned to 6000.3. Load when: the Unity Editor, Project window, project files, package management, feature sets, or version identity; a question might accidentally mix Unity 6.3 with another Unity documentation version; a package API is proposed and its installation or compatibility must be checked.
- [GameObject, Component, Transform, and Hierarchy](references/gameobject-component-transform.md) — Defines Unity’s core scene object composition model and the exact distinctions among GameObject identity, Component behavior, Transform hierarchy, activation, lookup, and destruction. Load when: GameObject versus Component responsibilities; Transform parent/child behavior or local versus world space; component lookup, activation, creation, destruction, or composition.
- [Scenes, Additive Loading, and Hierarchy Boundaries](references/scenes-and-hierarchies.md) — Explains Scene assets, loaded Scene state, additive composition, root GameObjects, active Scene semantics, asynchronous loading, and responsibility boundaries for multi-scene projects. Load when: scene composition, level flow, bootstrap scenes, additive loading, or persistent systems; objects move between scenes or a request depends on active Scene semantics; a design must choose between one large Scene and multiple responsibility-oriented Scenes.
- [Prefabs, Instances, Variants, Overrides, and Runtime Cloning](references/prefabs-and-instances.md) — Defines prefab assets as reusable authored GameObject graphs and explains instance synchronization, nested prefabs, variants, overrides, runtime instantiation, and the boundary between authored template state and runtime instance state. Load when: prefab asset versus scene instance behavior; nested prefabs, variants, overrides, apply/revert, or runtime spawning; a reusable GameObject graph needs an ownership and configuration strategy.
- [Assets, Import Pipeline, Metadata, GUIDs, and Runtime Loading](references/assets-import-and-metadata.md) — Explains the distinction among source files, imported Unity objects, `.meta` identity, Asset Database state, sub-assets, import settings, and runtime-loading systems. Load when: asset import, GUIDs, `.meta` files, importer settings, sub-assets, or broken references; runtime asset loading must choose direct references, Resources, AssetBundles, or Addressables; Editor scripts create, move, query, or reimport assets.
- [Unity Object Model, ScriptableObject, and Serialization Fundamentals](references/object-model-scriptableobject-serialization.md) — Explains UnityEngine.Object native/managed identity, Unity null behavior, ScriptableObject assets, field serialization, managed references, unsupported shapes, and the distinction between authored data and mutable runtime state. Load when: UnityEngine.Object null behavior or destroyed-object checks; ScriptableObject usage, asset-backed configuration, or shared data; Inspector serialization, field eligibility, polymorphism, dictionaries, properties, records, or hot reload.
- [MonoBehaviour Lifecycle, PlayerLoop, and Script Execution Order](references/monobehaviour-lifecycle-execution-order.md) — Maps MonoBehaviour event functions to creation, activation, frame, physics, rendering, and destruction phases, including the limits of cross-object ordering and Unity 6.3 async continuation behavior. Load when: Awake, OnEnable, Start, Update, FixedUpdate, LateUpdate, OnDisable, or OnDestroy ordering; initialization races, event subscription lifetime, scene-load callbacks, or PlayerLoop timing; script execution order settings or DefaultExecutionOrder are being considered.
- [Assembly Definitions and Runtime/Editor Boundaries](references/assemblies-runtime-editor-boundaries.md) — Explains Unity script compilation units, assembly definition and reference files, dependency direction, auto-referencing, platform filters, define constraints, test assemblies, and the hard boundary between runtime and UnityEditor APIs. Load when: assembly definition files, compilation boundaries, cyclic dependencies, or compile times; runtime code accidentally references UnityEditor; platform-specific code, test assemblies, or optional package integration.

## Cross-skill boundary

- Use `unity63-foundations` for object/project/scene/prefab/asset fundamentals.
- Use `unity63-architecture` for ownership, dependency, lifetime, state, and structural choices.
- Use `unity63-csharp` for implementation language, serialization, async, memory, backend, and refactoring details.
- Use `unity63-game-systems` for a concrete Unity production system.
- Use `unity63-performance-quality` for measurement, testing, debugging, builds, and review gates.
