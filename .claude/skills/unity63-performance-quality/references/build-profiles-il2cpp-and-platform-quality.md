# Build Profiles, IL2CPP, Stripping, and Platform Quality Gates

## Scope

Covers Unity 6.3 Build Profiles, scenes/settings, BuildPipeline/BuildReport, development versus release configuration, IL2CPP/AOT, stripping, content builds, and target-specific acceptance.

Read this reference when the request involves:

- creating or reviewing build configurations
- a build differs from Editor behavior
- validating IL2CPP, AOT, stripping, platform, scenes, or publishing quality

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Build Profiles**
- **BuildPipeline**
- **IL2CPP**
- **managed stripping**
- **platform publishing**
- **BuildProfile**
- **BuildPipeline.BuildPlayer**
- **BuildReport**
- **PlayerSettings**

## Verified Unity 6.3 facts

- Unity 6.3 Build Profiles store target/build configuration, scene lists, scripting defines, and profile-specific settings for build workflows. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/build-profiles.html))
- The legacy Build Settings window remains documented but Build Profiles provide the newer profile-oriented workflow in Unity 6. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/BuildSettings.html))
- Unity 6.3 includes Build Profile API/workflow changes documented in the release changes. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html))
- BuildPipeline.BuildPlayer creates a Player build from explicit BuildPlayerOptions and returns a BuildReport. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/BuildPipeline.BuildPlayer.html))
- BuildReport and BuildSummary provide result, errors/warnings, duration, output path, size, platform, and step/file information where available. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Build.Reporting.BuildReport.html))
- IL2CPP converts managed IL to C++ and native code and has AOT restrictions distinct from Editor/Mono execution. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-backends-il2cpp.html))
- Managed code stripping can remove unreachable code and requires preservation for indirect/reflection/serialization/native use that analysis cannot see. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/managed-code-stripping.html))
- Platform development requirements vary by target, including modules, SDKs, architecture, graphics API, signing, permissions, packaging, and store/publishing rules. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/PlatformSpecific.html))
- Scriptable Build Pipeline 3.1 is a released Unity 6.3 package for content build pipelines such as AssetBundle build customization. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Version build intent as named profiles: development QA, release candidate, dedicated server, platform/store variants, and content validation should not depend on a developer’s local window state.
- **Interpretation:** Build from an explicit script/CI entry point that selects the profile/options, validates scenes/packages/settings, records commit and environment metadata, and fails on BuildResult other than success.
- **Interpretation:** Treat warnings as reviewed artifacts. Establish allowlists for known warnings rather than ignoring all build output.
- **Interpretation:** Run backend/platform smoke tests immediately after every successful build, including startup, scene load, input, persistence, network/service failure, and content availability.
- **Interpretation:** Test stripping/reflection/serialization with release-level stripping and IL2CPP, not only Development Mono builds.
- **Interpretation:** Keep signing credentials and store secrets outside the repository while versioning non-secret entitlement/permission configuration and validation.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Developer diagnostics build | Development profile with explicit debugging/profiler options | Never confuse with release performance/security. |
| Release candidate | Non-development target profile + IL2CPP/stripping as shipping | Acceptance matches production configuration. |
| Dedicated server | Server-specific profile/subtarget and smoke tests | Headless assets, code paths, and services differ. |
| Custom content bundle pipeline | Scriptable Build Pipeline 3.1 after package verification | Use explicit content versioning/dependency validation. |

## Risks and review checks

- Do not build release artifacts from whichever profile/settings happen to be active locally.
- Do not treat BuildResult.Succeeded as proof the application launches or content/services work.
- Do not validate IL2CPP/AOT/stripping only on the final release day.
- Do not preserve whole assemblies or lower stripping globally without understanding size/startup/security tradeoffs.
- Do not ship Development Build, Script Debugging, Autoconnect Profiler, test scenes, or verbose logs accidentally.
- Do not store signing passwords, service secrets, or private keys in build scripts or repository files.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEditor.BuildPipeline.BuildPlayer`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/BuildPipeline.BuildPlayer.html) | `public static BuildReport BuildPlayer(BuildPlayerOptions buildPlayerOptions)` | Editor | Builds a Player and returns structured report data. |
| [`UnityEditor.BuildPlayerOptions`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/BuildPlayerOptions.html) | `public struct BuildPlayerOptions` | Editor | Scenes, output, target, target group/subtarget, and BuildOptions for a scripted Player build. |
| [`UnityEditor.Build.Reporting.BuildReport.summary`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Build.Reporting.BuildReport-summary.html) | `public BuildSummary summary { get; }` | Editor | Overall result and build metadata. |
| [`UnityEditor.Build.Reporting.BuildSummary.result`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Build.Reporting.BuildSummary-result.html) | `public BuildResult result { get; }` | Editor | Succeeded, Failed, Cancelled, or Unknown result. |
| [`UnityEditor.Build.Profile.BuildProfile`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Build.Profile.BuildProfile.html) | `public sealed class BuildProfile : ScriptableObject` | Editor | Unity 6.3 Editor API surface for build-profile assets/settings; verify exact workflow members before automation. |
| [`UnityEngine.Scripting.PreserveAttribute`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Scripting.PreserveAttribute.html) | `[Preserve]` | Runtime | Narrow managed-stripping preservation marker. |

### Package prerequisites

- `com.unity.scriptablebuildpipeline@3.1` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@3.1/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Build Profiles — https://docs.unity3d.com/6000.3/Documentation/Manual/build-profiles.html
- Build Settings — https://docs.unity3d.com/6000.3/Documentation/Manual/BuildSettings.html
- Whats New Unity63 — https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html
- Scripting Backends Il2Cpp — https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-backends-il2cpp.html
- Managed Code Stripping — https://docs.unity3d.com/6000.3/Documentation/Manual/managed-code-stripping.html
- Platform Specific — https://docs.unity3d.com/6000.3/Documentation/Manual/PlatformSpecific.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Build Pipeline.Build Player — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/BuildPipeline.BuildPlayer.html
- Build Player Options — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/BuildPlayerOptions.html
- Build.Reporting.Build Report — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Build.Reporting.BuildReport.html
- Build.Reporting.Build Report Summary — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Build.Reporting.BuildReport-summary.html
- Build.Reporting.Build Summary Result — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Build.Reporting.BuildSummary-result.html
- Build.Profile.Build Profile — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Build.Profile.BuildProfile.html
- Scripting.Preserve Attribute — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Scripting.PreserveAttribute.html
- Com.Unity.Scriptablebuildpipeline@3.1 — https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@3.1/manual/index.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
