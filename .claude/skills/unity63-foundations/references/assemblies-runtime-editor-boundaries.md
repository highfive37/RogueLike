# Assembly Definitions and Runtime/Editor Boundaries

## Scope

Explains Unity script compilation units, assembly definition and reference files, dependency direction, auto-referencing, platform filters, define constraints, test assemblies, and the hard boundary between runtime and UnityEditor APIs.

Read this reference when the request involves:

- assembly definition files, compilation boundaries, cyclic dependencies, or compile times
- runtime code accidentally references UnityEditor
- platform-specific code, test assemblies, or optional package integration

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Assembly definitions**
- **Compilation and code reload**
- **Editor-only code**
- **Conditional compilation**
- **Assembly Definition properties**
- **UnityEditor assembly boundary**
- **Scripting define symbols**

## Verified Unity 6.3 facts

- Without custom assembly definitions, Unity compiles project scripts into predefined assemblies based on folder and language rules. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-intro.html))
- An Assembly Definition asset causes scripts in its folder and subfolders, subject to nested definitions, to compile into a separate managed assembly. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-intro.html))
- Assembly definitions make dependencies explicit and can reduce recompilation scope when unrelated code changes. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-intro.html))
- Assembly Definition References allow additional folders to contribute scripts to an existing assembly without creating a new assembly identity. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html))
- Assembly references must form an acyclic dependency graph; circular assembly references cannot compile. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-intro.html))
- Auto Referenced controls whether predefined assemblies automatically reference a custom assembly; explicit references are still required between custom assemblies. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html))
- Platform include/exclude settings and define constraints can control whether an assembly participates in a compilation target. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html))
- Version Defines can activate symbols when a specified module or package version expression is present. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html))
- The `UnityEditor` namespace and editor assemblies are unavailable in Player builds and must remain behind Editor-only compilation boundaries. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/platform-dependent-compilation.html))
- Unity provides platform-dependent compilation symbols for Editor, Player platform, scripting backend, architecture, and other target conditions. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/platform-dependent-compilation.html))
- Assembly definitions can mark test assemblies and add optional Unity test references for EditMode or PlayMode test code. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Align assemblies with cohesive change and dependency boundaries, not with every folder. A feature assembly should expose a narrow API and depend inward on stable contracts.
- **Interpretation:** Create a separate Editor companion assembly for importers, inspectors, build tooling, and menu commands; runtime assemblies must not reference it.
- **Interpretation:** Use an integration assembly for optional packages so the core feature compiles without that package when the integration is excluded.
- **Interpretation:** Prefer explicit references once a project is large enough to benefit from controlled dependency direction; document why Auto Referenced is enabled when retained.
- **Interpretation:** Keep platform-specific implementations behind a platform-neutral interface and use assembly/platform constraints to exclude unsupported implementations.
- **Interpretation:** Treat assembly boundaries as test seams: pure domain assemblies can be tested without loading scenes or depending on broad Unity systems.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Small prototype with few scripts | Predefined assemblies initially | Avoid premature fragmentation; add boundaries when change/dependency pressure appears. |
| Independent feature or reusable module | Dedicated asmdef | Controls dependencies and recompilation scope. |
| Custom inspectors/build tools for a runtime feature | Separate `.Editor` asmdef | Prevents UnityEditor references from entering Player code. |
| Optional package adapter | Version/define-constrained integration assembly | The core assembly remains package-agnostic. |

## Risks and review checks

- Do not create a cyclic assembly graph to mirror bidirectional runtime communication; invert one dependency through a contract or event.
- Do not put an asmdef at every folder level; excessive fragmentation increases configuration and dependency overhead.
- Do not rely only on `#if UNITY_EDITOR` inside a runtime assembly when the whole class belongs to Editor tooling.
- Do not reference concrete implementation assemblies from a lower-level domain assembly.
- Do not assume a platform symbol proves an API exists on every backend; verify the target API and backend restrictions.
- Do not expose `internal` implementation details merely to bypass a boundary; use explicit test visibility only when justified.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEditor.Compilation.CompilationPipeline`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Compilation.CompilationPipeline.html) | `public static class CompilationPipeline` | Editor | Editor-only events and queries for Unity script compilation and assemblies. |
| [`UnityEditor.Compilation.CompilationPipeline.GetAssemblies`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Compilation.CompilationPipeline.GetAssemblies.html) | `public static Assembly[] GetAssemblies(AssembliesType assembliesType)` | Editor | Returns Editor or Player compilation assemblies for tooling. |
| [`UnityEngine.Scripting.PreserveAttribute`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Scripting.PreserveAttribute.html) | `[Preserve]` | Runtime | Prevents eligible code from being removed by managed stripping; use narrowly and verify build behavior. |

## Official sources

- Assembly Definitions Intro — https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-intro.html
- Assembly Definition Files — https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html
- Platform Dependent Compilation — https://docs.unity3d.com/6000.3/Documentation/Manual/platform-dependent-compilation.html
- Scripting — https://docs.unity3d.com/6000.3/Documentation/Manual/scripting.html
- Compilation.Compilation Pipeline — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Compilation.CompilationPipeline.html
- Compilation.Compilation Pipeline.Get Assemblies — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Compilation.CompilationPipeline.GetAssemblies.html
- Scripting.Preserve Attribute — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Scripting.PreserveAttribute.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
