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
