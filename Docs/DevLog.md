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
