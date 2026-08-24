# Claude Session Summary — RogueLike (3D Arena Roguelite)

이 문서는 Claude Code + Unity MCP로 진행한 개발의 **현재 구조 스냅샷**이다.
날짜별 작업 사실은 `Docs/DevLog.md`, 개별 설계 결정의 대안/근거는
`Docs/AI_Decision_Log.md`를 참고. 원본 기획 문서는 `Docs/ProjectPlan.md`.
각 문서의 역할 구분은 `Docs/DocumentationGuide.md` 참고.

마지막 갱신: 2026-08-21 (Stage 2 상당 부분 완료 시점 기준).

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
- New Input System **정식 도입 완료** — `PlayerInputActions` 컴포넌트가
  `InputActionAsset`을 감싸고, 각 플레이어 스크립트는 `FindAction()`으로 액션을
  가져와 사용. Stage 1 시점의 `Keyboard.current`/`Mouse.current` 저수준 폴링은
  더 이상 쓰지 않음.
- Cinemachine 미설치, AI Navigation 설치되어 있으나 여전히 미사용 (적 이동은
  FSM 상태 + `Rigidbody` 기반)

### 폴더 구조 (대표 클래스만, 전체 목록은 코드 참고)
```
Assets/Scripts/
├─ Player/    (PlayerMovement, PlayerAim, PlayerDodgeRoll, PlayerCombat,
│              PlayerInputActions, SkillController, PlayerSkillInventory,
│              CharacterSelectController, PrototypeSkillSwitcher 등)
├─ Enemy/     (EnemyStateMachine + FSM 상태 6종, IAttackPattern 구현체 3종,
│              EnemyData / AttackPatternData ScriptableObject)
├─ Combat/    (IDamageable, Health, HealthComponent, IWeapon 구현체 2종,
│              ISkillItem 구현체 3종, Projectile류, SkillItemData류)
├─ Camera/    (QuarterViewCameraFollow)
├─ Room/      (RoomManager, RoomFlowManager — 같은 GameObject에 함께 배치)
├─ UI/        (UpgradeSelectionUI, EquipSelectionUI, CharacterSelectUI,
│              HealthBarUI, SkillCooldownUI 등)
└─ Pooling/   (GameObjectPool, PooledObject)
```
asmdef(어셈블리 분리)는 여전히 도입하지 않음 — 스크립트 규모상 과설계로 판단.

### 씬 구성 (SampleScene)
- `Ground`(Ground 레이어 지정), `Player`(**프리팹 인스턴스**, 2026-08-21부터 —
  이전에는 씬에 직접 배치된 오브젝트였음), `Main Camera`
- `Enemy_Basic`/`Boss_Basic`은 씬에 고정 배치가 아니라 각각 `EnemyPool`/
  `BossPool`에서 `RoomFlowManager`가 스폰 (단, 씬에 비활성 테스트용
  `Boss_Basic` 플레이스홀더 인스턴스가 하나 남아있음 — 정리 여부 미결정)
- 풀 4종: `EnemyPool`, `BossPool`, `GrenadePool`, `ProjectilePool`
  (전부 `GameObjectPool` 컴포넌트 공용)
- `RoomManager` 오브젝트 하나에 `RoomManager`+`RoomFlowManager` 컴포넌트가
  함께 붙어 있음
- `Canvas`(UGUI 1개) 아래에 HUD(`HealthBarUI`/`SkillCooldownUI`),
  `UpgradePanel`, `EquipPanel`, `CharacterSelectPanel`이 전부 자식으로 배치,
  `EventSystem` 별도

### 프리팹
- `Assets/Prefabs/Player.prefab` (2026-08-21 신규 — 이전엔 씬 전용 오브젝트)
- `Assets/Prefabs/Enemy_Basic.prefab`, `Boss_Basic.prefab`
- `Assets/Prefabs/Projectile_Basic.prefab`, `Grenade.prefab`

### 태그 / 레이어
- 태그: `Player`(기본), `Enemy`(추가) — 변경 없음
- 레이어: `Ground`(2026-08-21 신규) — 그레네이드 착탄 지점 레이캐스트가 적
  콜라이더를 잘못 맞히던 버그를 고치기 위해 도입, `Ground` 오브젝트를 이
  레이어로 지정하고 관련 레이캐스트를 이 레이어로 제한

### 클래스 책임 요약 (영역별 대표 클래스 — 전체 목록 아님)
| 영역 | 대표 클래스 | 책임 |
|---|---|---|
| 이동/조준 | `PlayerMovement`, `PlayerAim`, `PlayerDodgeRoll` | Rigidbody 기반 이동/회전/구르기, New Input System 액션 소비 |
| 전투(플레이어) | `PlayerCombat`+`IWeapon`(`MeleeWeapon`/`RangedWeapon`), `SkillController`+`ISkillItem`(`EnhancedShotSkill`/`GrenadeSkill`/`FlamethrowerSkill`) | 기본 공격과 장착 스킬을 각각 다형성 구조로 분리 |
| 캐릭터 선택 | `CharacterSelectController`, `CharacterSelectUI`, `CharacterArchetypeData` | 런 시작 시 근접/원거리 아키타입 선택 UI, 선택 전까지 입력 잠금 |
| 스킬 보유/장착 | `PlayerSkillInventory`, `OwnedSkill` | 스킬 소지/레벨 추적, 장착 전환 |
| 적 AI | `EnemyStateMachine`(FSM: Idle→Patrol→Chase→Attack→Stunned/Dead), `IAttackPattern`(`MeleeAttackPattern`/`RangedAttackPattern`/`RetreatAndShootPattern`) | 일반 적과 보스가 같은 FSM을 공유, 공격 패턴 데이터로 차별화 (별도 `BossStateMachine` 클래스는 없음) |
| 데미지 | `IDamageable`, `Health`(순수 C#), `HealthComponent` | 데미지 판정 + 수치·가해자 로그(2026-08-21 중앙화), 체력 상태, 사망 이벤트 |
| 방 진행 | `RoomManager`, `RoomFlowManager` | 전멸 판정 → 업그레이드 선택 → 장착 선택 → 다음 방(일반/보스 교대) 스폰 |
| 풀링 | `GameObjectPool`, `PooledObject` | Projectile/Enemy/Boss/Grenade 공용 재사용 풀 |
| UI | `HealthBarUI`, `SkillCooldownUI`, `UpgradeSelectionUI`, `EquipSelectionUI`, `CharacterSelectUI` | HUD 및 3종 선택 화면(업그레이드/장착/캐릭터) |

---

## 3. Stage 1~3 계획과 진행 상황

원래 `ProjectPlan.md`의 Phase 1~7을 그대로 따르지 않고, **프로토타입 → 핵심 시스템
→ 최적화** 3단계로 재구성해서 진행 중.

### Stage 1 — 프로토타입 (✅ 완료, U1~U15)
최소 플레이 가능한 전투 루프. Player 이동/조준/근접·원거리 공격, Enemy 추적,
데미지·사망 처리, Room Cleared 판정까지 구현 완료.

### Stage 2 — 핵심 시스템 (🔶 대부분 완료, 2개 항목 남음)

✅ 완료:
- Dodge/Dash(`PlayerDodgeRoll`), 단일 슬롯 스킬 시스템(`SkillController`/
  `ISkillItem`, E키)
- `EnemyStateMachine` FSM. 단, `BossStateMachine`은 별도 클래스로 만들지 않고
  `EnemyStateMachine`을 보스와 공유 + `AttackPatternData`로 차별화하는 방식으로
  목적을 달성
- `WeaponData`/`SkillData`/`EnemyData`/`CharacterArchetypeData` ScriptableObject
  전부 도입
- `IWeapon`/`MeleeWeapon`/`RangedWeapon` 다형성 구조로 승격 (Stage 1의 단순
  enum 분기 대체)
- Boss 프로토타입(`Boss_Basic`+`BossPool`+`RetreatAndShootPattern` kiting 패턴)
- `RoomFlowManager`(일반/보스 교대 진행), `PlayerSkillInventory`+
  `UpgradeSelectionUI`+`EquipSelectionUI`(업그레이드 선택 — 별도
  `UpgradeManager` 클래스 없이 대체), HUD(`HealthBarUI`/`SkillCooldownUI`)
- `CharacterSelectController`+`CharacterSelectUI`(런 시작 시 근접/원거리 선택,
  2026-08-21 완료)

⏳ 남음:
- 여러 종류의 일반 Enemy — 지금은 `EnemyBasicData` 하나뿐, 데이터만 다른 변종
  추가 필요 (스크립트 변경은 불필요한 구조)
- 다중 방 콘텐츠 — 지금은 방마다 적 1마리, 일반/보스를 무한 교대하는 최소
  프로토타입 루프뿐

### Stage 3 — 최적화 (부분 착수)
- ✅ Object Pooling — `Projectile`/`Enemy`/`Boss`/`Grenade` 4종 전부 적용
- ⏳ 나머지: AI tick 분산(`EnemyAIManager`), `GetComponent` 캐싱 정리,
  `BenchmarkSpawner`(20/50/100/200 시나리오), Profiler 기반 Before/After 측정 문서화

Phase 4(아트/연출), Phase 5(Addressables), Phase 7(포트폴리오 문서화)는 이
3단계 밖의 별도 트랙으로 남겨둠 — 필요해지는 시점에 진행.

---

## 4. 지금까지 선택한 설계 방향과 이유 (핵심만 — 개별 결정의 대안 비교는 `AI_Decision_Log.md`)

- **이동/회전은 Rigidbody + MovePosition/MoveRotation**: Update에서 입력을
  샘플링하고 FixedUpdate에서 소비하는 패턴. CharacterController 대신 선택한
  이유는 향후 넉백 등 물리 상호작용 확장이 쉬워서.
- **Input Actions 에셋 정식 도입 완료**: `PlayerInputActions`가
  `InputActionAsset`을 래핑하고, 모든 플레이어 입력(이동/조준/공격/스킬/구르기)이
  `FindAction()`으로 캐시한 액션을 통해 흐름. `CharacterSelectController`가
  선택 전 입력을 잠글 때도 `PlayerInputActions.enabled`를 끄는 방식으로 이
  구조를 그대로 활용(모든 `Awake`가 모든 `OnEnable`보다 먼저 실행된다는
  Unity의 순서 보장 이용).
- **무기 타입은 IWeapon 다형성으로 승격 완료**: `PlayerCombat`은 `IWeapon`
  인터페이스만 들고 있고, 실제 무기(근접/원거리)는 `CharacterSelectController`가
  런 시작 시 아키타입 선택 UI 결과에 따라 결정. `PlayerCombat.weaponData`처럼
  별도로 기본값을 들고 있던 중복 필드는 2026-08-21에 제거.
- **`Health`를 plain C# 클래스로 분리**: `MonoBehaviour`에 직접 로직을 두지 않고
  UnityEngine 비의존 클래스로 만들어 EditMode 테스트가 가능하도록 함.
  `HealthComponent`는 이를 감싸는 얇은 어댑터 역할만 — 단, 데미지 로그(수치 +
  가해자)는 Unity `Transform` 정보가 필요해 `HealthComponent` 쪽에서만 남김
  (`Health`는 여전히 UnityEngine 비의존 유지).
- **이벤트 기반 결합도 관리**: `HealthComponent`와 `RoomManager`는 서로를 직접
  참조하지 않고 `Health.Died` 이벤트로만 연결됨.
- **오브젝트 풀링을 Stage 1로 앞당김**: 원래 Stage 3(최적화) 항목이었으나,
  사용자가 발사체/적 파괴 시 `Destroy` 대신 재사용을 명시적으로 요청하여 즉시
  반영. Unity 6.3 내장 `UnityEngine.Pool.ObjectPool<T>`를 감싸는 범용
  `GameObjectPool` 컴포넌트 하나로 Projectile/Enemy/Boss/Grenade 전부 처리.
- **`destroyOnDeath` 플래그를 폴백으로 유지**: `PooledObject`가 없는 오브젝트
  (Player)는 여전히 `destroyOnDeath` bool로 제어되고 `false`로 고정되어 있음.
  Player는 여전히 Game Over 흐름이 없어 사라지지 않게 해둔 상태 그대로 —
  단, 적 공격이 실제로 Player에게 데미지를 줄 수 있다는 것 자체는 이제
  코드로 확인됨(아래 §5 참고).
- **Player를 씬 전용 오브젝트에서 프리팹으로 전환**(2026-08-21): 캐릭터 선택
  기능 도입을 계기로, 기존 씬 인스턴스를 그대로 유지한 채(`PrefabUtility.
  SaveAsPrefabAssetAndConnect`) `Assets/Prefabs/Player.prefab`으로 연결.
  런타임 Instantiate 전환은 아직 하지 않음.

---

## 5. 아직 결정하지 않은 항목

- **Game Over 흐름**: Player 사망 시 어떻게 처리할지(씬 재시작? UI?) 여전히
  미정. 이제 적 공격(근접/원거리/키팅)이 실제로 Player의 `HealthComponent`에
  데미지를 줄 수 있다는 것까지 확인된 상태라(2026-08-21), 이 결정이 더 급해짐.
- **여러 종류의 일반 Enemy 데이터 설계**: 구조(EnemyData ScriptableObject +
  공유 스크립트)는 준비돼 있으나, 실제로 어떤 변종(빠르고 약함/느리고
  단단함 등)을 추가할지는 미정.
- **Room 진행/멀티룸 흐름 세부 설계**: 지금의 `RoomFlowManager`는 방마다 적
  1마리, 일반/보스 무한 교대뿐인 최소 루프. 실제 5~10개 스테이지 흐름이나
  종료 조건은 미정.
- **카메라 시스템에 Cinemachine 도입 여부**: 지금은 수동 스크립트로 충분하지만
  연출이 복잡해지면(줌, 화면 흔들림 등) 재검토 필요.
- **`FlamethrowerSkill.CooldownRemaining`이 항상 0을 반환하는 갭**: 쿨다운
  UI에서 화염방사기만 항상 "준비됨"으로 보이는 상태 — 의도된 설계인지 결정 필요.
- **씬에 남은 비활성 `Boss_Basic` 플레이스홀더 인스턴스**: 테스트용으로 남아있는
  것을 정리할지 여부 미결정.
- **`PrototypeSkillSwitcher`와 무기 아키타입 디버그 전환의 관계**: 스킬은
  숫자키로 디버그 전환이 가능한데 무기 아키타입도 비슷한 단축키를 둘지 —
  우선순위 낮음으로 보류 중.

---

## 6. 앞으로 해야 할 작업 (우선순위 순)

1. **Game Over 흐름 추가** — 적 공격이 실제로 작동하는 게 확인된 만큼 가장 시급
2. **여러 종류의 일반 Enemy 데이터 추가** — 스크립트 변경 없이 `EnemyData`
   애셋만 추가하면 되는 구조라 상대적으로 작은 작업
3. **Room 진행 방식 확장 여부 결정** — 방당 다중 스폰/종료 조건 등 범위 논의
   필요
4. **잔여 정리 항목**: Flamethrower 쿨다운 UI 처리 방식, 비활성 `Boss_Basic`
   플레이스홀더 정리
5. **Stage 3 잔여 항목**: `EnemyAIManager`(AI tick 분산), `GetComponent` 캐싱
   정리, `BenchmarkSpawner`(20/50/100/200 적), Profiler 기반 Before/After 측정
6. **Phase 4/5/7** (아트 교체, Addressables, 포트폴리오 문서화)는 위 단계들이
   안정화된 이후 별도로 진행
