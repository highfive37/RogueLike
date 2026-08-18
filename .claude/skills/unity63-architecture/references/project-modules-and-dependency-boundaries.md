# Project Modules and Dependency Boundaries

## Scope

Turns Unity 6.3 project, package, folder, namespace, and assembly behavior into decision criteria for modular projects with explicit dependency direction.

Read this reference when the request involves:

- designing project folders, namespaces, asmdefs, feature modules, or package boundaries
- reducing compilation scope or preventing cyclic dependencies
- separating runtime, editor, tests, platform adapters, or optional package integrations

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Project organization**
- **Packages**
- **Assembly Definitions**
- **Runtime and Editor separation**
- **Assembly Definition assets**
- **CompilationPipeline**
- **conditional compilation**

## Verified Unity 6.3 facts

- Unity treats Assets as authored project content and Packages as package dependencies; package content can be visible in the Project window but has different ownership and update semantics. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/ProjectView.html))
- Assembly Definition assets create managed compilation units and nested definitions establish the assembly that owns scripts under a folder. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-intro.html))
- Assembly Definition References can place multiple folders in one assembly without creating a second assembly identity. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html))
- Custom assembly dependencies are explicit and circular assembly references are not supported. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-intro.html))
- Auto Referenced, platform filters, define constraints, version defines, unsafe-code settings, and test assembly settings are assembly-level controls. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html))
- Editor APIs belong to UnityEditor assemblies and are unavailable in a Player build. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/platform-dependent-compilation.html))
- Released Unity packages are versioned dependencies tested for the target Editor line, but a released package is not automatically installed in a project. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- Feature sets group packages by use case without turning those packages into one runtime assembly or one architectural module. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/FeatureSets.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Use a feature or capability as the primary module boundary when its data, runtime behavior, tests, and optional Editor tooling change together.
- **Interpretation:** Put abstractions in the lower-level assembly only when multiple higher-level assemblies genuinely need the contract; otherwise keep the interface beside its consumer.
- **Interpretation:** Separate a runtime assembly from its `.Editor` and `.Tests` companions so Player compilation cannot acquire tool-only dependencies.
- **Interpretation:** Place optional package integration in a constrained adapter assembly. The core feature should not require a package merely to compile when the integration is disabled.
- **Interpretation:** Use namespaces to communicate conceptual ownership and asmdefs to enforce compilation dependencies; neither replaces the other.
- **Interpretation:** Prefer a small acyclic dependency graph over a large “common” assembly. A broadly referenced common assembly becomes expensive to change and easy to misuse.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Small prototype with one team and low compile pressure | Few coarse assemblies | Keep boundaries visible but avoid configuration overhead before there is real dependency pressure. |
| Feature owned and released independently | Feature runtime + Editor + tests assemblies | The unit can compile, test, and evolve without broad project coupling. |
| Optional third-party or released Unity package | Dedicated integration assembly | Package types do not leak through the project core. |
| Code shared by unrelated features | Narrow stable contract assembly | Only after demonstrating a true shared abstraction; avoid utility dumping grounds. |

## Risks and review checks

- Do not mirror every filesystem folder with an assembly; assemblies are compile and dependency boundaries, not decoration.
- Do not solve a cycle by merging all assemblies. Identify ownership, invert one dependency, or exchange data/events through a smaller contract.
- Do not put UnityEditor references in runtime assemblies even when wrapped by a runtime method that “only runs in Editor.”
- Do not expose package-specific types from a core public API when the package is optional.
- Do not use a global static service merely to bypass an assembly dependency; that hides rather than removes coupling.
- Review assembly platform filters in CI build targets; an assembly that compiles in the Editor can be absent on a Player target.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEditor.Compilation.CompilationPipeline`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Compilation.CompilationPipeline.html) | `public static class CompilationPipeline` | Editor | Editor-only inspection of script assemblies and compilation events. |
| [`UnityEditor.Compilation.CompilationPipeline.GetAssemblies`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Compilation.CompilationPipeline.GetAssemblies.html) | `public static Assembly[] GetAssemblies(AssembliesType assembliesType)` | Editor | Queries Editor or Player compilation assemblies for validation tooling. |
| [`UnityEngine.Scripting.PreserveAttribute`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Scripting.PreserveAttribute.html) | `[Preserve]` | Runtime | A targeted managed-stripping preservation marker; not a dependency-management substitute. |

## Official sources

- Project View — https://docs.unity3d.com/6000.3/Documentation/Manual/ProjectView.html
- Packages List — https://docs.unity3d.com/6000.3/Documentation/Manual/PackagesList.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Feature Sets — https://docs.unity3d.com/6000.3/Documentation/Manual/FeatureSets.html
- Assembly Definitions Intro — https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-intro.html
- Assembly Definition Files — https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html
- Platform Dependent Compilation — https://docs.unity3d.com/6000.3/Documentation/Manual/platform-dependent-compilation.html
- Compilation.Compilation Pipeline — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Compilation.CompilationPipeline.html
- Compilation.Compilation Pipeline.Get Assemblies — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Compilation.CompilationPipeline.GetAssemblies.html
- Scripting.Preserve Attribute — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Scripting.PreserveAttribute.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
