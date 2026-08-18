---
name: unity63-performance-quality
description: Use when profiling, optimizing, testing, debugging, building, publishing, or reviewing Unity 6.3 quality, including CPU, GPU, memory, GC, loading, physics, UI, tests, diagnostics, Build Profiles, IL2CPP, stripping, and platform gates.
---

# Unity 6.3 Performance and Quality

## Knowledge scope

Measurement, diagnosis, testing, build, and code-review quality guidance.

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

- [Profiling CPU, Frames, and the PlayerLoop](references/profiling-cpu-and-player-loop.md) — Provides a measurement-first workflow for CPU frame time, main/render/job threads, PlayerLoop phases, scripts, custom markers, call stacks, comparisons, and representative Player captures. Load when: a frame-rate, CPU, or main-thread problem is reported; using Profiler, ProfilerRecorder, Profile Analyzer, or custom markers; reviewing Update/FixedUpdate cost or intermittent spikes.
- [Memory, GC, Snapshots, and Pooling Quality](references/memory-gc-and-pooling.md) — Covers managed/native/GPU memory categories, allocation tracing, incremental GC, snapshots, retained references, asset lifetime, pools, and memory-budget verification. Load when: memory grows, GC spikes, or out-of-memory occurs; using Memory Profiler 1.1 or managed allocation call stacks; reviewing pools, caches, asset unloading, or retained references.
- [GPU, Rendering, Shader, Texture, Mesh, and Asset Performance](references/gpu-rendering-and-assets.md) — Provides a quality workflow for GPU-bound frames, render passes, overdraw, batches, shaders/variants, texture and mesh import, LOD, asset loading, and visual-equivalence checks. Load when: a build is GPU-bound or has rendering spikes; reducing draw calls, SetPasses, overdraw, shadows, shader variants, texture or mesh memory; reviewing asset import settings, LOD, Addressables, or pipeline quality.
- [Physics, UI, Scene, and Asset Loading Performance](references/physics-ui-and-loading-performance.md) — Covers fixed-step catch-up, physics queries/contacts, UI layout and rebuilds, scene activation, asynchronous asset loading, Addressables ownership, and transition-budget design. Load when: physics, UI, scene transition, or asset loading causes frame spikes; optimizing FixedUpdate, Canvas/UI Toolkit layout, additive scenes, or Addressables; designing loading screens and cancellation/release behavior.
- [EditMode, PlayMode, UI, Coverage, and Performance Testing](references/testing-editmode-playmode-ui-and-performance.md) — Defines a layered Unity 6.3 test strategy using EditMode and PlayMode tests, UnityTest frame flows, logs, scenes, UI Test Framework additions, code coverage, and performance measurements. Load when: creating a Unity test strategy; choosing EditMode versus PlayMode tests; testing coroutines, scenes, physics, UI, performance, or coverage.
- [Debugging, Console, Logs, Assertions, and Diagnostics](references/debugging-console-and-diagnostics.md) — Provides a reproducible diagnostic workflow using Console/logging, stack traces, Development Builds, debugger attachment, crash/player logs, Frame Debugger, Profiler, and targeted assertions. Load when: investigating an exception, crash, hang, rendering error, or device-only defect; using Console, logs, stack traces, debugger, Development Build, Frame Debugger, or diagnostics; designing actionable logging and failure reports.
- [Build Profiles, IL2CPP, Stripping, and Platform Quality Gates](references/build-profiles-il2cpp-and-platform-quality.md) — Covers Unity 6.3 Build Profiles, scenes/settings, BuildPipeline/BuildReport, development versus release configuration, IL2CPP/AOT, stripping, content builds, and target-specific acceptance. Load when: creating or reviewing build configurations; a build differs from Editor behavior; validating IL2CPP, AOT, stripping, platform, scenes, or publishing quality.
- [Performance and Quality Code Review and Triage](references/code-review-checklists-and-triage.md) — Converts Unity 6.3 official behavior into issue-classification, evidence, review, regression, and anti-optimization checklists for architecture and code reviews. Load when: reviewing a pull request for Unity performance or quality risk; triaging a regression with incomplete evidence; deciding whether an optimization is justified and how to verify it.

## Cross-skill boundary

- Use `unity63-foundations` for object/project/scene/prefab/asset fundamentals.
- Use `unity63-architecture` for ownership, dependency, lifetime, state, and structural choices.
- Use `unity63-csharp` for implementation language, serialization, async, memory, backend, and refactoring details.
- Use `unity63-game-systems` for a concrete Unity production system.
- Use `unity63-performance-quality` for measurement, testing, debugging, builds, and review gates.
