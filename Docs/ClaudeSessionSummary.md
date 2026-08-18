# Claude Session Summary — RogueLike (3D Arena Roguelite)

이 문서는 Claude Code + Unity MCP로 진행한 개발 세션의 결과를 나중에 다시 봐도
이해할 수 있도록 정리한 요약이다. 대화 원문이 필요하면 `Docs/talk_history.txt`를
참고. 원본 기획 문서는 `Docs/ProjectPlan.md`.

---

## 1. 프로젝트 목표

- **장르**: 3D 쿼터뷰 액션 로그라이트
- **목적**: 완성도 있는 게임 자체보다, **게임 클라이언트 개발자 포트폴리오**로서
  기술적 의사결정을 설명할 수 있는 작지만 완결된 프로젝트를 만드는 것
- **핵심 루프**: 캐릭터/무기 아키타입 선택 → 방 입장 → 전투 → 클리어 → 업그레이드
  선택 → 다음 방 → 보스 → 런 종료 (플레이타임 15~20분)
- **원칙**: 기술 요소(FSM, ScriptableObject, Object Pooling, Addressables, Profiler
  최적화 등)를 한꺼번에 넣지 않고, 실제로 필요해지는 시점에 단계적으로 도입한다.
  AI(Claude) 제안을 그대로 수용하지 않고 비판적으로 검토했는지가 포트폴리오
  평가 포인트 중 하나.

---

## 2. 지금까지 결정한 구조

### 프로젝트 환경 (확인된 사실)
- Unity 6000.3.22f1 (Unity 6.3), URP 17.3.0
- New Input System 1.20.0 설치 (단, 현재는 Input Actions 에셋을 쓰지 않고
  `Keyboard.current` / `Mouse.current` 저수준 폴링 사용 — 아래 4번 참고)
- Cinemachine 미설치, AI Navigation 설치되어 있으나 아직 미사용

### 폴더 구조
```
Assets/Scripts/
├─ Player/    (PlayerMovement, PlayerAim, PlayerCombat)
├─ Enemy/     (EnemyChase)
├─ Combat/    (IDamageable, Health, HealthComponent, Projectile)
├─ Camera/    (QuarterViewCameraFollow)
├─ Room/      (RoomManager)
└─ Pooling/   (GameObjectPool, PooledObject)
```
asmdef(어셈블리 분리)는 아직 도입하지 않음 — 스크립트 규모상 과설계로 판단.

### 씬 구성 (SampleScene)
Ground(바닥 기준면), Player, Enemy_Basic(프리팹 인스턴스), Main Camera,
RoomManager, ProjectilePool, EnemyPool.

### 프리팹
- `Assets/Prefabs/Projectile_Basic.prefab` — Rigidbody(kinematic, no gravity),
  SphereCollider(trigger), `Projectile`, `PooledObject`
- `Assets/Prefabs/Enemy_Basic.prefab` — Rigidbody(중력 없음, Y position +
  X/Z rotation freeze), `HealthComponent`, `EnemyChase`, `PooledObject`

### 태그
`Player`(기본 태그), `Enemy`(신규 추가) — 상호 탐색(`FindGameObjectWithTag`)에 사용.

### 클래스 책임 요약
| 클래스 | 책임 |
|---|---|
| `PlayerMovement` | WASD 입력 → Rigidbody.MovePosition (FixedUpdate) |
| `PlayerAim` | 마우스 → 지면 레이캐스트 → Rigidbody.MoveRotation |
| `PlayerCombat` | WeaponType(Melee/Ranged) 분기, 근접 OverlapSphere / 원거리 투사체 |
| `EnemyChase` | Player 방향으로 단순 추적 이동 (FSM 아님) |
| `IDamageable` / `Health` / `HealthComponent` | 데미지 판정·체력 상태·사망 이벤트 |
| `Projectile` | 직선 이동, 충돌 시 데미지, 수명 만료 시 풀 반환 |
| `QuarterViewCameraFollow` | LateUpdate 오프셋 추적 |
| `RoomManager` | Enemy 사망 이벤트 구독, 전멸 시 Room Cleared |
| `GameObjectPool` / `PooledObject` | Unity 내장 `ObjectPool<GameObject>` 기반 재사용 풀 |

---

## 3. Stage 1~3 계획과 진행 상황

원래 `ProjectPlan.md`의 Phase 1~7을 그대로 따르지 않고, **프로토타입 → 핵심 시스템
→ 최적화** 3단계로 재구성해서 진행 중.

### Stage 1 — 프로토타입 (✅ 완료, U1~U15)
최소 플레이 가능한 전투 루프. Player 이동/조준/근접·원거리 공격, Enemy 추적,
데미지·사망 처리, Room Cleared 판정까지 구현 완료.
추가로 원래 Stage 3 항목이었던 **오브젝트 풀링(Projectile, Enemy)** 을 사용자
요청으로 앞당겨 Stage 1에서 함께 구현함.

### Stage 2 — 핵심 시스템 (⏳ 미착수)
계획된 범위:
- Dodge/Dash, Skill(Q/E) 시스템
- `EnemyStateMachine`(FSM: Idle→Patrol→Chase→Attack→Stunned/Dead), `BossStateMachine`
- `WeaponData`/`SkillData`/`EnemyData`/`CharacterArchetypeData` (ScriptableObject)
- `IWeapon`/`MeleeWeapon`/`RangedWeapon` 다형성 구조로 승격 (현재는 `PlayerCombat`
  내 단순 enum 분기)
- `CharacterSelectController` (런 시작 시 Ranged/Melee 아키타입 선택)
- 여러 종류의 Enemy(데이터만 다르고 스크립트는 공유), Boss 프로토타입
- `RoomFlowManager`(다중 방 진행), `UpgradeManager`, HUD/업그레이드 선택 UI

### Stage 3 — 최적화 (부분 착수)
- ✅ Object Pooling (Projectile, Enemy) — Stage 1에서 조기 구현 완료
- ⏳ 나머지: AI tick 분산(`EnemyAIManager`), GetComponent 캐싱 정리,
  `BenchmarkSpawner`(20/50/100/200 시나리오), Profiler 기반 Before/After 측정 문서화

Phase 4(아트/연출), Phase 5(Addressables), Phase 7(포트폴리오 문서화)는 이
3단계 밖의 별도 트랙으로 남겨둠 — 필요해지는 시점에 진행.

---

## 4. 지금까지 선택한 설계 방향과 이유

- **이동/회전은 Rigidbody + MovePosition/MoveRotation**: Update에서 입력을
  샘플링하고 FixedUpdate에서 소비하는 패턴. CharacterController 대신 선택한
  이유는 향후 넉백 등 물리 상호작용 확장이 쉬워서.
- **Input Actions 에셋 대신 저수준 폴링(`Keyboard.current`/`Mouse.current`)**:
  Stage 1에서는 액션 에셋을 formalize하지 않고 직접 폴링. 일관성을 위해 이동과
  조준 모두 동일한 방식 사용. Input Actions 에셋 전환은 Stage 2 이후 검토.
- **무기 타입은 아직 단순 enum 분기**: `PlayerCombat` 안에 `WeaponType` enum과
  if/switch만 존재. `IWeapon` 다형성 구조는 스킬/여러 무기가 실제로 필요해지는
  Stage 2에서 도입 — Stage 1 시점에 미리 만드는 것은 과설계로 판단.
  (`Docs/ProjectPlan.md` §10의 "필요 이상의 추상화 지양" 원칙 적용 사례)
  아키타입(Ranged/Melee) 선택 UI/데이터도 아직 없음 — 지금은 Inspector에서
  `WeaponType` 값을 수동으로 바꿔 테스트하는 수준.
- **`Health`를 plain C# 클래스로 분리**: `MonoBehaviour`에 직접 로직을 두지 않고
  UnityEngine 비의존 클래스로 만들어 EditMode 테스트가 가능하도록 함.
  `HealthComponent`는 이를 감싸는 얇은 어댑터 역할만.
- **이벤트 기반 결합도 관리**: `HealthComponent`와 `RoomManager`는 서로를 직접
  참조하지 않고 `Health.Died` 이벤트로만 연결됨.
- **오브젝트 풀링을 Stage 1로 앞당김**: 원래 Stage 3(최적화) 항목이었으나,
  사용자가 발사체/적 파괴 시 `Destroy` 대신 재사용을 명시적으로 요청하여 즉시
  반영. Unity 6.3 내장 `UnityEngine.Pool.ObjectPool<T>`를 감싸는 범용
  `GameObjectPool` 컴포넌트 하나로 Projectile/Enemy 모두 처리하도록 설계
  (타입별로 각각 만들지 않음).
- **`destroyOnDeath` 플래그를 폴백으로 유지**: `PooledObject`가 없는 오브젝트
  (현재는 Player)는 기존처럼 `destroyOnDeath` bool로 제어. Player는 아직 Game
  Over 흐름이 없어 `false`로 고정해 사라지지 않게 해둠.

---

## 5. 아직 결정하지 않은 항목

- **Input Actions 에셋 formalize 시점**: 계속 저수준 폴링을 쓸지, Stage 2에서
  `InputSystem_Actions.inputactions`를 정식으로 구성할지 미정.
- **무기 다형성 구조 설계**: `IWeapon`/`WeaponData` 도입 시 상속 vs 컴포지션,
  근접·원거리 외 스킬(Q/E)과의 관계 미정.
- **Enemy가 Player를 공격하는 로직**: 현재 `EnemyChase`는 추적만 하고 공격하지
  않음 — 데미지를 주는 경로가 없어 Player의 `destroyOnDeath=false` 설정이 아직
  실제로 검증되지 않은 상태.
- **Game Over 흐름**: Player 사망 시 어떻게 처리할지(씬 재시작? UI?) 미정.
- **`HealthComponent` 재사용 시 체력 리셋**: 풀에서 꺼내 재사용할 때 HP를 초기화하는
  로직이 없음. Stage 1에는 적 스포너가 없어 문제가 드러나지 않지만, Stage 2에서
  방마다 적을 다시 스폰하면 필요해짐(`ResetHealth()` 같은 메서드 추가 예정).
- **Room 진행/멀티룸 흐름 세부 설계**: 지금의 `RoomManager`는 로그만 남기는
  수준이며, 여러 방을 어떻게 연결/전환할지 미정.
- **카메라 시스템에 Cinemachine 도입 여부**: 지금은 수동 스크립트로 충분하지만
  연출이 복잡해지면(줌, 화면 흔들림 등) 재검토 필요.
- **업그레이드 선택 시스템의 데이터 구조**: `UpgradeManager`/`UpgradeOption`의
  구체적 설계는 아직 착수 전.

---

## 6. 앞으로 해야 할 작업 (우선순위 순)

1. **Enemy 공격 로직 추가** — Player가 실제로 데미지를 받는 경로를 만들어야
   `destroyOnDeath`/Game Over 관련 결정을 검증할 수 있음
2. **Stage 2 착수**: Dodge/Dash → Skill 시스템 → `WeaponData`/`IWeapon`
   다형성 구조로 무기 시스템 정식화 → `EnemyStateMachine`(FSM) → 다중 Enemy
   타입 → Boss 프로토타입 → Room 진행/업그레이드 선택 → 기본 UI(HUD)
3. **`HealthComponent` 재사용 리셋 로직** 추가 (Stage 2의 적 스포너 도입과 함께)
4. **Input Actions 에셋 도입 여부 재검토** — 스킬/대시 등 입력이 늘어나는
   시점에 저수준 폴링을 유지할지 정식 액션 에셋으로 전환할지 결정
5. **Stage 3 잔여 항목**: `EnemyAIManager`(AI tick 분산), `GetComponent` 캐싱
   정리, `BenchmarkSpawner`(20/50/100/200 적), Profiler 기반 Before/After 측정
6. **Phase 4/5/7** (아트 교체, Addressables, 포트폴리오 문서화)는 위 단계들이
   안정화된 이후 별도로 진행
