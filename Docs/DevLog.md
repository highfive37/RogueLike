# Dev Log

날짜별로 진행한 작업과 Play Mode 테스트 결과를 기록하는 로그.
최신 항목이 위로 오도록 역순으로 누적한다.

## 작성 가이드

- 날짜는 `## YYYY-MM-DD` 형식.
- "진행 내용"은 무엇을 했는지, "테스트 결과"는 실제 Play Mode에서
  확인한 결과만 적는다 (확인 전이면 "미확인/예정"으로 남겨둘 것).
- 다음 항목을 추가할 때는 아래 템플릿을 복사해서 맨 위에 붙여넣는다.

### 템플릿

```markdown
## YYYY-MM-DD

### 진행 내용
-

### 테스트 결과
- [ ] 항목:

### 이슈/메모
-

### 다음 작업
-
```

---

## 2026-08-21

### 진행 내용
- Docs/ClaudeSessionSummary.md의 Stage 2 범위(7개 항목) 기준으로 실제 구현 상태를
  재점검 (문서 자체가 Stage 1 시점 스냅샷이라 헤더에 "⏳ 미착수"로 남아있던 것을
  실제 코드와 대조)
  - #1 (Dodge/Dash, Skill 시스템), #2 (EnemyStateMachine FSM 및 보스 패턴
    차별화), #3 (WeaponData/SkillData/EnemyData/CharacterArchetypeData
    ScriptableObject), #4 (IWeapon/MeleeWeapon/RangedWeapon 다형성)는 전부
    이전 작업일(2026-08-17, 2026-08-18)에 이미 완료되어 있던 것으로 확인됨 —
    오늘 새로 구현한 내용이 아니라 기존 완료 상태를 재확인/문서 정리한 것.
    단, `BossStateMachine`이라는 별도 클래스는 없고 `EnemyStateMachine`을
    보스와 공유 + `AttackPatternData`로 차별화하는 방식으로 목적만 달성됨,
    `UpgradeManager`도 별도 클래스 없이 `PlayerSkillInventory`+UI 조합으로 대체.
  - #5 (`CharacterSelectController`의 런타임 아키타입 선택 UI)는 미완료 상태였음
    (Inspector에 고정값만 넣어두고 선택 UI 자체가 없었음) — 이번 세션에서 착수.
  - #6 (여러 종류의 일반 Enemy), #7 (다중 방 콘텐츠 확장)은 여전히 미착수로 확인,
    이번 세션 범위에서는 보류.
- 수류탄 스킬 리뷰 및 수정 (별도 대화 흐름, 상세는 `AI_Decision_Log.md` 참고)
  - 폭발 데미지 판정(OverlapSphere+TakeDamage) 자체는 정상 동작 확인
  - "적과 충돌하면 공중에 고정되어 보이는" 증상의 원인 파악: 착탄 지점 계산용
    레이캐스트가 레이어 마스크 없이 모든 콜라이더를 대상으로 해서 적 콜라이더에
    먼저 맞아버리는 버그였음 → 전용 `Ground` 레이어 신설로 해결
  - 수류탄을 "1차 E로 들기(플레이어 오른쪽에 부착, 이 순간부터 퓨즈 시작) →
    2차 E로 던지기" 2단계 상호작용이 실제 씬 오브젝트로 보이도록
    `GrenadeProjectile`을 `Hold()`/`Throw()`로 재설계, 기존에 중복돼 있던
    `GrenadeSkill.Explode()`/`GrenadeProjectile.Explode()`를 하나로 통합(owner
    제외 로직 제거 — 너무 오래 들고 있으면 자기 자신도 피해를 입을 수 있다는
    의도된 설계를 살림)
  - 수류탄을 든 채 디버그 스킬 전환(`PrototypeSkillSwitcher`)이 안 되도록
    `ISkillItem.IsBusy` 추가, `SkillController.Equip()`에서 확인
  - `IDamageable.TakeDamage`에 `source`(Transform) 파라미터 추가, 데미지
    수치+가해자 로그를 `HealthComponent` 한 곳에서만 남기도록 중앙화 (기존에
    무기별로 흩어져 있던 개별 `Debug.Log` 제거)
- Player가 씬에 직접 배치된 오브젝트였던 것을 `Assets/Prefabs/Player.prefab`으로
  전환 (`PrefabUtility.SaveAsPrefabAssetAndConnect`로 기존 씬 인스턴스를 그대로
  유지한 채 연결 — `QuarterViewCameraFollow`/`RoomFlowManager`/
  `UpgradeSelectionUI`/`PrototypeSkillSwitcher`의 기존 직접 참조가 깨지지 않음을
  씬 파일의 `PrefabInstance` 블록으로 확인)
- #5 (`CharacterSelectController` 런타임 아키타입 선택 UI) 구현
  - `Assets/Scripts/UI/CharacterSelectUI.cs` 신규 — `UpgradeSelectionUI`와
    동일한 `Show(callback)` 패턴, 근접/원거리 버튼 2개
  - `CharacterSelectController`를 재작성: `Awake()`에서 `PlayerInputActions`를
    비활성화(모든 Awake가 모든 OnEnable보다 먼저 실행된다는 Unity의 순서
    보장을 이용해 입력이 한 프레임도 새지 않도록 함)하고, 선택 콜백에서 무기
    장착 + 입력 재활성화
  - Unity 에디터에서 Canvas 하위에 전체 화면을 완전히 가리는 불투명 배경
    패널 + 제목 텍스트 + "근접 공격"/"원거리 공격" 버튼 2개를 직접 생성하고
    배선(Player 인스턴스의 `selectUI` 씬 참조 포함, RunCommand로 처리)
  - 중복 필드 정리: `CharacterSelectController.selectedArchetype`,
    `PlayerCombat.weaponData` 필드 제거(이제 `CharacterSelectController`가
    시작 무기 결정의 유일한 경로), `Player.prefab` 재저장으로 잔여 값 정리

### 테스트 결과
- [x] 위 변경 전부(그레네이드, Player 프리팹화, CharacterSelectUI, 필드 정리)
      컴파일 에러/경고 없음 (Unity MCP 콘솔 확인)
- [ ] 실제 Play Mode에서 수류탄 들기→던지기 시각적 확인, 자폭 데미지 로그 출력,
      스킬 전환 차단 동작 — 미확인/예정
- [ ] 적 공격으로 플레이어 HP가 실제로 깎이고 로그가 남는지 — 미확인/예정
      (`HealthComponent`가 Player에 이미 붙어 있고 `EnemyStateMachine`이
      정상적으로 참조를 찾는 것은 코드로 확인, 실플레이 로그는 아직 안 봄)
- [ ] 게임 시작 시 캐릭터 선택 화면이 배경을 완전히 가리는지, 버튼 클릭으로
      근접/원거리가 정상 전환되는지, 선택 전까지 조작이 안 되는지 — 미확인/예정
      (자동화 세션은 Time.frameCount가 고정되는 한계가 있어 사람 플레이 필요)

### 이슈/메모
- `ClaudeSessionSummary.md`가 Stage 1 시점 이후 갱신되지 않아 폴더 구조,
  클래스 목록, Stage 2 진행 상태 헤더가 전부 실제와 어긋나 있던 것을 이번에
  최신 상태로 재작성함 (Stage 2 진행 상황, 씬/프리팹/태그·레이어 구성, 클래스
  책임 요약, 미결정 항목·다음 작업 전부 갱신).
- 문서 4종(`ProjectPlan.md`/`ClaudeSessionSummary.md`/`DevLog.md`/
  `AI_Decision_Log.md`)의 역할이 헷갈릴 수 있어 `Docs/DocumentationGuide.md`
  신규 작성. `talk_history.txt`(대화 원문 백업)는 `DevLog.md`+
  `AI_Decision_Log.md`만으로 충분하다고 판단해 더 이상 유지하지 않기로 함 —
  가이드 문서에서 관련 언급 제거.
- `ProjectPlan.md`는 전체를 다시 검토했지만 실제 구현과 어긋나는 내용이 없어
  (기획 수치까지 현재 스킬 데이터 기본값과 일치) 별도 수정 없음 — 애초에
  "정적 기획 원본"으로 유지하는 문서라는 원칙과도 부합.

### 다음 작업
- 위 항목들(수류탄, Player 프리팹화, 캐릭터 선택 UI) 실제 Play Mode 확인
- #6 (여러 종류의 일반 Enemy), #7 (다중 방 콘텐츠 확장) 착수 여부 결정
- `PrototypeSkillSwitcher`와 무기 아키타입 디버그 전환의 관계는 우선순위 낮음
  판단으로 보류 — 필요해지면 재검토

---

## 2026-08-18

### 진행 내용
- Stage 2-A #10~#13 마무리 작업
  - 보스 패턴1을 근접(`MeleeAttackPattern`)에서 "근접 침입 시 후퇴하며 원거리 발사"
    (`RetreatAndShootPattern`)로 교체, `EnemyAttackState`의 패턴 선택을
    라운드로빈 → 고정 우선순위(배열 앞쪽이 우선)로 변경
  - `PrototypeSkillSwitcher`가 `PlayerSkillInventory`의 실제 소지 레벨을
    반영하도록 수정 (미소지 스킬은 Lv1 고정, 소지 레벨을 부풀리지 않음)
  - `SkillCooldownUI`/`HealthBarUI`에 잔여 쿨타임·현재/최대 체력 수치 텍스트
    추가 (쿨타임 0초 도달 후 0.5초 유지 후 숨김, 배경 대비 텍스트 색 반전)
  - 스킬 쿨다운 게이지를 세로 바 방식에서 방사형(Radial360) 스와이프로 변경
    (WoW류 MMORPG 쿨다운 인디케이터 스타일)
  - 방 진행 순서를 "일반 적 스테이지 ↔ 보스 스테이지" 교대 반복으로 변경
    (`RoomFlowManager` 확장, `BossPool` 신규 추가)
- 버그 수정 (플레이 테스트 중 발견)
  - `CooldownOverlay`/`HealthBarFill` Image가 `sprite == null` 상태로
    `Type=Filled`를 쓰고 있어 `fillAmount`가 무시되고 항상 100% 칠해진 채로
    렌더링되던 문제 → Unity 내장 UISprite 할당으로 해결
  - `UpgradeSlot`/`EquipSlot`/`ConfirmButton`의 라벨 텍스트 색이 버튼 배경과
    동일한 흰색이라 안 보이던 문제 → 텍스트 색을 검정으로 변경
  - `Boss_Basic.prefab`의 `EnemyStateMachine.data`가 프리팹 자산 자체엔
    비어있어(씬의 비활성 테스트 인스턴스에만 오버라이드로 채워져 있었음)
    `BossPool`에서 새로 스폰할 때 NullReferenceException 발생 →
    프리팹에 `BossData.asset` 참조 직접 추가
  - `Boss_Basic.prefab`의 `projectilePool`(씬 전용 참조라 프리팹에 구울 수
    없음)이 비어 있어 보스가 원거리 공격을 조용히 무시하던 문제 →
    `BuildAttackPatterns()`를 `Awake()` → `Start()`로 이동하고,
    `RoomFlowManager`가 스폰 직후 `SetProjectilePool()`로 주입하는 구조로 변경

### 테스트 결과
- [x] 스킬 쿨다운 방사형 게이지 + 잔여시간 텍스트 + 색 반전 정상 동작 (사용자 확인)
- [x] HP 바 게이지 + 수치 텍스트 정상 동작 (사용자 확인)
- [x] 업그레이드/장착 선택 UI 슬롯에 스킬명·레벨 텍스트 정상 표시 (사용자 확인)
- [x] 방 클리어 → 업그레이드 UI → 장착 UI → 다음 스테이지(일반 적/보스 교대)
      흐름 정상 동작, 콘솔 에러 없음 (실제 플레이 로그로 확인: Enemy_Basic
      처치 → Room Cleared → 장착 확인 → Boss_Basic(Clone) 스폰 → 처치 →
      Room Cleared)
- [x] `BossPool` 스폰 시 NullReferenceException 재발하지 않음
      (RunCommand로 재현 검증 + 사용자 플레이로 재확인)
- [x] 보스 원거리 공격(`RetreatAndShootPattern`)이 실제로 발사되는지
      (사용자 확인)

### 이슈/메모
- 자동화 세션(Unity MCP RunCommand)에서 Play Mode에 진입해도
  `Time.frameCount`가 1에서 멈춰 있는 경우가 있었음 — 에디터가
  포커스/리페인트를 못 받는 자동화 환경의 한계로 추정, Update() 기반 로직
  검증은 결국 실제 사람 플레이로 확인해야 했음
- `UnityEngine.UI.Image`는 `Type=Filled`라도 `sprite == null`이면
  `OnPopulateMesh`가 채우기 로직을 건너뛰고 항상 꽉 찬 사각형만 그림 —
  Filled 타입 Image를 새로 만들 때는 항상 sprite를 채워둘 것
- RunCommand 스크립트에서 `Image`, `Mesh` 같은 이름을 bare로 쓰면 네임스페이스
  충돌 발생 → `UnityEngine.UI.Image`, `UnityEngine.Mesh`처럼 완전 정규화 필요
- RunCommand는 `System.Reflection` 네임스페이스 사용이 차단됨 → private
  필드/메서드 테스트가 필요하면 임시 public 디버그 메서드를 추가했다가
  검증 후 제거하는 방식으로 우회

### 다음 작업
- 남은 Stage 2 항목 확인 (`Docs/ProjectPlan.md` 기준)
- Flamethrower의 `CooldownRemaining`이 항상 0을 반환하는 알려진 갭 처리 여부 결정
- 씬에 남아있는 비활성 테스트용 Boss_Basic 플레이스홀더 인스턴스 정리 여부 결정

---

## 2026-08-17

### 진행 내용
- Stage 1 프로토타입(U1~U15) 구현 완료: Player 이동/조준/근접·원거리 공격,
  Enemy 추적, 데미지·사망 처리, Room Cleared 판정
- Object Pooling 도입(Stage 3에서 조기 반영): `GameObjectPool`,
  `PooledObject`로 Projectile/Enemy를 Destroy 대신 재사용하도록 변경
- `Docs/ClaudeSessionSummary.md`, `Docs/talk_history.txt` 작성

### 테스트 결과
- [ ] WASD 이동 / 카메라 추적 정상 동작
- [ ] 마우스 조준 정상 동작
- [ ] 근접/원거리 공격 데미지 및 사망 처리 정상 동작
- [ ] Room Cleared 로그 정상 발생 (중복 없음)
- [ ] 투사체/적 재사용이 Destroy 없이 비활성화로만 처리되는지
- 위 항목은 구현 직후 체크리스트만 전달된 상태이며, 실제 Play Mode
  확인 결과는 아직 기록되지 않음 (다음 항목 작성 시 갱신)

### 이슈/메모
- Unity MCP `set_component_property`가 필드명이 정확히 `target`일 때
  내부 오류 발생 → 필드명은 `target` 사용을 피할 것
- 오브젝트 참조는 `{"instanceID": N}` 형식만 정상 동작
- 프리팹 애셋에는 `components_to_add`(modify)가 적용되지 않음 →
  삭제 후 `create`+`save_as_prefab`로 재생성 필요

### 다음 작업
- 위 테스트 항목 Play Mode에서 직접 확인 후 결과 갱신
- Enemy 공격(플레이어 피격) 로직 추가
- Stage 2 착수 (Dodge, Skill, WeaponData/IWeapon, EnemyStateMachine 등)
