# AI Navigation, Inference, XR, AR, and Spatial Systems

## Scope

Maps Unity 6.3’s AI Navigation 2.0, AI Inference 2.6, built-in NavMesh agents, AR Foundation 6.6, OpenXR 1.17, and XR Interaction Toolkit 3.5 to optional-package architecture and runtime constraints.

Read this reference when the request involves:

- implementing NavMesh pathfinding, runtime navigation surfaces, or agents
- running neural-model inference with Unity AI Inference
- building AR, VR, OpenXR, tracking, spatial anchors, or XR interactions

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Unity AI**
- **AI Navigation**
- **AI Inference**
- **XR**
- **AR Foundation**
- **OpenXR**
- **NavMeshAgent**
- **NavMeshSurface**
- **XRSettings**
- **package-specific XR providers**

## Verified Unity 6.3 facts

- Unity 6.3’s AI documentation distinguishes AI Navigation and AI Inference capabilities and directs package-specific behavior to official package documentation. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/unity-ai.html))
- AI Navigation 2.0 is a released package for Unity 6.3 and provides high-level NavMesh authoring components including runtime/buildable surfaces and links. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- NavMeshAgent provides pathfinding/movement over a baked or built navigation mesh, with speed, acceleration, stopping, avoidance, area, and path state. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AI.NavMeshAgent.html))
- NavMeshObstacle can carve or obstruct navigation according to its settings and runtime behavior. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AI.NavMeshObstacle.html))
- AI Inference 2.6 is a released package for Unity 6.3; package installation/model support/backend constraints must be verified before using its APIs. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- Unity XR documentation covers provider/plugin management, display/input/tracking, AR, VR, MR, OpenXR, and platform-specific requirements. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/XR.html))
- AR Foundation 6.6 provides a multi-platform AR layer that depends on a provider plug-in such as ARCore or ARKit for the target. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- OpenXR 1.17 and XR Interaction Toolkit 3.5 are released packages for Unity 6.3. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- XR package/platform features are optional dependencies and their support varies by device, runtime, graphics API, provider, and package configuration. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/XR.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Wrap navigation requests behind an agent/path service so gameplay reasons about destinations and outcomes rather than NavMesh component details.
- **Interpretation:** Treat a computed path as transient. Dynamic obstacles, area masks, off-mesh links, scene loads, and rebakes can invalidate assumptions.
- **Interpretation:** Place AI Inference model loading, tensor conversion, backend choice, scheduling, and output decoding in one adapter; domain code consumes semantic results.
- **Interpretation:** For XR, separate application interaction intent from provider/device input and tracking. Package/provider assemblies should stay at the platform edge.
- **Interpretation:** Design tracking-loss, permission denial, unsupported feature, origin changes, and session restart as normal states rather than exceptional afterthoughts.
- **Interpretation:** Test on target devices early. Editor simulation cannot prove tracking quality, thermals, motion-to-photon latency, permissions, or provider support.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Agent movement over authored walkable surfaces | NavMeshAgent + AI Navigation surface workflow | Pathfinding and authoring components match the requirement. |
| Model inference in application | AI Inference 2.6 adapter | Confirm model operators/backend/platform and package installation. |
| Cross-vendor VR runtime | OpenXR 1.17 where target runtime supports it | Use provider capabilities rather than vendor assumptions. |
| Cross-platform handheld AR | AR Foundation 6.6 + exact provider | Foundation alone does not implement device tracking. |

## Risks and review checks

- Do not assume NavMeshAgent destination assignment guarantees a complete path; inspect pathPending/status/remaining distance under the documented states.
- Do not rebuild a large NavMesh synchronously during an interactive frame without measuring.
- Do not assume AI Inference package APIs or model operators exist when the package/model compatibility is unverified.
- Do not include every XR provider package in all builds; constrain target integrations.
- Do not use head/controller tracking data from a background thread or retain stale poses after tracking loss.
- Do not treat AR Foundation as a provider; install/configure the exact target provider.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.AI.NavMeshAgent.SetDestination`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AI.NavMeshAgent.SetDestination.html) | `public bool SetDestination(Vector3 target)` | Runtime | Requests a path; path calculation can remain pending. |
| [`UnityEngine.AI.NavMeshAgent.pathStatus`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AI.NavMeshAgent-pathStatus.html) | `public NavMeshPathStatus pathStatus { get; }` | Runtime | Status of the current path once available. |
| [`UnityEngine.AI.NavMesh.SamplePosition`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AI.NavMesh.SamplePosition.html) | `public static bool SamplePosition(Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, int areaMask)` | Runtime | Finds nearest NavMesh point within range/area mask. |
| [`Unity.AI.Navigation.NavMeshSurface`](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshSurface.html) | `public class NavMeshSurface : MonoBehaviour` | Package runtime/editor | AI Navigation 2.0 component that collects sources and builds/owns NavMesh data. |
| [`UnityEngine.XR.XRSettings.enabled`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/XR.XRSettings-enabled.html) | `public static bool enabled { get; set; }` | XR module | Legacy XR settings surface; modern package/provider initialization can supersede direct use. |

### Package prerequisites

- `com.unity.ai.navigation@2.0` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.ai.inference@2.6` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.ai.inference@2.6/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.xr.arfoundation@6.6` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.6/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.xr.arkit@6.6` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.xr.arkit@6.6/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.xr.arcore@6.6` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.xr.arcore@6.6/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.xr.openxr@1.17` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.17/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.xr.interaction.toolkit@3.5` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.5/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Unity Ai — https://docs.unity3d.com/6000.3/Documentation/Manual/unity-ai.html
- Com.Unity.Ai.Navigation — https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.ai.navigation.html
- Xr — https://docs.unity3d.com/6000.3/Documentation/Manual/XR.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Ai.Nav Mesh Agent — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AI.NavMeshAgent.html
- Ai.Nav Mesh Agent.Set Destination — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AI.NavMeshAgent.SetDestination.html
- Ai.Nav Mesh Agent Path Status — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AI.NavMeshAgent-pathStatus.html
- Ai.Nav Mesh Obstacle — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AI.NavMeshObstacle.html
- Ai.Nav Mesh.Sample Position — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AI.NavMesh.SamplePosition.html
- Unity.Ai.Navigation.Nav Mesh Surface — https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshSurface.html
- Com.Unity.Ai.Navigation@2.0 — https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html
- Com.Unity.Ai.Inference@2.6 — https://docs.unity3d.com/Packages/com.unity.ai.inference@2.6/manual/index.html
- Com.Unity.Xr.Arfoundation@6.6 — https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.6/manual/index.html
- Com.Unity.Xr.Arkit@6.6 — https://docs.unity3d.com/Packages/com.unity.xr.arkit@6.6/manual/index.html
- Com.Unity.Xr.Arcore@6.6 — https://docs.unity3d.com/Packages/com.unity.xr.arcore@6.6/manual/index.html
- Com.Unity.Xr.Openxr@1.17 — https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.17/manual/index.html
- Com.Unity.Xr.Interaction.Toolkit@3.5 — https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.5/manual/index.html
- Xr.Xrsettings Enabled — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/XR.XRSettings-enabled.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
