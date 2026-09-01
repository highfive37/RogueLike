# Claude Session Summary — RogueLike (3D Arena Roguelite)

이 문서는 Claude Code + Unity MCP로 진행한 개발의 **현재 구조 스냅샷**이다.
날짜별 작업 사실은 `Docs/DevLog.md`, 개별 설계 결정의 대안/근거는
`Docs/AI_Decision_Log.md`를 참고. 원본 기획 문서는 `Docs/ProjectPlan.md`.
각 문서의 역할 구분은 `Docs/DocumentationGuide.md` 참고.

마지막 갱신: 2026-08-31 (Stage 2.5 — 아이작 스타일 방/문 시스템 전환 +
액티브/패시브 아이템 시스템 도입 시점 기준).

---

## 1. 프로젝트 목표

- **장르**: 3D 쿼터뷰 액션 로그라이트
- **목적**: 완성도 있는 게임 자체보다, **게임 클라이언트 개발자 포트폴리오**로서
  기술적 의사결정을 설명할 수 있는 작지만 완결된 프로젝트를 만드는 것
- **핵심 루프**: 캐릭터/무기 아키타입 선택 → 방 입장 → 전투 → 클리어(문 해제) →
  다음 방으로 카메라 연출 전환 → 보스 → 런 종료 (플레이타임 15~20분)
  - "업그레이드 선택" 단계는 폐기되고 바닥 드롭 아이템 픽업으로 대체됨(§3, §4
    참고) — 그 외 핵심 루프 자체는 `ProjectPlan.md` 원안과 동일.
- **원칙**: 기술 요소(FSM, ScriptableObject, Object Pooling, Addressables, Profiler
  최적화 등)를 한꺼번에 넣지 않고, 실제로 필요해지는 시점에 단계적으로 도입한다.
  AI(Claude) 제안을 그대로 수용하지 않고 비판적으로 검토했는지가 포트폴리오
  평가 포인트 중 하나.

---

## 2. 지금까지 결정한 구조

### 프로젝트 환경 (확인된 사실)
- Unity 6000.3.22f1 (Unity 6.3), URP 17.3.0
- New Input System 정식 도입 완료 — 게임플레이용 `PlayerInputActions` 외에,
  ESC 메뉴 토글 전용 `EquipMenuActions.inputactions`(Menu 맵, `ToggleEquip`)도
  별도 InputActionAsset으로 분리(§4 참고)
- Cinemachine 미설치, AI Navigation 설치되어 있으나 여전히 미사용 (적 이동은
  FSM 상태 + `Rigidbody` 기반)
- 밸런스 수치(플레이어 기본 스탯, 패시브 아이템 효과)는 코드/씬이 아니라
  `Assets/StreamingAssets/Balance/*.json` 평문 JSON 파일에서 로드(§4 참고) —
  Unity/Claude 없이 텍스트 에디터로 직접 수정 가능한 워크플로가 실사용 중

### 폴더 구조 (대표 클래스만, 전체 목록은 코드 참고)
```
Assets/Scripts/
├─ Player/    (PlayerMovement, PlayerAim, PlayerDodgeRoll, PlayerCombat,
│              PlayerInputActions, PlayerPassiveStats, PlayerActiveItemInventory,
│              ActiveItemController, CharacterSelectController,
│              PrototypeActiveItemSwitcher, EquipMenuController 등)
├─ Enemy/     (EnemyStateMachine + FSM 상태 6종, IAttackPattern 구현체 3종,
│              EnemyData / AttackPatternData ScriptableObject, EnemySpawner)
├─ Room/      (RoomController, DoorController, RoomTransitionController,
│              RoomData/RoomDataAsset, RoomDoorConnection, DoorDirection,
│              RoomType, IFloorLayoutProvider/ScriptableObjectFloorLayoutProvider,
│              StageManager — 층/방 그래프 관리로 책임 변경, 클래스명은 씬 참조
│              보존을 위해 유지, RoomFlowManager — 업그레이드/장착 UI
│              시퀀싱만 담당) — 2026-08-31 전면 재작업 (§3 참고)
├─ Combat/    (IDamageable, Health, HealthComponent, IWeapon 구현체 2종,
│              IActiveItem 구현체 3종(`GrenadeActiveItem`/`EnhancedShotActiveItem`/
│              `FlamethrowerActiveItem`), Projectile류, ActiveItemData류,
│              PassiveItemData, PassiveItemType, ItemPickup, ItemDropManager)
├─ Balance/   (GameBalance — JSON 밸런스 파일 로더, 실패 시 내장 기본값 폴백)
├─ Camera/    (QuarterViewCameraFollow)
├─ UI/        (UIManager, ActiveItemEquipUI, PlayerStatsPanelUI,
│              CharacterSelectUI, ClearOverlayUI, HealthBarUI,
│              ActiveItemCooldownUI, BillboardUI, MinimapUI,
│              PickupNotificationUI, DebugOverlayController 등)
└─ Pooling/   (GameObjectPool, PooledObject)
```
asmdef(어셈블리 분리)는 여전히 도입하지 않음 — 스크립트 규모상 과설계로 판단.

`StageData`/`StageDataAsset`/`IStageDataProvider`/
`ScriptableObjectStageDataProvider`(4개, 웨이브 반복 방식)와
`Assets/Data/Stages/Stage01~10.asset`(10개)는 룸 시스템 전환으로 완전히
대체되어 삭제됨. `UpgradeSelectionUI`/`UpgradeSlotView`도 바닥 드롭 아이템
방식으로 대체되어 삭제됨.

"스킬" → "액티브 아이템" 전면 리네임 완료(2026-08-31): `SkillItemData`→
`ActiveItemData`, `ISkillItem`→`IActiveItem`, `SkillController`→
`ActiveItemController`, `PlayerSkillInventory`→`PlayerActiveItemInventory`,
`EquipSelectionUI`→`ActiveItemEquipUI` 등. 단, 직렬화된 `[SerializeField]`
필드 이름 자체는 기존 씬/에셋 데이터 보존을 위해 그대로 유지(공개 API만 새
이름으로 감쌈). 드롭/픽업 인프라(`ItemPickup`/`ItemDropManager`)는 반대로
"Active" 접두를 떼고 아이템 종류에 중립적인 이름으로 유지 — 패시브 아이템도
같은 드롭 방식을 쓸 예정이기 때문.

### 씬 구성 (`Assets/Scenes/Prototype.unity`)
- `Ground`(Ground 레이어 지정), `Player`(프리팹 인스턴스), `Main Camera`
- **방/문 그리드 시스템**(2026-08-31 신규): 씬에 방이 사전 배치되어 있지 않고,
  `StageManager.BuildFloor()`가 `roomAssets`(`RoomDataAsset[]`)를 기준으로
  `Room_Base.prefab`을 그리드(현재 5개 방, 간격 `roomGap`)에 전부
  인스턴스화(백트래킹 가능). 각 방 인스턴스는 자체 `RoomManager`+
  `RoomController`를 가짐(스테이지 1개당 하나였던 기존 `RoomManager`와
  달리, 방마다 별도 인스턴스)
- 방-방 이동은 물리적 도보 이동이 아니라 최상위 `RoomTransitionController`가
  담당하는 카메라 연출 전환(문 트리거 통과 시 0.6초 `SmoothStep` 카메라 보간,
  전환 중 플레이어 렌더러/입력/물리 임시 비활성화 후 원복)
- 문 잠금 정책: 기본적으로 모든 방의 모든 문은 열림 상태 → 플레이어가
  미클리어 방에 **입장하는 순간** 그 방의 모든 문이 잠기고, 클리어하면 다시
  전부 열림(아이작 방식, §4 참고). 문은 항상 존재하는 반투명 마커
  오브젝트(`MaterialPropertyBlock` 알파 — 잠김=불투명, 열림=반투명)로 상태
  표시, 벽 모양과는 무관
- 적/보스는 씬에 사전 배치되어 있지 않음 — `EnemySpawner.SpawnRoom()`이 방
  진입 시 각 방의 `RoomData`에 authoring된 구성대로 스폰. 스폰 좌표는 방
  로컬 오프셋을 방의 월드 트랜스폼으로 변환해 계산
- 풀 5종: `EnemyPool_Melee`, `EnemyPool_Ranged`, `BossPool`, `GrenadePool`,
  `ProjectilePool` (전부 `GameObjectPool` 컴포넌트 공용)
- top-level 매니저 오브젝트: `StageManager`(층/방 그래프 관리),
  `RoomFlowManager`(업그레이드/장착 UI 시퀀싱), `RoomTransitionController`,
  `EnemySpawner`, `ItemDropManager`
- `Canvas`(UGUI 1개, `UIManager` 컴포넌트가 이 오브젝트에 직접 붙어 있음) 하위에
  `HUD`(`HealthBarUI`/`ActiveItemCooldownUI`/`StageText`),
  `EquipPanel`(ESC로 토글, 자식으로 액티브 아이템 슬롯 목록 +
  `StatsPanel`(패시브 스탯 Pip 바 4종)), `CharacterSelectPanel`,
  `ClearOverlayPanel`, `Minimap`(우상단, 안개 효과 — §4 참고)이 자식으로 배치,
  `EventSystem` 별도. Player 자식으로 `PickupNotificationCanvas`(월드 스페이스,
  `BillboardUI`) 추가.
  - `UpgradePanel`은 삭제됨(업그레이드 선택 UI 폐기, §3 참고)
- 업그레이드/장착 UI가 아니라 **ESC 키**가 `EquipPanel`을 열고 닫음
  (`EquipMenuController`, Player에 배치) — 방 클리어가 더 이상 게임을
  중단시키지 않음(§4 참고)

### 프리팹
- `Assets/Prefabs/Player.prefab`
- `Assets/Prefabs/Enemy_Basic.prefab`(근접), `Enemy_Ranged.prefab`(원거리),
  `Boss_Basic.prefab` — 3개 전부 자식으로 `HealthBarCanvas`(World Space,
  `BillboardUI`+`HealthBarUI`) 포함
- `Assets/Prefabs/Projectile_Basic.prefab`, `Grenade.prefab`
- `Assets/Prefabs/Room_Base.prefab`(2026-08-31, 빈 껍데기 — `RoomManager`+
  `RoomController`만 포함, 바닥/벽/문 지오메트리는 전부
  `RoomController.Initialize()`가 코드로 절차적 생성)
- `Assets/Prefabs/ActiveItemPickup.prefab`(바닥 드롭 픽업 — 납작한 원기둥,
  노란색 머티리얼)
- `Assets/Prefabs/UI/MinimapIcon.prefab`, `MinimapConnector.prefab`
- `Assets/Materials/DoorMarker.mat`(URP Lit → Transparent 서페이스로 스크립트
  설정, 문 상태 표시용 반투명 마커)

### 태그 / 레이어
- 태그: `Player`(기본), `Enemy`(추가) — 변경 없음
- 레이어: `Ground` — 그레네이드 착탄 지점 레이캐스트가 적 콜라이더를 잘못
  맞히던 버그를 고치기 위해 도입, `Ground` 오브젝트를 이 레이어로 지정하고
  관련 레이캐스트를 이 레이어로 제한

### 클래스 책임 요약 (영역별 대표 클래스 — 전체 목록 아님)
| 영역 | 대표 클래스 | 책임 |
|---|---|---|
| 이동/조준 | `PlayerMovement`, `PlayerAim`, `PlayerDodgeRoll` | Rigidbody 기반 이동/회전/구르기, New Input System 액션 소비 |
| 전투(플레이어) | `PlayerCombat`+`IWeapon`(`MeleeWeapon`/`RangedWeapon`), `ActiveItemController`+`IActiveItem`(`EnhancedShotActiveItem`/`GrenadeActiveItem`/`FlamethrowerActiveItem`) | 기본 공격과 장착 액티브 아이템을 각각 다형성 구조로 분리 |
| 캐릭터 선택 | `CharacterSelectController`, `CharacterSelectUI`, `CharacterArchetypeData` | 런 시작 시 근접/원거리 아키타입 선택 UI, 선택 전까지 입력 잠금 |
| 액티브 아이템 보유/장착 | `PlayerActiveItemInventory`, `OwnedActiveItem`, `ActiveItemEquipUI` | ESC 메뉴에서 소지 아이템 목록 확인 + 장착 전환 |
| 패시브 스탯 | `PlayerPassiveStats`, `PlayerStatsPanelUI` | 공격력/이동속도/사거리/공격속도 4종 전부 "base + 고정값 보너스" 덧셈 모델로 통일(2026-08-31, §4 참고), ESC 메뉴에 Pip 바(10칸)로 표시 |
| 밸런스 수치 | `GameBalance`, `Assets/StreamingAssets/Balance/*.json` | 플레이어 기본 스탯/패시브 아이템 효과를 JSON에서 로드, 파일 없거나 파싱 실패 시 내장 기본값으로 폴백 |
| 적 AI | `EnemyStateMachine`(FSM: Idle→Patrol→Chase→Attack→Stunned/Dead), `IAttackPattern`(`MeleeAttackPattern`/`RangedAttackPattern`/`RetreatAndShootPattern`) | 일반 적과 보스가 같은 FSM을 공유, 공격 패턴 데이터로 차별화 |
| 적 스폰 | `EnemySpawner` | `EnemyVariantType`(Melee/Ranged/Boss) → `GameObjectPool` 매핑 보유, 방 진입 시 `RoomData` 구성대로 스폰(방 로컬 오프셋 → 월드 좌표 변환), 사망 시 드롭 굴림(`ItemDropManager`) 트리거 |
| 방/문 진행 | `StageManager`(층/방 그래프 빌드+현재 방 추적), `RoomController`(방 하나의 생명주기: 지연 스폰, 클리어 추적, 지오메트리 생성, 문 배선), `DoorController`(문 잠금/해제 + 물리 차단 콜라이더 + 시각 마커), `RoomTransitionController`(문 통과 시 카메라 연출 전환) | 스테이지 기반 진행을 아이작 스타일 공간형 방/문 시스템으로 전면 대체(2026-08-31) |
| 아이템 드롭/픽업 | `ItemDropManager`, `ItemPickup` | 적 사망 시 확률 굴림(중복 방지 — 드롭된 아이템은 풀에서 제거), 바닥 픽업 오브젝트가 트리거로 습득 처리 + `PickupNotificationUI` 표시 |
| 데미지 | `IDamageable`, `Health`(순수 C#), `HealthComponent` | 데미지 판정 + 수치·가해자 로그, 체력 상태, 사망 이벤트 |
| 방 클리어 후 흐름 | `RoomFlowManager` | "업그레이드 선택" 흐름은 폐기됨(§3) — 지금은 `FloorCompleted`(보스 클리어 → Clear 오버레이) 처리만 담당 |
| 풀링 | `GameObjectPool`, `PooledObject` | Projectile/Enemy(근접·원거리)/Boss/Grenade 공용 재사용 풀. 같은 프레임 중복 반환(`_hasReturned`/`_isReleased` 가드, 2026-08-31)으로 `InvalidOperationException` 방지 |
| UI | `UIManager`(파사드), `HealthBarUI`, `ActiveItemCooldownUI`, `ActiveItemEquipUI`, `PlayerStatsPanelUI`, `CharacterSelectUI`, `ClearOverlayUI`, `MinimapUI`, `PickupNotificationUI`, `BillboardUI`, `DebugOverlayController` | `UIManager`가 선택/오버레이 화면을 감싸 `ShowXxx(...)` 메서드로 노출. 상속 없는 단일 클래스. `BillboardUI`가 실제 카메라 추적 회전(`LateUpdate`)을 전담해 데미지 팝업/HP바/미니 알림이 항상 카메라를 향함(2026-08-31 — 이전엔 무회전 근사에 의존) |

---

## 3. Stage 1~3 계획과 진행 상황

원래 `ProjectPlan.md`의 Phase 1~7을 그대로 따르지 않고, **프로토타입 → 핵심 시스템
→ 최적화** 3단계로 재구성해서 진행 중이었으나, Stage 2 완료 이후 **Stage 2.5(룸
시스템 전환)**가 그 사이에 끼어들었다 — Stage 3(최적화)는 그동안 보류.

### Stage 1 — 프로토타입 (✅ 완료, U1~U15)
최소 플레이 가능한 전투 루프.

### Stage 2 — 핵심 시스템 (✅ 완료, 2026-08-24)
Dodge/Dash, 단일 슬롯 스킬 시스템, `EnemyStateMachine` FSM, ScriptableObject
데이터 전면 도입, `IWeapon` 다형성, Boss 프로토타입, 스테이지 기반 진행
(`StageManager`+`StageDataAsset` 10개), 캐릭터 선택, 여러 종류의 일반 Enemy,
적 체력 바 + `UIManager` 파사드. 자세한 내용은 `DevLog.md` 2026-08-24 항목 참고.

### Stage 2.5 — 아이작 스타일 방/문 시스템 전환 (✅ 대부분 완료, 2026-08-31)

"웨이브 반복" 방식이던 Stage 진행을 아이작(Binding of Isaac) 스타일의 공간형
방/문 시스템으로 전환. Stage 3(최적화)는 이 작업이 끝날 때까지 보류.

✅ 완료:
- 방/문 authoring 구조(`RoomData`/`RoomDataAsset`, 문 방향별 연결 리스트),
  층 전체 그리드 사전 인스턴스화(백트래킹 가능)
- 방 지오메트리 런타임 절차적 생성(방향 조합 프리팹 대신 `RoomData` 연결
  정보를 그대로 재사용, §2 AI_Decision_Log 참고)
- 방-방 이동을 물리적 도보 대신 카메라 연출 전환(`RoomTransitionController`)
  으로 변경
- 문 상태를 벽과 무관한 반투명 마커로 표시, 잠금 정책을 아이작 방식(기본
  열림 + 미클리어 방 입장 시 전체 잠금)으로 확정
- 미니맵(엔터 더 겅전 스타일 — 3단계 안개 + 연결선 + 배경 패널)
- "업그레이드 선택" UI 완전 폐기, ESC 키로 여닫는 장비(액티브 아이템 장착 +
  패시브 스탯 확인) 메뉴로 대체 — 방 클리어가 더 이상 게임을 중단시키지 않음
- 액티브 아이템(기존 "스킬") 바닥 드롭/픽업 시스템 신규 구현, 플레이어는
  이제 아무것도 장착하지 않은 채로 시작(드롭 픽업이 유일한 습득 경로)
- "스킬"→"액티브 아이템" 전면 리네임
- 패시브 아이템(공격력/이동속도/사거리/공격속도 4종) 도입, ESC 메뉴에 Pip
  바로 표시. 공격속도를 배율에서 고정값 덧셈으로 전환해 4종 전부 동일한
  공식으로 통일(2026-08-31, `AI_Decision_Log.md` 참고)
- 밸런스 수치를 코드/씬이 아니라 JSON 파일로 분리(`GameBalance`)
- F1 디버그 오버레이(FPS + 데미지 숫자 팝업), 실제 카메라 추적 빌보딩

⏳ 남은 작업:
- 아이템 방(#3) — 바닥 드롭 아이템 로직은 만들어졌으니, "전투 없이 보장된
  픽업 배치"만 추가하면 될 것으로 예상
- 방 안 장애물(#2) — 지형 애셋 교체 시점까지 보류
- 테스트 층(5개 방)을 원래 목표(5~7개 이상)로 확장
- 방-방 카메라 전환 연출, 방 경계 재프레이밍 필요 여부, 미니맵/빌보딩/공격
  속도 체감 등 여러 항목이 실플레이 최종 확인 대기 중(`DevLog.md` "다음
  작업" 참고)

### Stage 3 — 최적화 (부분 착수, 재개 시점 미정)
- ✅ Object Pooling — `Projectile`/`Enemy`/`Boss`/`Grenade` 4종 전부 적용
- ⏳ 나머지: AI tick 분산(`EnemyAIManager`), `GetComponent` 캐싱 정리,
  `BenchmarkSpawner`(20/50/100/200 시나리오), Profiler 기반 Before/After 측정 문서화
- Stage 2.5(룸 시스템)가 실질적으로 안정화된 후 재개 여부 논의 예정

Phase 4(아트/연출), Phase 5(Addressables), Phase 7(포트폴리오 문서화)는 이
단계들 밖의 별도 트랙으로 남겨둠 — 필요해지는 시점에 진행.

---

## 4. 지금까지 선택한 설계 방향과 이유 (핵심만 — 개별 결정의 대안 비교는 `AI_Decision_Log.md`)

- **이동/회전은 Rigidbody + MovePosition/MoveRotation**: Update에서 입력을
  샘플링하고 FixedUpdate에서 소비하는 패턴.
- **Input Actions 에셋 정식 도입 완료, 용도별로 분리**: 게임플레이 입력은
  `PlayerInputActions`, ESC 메뉴 토글은 별도 `EquipMenuActions`(2026-08-31)
  — 메뉴가 열려있는 동안 게임플레이 입력 전체를 잠그는데, 토글 키 자체가 그
  안에 있으면 잠근 순간 닫을 수 없게 되기 때문.
- **무기 타입은 IWeapon 다형성으로 승격 완료**: `PlayerCombat`은 `IWeapon`
  인터페이스만 들고 있고, 실제 무기는 `CharacterSelectController`가 런 시작
  시 결정.
- **`Health`를 plain C# 클래스로 분리**: `MonoBehaviour`에 직접 로직을 두지 않고
  UnityEngine 비의존 클래스로 만들어 EditMode 테스트가 가능하도록 함.
- **이벤트 기반 결합도 관리**: `HealthComponent`와 방 진행 로직은 서로를 직접
  참조하지 않고 `Health.Died` 이벤트로만 연결됨. 방 클리어(`RoomController`)→
  미니맵/문 잠금도 같은 방식(`RoomEntered`/`Cleared` 이벤트).
- **오브젝트 풀링**: `UnityEngine.Pool.ObjectPool<T>`를 감싸는 범용
  `GameObjectPool` 컴포넌트 하나로 Projectile/Enemy/Boss/Grenade 전부 처리.
  같은 프레임 중복 트리거/수명만료가 겹칠 때의 중복 반환은 `_hasReturned`/
  `_isReleased` 가드로 방지(2026-08-31).
- **스테이지 진행을 아이작 스타일 방/문 시스템으로 전환(2026-08-31)**: 맵
  구조는 고정 authoring 그래프(런타임 랜덤 없음, 문 방향별 연결 리스트로
  authoring해 추후 랜덤 분기 대비), 방 배치는 층 전체를 그리드에 미리
  인스턴스화(백트래킹 가능). 방 지오메트리/방-방 이동/문 잠금 정책/미니맵
  각각의 대안 비교는 `AI_Decision_Log.md` 2026-08-31 항목들 참고.
- **"업그레이드 선택" UI 폐기 → 바닥 드롭 + ESC 메뉴로 대체(2026-08-31)**:
  스킬 습득 방식을 클리어 후 강제 선택에서 바닥 드롭 픽업으로 재정의, 장착
  전환은 언제든 ESC로 열 수 있는 메뉴로 이동 — 방 클리어가 더 이상 게임
  흐름을 중단시키지 않음.
- **패시브 스탯 4종을 "base + 고정값 보너스" 모델로 통일(2026-08-31)**:
  공격속도만 배율 방식이라 Pip 바 공식이 스탯마다 갈라졌던 것을 고정값
  덧셈으로 바꿔 통일. 상세 비교는 `AI_Decision_Log.md` 참고.
- **밸런스 수치를 JSON 파일로 분리(`GameBalance`)**: 플레이어 기본 스탯/
  패시브 아이템 효과를 코드나 씬이 아니라 `Assets/StreamingAssets/Balance/
  *.json`에서 로드, 실패 시 내장 기본값으로 폴백. Unity/Claude 없이도 텍스트
  에디터로 밸런스 조정이 가능하도록 하는 것이 목적이며 실사용 확인됨(사용자가
  직접 `rangedDamage`/`AttackPower.amount` 등을 수정).
- **실제 카메라 빌보딩 도입(2026-08-31)**: 기존엔 "고정 각도 쿼터뷰 카메라라
  무회전으로 둬도 대충 맞는다"는 근사에 의존했으나(부모 회전 상속·원근
  왜곡 문제 있음), `BillboardUI`가 `LateUpdate()`에서 매 프레임 카메라
  회전과 정확히 일치시키는 방식으로 교체.
- **적 체력 바는 Enemy 프리팹에 직접 포함**: 각 Enemy 프리팹의 자식으로
  World Space `Canvas`를 두고 `HealthBarUI`+`BillboardUI`로 처리. 위치
  추적과 사망 시 숨김이 부모-자식 관계만으로 자동 해결됨.
- **자기 자신을 `Awake()`에서 `SetActive(false)`하는 UI 패턴의 함정**:
  이 패턴을 쓰는 UI(패널 GameObject 자체에 컨트롤러 컴포넌트가 붙고, 그
  컴포넌트가 자기 자신을 끄는 방식)는 그 패널이 **씬에 비활성 상태로
  저장돼 있으면** `Awake()` 자체가 지연되다가, 최초 `Show()`의
  `SetActive(true)` 호출이 그 지연된 `Awake()`를 트리거해 즉시 자기 자신을
  다시 꺼버리는 문제가 있음(2026-08-31, `ActiveItemEquipUI` 사례로 확인 —
  자세한 원인/수정은 `DevLog.md` 참고). 이 패턴을 쓰는 새 UI는 패널의 씬
  저장 상태(활성/비활성)와 `Awake()`의 자기-비활성화 호출이 서로 상충하지
  않는지 확인할 것.

---

## 5. 아직 결정하지 않은 항목

- **Game Over 흐름**: Player 사망 시 어떻게 처리할지 여전히 미정.
- **런 종료 이후 흐름**: Clear 오버레이까지만 구현, 이후 재시작/메인 메뉴
  전환은 보류 중.
- **카메라 시스템에 Cinemachine 도입 여부**: 방이 40x40으로 커지면서 방 경계
  재프레이밍이 필요한지도 함께 재검토 대상.
- **아이템 방(#3) 구체 설계**: 바닥 드롭 인프라는 있으니 "전투 없이 보장된
  픽업 배치"만 추가하면 될 것으로 예상되나 미착수.
- **방 안 장애물(#2)**: 지형 애셋 교체 시점까지 보류.
- **아이콘 스와치 → 실제 픽토그램 교체**: 우선순위 낮음으로 이월 중.
- **Enhanced Shot 스킬 사거리가 다른 변경과 함께 짧아진 게 의도한 바인지**:
  미확인.
- **`FlamethrowerActiveItem.CooldownRemaining`이 항상 0을 반환하는 갭**:
  의도된 설계인지 결정 필요.
- **씬에 남은 비활성 `Boss_Basic` 플레이스홀더 인스턴스**: 정리 여부 미결정.
- **`UIManager`를 상속 구조로 확장할지**: 두 번째 구체적인 UI 매니저 사례가
  실제로 생기기 전까지는 보류(2026-08-24 결정 유지).

---

## 6. 앞으로 해야 할 작업 (우선순위 순)

1. **실플레이 검증** — Stage 2.5 작업 다수가 자동화 환경 한계로 직접 호출/
   물리 스텝 강제 실행으로만 검증됨: 방-방 카메라 전환 연출 전체, ESC 메뉴가
   실제 키 입력으로 정상적으로 열리는지, 패시브 스탯 Pip 바/공격속도 체감,
   빌보딩, F1 디버그 오버레이, JSON 밸런스 수정 워크플로, 발사체 사거리
2. **아이템 방(#3) 설계 및 구현**
3. **Game Over 흐름 추가**
4. **잔여 정리 항목**: Flamethrower 쿨다운 UI, 비활성 `Boss_Basic` 플레이스홀더,
   아이콘 픽토그램 교체, Enhanced Shot 사거리 확인
5. **Stage 3 잔여 항목 재개**: `EnemyAIManager`, `GetComponent` 캐싱 정리,
   `BenchmarkSpawner`, Profiler 기반 Before/After 측정
6. **Phase 4/5/7** (아트 교체, Addressables, 포트폴리오 문서화)는 위 단계들이
   안정화된 이후 별도로 진행
