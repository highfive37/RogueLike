# AI Decision Log

Claude Code와 논의하며 내린 설계 결정을 기록하는 로그. 목적은 두 가지:
1. "AI 제안을 비판 없이 그대로 수용하지 않았다"는 근거를 남기는 것
   (`Docs/ProjectPlan.md` §9~§10 포트폴리오 원칙)
2. 나중에 "왜 이렇게 했더라?"를 다시 찾아볼 수 있게 하는 것

새 결정이 생길 때마다 아래 템플릿을 복사해서 맨 위에 추가한다 (최신이 위).

## 작성 가이드

- **검토한 대안**은 최소 2개 이상 적는다 (선택하지 않은 것도 포함).
- **선택 이유**에는 트레이드오프를 반드시 적는다 — "이게 더 좋아서"가
  아니라 "지금 시점엔 A가 낫고, B는 조건 X가 생기면 재검토"처럼.
- Claude 제안을 그대로 받아들였다면 "그대로 채택"이라고 적어도 되지만,
  왜 그 제안이 타당했는지는 한 줄이라도 남긴다.

### 템플릿

```markdown
## YYYY-MM-DD — <결정 제목>

### 배경 / 문제
-

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A | | |
| B | | |

### Claude 제안
-

### 선택한 방향
-

### 선택 이유
-

### 관련 파일
-
```

---

## 2026-08-21 — 캐릭터 선택 UI: 입력 잠금 방식과 표시 시점

### 배경 / 문제
- `CharacterSelectController`가 Inspector 고정값만 적용하고 실제 "선택" UI가
  없던 상태(Stage 2 #5 미완료 항목)를 채우는 작업. 요구사항: 근접/원거리
  버튼 2개, 게임 시작 시 1회, 하나의 씬에서 오버레이로 표시, 선택 전까지
  배경이 안 보이고 플레이어 조작도 안 되어야 함.

### 검토한 대안 (입력 잠금 방식)
| 대안 | 장점 | 단점 |
|---|---|---|
| `Time.timeScale = 0`으로 전체 일시정지 | 구현 1줄, UI 버튼 클릭은 timeScale 무관하게 동작 | `Attack`/`Skill`/`DodgeRoll` 같은 즉시성 입력 액션은 deltaTime에 의존하지 않아 timeScale=0에서도 그대로 발동됨 — 예를 들어 대시 중간에 `_isDodgeRolling=true`만 켜진 채 `FixedUpdate`가 멈춰서 상태가 꼬일 수 있음 |
| `PlayerInputActions.enabled = false`로 입력 액션 자체를 비활성화 | `PlayerInputActions.OnDisable()`이 `InputActionAsset.Disable()`을 호출해서 모든 액션이 완전히 죽음(값 읽기도 0 반환, `.performed` 콜백도 아예 안 옴), 다른 시스템(적 AI 등)에는 영향 없음 | Player 하위 컴포넌트들이 이미 `FindAction()`으로 참조를 캐시해두는 구조라 타이밍을 잘못 잡으면 한 프레임 새어나갈 여지 이론상 존재 |

### Claude 제안
- 두 번째 대안 채택 제안. Unity의 "같은 프레임 내 모든 Awake가 모든 OnEnable보다
  먼저 실행된다"는 보장을 근거로, `CharacterSelectController.Awake()`에서
  비활성화하면 `PlayerInputActions.OnEnable()`이 애초에 그 프레임에 호출되지
  않아 새는 프레임 없이 안전하다는 점을 확인.

### 선택한 방향
- 두 번째 대안 채택. `CharacterSelectController.Awake()`에서
  `GetComponent<PlayerInputActions>().enabled = false`, 아키타입 선택 콜백에서
  `true`로 복구.
- 표시 시점: 별도 씬 분리 없이 현재 씬의 Canvas 위에 전체 화면을 덮는 불투명
  `Image` 오버레이(`CharacterSelectPanel`)로 구현, `CharacterSelectController.
  Start()`에서 즉시 `Show()`.
- UI 구조는 `UpgradeSelectionUI`와 동일한 `Show(Action<T> onChosen)` 콜백
  패턴을 그대로 재사용(`CharacterSelectUI`).

### 선택 이유
- 게임 전체를 멈추는 대신 "플레이어 입력만" 끊는 게 요구사항(적 AI 등 다른
  시스템은 언급 없음)에 더 정확히 맞고, 즉시성 입력 액션이 얼어붙은 상태값을
  남기는 timeScale 방식의 부작용을 원천적으로 피할 수 있음.
- 별도 씬 분리는 사용자가 명시적으로 "일단 하나의 씬으로 진행"이라고 지정.

### 관련 파일
- `Assets/Scripts/UI/CharacterSelectUI.cs`
- `Assets/Scripts/Player/CharacterSelectController.cs`
- `Assets/Scripts/Player/PlayerInputActions.cs`

---

## 2026-08-21 — `PlayerCombat.weaponData` / `CharacterSelectController.selectedArchetype` 중복 필드 제거

### 배경 / 문제
- 캐릭터 선택 UI 도입 전에는 시작 무기를 결정하는 값이 두 군데
  (`PlayerCombat.weaponData`, `CharacterSelectController.selectedArchetype`)에
  각각 Inspector로 하드코딩되어 있었음(둘 다 Ranged로 우연히 일치). 선택 UI가
  생기면 이 둘 중 하나가 실질적으로 무의미해짐.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| `PlayerCombat.weaponData`를 기본값(폴백)으로 유지 | `CharacterSelectController` 없이 `PlayerCombat` 단독 테스트 가능 | 두 값이 다시 어긋날 경우 어느 쪽이 실제로 적용됐는지 헷갈리는 원래 문제가 재발 가능 |
| `PlayerCombat.weaponData` 필드 자체를 제거, `CharacterSelectController`를 시작 무기 결정의 유일한 경로로 단일화 | 중복 원천 차단 | `PlayerCombat` 단독으로는 무기 없이 시작(단, `CharacterSelectController`가 입력을 선택 전까지 잠가두므로 실제 위험은 없음) |

### Claude 제안
- 두 번째 대안 채택 제안. `CharacterSelectController`가 입력을 잠가두는 구조상
  `EquipWeapon()`이 항상 선택 콜백에서 호출되도록 보장되므로, `PlayerCombat`
  쪽의 기본값을 안전장치로 남겨둘 실익이 없다고 판단.

### 선택한 방향
- 그대로 채택. `PlayerCombat.weaponData` 필드와 `Awake()`의 자동 장착 로직
  삭제. `CharacterSelectController.selectedArchetype`도 이미 이전 결정으로
  제거되어 있어, 이제 시작 무기는 `CharacterSelectUI`가 들고 있는
  `meleeArchetype`/`rangedArchetype` 두 애셋 참조가 유일한 출처.
- `Player.prefab`을 재저장해서 더 이상 존재하지 않는 필드의 잔여 직렬화 값도
  제거.

### 선택 이유
- "값을 두 곳에서 따로 관리하다 어긋난다"는 게 애초에 이 정리 작업의 출발점이었던
  문제라, 폴백을 남기는 것보다 출처를 하나로 강제하는 쪽이 재발을 막음. 지금은
  `PlayerCombat`이 `CharacterSelectController` 없이 단독으로 쓰이는 경로가 없어
  안전.

### 관련 파일
- `Assets/Scripts/Player/PlayerCombat.cs`
- `Assets/Scripts/Player/CharacterSelectController.cs`
- `Assets/Prefabs/Player.prefab`

---

## 2026-08-21 — Player를 씬 전용 오브젝트에서 프리팹으로 전환

### 배경 / 문제
- `Player`가 프리팹이 아니라 `SampleScene`에 직접 배치된 오브젝트였음.
  `QuarterViewCameraFollow`/`RoomFlowManager`/`UpgradeSelectionUI`/
  `PrototypeSkillSwitcher` 4개 스크립트가 이 인스턴스를 직접 인스펙터 참조로
  물고 있어, 손대는 범위에 따라 위험도가 달라지는 상황.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 그대로 둠 | 지금 당장 작업 없음, 기존 참조 안전 | 씬 diff 노이즈, 재사용/버전관리 이점 없음, 향후 캐릭터 선택으로 인한 멀티 아키타입 확장과 방향이 어긋남 |
| 지금 프리팹으로만 전환(씬은 그대로) | 낮은 위험(같은 인스턴스 ID 유지되어 기존 4개 스크립트 참조 안 깨짐), 즉시 diff/재사용 이점 | 런타임 Instantiate화는 아직 안 됨(추후 별도 작업 필요) |
| 프리팹 전환 + 런타임 Instantiate까지 전환 | 멀티 씬/캐릭터 선택 흐름에 최종적으로 필요한 구조 | 4개 스크립트의 직접 참조를 전부 동적 탐색으로 재배선해야 하는 더 큰 작업, 아직 필요해지지 않은 시점 |

### Claude 제안
- 두 번째 대안(지금 프리팹으로만 전환) 제안. `PrefabUtility.
  SaveAsPrefabAssetAndConnect`를 쓰면 기존 씬 인스턴스의 identity가 유지되어
  기존 참조가 전혀 깨지지 않는다는 점을 근거로 제시.

### 선택한 방향
- 두 번째 대안 채택. `Assets/Prefabs/Player.prefab` 생성 및 연결, 런타임
  Instantiate 전환은 보류.

### 선택 이유
- 캐릭터 선택 UI(아키타입 선택) 작업이 예정돼 있어 프리팹화 자체는 필요한
  방향이지만, 지금 시점에 런타임 Instantiate까지 가면 4개 스크립트 재배선이라는
  범위가 불필요하게 커짐 — 필요해지는 시점(멀티 씬/런 재시작 등)에 재검토.

### 관련 파일
- `Assets/Prefabs/Player.prefab`
- `Assets/Scenes/SampleScene.unity`

---

## 2026-08-21 — 그레네이드 착탄 지점 레이캐스트가 적 콜라이더를 맞히는 버그

### 배경 / 문제
- 던진 수류탄이 적과 가까운 곳에 착탄할 때 "공중에 고정된 것처럼" 보인다는
  제보. 원인은 `GrenadeSkill.GroundHeight()`의 `Physics.Raycast`가 레이어
  마스크 없이 모든 콜라이더를 대상으로 해서, 목표 지점 위에 적이 서 있으면
  바닥이 아니라 적의 콜라이더 상단에 먼저 맞아 착탄 y좌표가 적의 몸통 높이로
  잘못 계산되는 것이었음.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 전용 `Ground` 레이어 신설 + 레이캐스트를 그 레이어로만 제한 | Unity에서 일반적인 방식, 다른 투사체/스킬에도 재사용 가능 | 프로젝트에 새 레이어 추가 + 바닥 오브젝트 레이어 재지정 필요 |
| 레이어 변경 없이 `Physics.RaycastAll`로 모든 히트를 구한 뒤 `IDamageable`을 가진 콜라이더는 건너뛰기 | 씬 설정 변경 불필요 | 데미지 가능 오브젝트가 아닌 다른 소품(상자 등)이 생기면 다시 뚫림, 히트 정렬/필터링 코드 추가 필요 |

### Claude 제안
- 두 대안을 제시하고 사용자에게 선택 요청.

### 선택한 방향
- 첫 번째 대안(전용 `Ground` 레이어) 채택. `ProjectSettings/TagManager.asset`에
  레이어 6 `Ground` 추가(Unity MCP `AddLayer` API로 등록), 씬의 `Ground`
  오브젝트를 그 레이어로 지정, `GrenadeSkill.GroundHeight()`가
  `LayerMask.GetMask("Ground")`만 대상으로 레이캐스트하도록 수정.

### 선택 이유
- Unity에서 "바닥 판정"에 전용 레이어를 쓰는 게 표준적인 방식이고, 지금은
  바닥 오브젝트가 씬에 하나뿐이라 레이어 재지정 비용이 매우 낮음. 필터링
  기반 대안은 당장은 더 적은 설정으로 끝나지만 "데미지 불가 소품"이 늘어날
  때마다 다시 뚫릴 여지가 있어 근본적이지 않다고 판단.

### 관련 파일
- `Assets/Scripts/Combat/GrenadeSkill.cs`
- `ProjectSettings/TagManager.asset`
- `Assets/Scenes/SampleScene.unity` (Ground 오브젝트 레이어)

---

## 2026-08-21 — 그레네이드 "들고 있다가 던지기" 시각화 및 폭발 로직 통합

### 배경 / 문제
- 기존엔 1차 E(무장)와 2차 E(투척) 사이에 실제 씬 오브젝트가 없어서, 손에
  들고 있다는 게 시각적으로 표현되지 않았음. 또한 "들고 있다가 퓨즈가 다 되어
  터지는 경우"(`GrenadeSkill.Explode()`)와 "던진 후 터지는 경우"
  (`GrenadeProjectile.Explode()`)에 동일한 OverlapSphere+TakeDamage 로직이
  중복돼 있었고, 그 과정에서 전자만 owner를 제외하는 등 동작이 갈라져 있었음.
  요구사항: 1차 E 시점에 수류탄이 실제로 플레이어 오른쪽에 부착되어 보이고,
  너무 오래 들고 있으면(자폭 데모용 설계 의도) 실제로 자기 자신도 피해를
  입을 수 있어야 함.

### 검토한 대안
- 별도 대안 비교 없이 사용자가 방향을 직접 지정(1차 E 시점에 실제 오브젝트를
  플레이어 오른쪽에 부착, 2차 E에 분리해서 기존 포물선으로 투척).

### Claude 제안
- 지정된 방향을 구현하면서, 겸사겸사 두 곳에 중복돼 있던 폭발 로직을 하나로
  합칠 것을 제안: `GrenadeProjectile`에 `Hold(holder, localOffset, ...)` /
  `Throw(target, duration)` 두 생명주기 메서드를 추가해서 "들고 있음"과
  "던져짐"을 같은 오브젝트의 두 상태로 표현하면, 폭발 판정이 `Explode()`
  하나로 자연스럽게 합쳐지고 owner 제외 로직 없이 둬도(자폭 가능) 일관됨.

### 선택한 방향
- 그대로 채택. `GrenadeSkill.Explode()` 삭제, `GrenadeProjectile.Explode()`
  하나로 통합(owner 제외 없음). 수류탄을 든 채 디버그 스킬 전환이 되지 않도록
  `ISkillItem.IsBusy`(최초엔 `CanUnequip`으로 이름 붙였다가, "허가"가 아니라
  "상태" 관점이 맞다는 사용자 피드백으로 `IsBusy`로 변경 — 기존
  `IAttackPattern.IsBusy`와 이름 통일) 추가, `SkillController.Equip()`에서 확인.

### 선택 이유
- 폭발 로직을 하나로 합치는 게 중복 코드 제거일 뿐 아니라, "들고 있다가
  터지면 자기 피해가 가능해야 한다"는 설계 의도를 두 곳에서 따로 챙길
  필요 없이 자동으로 만족시킴.
- `IsBusy`로 이름을 바꾼 이유: `CanUnequip`은 "장착 해제 가능 여부"라는 좁은
  의미라 이 플래그를 스킬 전환 판단 외의 다른 용도(예: 향후 UI 표시)로
  재사용하기 어려움. `IsBusy`는 상태를 그대로 노출하므로 범용적이고, 기존
  `IAttackPattern.IsBusy`(보스 후퇴 중 패턴 차단 용도)와 같은 어휘를 씀.

### 관련 파일
- `Assets/Scripts/Combat/GrenadeProjectile.cs`
- `Assets/Scripts/Combat/GrenadeSkill.cs`
- `Assets/Scripts/Combat/ISkillItem.cs`
- `Assets/Scripts/Player/SkillController.cs`

---

## 2026-08-21 — 데미지 판정에 가해자(source) 정보 추가 및 로그 중앙화

### 배경 / 문제
- 수류탄 자폭이 실제로 가능한지 검토하던 중, Player에 `HealthComponent`가
  이미 붙어 있어 자폭이 실제로 가능하다는 게 확인됨. 이걸 눈으로 확인할
  방법(로그)이 필요했고, 기존엔 무기마다(`MeleeWeapon`, `FlamethrowerSkill`,
  `Projectile`) 각자 다른 형식으로 `Debug.Log`를 찍거나 아예 안 찍는 곳
  (`GrenadeProjectile`, `MeleeAttackPattern`)이 섞여 있었음.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 각 무기/스킬 호출부에 `Debug.Log(damage, hit.name)` 개별 추가 | `IDamageable` 인터페이스 변경 불필요, 호출부 몇 줄만 추가 | "가해자가 누구인지"는 호출부마다 다시 챙겨야 함, 형식이 또 제각각이 될 위험 |
| `IDamageable.TakeDamage`에 `source` 파라미터를 추가하고 로그를 구현체(`HealthComponent`) 한 곳에서만 남김 | 모든 데미지 경로가 강제로 같은 형식의 로그를 남기게 됨, 향후 데미지 관련 공통 처리(예: 데미지 감소 버프)도 같은 자리에 추가 가능 | `IDamageable`의 유일한 구현체(`HealthComponent`)는 하나뿐이라 안전하지만, 호출부 6곳(+누락됐던 `RangedWeapon` 1곳) 전부 `source` 인자를 추가해야 하는 더 넓은 변경 |

### Claude 제안
- 두 번째 대안 채택 제안. 사용자가 명시적으로 "IDamageable이든지 데미지
  처리하는 상위 클래스에서" 로그를 남기라고 지정했고, `IDamageable` 구현체가
  `HealthComponent` 하나뿐이라 인터페이스 변경의 파급 범위가 통제 가능하다고
  판단.

### 선택한 방향
- 그대로 채택. `IDamageable.TakeDamage(float amount, Transform source)`로
  변경, `HealthComponent.TakeDamage`에서
  `Debug.Log($"{name} took {amount} damage from {source.name}.")` 형태로
  로그. 각 호출부(`GrenadeProjectile`, `MeleeWeapon`, `FlamethrowerSkill`,
  `Projectile`/`ProjectileSpawner`, `RangedWeapon`, `MeleeAttackPattern`)에
  owner/source Transform 전달하도록 수정, 기존에 흩어져 있던 개별
  `Debug.Log` 제거(`FlamethrowerSkill`의 "1회만 로그" 용 `HashSet` 필드도
  같이 정리 — 더 이상 쓸 곳이 없어짐).
- `Health`(순수 C# 클래스, UnityEngine 비의존)에는 `source` 개념을 넣지 않고
  어댑터인 `HealthComponent`에서만 처리 — Unity 의존성을 낮은 레이어로
  누출시키지 않기 위함.

### 선택 이유
- 무기 개수가 늘어날수록 "로그 형식 통일"을 호출부마다 강제하기 어려워지므로,
  강제성이 있는 인터페이스 레벨에서 처리하는 쪽이 장기적으로 더 안전.

### 관련 파일
- `Assets/Scripts/Combat/IDamageable.cs`, `HealthComponent.cs`
- `Assets/Scripts/Combat/GrenadeProjectile.cs`, `MeleeWeapon.cs`,
  `FlamethrowerSkill.cs`, `Projectile.cs`, `ProjectileSpawner.cs`,
  `RangedWeapon.cs`
- `Assets/Scripts/Enemy/MeleeAttackPattern.cs`, `RangedAttackPattern.cs`,
  `RetreatAndShootPattern.cs`

---

## 2026-08-18 — 풀 스폰 인스턴스가 씬 전용 참조(ProjectilePool)를 받는 방식

### 배경 / 문제
- `BossPool`이 `Boss_Basic.prefab`에서 직접 `Instantiate`하는데,
  `EnemyStateMachine.projectilePool`은 씬에만 존재하는 `GameObjectPool`
  (투사체 풀) 참조라 프리팹 애셋 자체에는 구워 넣을 수 없음 (Unity의 구조적
  제약: 프리팹 애셋은 씬 오브젝트를 참조할 수 없음).
- 결과적으로 풀에서 새로 찍어낸 보스는 원거리 공격을 시도해도 투사체 스폰이
  조용히 무시됨 (크래시는 안 나지만 공격이 실제로는 안 나감).

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| `EnemyStateMachine.Awake()`에서 `GameObject.Find("ProjectilePool")`로 자체 탐색 | 스포너 쪽 코드를 안 건드려도 됨, 코드 몇 줄로 끝남 | 오브젝트 이름("ProjectilePool")이라는 암묵적 문자열 계약에 의존, 이름이 바뀌면 조용히 깨짐 |
| 풀 스폰 시점에 스포너(`RoomFlowManager`)가 `SetProjectilePool()`로 직접 주입 | 의존 관계가 코드상에 명시적으로 드러남, 문자열 이름에 의존하지 않음 | `BuildAttackPatterns()`가 `Awake()`에서 즉시 실행되던 걸 `Start()`로 늦춰야 함(주입이 그 사이에 끝나야 하므로), 구조 변경 필요 |

### Claude 제안
- 두 대안을 모두 제시하고 사용자에게 선택 요청. 문자열 이름 의존 없이 명시적
  의존성 주입으로 갈지, 최소 변경으로 갈지의 트레이드오프라 임의로 정하지 않음.

### 선택한 방향
- "풀 스폰 시점에 주입" 채택.
- `EnemyStateMachine.BuildAttackPatterns()` 호출을 `Awake()` → `Start()`로
  이동, `public void SetProjectilePool(GameObjectPool)` 추가.
- `RoomFlowManager.OnEquipConfirmed()`에서 `pool.Get()` 직후 반환된
  인스턴스에 `SetProjectilePool()` 호출 (보스뿐 아니라 일반 Enemy 스폰
  경로에도 동일 적용 — 나중에 일반 적이 원거리 패턴을 갖게 되어도 재작업
  불필요하도록).

### 선택 이유
- `GameObject.Find` 기반 자체 탐색은 지금 당장은 더 적은 코드로 끝나지만,
  "오브젝트 이름"이라는 암묵적 문자열 계약에 의존하게 되어 나중에 씬 구조가
  바뀌면 원인 파악이 어려운 조용한 버그로 이어질 위험이 있다고 판단.
- `Awake()` → `Start()` 이동은 Unity가 보장하는 "같은 프레임 내
  Awake/OnEnable은 즉시, Start는 그 다음"이라는 표준 라이프사이클 순서를
  그대로 활용한 것이라 별도의 타이밍 코드(코루틴, 프레임 카운터 등)가
  필요 없음.

### 관련 파일
- `Assets/Scripts/Enemy/EnemyStateMachine.cs`
- `Assets/Scripts/Room/RoomFlowManager.cs`

---

## 2026-08-18 — 보스 패턴1: 근접 대신 "반응 사거리 ≠ 공격 사거리" Kiting 구조 도입

### 배경 / 문제
- 임시로 만들었던 보스 패턴1(`MeleeAttackPattern`)이 일반 Enemy와 동일한
  근접 공격이라, 패턴2(원거리)와 컨셉이 겹치고 보스다운 차별점이 없었음.
- 플레이어가 근접 사거리 안으로 들어왔을 때 보스가 원거리 사거리까지
  후퇴한 뒤 발사하도록 바꾸고 싶다는 요청.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 감지(반응) 사거리 = 공격 사거리로 단일화 | 구현 단순 | "후퇴 후 발사"라는 의도한 행동을 표현할 수 없음 |
| 거리 구간별로 패턴 자체를 완전히 스왑(근접/중거리/원거리 3패턴 이상) | 확장성 최고, 다양한 보스 패턴 설계에 유리 | 지금 시점엔 패턴이 2개뿐이라 과설계, 구현 비용 대비 이득 낮음 |
| 트리거 사거리(반응 range)와 발사 사거리(선호 range)를 분리한 단일 패턴(`RetreatAndShootPattern`) | 지금 요구사항에 정확히 맞음, 기존 `IAttackPattern`에 `IsBusy` 하나만 추가하면 구현 가능 | 트리거 사거리와 일반 원거리 패턴 사거리가 겹치는 구간에서 패턴 선택 로직을 신경 써야 함 |

### Claude 제안
- 세 번째 대안 채택 제안. WoW류 게임에서도 저격수/원거리 AI가 "반응
  사거리(넓음) → 선호 사거리까지 후퇴 → 발사"하는 kiting 패턴이 일반적이라는
  근거 제시. 다만 트리거 사거리와 일반 원거리 패턴 사거리가 겹치는 문제가
  있어, 패턴 선택을 기존 라운드로빈에서 "배열 고정 순서(앞쪽이 우선)"로
  바꿔 겹치는 구간에서 항상 kiting이 이기도록 함께 변경 제안.

### 선택한 방향
- 그대로 채택. `RetreatAndShootPattern`(트리거 range=근접+1, 선호 range=
  원거리 발사 지점) 신규 작성, `BossData.asset`의 `attackPatterns` 배열에서
  kiting 패턴을 원거리 패턴보다 앞에 배치.
- `IAttackPattern`에 `IsBusy` 추가하여 "후퇴 중"인 여러 프레임짜리 행동이
  다른 패턴에 의해 중간에 끊기지 않도록 `EnemyAttackState`가 활성 패턴을
  붙잡고 있게 함.

### 선택 이유
- 지금 보스 패턴이 2개뿐인 상황에서 거리 구간별 스왑 구조까지 만드는 건
  과설계 — 패턴이 3개 이상으로 늘어나는 시점에 재검토하기로 함
  (`AttackPatternData[]` 배열 구조 자체는 이미 그 방향으로 확장 가능하게
  설계돼 있어, 나중에 필요해지면 데이터 애셋만 추가하면 됨).
- 라운드로빈 → 고정 우선순위 변경은 "공정한 로테이션"보다 "특정 상황에선
  특정 패턴이 항상 이겨야 한다"는 요구가 더 중요하다고 판단한 트레이드오프.

### 관련 파일
- `Assets/Scripts/Enemy/RetreatAndShootPattern.cs`,
  `Assets/Scripts/Enemy/RetreatAndShootPatternData.cs`
- `Assets/Scripts/Enemy/EnemyAttackState.cs`,
  `Assets/Scripts/Enemy/IAttackPattern.cs`
- `Assets/Data/AttackPatterns/RetreatAndShoot.asset`,
  `Assets/Data/Enemies/BossData.asset`

---

## 2026-08-17 — 발사체/적 파괴를 Object Pooling으로 전환

### 배경 / 문제
- 원거리 투사체가 수명 만료 시, Enemy가 사망 시 각각 `Destroy(gameObject)`를
  호출하고 있었음. Stage 3(최적화)에서 다루기로 계획했던 항목이었으나,
  반복 스폰/파괴가 잦은 발사체부터 먼저 구조를 바꿔두는 게 낫다고 판단해
  Stage 1 단계에서 앞당겨 반영하기로 함.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 그대로 Destroy 유지 (계획대로 Stage 3까지 보류) | 지금 당장 구현 비용 없음 | 반복 전투 테스트마다 Instantiate/Destroy 반복, 나중에 되돌아와 리팩터링 필요 |
| Projectile/Enemy 각각 전용 풀 클래스 작성 | 타입별 최적화 여지 | 코드 중복, 이후 무기/적 종류가 늘어날 때마다 풀 클래스도 늘어남 |
| Unity 내장 `ObjectPool<T>`를 감싼 범용 `GameObjectPool` 하나로 통일 | 타입 무관 재사용 가능, 내장 API라 신뢰도 높음(Unity 6.3 공식 지원) | 프리팹마다 `PooledObject` 부착을 신경 써야 함, 재사용 시 상태 리셋(HP 등)은 별도로 챙겨야 함 |

### Claude 제안
- 세 번째 대안(범용 `GameObjectPool` + `PooledObject` 마커 컴포넌트) 제안.
  근거: `unity63-performance-quality` 스킬로 `UnityEngine.Pool.ObjectPool<T>`가
  Unity 6.3 공식 API임을 확인했고, Projectile과 Enemy가 구조적으로 동일한
  "죽으면 비활성화, 필요하면 재사용" 패턴이라 하나의 컴포넌트로 충분하다고 판단.

### 선택한 방향
- 그대로 채택. `GameObjectPool`(범용) + `PooledObject`(마커) 조합으로 구현.
- 단, `HealthComponent`의 사망 처리 로직은 `PooledObject`가 있으면 그쪽으로,
  없으면 기존 `destroyOnDeath` 플래그로 폴백하도록 이원화(Player는 아직
  풀링 대상이 아니므로).

### 선택 이유
- Stage 1 시점에는 적 스포너가 없어 "재사용 시 HP 리셋 누락" 문제가 아직
  드러나지 않음 — 지금 당장 완벽한 리셋 로직까지 만드는 건 과설계로 판단,
  Stage 2에서 스포너를 만들 때 함께 처리하기로 함(`Docs/ClaudeSessionSummary.md`
  §5에 미결정 항목으로 기록).
- 타입별 풀 클래스를 따로 만들지 않은 이유는 지금 시점에 Projectile/Enemy
  외 풀링 대상이 없어 중복 클래스를 만들 근거가 부족했기 때문.

### 관련 파일
- `Assets/Scripts/Pooling/GameObjectPool.cs`
- `Assets/Scripts/Pooling/PooledObject.cs`
- `Assets/Scripts/Combat/Projectile.cs`, `Assets/Scripts/Combat/HealthComponent.cs`
- `Assets/Prefabs/Projectile_Basic.prefab`, `Assets/Prefabs/Enemy_Basic.prefab`
