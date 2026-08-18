---
name: unity63-csharp
description: Use when writing, reviewing, refactoring, or debugging C# for Unity 6.3, especially compiler support, UnityEngine.Object null behavior, serialization, callbacks, coroutines, Awaitable, allocations, GC, pooling, IL2CPP, AOT, stripping, events, errors, or tests.
---

# Unity 6.3 C#

## Knowledge scope

Target-version-safe C# implementation and review knowledge.

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

- [Supported C# Compilation, Profiles, and Assemblies](references/supported-csharp-compilation-and-assemblies.md) — Pins C# implementation decisions to Unity 6.3’s documented compiler, language level, .NET profiles, predefined/custom assemblies, and compilation boundaries. Load when: checking whether C# syntax or a base-class-library API is supported; choosing API Compatibility Level, asmdefs, analyzers, or compile symbols; diagnosing Editor-versus-Player or assembly compilation differences.
- [UnityEngine.Object Lifetime, Equality, Null, and Destruction](references/unityengine-object-null-and-destruction.md) — Explains the managed/native dual lifetime of UnityEngine.Object, overloaded equality and Boolean checks, fake-null behavior, destruction timing, and safe reference use. Load when: a Unity object compares equal to null unexpectedly; MissingReferenceException or destroyed-object access occurs; reviewing `?.`, `??`, caching, destruction, or reference lifetime.
- [Serialization: Fields, References, Containers, and Refactoring](references/serialization-fields-properties-and-generics.md) — Provides exact Unity 6.3 serialization boundaries for fields, Unity object references, inline values, managed references, containers, callbacks, and safe field renaming. Load when: a field is missing from the Inspector or fails to persist; designing serializable data, polymorphism, references, collections, or field renames; reviewing domain reload, hot reload, prefab, scene, or ScriptableObject data.
- [MonoBehaviour Messages, Callback Contracts, and Coroutines](references/monobehaviour-callbacks-and-coroutines.md) — Documents message signatures, activation requirements, callback ordering limits, coroutine ownership, yield instructions, and implementation review rules. Load when: implementing or reviewing Unity messages; a callback does not execute; starting, stopping, nesting, or cancelling coroutines.
- [Awaitable, Task Interop, Exceptions, and Main-Thread Safety](references/awaitable-tasks-and-main-thread.md) — Provides implementation-level rules for Unity Awaitable and Task-returning code, pooled awaitables, continuation scheduling, exceptions, cancellation, and Unity main-thread access. Load when: writing async methods in Unity 6.3; interoperating with Task-based libraries; debugging async exceptions, thread affinity, cancellation, or duplicate awaits.
- [Managed Allocations, Garbage Collection, and Object Pooling](references/allocations-gc-and-object-pooling.md) — Connects C# allocation sources, Unity incremental GC, collection reuse, non-alloc APIs, and UnityEngine.Pool to code-level review and measurement. Load when: reducing GC.Alloc or frame spikes; reviewing LINQ, closures, boxing, strings, arrays, or per-frame collections; designing a pool for managed objects or GameObjects.
- [IL2CPP, AOT, Managed Stripping, and Platform Code](references/il2cpp-aot-stripping-and-platform-code.md) — Explains Unity 6.3 scripting backends, IL2CPP/AOT restrictions, managed code stripping, reflection/generic preservation, platform compilation, and build-time verification. Load when: a project fails only in IL2CPP or on device; reflection or generic code disappears in a build; choosing scripting backend, stripping settings, or platform-specific code.
- [Errors, Events, Refactoring Safety, and Testable C#](references/errors-events-refactoring-and-tests.md) — Provides C# review rules for exceptions, logging, assertions, event lifetime, serialized refactors, and choosing EditMode versus PlayMode tests. Load when: handling exceptions or diagnostics; reviewing event subscription leaks; refactoring serialized or Unity-callback code safely; choosing EditMode or PlayMode tests.

## Cross-skill boundary

- Use `unity63-foundations` for object/project/scene/prefab/asset fundamentals.
- Use `unity63-architecture` for ownership, dependency, lifetime, state, and structural choices.
- Use `unity63-csharp` for implementation language, serialization, async, memory, backend, and refactoring details.
- Use `unity63-game-systems` for a concrete Unity production system.
- Use `unity63-performance-quality` for measurement, testing, debugging, builds, and review gates.
