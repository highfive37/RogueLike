# Prefabs, Instances, Variants, Overrides, and Runtime Cloning

## Scope

Defines prefab assets as reusable authored GameObject graphs and explains instance synchronization, nested prefabs, variants, overrides, runtime instantiation, and the boundary between authored template state and runtime instance state.

Read this reference when the request involves:

- prefab asset versus scene instance behavior
- nested prefabs, variants, overrides, apply/revert, or runtime spawning
- a reusable GameObject graph needs an ownership and configuration strategy

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Prefabs**
- **Prefab instances**
- **Nested prefabs**
- **Prefab variants**
- **Overrides**
- **Runtime instantiation**
- **UnityEngine.Object.Instantiate**
- **UnityEditor.PrefabUtility**

## Verified Unity 6.3 facts

- A prefab asset stores a GameObject with its components and properties as a reusable template. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/Prefabs.html))
- Prefab instances remain linked to their prefab asset so asset changes can propagate to instances. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/prefabs-introduction.html))
- A prefab can be reused in one Scene, across Scenes, or instantiated later at runtime. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/prefabs-introduction.html))
- Nested prefabs preserve the nested instance connection to its own source prefab while participating in the outer prefab graph. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/NestedPrefabs.html))
- A prefab variant inherits from a base prefab and stores a meaningful collection of overrides as a reusable variation. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/PrefabVariants.html))
- Instance overrides create differences on a particular instance while retaining the link to the same prefab asset. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/prefabs-override.html))
- Runtime prefab creation uses `Object.Instantiate`; the clone receives the hierarchy and serialized component configuration of the original. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Instantiate.html))
- Generic `Instantiate<T>` returns the same Unity object type as the original, which allows a component reference on a prefab to produce a typed component reference on the clone. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Instantiate.html))
- A clone can be created with position, rotation, parent, or world-space options through different `Instantiate` overloads. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Instantiate.html))
- Prefab editing and apply/revert operations are Editor workflows exposed through Editor-only `PrefabUtility` APIs, not runtime Player APIs. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PrefabUtility.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Use a prefab for authored composition: required components, child structure, visual references, and default configuration. Keep mutable save/session state outside the prefab asset.
- **Interpretation:** Use variants for named reusable categories with stable inherited differences; use per-instance overrides for exceptional placement-specific changes.
- **Interpretation:** Keep nested prefab boundaries aligned with reusable subassemblies. Excessive nesting makes override provenance and apply operations harder to review.
- **Interpretation:** When spawning, return or cache the narrow component interface needed by callers rather than exposing the entire GameObject graph.
- **Interpretation:** If objects are spawned frequently, combine prefab-authored setup with a pool whose reset contract restores all mutable instance state.
- **Interpretation:** Review overrides in source control and the Inspector; accidental overrides can silently prevent base prefab changes from reaching an instance.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Same authored graph appears repeatedly | Prefab asset | Centralizes composition and defaults. |
| Reusable named specialization of a base | Prefab variant | Inheritance of authored defaults is intentional and reviewable. |
| One placement needs a small difference | Instance override | Avoid creating a variant for a one-off change. |
| High-frequency runtime creation | Prefab + object pool | Preserves authoring workflow while controlling allocation and activation costs. |

## Risks and review checks

- Do not store live runtime progress back into a prefab asset or shared ScriptableObject configuration.
- Do not apply overrides to the source asset without checking which nested source receives the change.
- Do not use long variant chains when composition or separate data assets express the difference more clearly.
- Do not assume `Instantiate` calls lifecycle callbacks at a point convenient to the caller; dependencies should already be valid or initialization should be explicit.
- Do not pool an object without defining how every mutable component, coroutine, subscription, particle system, and physics body is reset.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Object.Instantiate<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Instantiate.html) | `public static T Instantiate<T>(T original) where T : Object` | Runtime | Clones the original and returns the same Unity object type. |
| [`UnityEngine.Object.Instantiate<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Instantiate.html) | `public static T Instantiate<T>(T original, Transform parent, bool instantiateInWorldSpace) where T : Object` | Runtime | Parents the clone and selects whether to preserve world-space transform. |
| [`UnityEngine.Object.Instantiate<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Instantiate.html) | `public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object` | Runtime | Creates a positioned and parented clone. |
| [`UnityEngine.Object.InstantiateAsync<T>`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.InstantiateAsync.html) | `public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count = 1, Transform parent = null, ReadOnlySpan<Vector3> positions = default, ReadOnlySpan<Quaternion> rotations = default)` | Runtime | Asynchronous instantiation surface for GameObject-related Unity objects; verify overload requirements before adoption. |
| [`UnityEditor.PrefabUtility`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PrefabUtility.html) | `public static class PrefabUtility` | Editor | Editor-only prefab creation, connection, override, apply, revert, and save utilities. |
| [`UnityEditor.PrefabUtility.InstantiatePrefab`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PrefabUtility.InstantiatePrefab.html) | `public static Object InstantiatePrefab(Object assetComponentOrGameObject)` | Editor | Instantiates while preserving the prefab connection in Editor workflows. |

## Official sources

- Prefabs — https://docs.unity3d.com/6000.3/Documentation/Manual/Prefabs.html
- Prefabs Introduction — https://docs.unity3d.com/6000.3/Documentation/Manual/prefabs-introduction.html
- Nested Prefabs — https://docs.unity3d.com/6000.3/Documentation/Manual/NestedPrefabs.html
- Prefab Variants — https://docs.unity3d.com/6000.3/Documentation/Manual/PrefabVariants.html
- Prefabs Override — https://docs.unity3d.com/6000.3/Documentation/Manual/prefabs-override.html
- Instantiating Prefabs — https://docs.unity3d.com/6000.3/Documentation/Manual/instantiating-prefabs.html
- Object.Instantiate — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Instantiate.html
- Object.Instantiate Async — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.InstantiateAsync.html
- Prefab Utility — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PrefabUtility.html
- Prefab Utility.Instantiate Prefab — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PrefabUtility.InstantiatePrefab.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
