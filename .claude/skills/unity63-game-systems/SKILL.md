---
name: unity63-game-systems
description: Use when a Unity 6.3 request involves cameras, world building, input, physics, animation, Timeline, video, UI, accessibility, audio, lighting, shaders, render pipelines, post-processing, VFX, 2D, AI Navigation, inference, XR, multiplayer, platforms, or services.
---

# Unity 6.3 Game Systems

## Knowledge scope

System-by-system Unity game-development knowledge and integration boundaries.

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

- [Cameras, Cinemachine, and World Building](references/cameras-cinemachine-and-world-building.md) — Covers Camera projection and rendering responsibility, Cinemachine 3.1 coordination, Terrain and geometry authoring, LOD/occlusion, and scene-scale world composition. Load when: designing cameras or camera transitions; using Cinemachine 3.1; building Terrain, ProBuilder, spline, LOD, or large-world scene content.
- [Input Systems, Actions, Devices, and Update Flow](references/input-and-device-flow.md) — Distinguishes the Input System 1.20 package from the legacy Input Manager and defines action maps, devices, control schemes, update timing, UI integration, and rebinding boundaries. Load when: implementing keyboard, mouse, gamepad, touch, sensor, or device input; choosing legacy Input Manager versus Input System 1.20; connecting input to physics, UI, local multiplayer, or rebinding.
- [3D and 2D Physics Systems](references/physics-3d-and-2d.md) — Covers Rigidbody/Rigidbody2D simulation, colliders, triggers, queries, layers, joints, character movement, fixed timing, and performance boundaries without mixing 3D and 2D APIs. Load when: implementing collisions, triggers, Rigidbody motion, raycasts, or character movement; choosing 3D versus 2D physics APIs; debugging unstable simulation or physics performance.
- [Animation, Playables, Timeline, and Video](references/animation-timeline-and-video.md) — Connects Animator state machines, clips, parameters, root motion, Playables, Timeline 1.8, animation rigging, and VideoPlayer to clear authority and update flow. Load when: implementing Animator controllers, clips, transitions, root motion, or IK; using Timeline 1.8, Playables, cutscenes, or VideoPlayer; coordinating gameplay state with animation and cinematic sequencing.
- [UI Toolkit, Unity UI, IMGUI, and Accessibility](references/ui-toolkit-ugui-and-accessibility.md) — Compares Unity’s UI systems and integrates layout, events, focus, data binding, rendering, native screen readers, and accessible interaction requirements in Unity 6.3. Load when: choosing UI Toolkit, Unity UI (uGUI), or IMGUI; building runtime/editor UI, focus/navigation, localization, or responsive layout; implementing screen reader and accessibility support in Unity 6.3.
- [Audio Sources, Mixers, Spatialization, and Scriptable Audio](references/audio-and-scriptable-audio.md) — Covers clips, sources, listeners, mixers, snapshots, spatial audio, scheduling, import/load choices, profiling, and Unity 6.3’s Scriptable Audio Pipeline addition. Load when: playing music, sound effects, dialogue, or spatial audio; designing AudioMixer routing, snapshots, scheduling, or voice limits; evaluating the Unity 6.3 Scriptable Audio Pipeline.
- [Lighting, Materials, Shaders, and Per-Renderer Data](references/lighting-materials-and-shaders.md) — Explains light sources, environment lighting, baked/realtime/mixed workflows, probes, materials, shader properties/variants, and per-renderer overrides across Unity 6.3 pipelines. Load when: authoring or debugging lights, shadows, lightmaps, probes, materials, or shaders; setting shader properties from C# without duplicating materials; controlling shader variants, keywords, or per-renderer values.
- [Render Pipelines, Graphics Settings, and Post-Processing](references/render-pipelines-and-post-processing.md) — Defines the Built-in Render Pipeline, URP/HDRP selection boundary, Scriptable Render Pipeline assets, camera/pipeline interaction, render callbacks, and post-processing ownership. Load when: choosing or migrating a render pipeline; configuring pipeline assets or quality tiers; implementing custom render callbacks/features or post-processing.
- [Particle Systems, VFX Graph, Lines, Trails, and Visual Effects](references/particles-vfx-and-visual-effects.md) — Covers Shuriken ParticleSystem, VFX Graph, line/trail renderers, effect lifetime, simulation spaces, pooling, bounds, events, and CPU/GPU performance choices. Load when: creating particles, GPU effects, trails, lines, decals, or transient effects; choosing Particle System versus VFX Graph; pooling effects or diagnosing VFX performance/culling.
- [2D Sprites, Tilemaps, Sorting, Animation, and Physics](references/2d-sprites-tilemaps-and-sorting.md) — Covers Sprite assets/renderers, atlases, sorting layers/groups, Tilemaps, 2D cameras and lighting boundaries, 2D animation, and Physics2D integration. Load when: building a 2D game or mixed 2D/3D scene; using sprites, sprite atlases, tilemaps, sorting, or 2D animation; debugging 2D rendering order, collider generation, or batching.
- [AI Navigation, Inference, XR, AR, and Spatial Systems](references/ai-navigation-xr-and-spatial.md) — Maps Unity 6.3’s AI Navigation 2.0, AI Inference 2.6, built-in NavMesh agents, AR Foundation 6.6, OpenXR 1.17, and XR Interaction Toolkit 3.5 to optional-package architecture and runtime constraints. Load when: implementing NavMesh pathfinding, runtime navigation surfaces, or agents; running neural-model inference with Unity AI Inference; building AR, VR, OpenXR, tracking, spatial anchors, or XR interactions.
- [Multiplayer, Platform Development, and Unity Services](references/multiplayer-platform-toolkit-and-services.md) — Separates networking model choice, authority, serialization, prediction, testing, platform adaptation, and optional Unity Services/packages in Unity 6.3. Load when: designing multiplayer architecture or choosing a Unity netcode stack; testing multiple players locally or profiling network traffic; integrating platform-specific capabilities or Unity Services.

## Cross-skill boundary

- Use `unity63-foundations` for object/project/scene/prefab/asset fundamentals.
- Use `unity63-architecture` for ownership, dependency, lifetime, state, and structural choices.
- Use `unity63-csharp` for implementation language, serialization, async, memory, backend, and refactoring details.
- Use `unity63-game-systems` for a concrete Unity production system.
- Use `unity63-performance-quality` for measurement, testing, debugging, builds, and review gates.
