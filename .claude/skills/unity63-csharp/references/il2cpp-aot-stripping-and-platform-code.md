# IL2CPP, AOT, Managed Stripping, and Platform Code

## Scope

Explains Unity 6.3 scripting backends, IL2CPP/AOT restrictions, managed code stripping, reflection/generic preservation, platform compilation, and build-time verification.

Read this reference when the request involves:

- a project fails only in IL2CPP or on device
- reflection or generic code disappears in a build
- choosing scripting backend, stripping settings, or platform-specific code

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Scripting backends**
- **IL2CPP**
- **AOT restrictions**
- **managed code stripping**
- **platform compilation**
- **PreserveAttribute**
- **link.xml**
- **RuntimePlatform**
- **Application**

## Verified Unity 6.3 facts

- Unity supports documented scripting backends including Mono and IL2CPP, selected per build target where available. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-backends.html))
- IL2CPP compiles managed assemblies to C++ and then native machine code ahead of time. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-backends-il2cpp.html))
- AOT platforms have restrictions around runtime code generation and some generic/reflection patterns because required native code must be known at build time. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-restrictions.html))
- Managed code stripping removes code considered unreachable according to the selected stripping level and dependency analysis. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/managed-code-stripping.html))
- Reflection, serialization, native callbacks, and other indirect activation can require preservation metadata when static analysis cannot identify usage. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/managed-code-stripping.html))
- `PreserveAttribute` can mark code for preservation and `link.xml` can describe preservation rules at assembly/type/member granularity. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/managed-code-stripping.html))
- Platform-dependent compilation symbols allow target-specific source inclusion, while assembly definitions can include/exclude whole assemblies by platform and define. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/platform-dependent-compilation.html))
- Unity 6.3 includes IL2CPP improvements such as reduced metadata size documented in the 6.3 release changes, but project-specific stripping and AOT validation remain required. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Continuously build at least one IL2CPP target during development; do not postpone AOT discovery until release.
- **Interpretation:** Make reflected or serialized type registration explicit. Prefer generated/static registration over broad preservation when feasible.
- **Interpretation:** Apply the narrowest preservation rule that matches actual indirect use, then inspect build reports and functional tests to confirm both presence and stripping effectiveness.
- **Interpretation:** Place platform implementations in separate assemblies selected by platform constraints and expose a platform-neutral contract to shared code.
- **Interpretation:** Avoid dynamic code generation assumptions and test generic combinations actually used through interfaces/reflection on AOT targets.
- **Interpretation:** Treat backend/platform build failures as architecture feedback: static reachability, native plugin ABI, threading, filesystem, and codegen need explicit boundaries.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Fast iteration target that supports Mono | Mono for development plus regular IL2CPP CI build | Iteration speed must not replace release-backend validation. |
| Release platform requires/uses IL2CPP | IL2CPP | AOT restrictions and generated native build are part of acceptance. |
| One indirectly used type/member | Narrow Preserve or link.xml entry | Avoid preserving entire assemblies without evidence. |
| Whole platform adapter | Asmdef platform constraints | Excludes unsupported implementation and dependencies at compile time. |

## Risks and review checks

- Do not interpret an Editor/Mono pass as proof of IL2CPP/AOT correctness.
- Do not set stripping to minimal or preserve every assembly to mask missing registration without measuring size and startup impact.
- Do not rely on runtime Reflection.Emit or dynamic code generation on AOT platforms.
- Do not assume every closed generic combination is generated merely because the generic type compiles.
- Do not scatter platform `#if` branches through domain logic.
- Do not use an undocumented backend define or platform API based on memory from another Unity version.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Scripting.PreserveAttribute`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Scripting.PreserveAttribute.html) | `[Preserve]` | Runtime | Marks eligible code so managed stripping retains it. |
| [`UnityEngine.Application.platform`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-platform.html) | `public static RuntimePlatform platform { get; }` | Runtime | Runtime platform identity; prefer capability abstraction where applicable. |
| [`UnityEngine.Application.isEditor`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-isEditor.html) | `public static bool isEditor { get; }` | Runtime | True when running in the Unity Editor; not a compilation boundary. |
| [`UnityEngine.SystemInfo`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SystemInfo.html) | `public static class SystemInfo` | Runtime | Runtime capability and hardware information. |

## Official sources

- Scripting Backends — https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-backends.html
- Scripting Backends Il2Cpp — https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-backends-il2cpp.html
- Scripting Restrictions — https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-restrictions.html
- Managed Code Stripping — https://docs.unity3d.com/6000.3/Documentation/Manual/managed-code-stripping.html
- Platform Dependent Compilation — https://docs.unity3d.com/6000.3/Documentation/Manual/platform-dependent-compilation.html
- Whats New Unity63 — https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html
- Scripting.Preserve Attribute — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Scripting.PreserveAttribute.html
- Application Platform — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-platform.html
- Application Is Editor — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-isEditor.html
- System Info — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SystemInfo.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
