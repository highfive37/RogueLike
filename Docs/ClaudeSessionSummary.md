# Claude Session Summary — RogueLike (3D Arena Roguelite)

이 문서는 Claude Code + Unity MCP로 진행한 개발의 **현재 구조 스냅샷**이다.
날짜별 작업 사실은 `Docs/DevLog.md`, 개별 설계 결정의 대안/근거는
`Docs/AI_Decision_Log.md`를 참고. 원본 기획 문서는 `Docs/ProjectPlan.md`.
각 문서의 역할 구분은 `Docs/DocumentationGuide.md` 참고.

이 문서는 매 세션 이어 붙이는 로그가 아니라, 구조가 크게 바뀌었을 때 **통째로
다시 써서** 갱신하는 문서다(`DocumentationGuide.md` 참고). 이전 버전은 2026-08-24
(Stage 2 완료 시점) 기준으로 방치되다 2026-08-31에 재작성됐고, 이번이 두 번째
재작성이다.

**마지막 재작성: 2026-09-18**, 2026-09-17 세션 종료 시점(커밋 `341b0e6`)의
프로젝트 상태를 기준으로 함 — 절차적 던전 생성·그리드 A*·방 공격 슬롯·Phase 4
아트/애니메이션·보스 AI 확정·VFX 풀링·§6.5 Before/After 프로파일링까지 전부
반영. 다음 재작성은 구조가 다시 크게 바뀔 때(예: 세이브/로드 도입, 4층 이상
확장 등).

**부분 갱신: 2026-10-02** — 커밋 `775511c`(2026-09-30)의 적 경로탐색/추격 정책
변경만 해당 항목에 반영(파인딩 그리드 1유닛 세분화, Chase 상태 항상 경로 추종,
전 적 실시간 위치 추적 + 12유닛 포기 거리, 커넥터 길이 축소). 나머지 내용은
2026-09-18 재작성 기준 그대로.

---

## 1. 프로젝트 목표

- **장르**: 3D 쿼터뷰 액션 로그라이트 — **3D 바인딩 오브 아이작 / 엔터 더 건전**
  계열 (유저가 2026-09-09에 명시적으로 선언: 문으로 연결된 공간형 방, 절차적
  생성 층, 타이트한 트윈스틱식 이동/회피, 런 기반). `ProjectPlan.md` §2
  Genre에도 반영.
- **목적**: 완성도 있는 게임 자체보다, **게임 클라이언트 개발자 포트폴리오**로서
  기술적 의사결정을 설명할 수 있는 작지만 완결된 프로젝트를 만드는 것.
- **핵심 루프**: 캐릭터/무기 아키타입 선택(타이틀) → 방 입장 → 전투 → 클리어
  (문 해제) → 다음 방으로 카메라 연출 전환 → 보스 → 런 종료(플레이타임
  15~20분). "업그레이드 선택" 단계는 폐기되고 바닥 드롭/보장 픽업으로
  대체됨(§2·§4 참고) — 그 외 핵심 루프는 `ProjectPlan.md` 원안과 동일.
- **원칙**: 기술 요소(FSM, ScriptableObject, Object Pooling, Profiler 최적화
  등)를 한꺼번에 넣지 않고 실제로 필요해지는 시점에 단계적으로 도입한다. AI
  제안을 그대로 수용하지 않고 비판적으로 검토했는지가 포트폴리오 평가
  포인트 중 하나 — 근거는 `AI_Decision_Log.md`.

---

## 2. 현재 구조

### 프로젝트 환경

- Unity 6000.3.22f1 (Unity 6.3), URP 17.3.0
- New Input System 정식 도입 — 게임플레이용 `PlayerInputActions`, ESC 메뉴
  토글 전용 `EquipMenuActions`, 전체 지도 열기 전용 `FullFloorMapActions`까지
  용도별로 3개의 InputActionAsset으로 분리
- Cinemachine 미설치, AI Navigation 설치되어 있으나 의도적으로 미사용 — 적
  이동/경로탐색은 FSM + `Rigidbody` + 자체 그리드 A*(`RoomPathfindingGrid`)
- 밸런스 수치(플레이어 기본 스탯, 대시, 패시브/액티브 아이템 효과, 드롭
  테이블)와 층/방 레이아웃은 코드/씬이 아니라 `Assets/StreamingAssets/
  Balance/*.json` 평문 JSON에서 로드 — Unity/Claude 없이 텍스트 에디터로
  직접 수정 가능. 절차적 생성 난이도 규칙은 `Stage/ProceduralRules.json`,
  전투방 장애물 패턴은 `Stage/ObstaclePatterns.json`
- 층 데이터 소스는 `IFloorSource` 뒤에 추상화됨: 기본은
  `ProceduralFloorSource`(시드 기반 절차 생성, 층 3개 고정), `JsonFloorSource`
  (손 authoring `Floor1~3.json`)는 디버그/레퍼런스 모드로 여전히 선택 가능.
  둘 다 손상 시 비상용 층 폴백

### 폴더 구조 (대표 클래스만)

```
Assets/Scripts/
├─ Core/       GameFlow(씬 라우팅 정적 클래스, PendingArchetype 씬 전달),
│              SceneTransitionController(프로젝트 유일의 DontDestroyOnLoad,
│              전체화면 페이드로 씬 전환)
├─ Player/     PlayerMovement/PlayerAim/PlayerDodgeRoll/PlayerCombat,
│              PlayerAnimationController, PlayerHitReaction(피격
│              넉백+히트스턴+애니), PlayerActiveItemInventory/
│              ActiveItemController, PlayerPassiveStats, WallClamp(공용 벽
│              클램프), InputBuffer(쿨다운 중 입력 0.15초 버퍼링),
│              CharacterSelectController(Title에서 넘어온 선택 소비),
│              EquipMenuController
├─ Enemy/      EnemyStateMachine + FSM 상태 6종, IAttackPattern 구현체
│              (Melee/Ranged/BurstRanged/ChargeStrike), EnemyAnimationController,
│              EnemyChargeHitbox(보스 돌진 판정), RangedKitingBehavior,
│              EnemyMovementFlavor, EnemyData/AttackPatternData(SO), EnemySpawner
├─ Room/       RoomController, DoorController, RoomTransitionController,
│              RoomData/RoomDoorConnection/RoomFootprint,
│              RoomPathfindingGrid(그리드 A*), RoomAttackSlots(적 포메이션 —
│              근접/원거리 링에 균등 분산 배치), ObstaclePlacement/
│              RoomObstacleSet/ObstaclePatternLibrary, RoomFlowManager
├─ Stage/      StageManager, GameFloors, IFloorSource(+ProceduralFloorSource/
│              JsonFloorSource), ProceduralFloorGenerator, DeterministicRng/
│              RngStreams, RunConfig, FloorGraph(연결성 검증 공용)
├─ Combat/     IDamageable, Health/HealthComponent, IWeapon 2종, IActiveItem
│              3종(Grenade/EnhancedShot/Flamethrower), Projectile류,
│              PassiveItemData, ItemPickup/ItemDropManager(확률 드롭 +
│              아이템 방/보스방 보장 지급), CombatVfxLibrary(VFX 풀 파사드),
│              DebugDamage
├─ Balance/    GameBalance(JSON 로더 + 내장 기본값 폴백)
├─ Camera/     QuarterViewCameraFollow
├─ UI/         UIManager, HUD(HealthBar/ActiveItemCooldown/DashCooldown/
│              StageText/Crosshair/RunTimer/BossHealthBar), MinimapUI,
│              FullFloorMapUI/MinimapMapOpenTrigger(전체 지도), FloorCompleteUI,
│              GameOverUI, SystemNotificationUI, PickupNotificationUI,
│              BillboardUI, DebugOverlayController(F1/F2/R 디버그)
├─ Pooling/    GameObjectPool, PooledObject, PooledVfxEffect
└─ Benchmark/  BenchmarkSpawner — 격리 스트레스 테스트 전용(킬/재보충 루틴,
               Add Boss 강제 트리거, 나선형 스폰)

Assets/Editor/  DataValidationMenu/DataValidationWindow(SO 데이터 검증),
                FloorGraphWindow(층 그래프 비주얼라이저) — 프로젝트 최초의
                Editor 전용 코드
```

asmdef: `Assets/Scripts/RogueLike.asmdef` 하나로 전체를 단일 어셈블리로
래핑(오직 Unity Test Framework가 프로젝트 코드를 참조하도록 하기 위함,
계층 분리는 실제 의존성 문제가 드러나면 그때). references:
`Unity.InputSystem`, `UnityEngine.UI`. 테스트는 `Assets/Tests/{EditMode,
PlayMode}` + 전용 asmdef 2개(`RogueLike.Tests.EditMode`/`.PlayMode`),
GitHub Actions(GameCI)가 매 push마다 두 스위트를 자동 실행. 순수 로직
추출(`KnockbackMath`/`RoomBounds`/`RoomPathfindingGrid`/`RoomAttackSlots`
등 씬-비의존 클래스), `GameFloors`/`GameBalance`는 `internal` 파싱
시크릿 + `InternalsVisibleTo`로 노출. 정확한 테스트 개수는 기능이 늘 때마다
회귀 테스트도 함께 추가돼 계속 바뀌므로 CI 결과로 확인(README의 CI 배지 참고).

`StageData`/`StageDataAsset`/`RoomDataAsset`(SO 29개)/`UpgradeSelectionUI` 등
룸 시스템 전환·JSON 외부화 이전 구조는 전부 삭제됨 — `RoomData`/
`StageEnemyEntry`/`RoomDoorConnection`/enum류만 JSON 역직렬화 대상이라 유지.

### 씬 구성

씬 2개: **`Title.unity`**(Build Settings index 0, 시작 씬) → **`Prototype.unity`**
(index 1, 인게임). 별도로 **`BenchmarkScene`**(§6.5 프로파일링 전용 격리 씬,
`BenchmarkSpawner`로 특정 적 유형을 50/100/200마리 스폰 + 킬/재보충 루틴 +
보스 강제 트리거)이 있음. 씬 rename(`Prototype`→`Gameplay`)은 보류 중.

**`Title.unity`**: Camera + EventSystem(`InputSystemUIInputModule`) + Canvas.
`TitleGroup`(로고 + Start 버튼) / `CharacterSelectPanel`(`CharacterSelectUI`).
`TitleScreenController`: Start → 캐릭터 선택 → `GameFlow.StartRun(archetype)`
→ `Prototype` 씬 로드.

**`Prototype.unity`**:
- `Player`, `Main Camera`. 정적 `Ground` Plane은 없음 — 룸 시스템이 방마다
  바닥 Plane을 절차적으로 생성.
- **방/문 그리드 시스템**: 씬에 방이 사전 배치되어 있지 않고,
  `StageManager.BuildFloor()`가 `IFloorSource`가 제공한 `FloorLayout.Rooms`
  기준으로 `Room_Base.prefab`을 그리드에 인스턴스화. 전투방은 정사각형/
  가로직사각형/노치 코너("ㄱ"자) 형태와 격자 셀 2~3개 실병합을 지원, 벽/바닥/
  문은 모듈형 타일(`RoomTileSet`)로 런타임 조립, 장애물은 시드 기반 패턴
  라이브러리에서 선택 + BFS 연결성 검사.
- 방-방 이동은 물리적 도보가 아니라 `RoomTransitionController`의 카메라 연출
  전환(문 트리거 통과 시 0.6초 `SmoothStep`).
- 문 잠금 정책: 미클리어 방 입장 시 그 방 문 전체 잠금, 클리어 시 해제
  (아이작 방식). 적/보스는 사전 배치 없이 `EnemySpawner`가 방 진입 시 스폰.
  아이템 방/보스방은 확률 드롭이 아니라 `ItemDropManager.
  SpawnGuaranteedRewards()`로 보장된 픽업 지급.
- 풀: `EnemyPool_Melee`/`EnemyPool_Ranged`/`BossPool`/`GrenadePool`/
  `ProjectilePool` + VFX 풀 5종(`VfxPool_HitImpact`/`MuzzleFlash`/
  `EnemyDeath`/`GrenadeExplosion`/`BossChargeDust`, `CombatVfxLibrary`가
  정적 싱글톤으로 `HealthComponent.AnyDamageTaken` 등 전역 이벤트를 구독해
  트리거).
- top-level 매니저: `StageManager`, `RoomFlowManager`, `RoomTransitionController`,
  `EnemySpawner`, `ItemDropManager`, `CombatVfxLibrary`.
- `Canvas`(`UIManager`) 하위: `HUD`(HealthBar/ActiveItemCooldown+
  DashCooldownSlot/StageText `x-y`/Crosshair/RunTimer/BossHealthBar),
  `SystemNotificationPanel`, `EquipPanel`(ESC 토글, 액티브 아이템 슬롯 +
  패시브 Pip 바), `ClearOverlayPanel`(`FloorCompleteUI`), `Minimap`(현재 방
  중심 로컬 스크롤 뷰포트 + 안개) + Tab으로 열리는 `FullFloorMapUI`(층 전체
  드래그 패닝 지도), `FloorTransition`(층 전환 검은 페이드). Player 자식으로
  `PickupNotificationCanvas`(월드 스페이스, `BillboardUI`).
- 업그레이드/장착은 **ESC 키**가 `EquipPanel`을 열고 닫는 방식으로 대체
  (방 클리어가 게임을 중단시키지 않음).

### 프리팹

- `Assets/Prefabs/Player.prefab` — `man_kit`(Quaternius, CC0) `Adventurer` 모델
  + `PlayerAnimationController`
- `Assets/Prefabs/Enemy_Basic.prefab`(근접), `Enemy_Ranged.prefab`(원거리),
  `Boss_Basic.prefab` — `cyberpunk_game_kit` 로봇 모델(`Enemy_2Legs`/
  `Enemy_2Legs_Gun`/`Enemy_Flying`/`Enemy_Flying_Gun`/보스용
  `Enemy_Large_Gun`) + `EnemyAnimationController`. 전부 자식으로
  `HealthBarCanvas`(World Space, `BillboardUI`+`HealthBarUI`) 포함,
  `Boss_Basic`은 추가로 `EnemyChargeHitbox` 자식 보유
- `Assets/Prefabs/Projectile_Basic.prefab`, `Grenade.prefab`
- `Assets/Prefabs/Room_Base.prefab` — 빈 껍데기(`RoomManager`+
  `RoomController`만), 지오메트리는 전부 런타임 절차적 생성
- `Assets/Prefabs/ActiveItemPickup.prefab`, `PassiveItemPickup` 계열
- `Assets/Prefabs/VfxPool_*` 5종(`CombatVfxLibrary`가 참조)
- `Assets/Prefabs/UI/MinimapIcon.prefab`, `MinimapConnector.prefab`

### 태그 / 레이어

- 태그: `Player`, `Enemy`
- 레이어: `Ground`(방 바닥 Plane, 레이캐스트 제한용), `Enemy`(대시 중
  `Physics.IgnoreLayerCollision(Player, Enemy)` 토글용)

### 클래스 책임 요약 (영역별 대표 클래스 — 전체 목록 아님)

| 영역 | 대표 클래스 | 책임 |
|---|---|---|
| 이동/조준 | `PlayerMovement`, `PlayerAim`, `PlayerDodgeRoll`(대시 = `linearVelocity` 구동, 대시 중 `Enemy` 레이어 관통 + 종료 시 보정), `WallClamp`(공용 벽 클램프), `InputBuffer`(쿨다운 입력 0.15초 버퍼링) | Rigidbody 기반 이동/회전/구르기. 모든 이동 명령 전 `WallClamp`로 벽 관통 방지 |
| 전투(플레이어) | `PlayerCombat`+`IWeapon`(Melee/Ranged), `ActiveItemController`+`IActiveItem`(Grenade/EnhancedShot/Flamethrower), `PlayerHitReaction`(피격 시 방향성 넉백 + 0.4초 히트스턴 + 애니) | 기본 공격/액티브 아이템/피격 반응을 각각 다형성 구조로 분리 |
| 비주얼/애니메이션 | `PlayerAnimationController`(상하체 레이어 분리, Avatar Mask), `EnemyAnimationController`(FSM 상태 전환에 맞춰 Animator 재생) | 프리미티브 → 실제 모델(Phase 4)로 교체 후 FSM/전투 상태와 Animator 상태를 동기화 |
| 적 AI | `EnemyStateMachine`(FSM: Idle→Patrol→Chase→Attack→Stunned/Dead, 모든 이동은 `MoveTo` 단일 초크포인트), `IAttackPattern`(Melee/Ranged/BurstRanged/`ChargeStrikePattern`+`EnemyChargeHitbox`), `RangedKitingBehavior`, `EnemyMovementFlavor`, `RoomPathfindingGrid`(1유닛 셀 그리드 A*, 실제 장애물 Bounds 기반 — Chase 중엔 시야와 무관하게 항상 경로 추종, 시야 O면 공격 슬롯·X면 플레이어 실시간 위치가 목표, 12유닛 넘게 멀어지면 포기), `RoomAttackSlots`(근접/원거리 링으로 균등 분산 배치해 뭉침 방지) | 일반 적 6종(Melee/Ranged/ErraticMelee/BurstRanged/RelentlessMelee)과 보스가 같은 FSM 공유, 데이터/공격 패턴/포메이션으로 차별화 (RelentlessMelee는 이동속도 -50%/공격 주기 -50% + 붉은 틴트) |
| 적 스폰 | `EnemySpawner` | `EnemyVariantType` → `GameObjectPool` 매핑, 방 진입 시 `RoomData` 구성대로 스폰, 사망 시 `ItemDropManager` 드롭 트리거 |
| 방/문 진행 | `StageManager`, `RoomController`(방 하나의 생명주기, 지오메트리/장애물/공격 슬롯 소유), `RoomFootprint`, `ObstaclePlacement`/`RoomObstacleSet`, `DoorController`, `RoomTransitionController` | 아이작 스타일 공간형 방/문 시스템, 멀티셀 병합 + 절차적 장애물 배치 |
| 던전 생성 | `IFloorSource`(`ProceduralFloorSource`/`JsonFloorSource`), `ProceduralFloorGenerator`, `DeterministicRng`/`RngStreams`, `RunConfig`, `FloorGraph`(연결성 검증), `GameFloors` | 시드 기반 결정적 절차 생성이 기본, JSON은 디버그 모드. 손상 시 비상용 층 폴백 |
| 아이템 드롭/픽업 | `ItemDropManager`(확률 드롭 + `SpawnGuaranteedRewards` 보장 지급), `ItemPickup` | 적 사망 시 확률 드롭, 아이템 방/보스방 클리어 시 보장된 픽업(액티브 1 + 패시브 1) |
| VFX | `CombatVfxLibrary`(정적 싱글톤, 전역 이벤트 구독) + `VfxPool_*` 5종 | 피격/총구/사망/폭발/보스돌진 이펙트를 전부 오브젝트 풀로 재생 |
| 데미지 | `IDamageable`, `Health`(순수 C#), `HealthComponent` | 데미지 판정, 체력 상태, 사망 이벤트 |
| 방 클리어 후 흐름 / 런 종료 | `RoomFlowManager` | `FloorCompleted`/`AutoAdvanceRequested`로 층 전환 지휘, 플레이어 사망(`Health.Died`) 시 `HandlePlayerDied` → `GameOverUI` |
| 풀링 | `GameObjectPool`, `PooledObject`, `PooledVfxEffect` | Projectile/Enemy/Boss/Grenade/VFX 공용 재사용 풀 |
| 벤치마크 | `BenchmarkSpawner` | 격리 스트레스 테스트 — 유형별 50/100/200 스폰, 킬/재보충 루틴, 보스 강제 트리거, 나선형 배치 |
| 에디터 툴링 | `DataValidationWindow`/`IValidatable`, `FloorGraphWindow` | SO 데이터 무결성 검증, 시드/층 입력만으로 절차 생성 그래프를 Edit Mode에서 미리보기 |
| UI | `UIManager`(파사드), `HealthBarUI`, `ActiveItemCooldownUI`, `MinimapUI`, `FullFloorMapUI`, `FloorCompleteUI`, `GameOverUI`, `FloorTransitionUI`, `PickupNotificationUI`, `BillboardUI`, `DebugOverlayController`(F1/F2/R) | `UIManager`가 화면 전환을 `ShowXxx(...)`로 노출 |

---

## 3. 진행 상황 요약

핵심 루프(타이틀 → 캐릭터 선택 → 시드 기반 3층 런 → 보스 → 클리어/게임오버)는
처음부터 끝까지 플레이 가능하며 유저가 1주파 실플레이로 확인했다.

- **던전/방**: 기본은 시드 기반 절차 생성, JSON은 디버그 모드로 유지. 층 수
  3 고정. 전투방 형태 다양화(정사각형/가로직사각형/ㄱ자) + 멀티셀 병합 +
  패턴 라이브러리 기반 장애물 배치 완료.
- **적 AI**: 공유 FSM + 그리드 A* 경로탐색 + 방 공격 슬롯(포메이션) 도입
  완료. 변형 6종(Melee/Ranged/ErraticMelee/BurstRanged/RelentlessMelee/Boss)
  전부 실플레이 검증됨.
- **아트/애니메이션(Phase 4)**: 플레이어·적 6종 프리미티브 → 실제 모델+
  애니메이션 교체 완료. 상하체 공격 레이어 분리, 피격 반응(넉백+히트스턴),
  보스 전용 돌진 공격(히트박스 기반 판정 + 전용 넉백 배율) 확정.
- **VFX**: `CombatVfxLibrary` + 풀 5종이 `Prototype.unity`/`BenchmarkScene`
  양쪽에 구성됨.
- **최적화/프로파일링**: 오브젝트 풀링(발사체/적/보스/그레네이드/VFX) 적용.
  격리 벤치마크 씬으로 §6.5 Before(2026-09-08)/After(2026-09-17) 실측 완료 —
  적 4종 + 보스 전부 프레임 예산 안. AI tick 분산/`GetComponent` 캐싱은 실측
  병목이 없어 보류. Addressables는 검토 후 도입하지 않기로 확정(런 전체가
  하나의 게임플레이 씬에서 절차 생성되는 구조라 비동기 로딩 이득 없음).
- **테스트/CI**: EditMode+PlayMode 스위트, GitHub Actions(GameCI)로 매 push
  자동 실행.
- **에디터 툴링**: SO 데이터 검증 패스 + 층 그래프 비주얼라이저 완료.
- **입력/조작**: 쿨다운 대기 입력 버퍼링, 대시 중 공격/스킬 차단, 프레임레이트
  독립성 전수 감사(위반 0건) 완료.
- **UI**: HUD, 미니맵 + 전체 지도, 일시정지/장비 메뉴, 클리어/게임오버 화면,
  씬 전환 페이드까지 구성됨.
- **문서화(Phase 7)**: 진행 중 — README 현행화 초안 작성됨, 서술형 포트폴리오
  문서는 아직.

세부 이력과 각 항목의 원인/수정 과정은 `Docs/DevLog.md`, 트레이드오프가
있었던 결정은 `Docs/AI_Decision_Log.md`를 참고.

---

## 4. 지금까지 선택한 설계 방향과 이유 (핵심만)

- **이동/회전은 Rigidbody + MovePosition/MoveRotation 또는 linearVelocity**:
  Update에서 입력 샘플링, FixedUpdate에서 소비.
- **Input Actions을 용도별로 분리**: 메뉴 토글용 액션맵이 게임플레이 액션과
  섞이면 메뉴를 잠근 순간 스스로 닫을 수 없게 되기 때문.
- **무기/액티브 아이템은 인터페이스 다형성**: `IWeapon`/`IActiveItem`으로
  분리, 실제 구현은 캐릭터 선택/장착 시점에 결정.
- **`Health`는 plain C# 클래스**: `MonoBehaviour`에서 분리해 EditMode
  테스트가 가능하도록 함.
- **이벤트 기반 결합도 관리**: `HealthComponent`/방 진행 로직이 서로 직접
  참조하지 않고 `Health.Died`/`RoomEntered`/`Cleared` 같은 이벤트로만 연결.
- **오브젝트 풀링**: `UnityEngine.Pool.ObjectPool<T>`를 감싸는 범용
  `GameObjectPool` 하나로 Projectile/Enemy/Boss/Grenade/VFX 전부 처리.
  같은 프레임 중복 반환은 `_hasReturned`/`_isReleased` 가드로 방지.
- **VFX는 정적 싱글톤 + 전역 이벤트 구독**: `CombatVfxLibrary`가
  `HealthComponent.AnyDamageTaken` 등 기존 이벤트를 구독해 트리거 — 각
  공격/무기 코드가 VFX 호출을 직접 알 필요 없음.
- **적 포메이션은 `RoomController` 소유의 순수 클래스**: `RoomAttackSlots`가
  `RoomPathfindingGrid`와 동일한 "씬 비의존, 방이 소유" 패턴을 따름 —
  중앙에서 여러 적을 관리하는 별도 매니저를 새로 만들지 않음.
- **던전은 아이작 스타일 방/문 시스템, 기본 생성은 시드 기반 절차 생성**:
  방 배치는 `IFloorSource` 뒤에 추상화, 결정적 자체 RNG(`DeterministicRng`)
  사용(`System.Random`은 런타임 버전에 따라 시퀀스가 바뀔 수 있어 배제).
- **"업그레이드 선택" UI 폐기 → 바닥 드롭 + 보장 지급 + ESC 메뉴**: 스킬
  습득을 클리어 후 강제 선택이 아니라 드롭/보장 픽업으로, 장착 전환은 ESC
  메뉴로 상시 가능 — 방 클리어가 게임 흐름을 중단시키지 않음.
- **패시브 스탯 4종을 "base + 고정값 보너스" 모델로 통일**: 스탯마다 공식이
  갈라지는 것을 방지.
- **밸런스/층 데이터를 JSON으로 외부화**: `Assets/StreamingAssets/*.json`,
  Unity 없이도 텍스트 에디터로 조정 가능하도록.
- **실제 카메라 빌보딩**: `BillboardUI`가 `LateUpdate()`에서 매 프레임
  카메라 회전과 일치시킴(고정 각도 근사 대신).
- **적 방 이탈 방지는 물리가 아니라 AABB 클램프**: `RoomController.
  ClampToInterior` — 물리 튜닝이나 "모든 이동 코드가 벽 체크를 기억"하는
  데 의존하지 않는 결정적 차단.
- **NavMesh 대신 자체 그리드 A***: 처음엔 방/장애물 생성기가 이미 만든 4유닛
  타일 그리드를 재사용할 수 있어서 채택. 2026-09-30에 그 해상도가 적의 미세 위치
  목표보다 성긴 게 문제가 되어, 생성기는 4유닛 그대로 두고 파인딩만 1유닛 셀 +
  실제 장애물 Bounds(이동 클램프와 같은 데이터) 기반으로 세분화. NavMesh 전환도
  재검토했으나 런타임 베이크·인접 방 누수 등 범위가 커서 보류
  (`AI_Decision_Log.md` 2026-09-30 (1)).
- **시야를 잃은 적은 실시간 위치를 쫓되 거리로 포기**: 전 적이 플레이어의
  실제 위치로 경로를 잡고 12유닛(`BlindChaseGiveUpRange`)을 넘으면 Patrol로
  복귀 — 시간 기반 포기는 방마다 다른 우회 거리를 못 맞춰서 교체. 옛
  "마지막 목격 지점 탐색" 로직은 휴면 보존(`AI_Decision_Log.md` 2026-09-30 (2)).
- **얇은 벽(커넥터)은 타일 경계의 1/4만 차지**: 벽 하나가 경계 전체를 막으면
  사방이 막힌 셀이 밀폐되므로, 길이를 파인 그리드 1칸(1유닛)으로 제한해 각
  경계에 항상 틈이 남게 함(`AI_Decision_Log.md` 2026-09-30 (3)).
- **Addressables는 도입하지 않음**: 런 전체가 하나의 게임플레이 씬에서
  절차적으로 방을 짓는 구조라 비동기 로딩 이득이 없음(검토 후 기각,
  `AI_Decision_Log.md` 2026-09-15 참고).

대안 비교가 필요했던 개별 결정은 각 항목이 가리키는 `AI_Decision_Log.md`
날짜를 참고.

---

## 5. 아직 결정하지 않은 항목

- **카메라 시스템에 Cinemachine 도입 여부**: 현재 `QuarterViewCameraFollow`
  (단순 오프셋 추적)로 충분하다고 판단, 필요해지면 재논의.
- **`UIManager`를 상속 구조로 확장할지**: 두 번째 구체적인 UI 매니저 사례가
  실제로 생기기 전까지는 보류.
- **아이콘 스와치 → 실제 픽토그램 교체**: 우선순위 낮음으로 이월 중.
- **플레이스홀더 지오메트리 시절 콜라이더/히트박스 이슈 3건**: `WallClamp`가
  아주 높은 이동속도에서 얕은 각도로 벽을 스치면 여전히 소폭 관통, 문 통과
  트리거가 보이는 문보다 깊음, 벽에 밀착한 두 오브젝트 사이 근접 발사 시
  발사체가 벽 판정으로 소멸. Phase 4에서 비주얼(모델)은 교체됐지만 콜라이더
  자체는 프리미티브 시절 크기 그대로라 셋 다 미해결로 남아있을 가능성 높음
  (재확인 필요).
- **BurstRanged 50/100 프로파일링 이상치**: §6.5 After 측정에서만 관측된
  100ms대 CPU 스파이크의 원인이 확정되지 않음(공용 풀의 일회성 성장 비용
  추정, 다음 재현 시 `GetFrameSelfTimeSample`로 확인 필요).

---

## 6. 남은 작업 (우선순위 순)

1. **Phase 7 — 포트폴리오 문서화 (다음 세션 최우선)**: `README.md` 현행화
   (초안 작성됨) 이후, Claude Code+Unity MCP 워크플로우/AI 제안 거부·수정
   사례/배운 점을 담을 서술형 포트폴리오 문서 + 스크린샷·GIF 캡처.
2. **세이브/로드(버전드 직렬화 + 런 이어하기)**: 아무것도 언블록하지 않는
   항목이라 가장 나중, 마감 임박 시 스킵 가능.
3. §5의 미해결 항목들(콜라이더/히트박스 이슈 재확인, BurstRanged 이상치
   원인 확인, 아이콘 픽토그램 교체) — 급하지 않음, 다른 작업과 묶어서 처리.

Stage 3 최적화(AI tick 분산 등)와 Addressables는 검토 후 "지금은 필요 없다/
하지 않는다"로 이미 결론 난 항목이라 이 목록에서 제외(§3·§4 참고).
