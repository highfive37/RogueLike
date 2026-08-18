# Supported C# Compilation, Profiles, and Assemblies

## Scope

Pins C# implementation decisions to Unity 6.3’s documented compiler, language level, .NET profiles, predefined/custom assemblies, and compilation boundaries.

Read this reference when the request involves:

- checking whether C# syntax or a base-class-library API is supported
- choosing API Compatibility Level, asmdefs, analyzers, or compile symbols
- diagnosing Editor-versus-Player or assembly compilation differences

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **C# compiler**
- **.NET profiles**
- **script compilation**
- **Assembly Definitions**
- **CompilationPipeline**
- **Application.unityVersion**
- **platform symbols**

## Verified Unity 6.3 facts

- Unity 6.3 uses the Roslyn C# compiler and documents C# 9.0 as the supported language version. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html))
- Unity 6.3 supports .NET Standard 2.1 as the default API compatibility profile and a .NET Framework profile compatible with .NET Framework 4.8 APIs where supported by Unity platforms. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/dotnet-profile-support.html))
- The API compatibility profile controls the managed API surface available to compilation and does not change Unity’s C# language version. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/dotnet-profile-support.html))
- Unity compiles scripts into predefined assemblies unless Assembly Definition assets assign them to custom assemblies. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-intro.html))
- Assembly Definition assets expose references, platform inclusion/exclusion, define constraints, version defines, unsafe-code settings, and test assembly options. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html))
- Runtime and Editor compilation targets differ; UnityEditor APIs cannot be used in Player assemblies. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/platform-dependent-compilation.html))
- Platform-dependent compilation symbols are defined for documented Editor, platform, backend, and architecture contexts. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/platform-dependent-compilation.html))
- The selected scripting backend compiles managed assemblies differently: Mono uses a managed runtime/JIT where supported, while IL2CPP converts IL to C++ and then native code ahead of time. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-backends.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Before adopting a language feature, verify C# 9 support rather than assuming the language version installed with the system .NET SDK controls Unity compilation.
- **Interpretation:** Prefer .NET Standard 2.1 for portable project code unless a verified platform requirement needs the broader .NET Framework profile.
- **Interpretation:** Keep an assembly’s public surface narrow. Every public type is a coupling and stripping/AOT consideration in addition to a C# design choice.
- **Interpretation:** Use assembly-level platform and version constraints for optional integrations; reserve preprocessor branches for small target-specific differences within otherwise shared code.
- **Interpretation:** Compile representative Player targets early because Editor compilation cannot expose every backend, API-profile, or target assembly issue.
- **Interpretation:** Treat generated source, analyzer, and unsafe-code use as project build infrastructure that must be validated under Unity’s documented compiler pipeline and target backends.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Portable runtime library code | .NET Standard 2.1 profile | Default and broad Unity platform compatibility. |
| API only present in broader profile and supported on all targets | .NET Framework profile after target verification | Profile availability does not guarantee every platform behavior. |
| Feature-level dependency boundary | Custom asmdef | Explicit references and compile scope. |
| Tiny target-specific implementation difference | Documented conditional symbol | Keep the branch local and test each target. |

## Risks and review checks

- Do not infer support from a modern standalone .NET SDK or IDE IntelliSense alone.
- Do not use C# features introduced after C# 9 without exact Unity 6.3 evidence.
- Do not reference UnityEditor from code that participates in Player compilation.
- Do not assume .NET Framework compatibility means desktop-only APIs work on every Unity target.
- Do not introduce unsafe code globally when only one constrained assembly needs it.
- Do not rely on a define symbol that is absent from the documented 6000.3 symbol set or project settings.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEditor.Compilation.CompilationPipeline`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Compilation.CompilationPipeline.html) | `public static class CompilationPipeline` | Editor | Editor-only compilation events and assembly metadata. |
| [`UnityEditor.Compilation.CompilationPipeline.GetAssemblies`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Compilation.CompilationPipeline.GetAssemblies.html) | `public static Assembly[] GetAssemblies(AssembliesType assembliesType)` | Editor | Retrieves the current Editor or Player compilation assembly graph. |
| [`UnityEngine.Application.unityVersion`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-unityVersion.html) | `public static string unityVersion { get; }` | Runtime | Reports the Unity runtime version; do not use it as a substitute for compile-time compatibility validation. |
| [`UnityEngine.Application.platform`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-platform.html) | `public static RuntimePlatform platform { get; }` | Runtime | Runtime platform identifier; capability checks can be preferable to platform-name branching. |

## Official sources

- Csharp Compiler — https://docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html
- Dotnet Profile Support — https://docs.unity3d.com/6000.3/Documentation/Manual/dotnet-profile-support.html
- Scripting — https://docs.unity3d.com/6000.3/Documentation/Manual/scripting.html
- Assembly Definitions Intro — https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-intro.html
- Assembly Definition Files — https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html
- Platform Dependent Compilation — https://docs.unity3d.com/6000.3/Documentation/Manual/platform-dependent-compilation.html
- Scripting Backends — https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-backends.html
- Compilation.Compilation Pipeline — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Compilation.CompilationPipeline.html
- Compilation.Compilation Pipeline.Get Assemblies — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Compilation.CompilationPipeline.GetAssemblies.html
- Application Unity Version — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-unityVersion.html
- Application Platform — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-platform.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
