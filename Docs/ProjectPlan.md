# Project Plan — 3D Arena Roguelite

## 1. Project Goal

Build a small-scale **3D quarter-view action roguelite** in Unity using Claude Code and Unity MCP.

The project is intended not only to produce a playable game, but also to serve as a **game client developer portfolio project** that demonstrates:

- C# / Unity fundamentals
- System design and architecture
- AI-assisted development workflow
- Profiling and optimization
- Ability to explain technical decisions in interviews

The goal is not to make the largest possible game. The priority is to complete a compact, polished, technically defensible project.

---

## 2. Game Concept

### Genre
3D quarter-view action roguelite.

### Starting Character / Weapon Type
The player's initial combat style will depend on the character selected at the start of a run.

- **Ranged character** — starts with a gun-type ranged weapon.
- **Melee character** — starts with a sword-type melee weapon.

The two starting archetypes should share the same overall game loop while providing clearly different attack ranges, positioning requirements, and combat feel.

### Core Flow
- Select a starting character and weapon archetype.
- Enter a room.
- Fight enemies directly using movement, aiming, attacks, skills, and dodge.
- Clear the room.
- Choose an upgrade or weapon/skill enhancement.
- Move to the next room.
- Continue until reaching the boss and ending the run.

### Target Playtime
Approximately **15–20 minutes per run**.

---

## 3. Target Content Scope

The planned game scope is:

- 1 player character
- 1 basic attack
- Multiple collectible skill items, with **1 skill item equipped and usable during a stage at a time**
- Dodge / dash
- 4–5 normal enemy types
- 1–2 elite enemy types
- 1 boss
- 5–10 stages or rooms
- Upgrade selection system
- HP / skill UI
- Sound and effects
- Settings
- Pause
- Game over
- A complete playable flow from start to ending

The project should avoid excessive content expansion. Technical quality and completion are more important than quantity.

---

## 4. Core Controls

Initial prototype controls:

- **WASD** — movement
- **Mouse** — aim
- **LMB** — basic attack
- **E** — equipped skill item
- **Space** — dodge

The first implementation should use simple primitives such as capsules and cubes before replacing them with art assets.

### Skill Item System

The player may own multiple skill items, but only **one skill item can be equipped and used during a stage**.

- The equipped skill is activated with the **E key**.
- The player starts the prototype with **Grenade equipped by default**.
- Skill items cannot be freely swapped during an active stage in the final gameplay design.
- After clearing a stage and before the next stage begins, the player will eventually be able to change the equipped skill through a dedicated **Skill Item Swap UI**.
- The exact UI and stage-transition flow will be implemented later.
- A **skill level system** is planned, but its detailed rules, upgrade method, scaling, and maximum level are not yet decided.

#### Prototype-Only Skill Switching

For prototype testing only, temporary number-key shortcuts will be available to swap the currently equipped skill without using the future Skill Item Swap UI.

- **1** — equip **Enhanced Shot**
- **2** — equip **Grenade**
- **3** — equip **Flamethrower**

These shortcuts are strictly a development/prototype convenience and are **not part of the intended final gameplay flow**. They should be removed or disabled once the proper between-stage Skill Item Swap UI is implemented.

#### Planned Skill Item — Grenade

The Grenade skill uses a two-step E-key interaction.

1. Press **E** once:
   - The player enters a grenade-ready state and holds the grenade while continuing to aim.
   - The grenade's **2.5-second explosion timer starts immediately at this moment**, not when the grenade is thrown.
2. Press **E** again:
   - The grenade is thrown in the direction the player is currently facing.
   - The throw follows a simple parabolic arc and travels approximately **2.5 meters**.
3. Explosion:
   - The grenade explodes when **2.5 seconds have elapsed since the first E press**, even if part of that time was spent holding it.
   - The explosion deals **15 damage** to enemies within approximately **1 meter** of the grenade.
4. Cooldown:
   - After the grenade is thrown, the skill enters a **3-second cooldown**.

Prototype values such as throw distance, explosion radius, damage, fuse time, and cooldown may be adjusted later through playtesting.

#### Planned Skill Item — Enhanced Shot

The Enhanced Shot skill is a simple single-projectile skill intended to provide a straightforward skill option alongside more complex skills.

- Press **E**:
  - The player temporarily uses a dedicated skill weapon and fires **one non-piercing enhanced projectile** in the direction the player is currently facing.
- Projectile behavior:
  - The projectile travels forward in a straight line.
  - It stops at the **first enemy it collides with**.
  - It deals **12 damage** to that enemy.
  - The projectile does not pierce through enemies.
- Cooldown:
  - The cooldown starts immediately after the shot is fired.
  - Cooldown duration: **2 seconds**.

This skill does not need to be included in the earliest prototype milestone, but should remain part of the planned skill-item pool because it provides a deliberately simple implementation compared with skills such as Grenade and Flamethrower.

#### Planned Skill Item — Flamethrower

The Flamethrower skill is a hold-to-use skill.

- Hold **E**:
  - The player equips/uses the flamethrower while the key remains held.
  - The flamethrower attacks in the direction the player is facing.
  - The final effect is intended to be fan/cone-shaped.
  - During the prototype phase, the damage area may be represented by a simple **rectangular zone**.
- Damage:
  - Enemies inside the active area take **3 damage per second**.
- Maximum duration:
  - The flamethrower can remain active for up to **7 seconds** per activation.
- End conditions:
  - If the player releases **E** before 7 seconds, the flamethrower ends immediately.
  - If 7 seconds elapse while E is still held, the flamethrower ends automatically.
  - When the skill ends, the player returns to the normal combat state.

The cooldown/recharge behavior after Flamethrower use has **not yet been finalized** and should not be assumed until explicitly decided.

---

## 5. Suggested Project Architecture

Initial high-level structure:

```text
Game
├─ Core
│  ├─ GameManager
│  ├─ SceneManager
│  └─ ObjectPool
│
├─ Character
│  ├─ Player
│  ├─ Enemy
│  └─ Stats
│
├─ Combat
│  ├─ DamageSystem
│  ├─ SkillSystem
│  └─ StatusEffect
│
├─ AI
│  ├─ StateMachine
│  ├─ EnemyStates
│  └─ BossStates
│
├─ Data
│  ├─ ScriptableObject
│  └─ GameData
│
└─ UI
```

This structure is a starting point, not a rigid requirement. Avoid unnecessary abstraction and over-engineering.

---

## 6. Portfolio-Focused Technical Features

The project should include the following technical elements where they are justified by the game.

### 6.1 FSM-Based Enemy AI

Normal enemy behavior can use states such as:

```text
Idle
↓
Patrol
↓
Chase
↓
Attack
↓
Stunned / Dead
```

Boss behavior can use phase-based FSM logic such as:

```text
Phase 1
↓ HP 60%
Phase 2
↓ HP 30%
Phase 3
```

The implementation should remain understandable enough to explain why FSM was selected instead of alternatives such as Behavior Trees.

---

### 6.2 ScriptableObject-Based Data

Avoid hard-coding weapon and skill values directly into behavior scripts.

Example:

```text
SkillData
- Damage
- Cooldown
- Range
- Icon
- Effect
- Prefab
```

Skill data must support multiple skill-item behaviors while preserving the rule that only **one skill item is equipped for active use during a stage**. Do not force all skills into identical fields if their behaviors differ significantly; the final data/behavior architecture should be chosen when the skill system is formally designed.

The project should make it possible to explain why ScriptableObject was chosen, including its advantages and limitations.

---

### 6.3 Object Pooling

Potential pooling targets:

- Projectiles
- Damage numbers
- VFX
- Enemies

The project should eventually compare performance before and after pooling.

Suggested benchmark cases:

- 20 enemies
- 50 enemies
- 100 enemies
- 200 enemies

Measure actual results such as FPS, CPU load, and GC Alloc instead of inventing numbers.

---

### 6.4 Addressables and Asynchronous Loading

Potential usage:

```text
Lobby
↓
Loading
↓
Gameplay
```

Technical topics to demonstrate:

- Async / Await
- Resource management
- Resource release
- Loading UI

Addressables should be introduced when the project reaches a stage where resource loading architecture is useful, rather than added prematurely.

---

### 6.5 Unity Profiler-Based Optimization

Profiling and optimization should be treated as a required portfolio step.

Potential optimization topics:

- Object pooling
- AI tick distribution
- GetComponent caching
- Reducing unnecessary Update calls
- GC allocation reduction
- Resource loading / unloading

Record actual before-and-after measurements.

Example format:

```text
Before

Enemy 200
FPS : [measured value]
GC Alloc : [measured value]

↓

Optimization
- Object Pool
- AI Tick distribution
- GetComponent caching

↓

After

Enemy 200
FPS : [measured value]
GC Alloc : [measured value]
```

---

## 7. Additional Systems to Consider

As the project grows, consider adding:

- Input System
- Event System
- Save System
- Shader / VFX
- Performance profiling
- Optimization

These should be added according to development needs rather than inserted only to increase the feature count.

---

## 8. Development Workflow with Claude Code

Claude Code should be used as a development tool, not as a replacement for engineering judgment.

Preferred workflow:

```text
Define requirements
↓
Ask Claude for a design proposal
↓
Review the proposed design
↓
Implement
↓
Test with Unity / Profiler
↓
Identify problems
↓
Revise with Claude
↓
Refactor
```

Avoid prompts such as:

> Build the entire game for me.

Prefer incremental requests such as:

> Review the current project and propose a Player movement architecture. Explain the required GameObjects, Components, and scripts before modifying files.

Then review the proposal before asking Claude to implement it.

---

## 9. AI-Assisted Development Responsibilities

### Claude Code

Claude can assist with:

- Repetitive code generation
- Refactoring suggestions
- Code review
- Test code drafts
- Profiler result analysis
- Unity Editor operations through Unity MCP

### Developer

The developer remains responsible for:

- System design
- Requirement definition
- Reviewing generated code
- Architecture decisions
- Unity debugging
- Performance measurement
- Optimization
- Understanding and explaining the final implementation

The portfolio should show that AI output was critically reviewed rather than accepted blindly.

---

## 10. Example of Critical AI Review

Example scenario:

```text
Problem

Enemy directly depends on Player,
making it difficult to add new targets.

Claude proposal
↓
Introduce ITarget interface

Developer review
↓
Too much abstraction for the current project size

Revision
↓
Use a simpler TargetProvider structure
```

This kind of decision is valuable because it demonstrates engineering judgment rather than simple AI-generated implementation.

---

## 11. Development Phases

### Phase 1 — Primitive Prototype

Use capsules, cubes, and simple materials.

Implement:

1. Player GameObject
2. Quarter-view camera
3. WASD movement
4. Mouse aiming
5. Basic attack
6. Enemy GameObject
7. Enemy chase behavior
8. Damage and hit handling
9. Enemy death
10. Basic room clear flow

The priority is obtaining the smallest playable combat loop.

---

### Phase 2 — Core Gameplay Systems

Add:

- Dodge / dash
- Single-slot equipped Skill Item system
- Enemy FSM
- Multiple enemy types
- Upgrade selection
- Room progression
- Basic UI
- Boss prototype

---

### Phase 3 — Data and Architecture

Introduce where appropriate:

- ScriptableObject data
- Shared stats
- Object pooling
- Event-based communication
- Save structure
- Project folder cleanup / refactoring

---

### Phase 4 — Content and Presentation

Replace primitives with suitable free or low-cost assets.

Add:

- Character / enemy art
- Animation
- VFX
- Sound
- Environment assets
- UI polish

Prefer a consistent visual style rather than mixing unrelated asset packs.

---

### Phase 5 — Loading and Resource Management

Add where useful:

- Addressables
- Async loading
- Loading UI
- Resource release logic

---

### Phase 6 — Profiling and Optimization

Use Unity Profiler to identify real bottlenecks.

Test scenarios such as increasing enemy counts and compare before/after results.

Document:

- Problem
- Measurement
- Cause
- Chosen solution
- Result

---

### Phase 7 — Portfolio Documentation

Prepare project documentation that explains:

- Game overview
- Architecture
- Key systems
- Technical decisions
- Optimization results
- Claude Code + Unity MCP workflow
- Cases where AI suggestions were rejected or modified
- Lessons learned

---

## 12. Interview Topics the Project Should Support

The final project should make it possible to answer questions such as:

- Why did you use Object Pooling?
- Why did you reduce Update usage?
- What are the disadvantages of ScriptableObject?
- How does Addressables reference counting work?
- Why was GC Alloc occurring?
- Why did you choose FSM instead of a Behavior Tree?
- How did you validate code generated by Claude?
- How did you identify and optimize bottlenecks?

The goal is to answer these questions using concrete examples from the project.

---

## 13. Project Principle

The project should aim for:

> Use AI to design, implement, measure, and optimize a small but complete game architecture.

Rather than:

> Use AI to make an impressive-looking game as quickly as possible.

A completed, technically explainable game is more valuable for this portfolio goal than a large but unfinished project.
