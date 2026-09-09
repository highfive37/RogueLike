# RogueLike

[![CI](https://github.com/highfive37/RogueLike/actions/workflows/ci.yml/badge.svg)](https://github.com/highfive37/RogueLike/actions/workflows/ci.yml)

A 3D quarter-view action roguelite in the style of *The Binding of Isaac* / *Enter the Gungeon* —
spatial rooms connected by doors, tight twin-stick-style movement and dodging, run-based structure.

Built with **Unity 6000.3.22f1** (URP) as a game-client portfolio project: the emphasis is a
compact, technically defensible game whose decisions can be explained in an interview, not the
largest possible scope. Development is done with Claude Code + Unity MCP, with every non-trivial
design decision recorded (and, where AI suggestions were rejected or reshaped, why).

## Highlights

- **Isaac-style room/door system** — floors are authored graphs of rooms pre-instantiated on a
  grid; room-to-room movement is a scripted camera transition, doors lock on entry to an
  uncleared room. Layout, sizes, door links and enemy composition are externalised to plain
  JSON (`Assets/StreamingAssets/Balance/Stage/Floors/FloorN.json`).
- **FSM enemy AI** — shared `EnemyStateMachine` (Idle / Patrol / Chase / Attack / Stunned /
  Dead) for regular enemies and the boss, differentiated by `ScriptableObject` data and
  polymorphic attack patterns (melee lunge, ranged kiting, burst).
- **Data-driven balance** — player base stats, passive/active item effects and drop tables load
  from text-editable JSON with built-in fallbacks.
- **Object pooling** for projectiles, enemies, boss and grenades, with benchmark scenes and a
  Profiler baseline.
- **Automated tests + CI** — EditMode + PlayMode suites (Unity Test Framework) run on every
  push via GitHub Actions / GameCI.

See [`Docs/`](Docs/) for the full picture: `ProjectPlan.md` (original plan),
`ClaudeSessionSummary.md` (current architecture snapshot), `DevLog.md` (dated work log),
`AI_Decision_Log.md` (design decisions with alternatives considered).

## Tests

- **In the Editor:** `Window > General > Test Runner` → run the *EditMode* and *PlayMode* tabs.
- **CI:** `.github/workflows/ci.yml` runs both on every push/PR to `main` (GameCI, Unity
  `6000.3.22f1`).

Test code lives under `Assets/Tests/`; the runtime code it exercises is in the `RogueLike`
assembly (`Assets/Scripts/RogueLike.asmdef`).
