# 3D and 2D Physics Systems

## Scope

Covers Rigidbody/Rigidbody2D simulation, colliders, triggers, queries, layers, joints, character movement, fixed timing, and performance boundaries without mixing 3D and 2D APIs.

Read this reference when the request involves:

- implementing collisions, triggers, Rigidbody motion, raycasts, or character movement
- choosing 3D versus 2D physics APIs
- debugging unstable simulation or physics performance

## Contents

- [Core concepts](#core-concepts)
- [Verified Unity 6.3 facts](#verified-unity-63-facts)
- [Official-fact-based design and implementation interpretation](#official-fact-based-design-and-implementation-interpretation)
- [Selection criteria](#selection-criteria)
- [Risks and review checks](#risks-and-review-checks)
- [Related official API](#related-official-api)
- [Official sources](#official-sources)

## Core concepts

- **3D Physics**
- **2D Physics**
- **fixed simulation**
- **queries and collision filtering**
- **Rigidbody**
- **Collider**
- **Physics**
- **Rigidbody2D**
- **Physics2D**

## Verified Unity 6.3 facts

- Unity provides separate 3D and 2D physics systems with different component and API families; their colliders and rigidbodies do not interact across systems. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/PhysicsSection.html))
- A Rigidbody enables 3D physics simulation for a GameObject and a Collider defines collision shape; static, kinematic, and dynamic roles have different movement expectations. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/PhysicsSection.html))
- Rigidbody2D and Collider2D serve the 2D physics simulation with 2D-specific body types, joints, queries, and settings. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/2d-physics/2d-physics.html))
- Physics simulation normally advances at fixed timesteps and can execute a different number of steps than rendered frames. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html))
- Collision layers and query masks determine which objects interact or are returned by queries. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.html))
- Trigger colliders report overlap events without ordinary collision response when the documented Rigidbody/collider conditions are met. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Collider-isTrigger.html))
- Physics queries provide allocating and non-allocating forms with explicit layer and trigger-query options. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.Raycast.html))
- Continuous collision detection, solver iterations, fixed timestep, broadphase, and collider complexity trade accuracy against CPU cost. ([official source](https://docs.unity3d.com/6000.3/Documentation/Manual/physics-optimization.html))
- Manual simulation is available through PhysicsScene/PhysicsScene2D when automatic simulation and ownership are deliberately controlled. ([official source](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PhysicsScene.Simulate.html))

## Official-fact-based design and implementation interpretation

The statements in this section are engineering interpretations derived from the cited official behavior; they are not presented as additional Unity guarantees.

- **Interpretation:** Choose one physics dimensionality for a gameplay interaction. Visual 3D presentation does not require 3D physics if the game rules are intentionally 2D.
- **Interpretation:** Move dynamic bodies through forces/velocity and kinematic bodies through the documented movement APIs in fixed time; avoid Transform teleports as the default.
- **Interpretation:** Centralize layer definitions and query masks so collision policy is reviewable rather than encoded as magic integers.
- **Interpretation:** Use primitive/compound colliders where possible and reserve MeshCollider complexity for measured needs and supported body configurations.
- **Interpretation:** Treat collision callbacks as observations of simulation results. Queue domain commands when modifying shared state would make callback ordering fragile.
- **Interpretation:** Profile contact counts, active bodies, broadphase, solver cost, queries, and fixed-step catch-up before reducing accuracy globally.

## Selection criteria

| Situation | Prefer | Reason / boundary |
|---|---|---|
| Full 3D movement/collision | Rigidbody + 3D Collider/Physics | Uses the 3D solver/API family. |
| Planar 2D game rules | Rigidbody2D + Collider2D/Physics2D | Uses the 2D solver/API family. |
| Player capsule requiring explicit non-Rigidbody controller semantics | CharacterController after reviewing constraints | Move API reports collision flags; gravity is user code. |
| High-frequency bounded query | NonAlloc query + owned result buffer | Avoid recurring allocations and handle capacity. |

## Risks and review checks

- Do not attach both 2D and 3D colliders expecting cross-system collisions.
- Do not move a dynamic Rigidbody by Transform every frame.
- Do not modify physics state from arbitrary async/background threads.
- Do not ignore the return count/capacity of NonAlloc queries.
- Do not set the fixed timestep extremely small to hide tunneling without measuring CPU catch-up and choosing collision detection appropriately.
- Do not assume collision callback order across objects defines game authority.

## Related official API

| API | Signature or surface | Scope | Use and constraints |
|---|---|---|---|
| [`UnityEngine.Rigidbody.AddForce`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.AddForce.html) | `public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)` | Runtime | Applies force under the selected force-mode semantics. |
| [`UnityEngine.Rigidbody.MovePosition`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.MovePosition.html) | `public void MovePosition(Vector3 position)` | Runtime | Moves a kinematic Rigidbody through the physics system. |
| [`UnityEngine.Physics.Raycast`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.Raycast.html) | `public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)` | Runtime | Performs a 3D ray query; overloads differ in returned data. |
| [`UnityEngine.Physics.OverlapSphereNonAlloc`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.OverlapSphereNonAlloc.html) | `public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, int layerMask = AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)` | Runtime | Writes overlapping colliders to caller buffer. |
| [`UnityEngine.Rigidbody2D.AddForce`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody2D.AddForce.html) | `public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Force)` | Runtime | Applies force in the 2D simulation. |
| [`UnityEngine.Physics2D.Raycast`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics2D.Raycast.html) | `public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance = Mathf.Infinity, int layerMask = DefaultRaycastLayers)` | Runtime | Performs a 2D ray query; overloads support filters and result buffers. |
| [`UnityEngine.CharacterController.Move`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html) | `public CollisionFlags Move(Vector3 motion)` | Runtime | Moves the controller by an absolute motion delta and does not apply gravity automatically. |

## Official sources

- Physics Section — https://docs.unity3d.com/6000.3/Documentation/Manual/PhysicsSection.html
- 2D Physics — https://docs.unity3d.com/6000.3/Documentation/Manual/2d-physics/2d-physics.html
- Physics Optimization — https://docs.unity3d.com/6000.3/Documentation/Manual/physics-optimization.html
- Execution Order — https://docs.unity3d.com/6000.3/Documentation/Manual/execution-order.html
- Rigidbody — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.html
- Rigidbody.Add Force — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.AddForce.html
- Rigidbody.Move Position — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.MovePosition.html
- Collider — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Collider.html
- Collider Is Trigger — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Collider-isTrigger.html
- Physics — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.html
- Physics.Raycast — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.Raycast.html
- Physics.Overlap Sphere Non Alloc — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.OverlapSphereNonAlloc.html
- Physics Scene.Simulate — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PhysicsScene.Simulate.html
- Rigidbody2D — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody2D.html
- Rigidbody2D.Add Force — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody2D.AddForce.html
- Physics2D — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics2D.html
- Physics2D.Raycast — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics2D.Raycast.html
- Character Controller.Move — https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html

## Evidence classification

- Sections labeled **Verified Unity 6.3 facts** and **Related official API** are target-version official-document facts.
- Sections labeled **Interpretation** are design or implementation judgments derived from those facts.
- Behavior not supported by the listed sources is outside this reference and must be verified before use.
