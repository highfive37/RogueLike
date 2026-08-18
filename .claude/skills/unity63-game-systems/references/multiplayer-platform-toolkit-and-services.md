# Multiplayer, Platform Development, and Unity Services

## Scope

Separates networking model choice, authority, serialization, prediction, testing, platform adaptation, and optional Unity Services/packages in Unity 6.3.

Read this reference when the request involves:

- designing multiplayer architecture or choosing a Unity netcode stack
- testing multiple players locally or profiling network traffic
- integrating platform-specific capabilities or Unity Services

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **Multiplayer**
- **Netcode for GameObjects**
- **Netcode for Entities**
- **Multiplayer Tools**
- **Platform development**
- **Unity Services**
- **NetworkManager**
- **NetworkObject**
- **Application platform/capabilities**
- **package services**

## Verified Unity 6.3 facts

- Unity 6.3 multiplayer documentation covers netcode packages, transport, dedicated server, multiplayer play mode, tools, services, and solution selection. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/multiplayer.html))
- Netcode for GameObjects 2.13 is a released high-level networking package for GameObject/MonoBehaviour workflows in Unity 6.3. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- Netcode for Entities 1.14 is a separate released netcode stack for Entities-oriented workflows; its architecture and types are not interchangeable with Netcode for GameObjects. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- Multiplayer Tools 2.2 and Multiplayer Play Mode 2.0 are released packages for Unity 6.3. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- NetworkObject/NetworkBehaviour ownership, spawning, RPCs, replicated variables, connection lifecycle, and scene management follow Netcode for GameObjects package contracts. ([official source](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/manual/index.html))
- Unity platform development documentation defines target-specific build, player, graphics, input, filesystem, store, and deployment requirements. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/PlatformSpecific.html))
- Platform Toolkit 1.1 is a released Unity 6.3 package intended to help integrate platform-specific services behind a common workflow. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))
- Unity Services are optional online capabilities with service-specific packages, initialization, authentication, quotas, data, and operational requirements. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/UnityServices.html))
- A released package is not proof that it is installed or configured in the current project. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Choose the networking stack from the project’s object model, scale, prediction needs, authority model, target topology, team expertise, and service requirements—not from package name similarity.
- **Interpretation:** Define authority for every mutable networked fact. Clients send validated intent; the authority commits state and replication presents it according to latency/prediction policy.
- **Interpretation:** Keep network serialization contracts versioned and narrow. Do not expose arbitrary scene/component state as the protocol.
- **Interpretation:** Separate offline domain rules from network transport so server, host, client prediction, replay, and tests can reuse deterministic logic where appropriate.
- **Interpretation:** Use Multiplayer Play Mode for iteration and automated multi-instance tests, but also test real devices, network conditions, dedicated server builds, reconnect, migration, and service failure.
- **Interpretation:** Put store/platform/service SDKs behind adapters and package-constrained assemblies. The core game should receive capabilities/results, not vendor objects.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| GameObject/MonoBehaviour-oriented co-op or session game | NGO 2.13 after topology/feature verification | High-level GameObject netcode package. |
| Entities-oriented high-scale/predicted simulation | Netcode for Entities 1.14 | Different ECS-focused stack and workflow. |
| Local multi-player-instance iteration | Multiplayer Play Mode 2.0 | Faster workflows but not a complete real-network test. |
| Optional platform/store/cloud feature | Adapter assembly + exact package/service | Core remains portable and package presence explicit. |

## Risks and review checks

- Do not use both NGO and Netcode for Entities types in one feature without a deliberate bridge architecture.
- Do not trust client-reported authoritative state or purchases without server/service validation appropriate to the design.
- Do not send per-frame full object state when an event, compressed delta, lower tick rate, or local prediction meets the requirement.
- Do not assume host mode exposes dedicated-server ownership, timing, headless, and security issues.
- Do not let platform SDK types spread through gameplay assemblies.
- Do not assume a Unity Service is available offline, in every region/platform, or without initialization/authentication/failure handling.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`Unity.Netcode.NetworkManager`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.NetworkManager.html) | `public class NetworkManager : MonoBehaviour` | Package runtime | NGO 2.13 session, transport, spawn, scene, and connection coordination. |
| [`Unity.Netcode.NetworkObject`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.NetworkObject.html) | `public class NetworkObject : MonoBehaviour` | Package runtime | NGO identity, spawn state, ownership, and attached NetworkBehaviours. |
| [`Unity.Netcode.NetworkBehaviour`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.NetworkBehaviour.html) | `public abstract class NetworkBehaviour : MonoBehaviour` | Package runtime | Base for NGO replicated/RPC behavior attached under NetworkObject. |
| [`UnityEngine.Application.platform`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-platform.html) | `public static RuntimePlatform platform { get; }` | Runtime | Runtime platform identity at an adapter/composition boundary. |
| [`UnityEngine.SystemInfo`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SystemInfo.html) | `public static class SystemInfo` | Runtime | Runtime hardware/capability queries. |

### Package prerequisites

- `com.unity.netcode.gameobjects@2.13` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.netcode@1.14` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.netcode@1.14/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.multiplayer.tools@2.2` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.multiplayer.tools@2.2/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.multiplayer.playmode@2.0` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@2.0/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- `com.unity.platformtoolkit@1.1` — documented as a Unity 6.3 released package. [Package documentation](https://docs.unity3d.com/Packages/com.unity.platformtoolkit@1.1/manual/index.html); [Unity 6.3 compatibility evidence](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html).
- Do not treat these package APIs as installed in every project. Confirm the package in `Packages/manifest.json` before using its types.

## Official sources

- Multiplayer — https://docs.unity3d.com/6000.3/Documentation/Manual/multiplayer.html
- Platform Specific — https://docs.unity3d.com/6000.3/Documentation/Manual/PlatformSpecific.html
- Unity Services — https://docs.unity3d.com/6000.3/Documentation/Manual/UnityServices.html
- Pack Safe — https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html
- Com.Unity.Netcode.Gameobjects@2.13 — https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/manual/index.html
- Com.Unity.Netcode@1.14 — https://docs.unity3d.com/Packages/com.unity.netcode@1.14/manual/index.html
- Com.Unity.Multiplayer.Tools@2.2 — https://docs.unity3d.com/Packages/com.unity.multiplayer.tools@2.2/manual/index.html
- Com.Unity.Multiplayer.Playmode@2.0 — https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@2.0/manual/index.html
- Com.Unity.Platformtoolkit@1.1 — https://docs.unity3d.com/Packages/com.unity.platformtoolkit@1.1/manual/index.html
- Unity.Netcode.Network Manager — https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.NetworkManager.html
- Unity.Netcode.Network Object — https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.NetworkObject.html
- Unity.Netcode.Network Behaviour — https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.NetworkBehaviour.html
- Application Platform — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-platform.html
- System Info — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SystemInfo.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
