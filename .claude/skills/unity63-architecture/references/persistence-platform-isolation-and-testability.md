# Persistence, Platform Isolation, and Testable Architecture

## Scope

Defines ports and adapters for save/load, filesystem, platform APIs, build variants, and tests without coupling domain rules to static Unity state.

Read this reference when the request involves:

- architecting save/load or configuration persistence
- supporting multiple platforms or scripting backends
- making code testable in EditMode and validating Player-only behavior

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Save/load boundary**
- **platform-dependent compilation**
- **test assemblies**
- **build variants**
- **Application paths**
- **JsonUtility**
- **PlayerPrefs**
- **SystemInfo**
- **Unity Test Framework**

## Verified Unity 6.3 facts

- Unity exposes platform-specific persistent, streaming, temporary, and data paths with different read/write and packaging behavior. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application.html))
- `Application.persistentDataPath` is intended for data retained between runs, but its concrete location and availability are platform-specific. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html))
- `StreamingAssets` handling differs by platform and some targets require URL-based access rather than direct filesystem APIs. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-streamingAssetsPath.html))
- Unity provides platform-dependent compilation symbols, but conditional compilation removes code at compile time and can hide target-only errors until that target is built. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/platform-dependent-compilation.html))
- Unity supports Mono and IL2CPP scripting backends with different compilation pipelines and restrictions. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-backends.html))
- Unity Test Framework provides EditMode and PlayMode tests and assembly definition settings can identify test assemblies. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html))
- JsonUtility uses Unity serialization rules for structured JSON conversion. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.html))
- PlayerPrefs stores simple string, float, and integer values in platform-specific storage and is not documented as encrypted secure storage. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerPrefs.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Define a persistence port around domain save/load operations and keep file paths, JSON encoding, atomic writes, cloud synchronization, and platform permissions in adapters.
- **Interpretation:** Keep platform selection at the composition boundary. Domain code should consume capability interfaces rather than branches on RuntimePlatform throughout the codebase.
- **Interpretation:** Test serialization and migration with fixture files in EditMode; use PlayMode or target-device tests for scene lifecycle, platform paths, permissions, and backend-specific behavior.
- **Interpretation:** Make save operations transactional: write a temporary file, validate/flush as appropriate, and replace the previous version while retaining a recovery policy.
- **Interpretation:** Use PlayerPrefs only for small preferences that fit its primitive key/value contract, not for authoritative or sensitive game state.
- **Interpretation:** Build every supported backend/platform in automation where practical; compiler symbols cannot be fully validated by one Editor target.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Small non-sensitive preference | PlayerPrefs | Primitive key/value API is sufficient. |
| Versioned player progress | Persistence adapter + explicit DTO/schema | Migration, integrity, recovery, and testing are required. |
| Bundled read-only seed content | StreamingAssets with platform-aware access | Packaging/access differs by target. |
| Platform service | Platform-neutral interface + target adapter assembly | Conditional code stays at the edge. |

## Risks and review checks

- Do not assume a desktop filesystem path or separator is valid on mobile, Web, or console targets.
- Do not store secrets, credentials, or anti-cheat authority in PlayerPrefs.
- Do not compile only the Editor target and infer IL2CPP/AOT compatibility.
- Do not let save DTOs contain live GameObject, MonoBehaviour, or transient instance references.
- Do not catch all deserialization errors and silently reset progress without retaining diagnostics or recovery options.
- Do not mock Unity behavior that is the subject of the test; use a PlayMode or target test at that boundary.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Application.persistentDataPath`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html) | `public static string persistentDataPath { get; }` | Runtime | Persistent application data location. |
| [`UnityEngine.Application.streamingAssetsPath`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-streamingAssetsPath.html) | `public static string streamingAssetsPath { get; }` | Runtime | Packaged StreamingAssets location with platform-specific access semantics. |
| [`UnityEngine.JsonUtility.ToJson`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.ToJson.html) | `public static string ToJson(object obj, bool prettyPrint = false)` | Runtime | Unity-serialization-based JSON output. |
| [`UnityEngine.PlayerPrefs.SetString`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerPrefs.SetString.html) | `public static void SetString(string key, string value)` | Runtime | Stores a string preference in platform-specific PlayerPrefs storage. |
| [`UnityEngine.PlayerPrefs.Save`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerPrefs.Save.html) | `public static void Save()` | Runtime | Writes modified preferences to storage and can cause a hiccup; avoid hot-path calls. |
| [`UnityEngine.SystemInfo`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SystemInfo.html) | `public static class SystemInfo` | Runtime | Runtime hardware/platform capability queries; branch on capabilities when more appropriate than platform names. |

## Official sources

- Platform Dependent Compilation — https://docs.unity3d.com/6000.3/Documentation/Manual/platform-dependent-compilation.html
- Scripting Backends — https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-backends.html
- Scripting Backends Il2Cpp — https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-backends-il2cpp.html
- Test Framework Introduction — https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html
- Assembly Definition Files — https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-files.html
- Application — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application.html
- Application Persistent Data Path — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html
- Application Streaming Assets Path — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-streamingAssetsPath.html
- Json Utility — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.html
- Json Utility.To Json — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.ToJson.html
- Player Prefs — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerPrefs.html
- Player Prefs.Set String — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerPrefs.SetString.html
- Player Prefs.Save — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerPrefs.Save.html
- System Info — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SystemInfo.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
