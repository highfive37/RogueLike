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

## 2026-09-09 (3) — 절차적 층 생성: 알고리즘 / 시드 / 범위 / 결정성 (포트폴리오 트랙 #2 설계 확정)

### 배경 / 문제
- 장르를 "3D Binding of Isaac / Enter the Gungeon 계열"로 명시(유저 선언 2026-09-09). 그런데 현재
  층은 손authoring된 고정 3개(`FloorN.json`) — "선언한 장르 = 절차적 생성"과 현 구현의 최대 간극.
- `IFloorLayoutProvider` 심은 애초에 이 교체를 위해 만들어짐(isaac-room-system 메모리:
  "user explicitly wants random map generation eventually", `RoomData`의 next-room을 방향→id
  리스트로 모델링한 것도 대비).

### 검토한 대안 (5개 축, 유저와 논의해 확정)
| 축 | 대안 | 선택 |
|---|---|---|
| **생성 알고리즘** | A 아이작식 큐 성장 / B 스패닝트리+추가엣지 / C 템플릿 조립 | **A** — 장르 그 자체 알고리즘, ~60줄, 시작에서 자라 자연히 연결, 데드엔드가 특수방으로 자연 배정. B는 A로 수렴, C는 authoring 과함 |
| **시드 출처** | a run마다 랜덤 / b 디버그 고정 / c 둘 다(기본 랜덤 + 오버라이드) | **c** — 타이틀 시드 입력칸은 Phase 2b로 분리 |
| **절차화 범위** | i 그래프+타입만 / ii + 방별 적 구성 / iii + 아이템 드랍 | **ii** — 스폰 위치는 `RoomBounds` 안 지터, 변형 믹스는 층별 가중 테이블. AI 미세 랜덤(wobble 위상)은 `UnityEngine.Random` 유지 |
| **JSON provider 운명** | 삭제 / 디버그 대안 모드로 유지 | **유지** — "이런 작업을 했었다"는 흔적 + 손상 폴백 로직(`GameFloors` 비상층 + `IsCorrupted` 팝업) 재활용 |
| **난이도 스케일 저장** | a 코드 상수 / b JSON 파일 / c ScriptableObject | **b** `Assets/StreamingAssets/Balance/Stage/ProceduralRules.json` — `GameFloors`/`GameBalance`와 동일 패턴(텍스트 튜닝 + 내장 폴백). 층별 rule 3줄(이산 표, 곡선 함수 아님): `roomCount{min,max}` / `itemRoomChance` / `enemiesPerCombatRoom{min,max}` / `variantWeights{Melee,Ranged,ErraticMelee,BurstRanged}`. 층 수 초과 시 마지막 rule 반복 |

### 결정성 범위
- 시드가 좌우: 레이아웃 + 적 구성. per-system 스트림 `RngStreams`(마스터 시드 → 이름별 서브스트림,
  안정 문자열 해시로 파생) — `layout` / `enemies` 스트림 사용, `drops`는 이름만 예약(범위 iii 확장 시).
- **같은 시드 + 같은 `ProceduralRules.json` + 같은 생성기 코드 → 항상 동일한 3개 층.** 규칙 파일은
  시드와 나란한 입력이라 편집하면 같은 시드라도 다른 층(정상, 문서 명시).
- 전투 미세 랜덤(크리·wobble 등)은 결정성 대상 아님.
- 층 번호(1/2/3)로 rule 인덱싱. 층 수가 가변이 되면 정규화 깊이 `depth∈[0,1]` + 공식/AnimationCurve로
  전환 — 후일 과제.

### 심 위치
- `FloorLayout` 리스트 생산지(`GameFloors.Floors`)에 `IFloorSource` 추상 → `JsonFloorSource`(현행
  래핑) / `ProceduralFloorSource`(신규). `StageManager`는 `FloorLayout` 형태가 같아 소비 코드 거의 불변.
- `RunConfig`(시드 + `FloorSourceMode`)를 `GameFlow`가 run 넘겨 전달. 에디터 직접 실행은 기본
  `RunConfig`(랜덤 시드) 폴백.

### 작업 단계 (승인됨, 순차)
A) RNG 아키텍처(`IRng`/`DeterministicRng`(자체 PRNG, xorshift+splitmix 시딩)/`RngStreams`/`RunConfig`).
B) `IFloorSource` 심 추출(`JsonFloorSource`, 동작 불변, 49 테스트 유지).
C) `ProceduralFloorGenerator`(아이작식 큐 성장 → `FloorLayout`).
D) 절차적 적 구성(`ProceduralRules.json` 로더 + `RoomBounds` 안 지터 배치).
E) 프로퍼티 테스트(같은 시드→동일 그래프 / 항상 연결+Start+Boss+유효 startRoomId / 방 수 범위 /
   보스방=데드엔드) + 기본을 절차적으로 전환, JSON 선택 유지.
F) 시드 노출(일시정지/게임오버). 타이틀 시드 입력은 2b.
규모 ~3~4 세션. 아트 리소스 적용(무료 CC0 3D 1세트)을 이 즈음 병행.

### 관련 파일 (예정)
- 신규: `Assets/Scripts/Core/{IRng,DeterministicRng,RngStreams,RunConfig}.cs`,
  `Assets/Scripts/Stage/{IFloorSource,JsonFloorSource,ProceduralFloorSource,ProceduralFloorGenerator}.cs`,
  `Assets/Scripts/Stage/ProceduralRules.cs`, `Assets/StreamingAssets/Balance/Stage/ProceduralRules.json`
- 수정: `GameFlow.cs`(RunConfig), `StageManager.cs`(IFloorSource 소비)

---

## 2026-09-09 (2) — 테스트 도입을 위한 asmdef: 단일 래핑 vs 계층 분리 vs 순수 코어만

### 배경 / 문제
- 포트폴리오 기술 트랙 #1 = 자동화 테스트(EditMode+PlayMode) + CI (`DevLog.md` 2026-09-09 확정 순서).
- Unity Test Framework는 테스트 코드가 **asmdef 어셈블리**에 있어야 하고, asmdef는 기본
  `Assembly-CSharp`를 **참조할 수 없음**(참조는 Assembly-CSharp → asmdef 단방향). ⇒ 테스트하려는
  게임 코드가 asmdef 안으로 들어와야 함.
- 현 프로젝트는 asmdef 0개(전부 `Assembly-CSharp`). 과거 "asmdef는 규모상 과설계"로 명시 보류
  (`ClaudeSessionSummary.md` §2).

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A1 — 단일 `RogueLike.asmdef`로 `Assets/Scripts/` 전체 래핑 + 테스트 asmdef 2개 | 파일 1개, 어셈블리 구조 불변(이름만 붙음), 회귀 위험 최소, EditMode/PlayMode 둘 다 가능. 참조 캐스케이드 조사 완료 = `Unity.InputSystem` + `UnityEngine.UI` 2개뿐 | 컴파일 스코핑·의존성 방향 강제 같은 모듈화 이득은 없음(테스트 인에이블러로만) |
| A2 — Core(순수)/Runtime(MonoBehaviour)/Editor + 영역별 테스트 | 진짜 모듈 경계 + 의존성 방향 명시 서사(§10) | 지금 의존성 얽힘을 실제로 풀어야(순환 위험), 번복을 크게, 러빗홀 위험 |
| A3 — 순수 C#만 Core.asmdef, 나머지 Assembly-CSharp 유지 | 가장 쌈 | PlayMode에서 실제 MonoBehaviour(FSM/룸/풀) 테스트 불가 = 유저가 원하는 목표 미달 |

### Claude 제안
- A1. 최소 단계로 목표(EditMode+PlayMode+CI) 달성, "필요할 때 도입" 원칙과 일치. A2 분리는 실제
  의존성 방향 문제가 드러나면 그때.

### 선택한 방향
- **A1 채택 (유저 승인 2026-09-09).** `Assets/Scripts/RogueLike.asmdef`
  (references: `Unity.InputSystem`, `UnityEngine.UI`; `autoReferenced: true`). Editor asmdef는
  `Assets/Scripts/`에 `UnityEditor` 사용처가 0이라 불필요(조사 완료). `Assets/TutorialInfo/`의
  템플릿 잔재 스크립트 2개는 `Assembly-CSharp`에 남되 게임 코드가 참조 안 하므로 무해. 테스트
  asmdef 2개(`RogueLike.Tests.EditMode` `includePlatforms:[Editor]`, `RogueLike.Tests.PlayMode`)는
  Phase 1에서.

### 선택 이유
- asmdef 도입이 이제 "필요"해짐(테스트) — 원칙상 최소 도입이 맞음. 전체를 한 어셈블리로 두면
  컴파일 단위·런타임 동작이 지금과 동일해 회귀 위험 사실상 0.
- A2의 계층 분리는 실제 중복·순환이 관측 안 된 상태의 추측 설계. §2가 asmdef를 미룬 이유(규모)가
  여전히 유효 — 테스트라는 단일 사유에만 대응.
- **재검토 조건**: 절차적 생성(#2)·경로탐색(#3)에서 순수 로직 계층이 뚜렷해지거나, 컴파일 시간이
  문제되거나, 의존성 방향을 강제하고 싶어지면 A2로 분리.

### 관련 파일
- `Assets/Scripts/RogueLike.asmdef` (신규, Phase 0)
- 이후 `Assets/Tests/EditMode/*`, `Assets/Tests/PlayMode/*` (Phase 1+)

---

## 2026-09-09 — 적이 방 밖으로 나가 진행 불가: 방 내부 클램프 vs 물리 완화 vs LOS

### 배경 / 문제
- 유저 제보(블로킹): 방 진입 시 간혹 적이 방 밖에 스폰되거나 밖으로 이동 → 못 죽여 방 클리어
  불가 → 진행 정지.
- 추적 결과 원인 3중: (1) 적 이동 6곳 전부 생짜 `Rigidbody.MovePosition`, 벽 클램프·스윕 전무 —
  방 유지가 벽 콜라이더 충돌뿐인데 적 Rigidbody가 Discrete, 벽 두께 1. (2) 2026-09-08 넉백이
  `MovePosition`으로 절대 목표점(`시작+방향×1.5`)을 ease-out 러프, 첫 스텝 ≈0.46유닛 → 벽 쪽
  넉백이면 캡슐 중심이 벽 중간면을 넘어 솔버가 바깥 배출, 절대점 재조준이라 반력 무효(2026-09-04
  플레이어 벽 관통과 동일 계열). 2026-09-08부터 모든 원거리 변형이 후퇴까지 해 빈도 증가.
  (3) `CanSeePlayer`가 순수 거리라 벽 밖 적도 플레이어를 "봄" → Chase/Attack 유지·벽 관통 사격,
  `leashRange 9`(방 40) 안이라 복귀도 안 함 → 영구 블록.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A. 방 내부 AABB 클램프(`RoomController.ClampToInterior` + `EnemyStateMachine.MoveTo` 단일 초크포인트) | 물리 튜닝·충돌 모드에 의존 안 하는 **결정적** 차단. 넉백/후퇴/향후 이동 헬퍼 자동 커버. 스폰 좌표도 같은 클램프. 플레이어 `WallClamp` 중앙화와 같은 패턴 | 적→방 참조 주입 1건, 이동 호출부 6곳 리팩터. 문 간격 무시(적은 방 간 이동 안 하므로 무해) |
| B. 안전망 — `FixedUpdate`에서 방 밖 감지 시 스폰 지점 스냅 | authoring 좌표가 벽에 박힌 경우·미래 회귀 대비 보험. 싸다 | A 있으면 거의 안 걸림(순수 보험) |
| C. 물리 완화 — 적 Rigidbody Continuous(Speculative) + 넉백 스텝 상한, 또는 넉백을 `AddForce`+damping으로 | 거의 공짜, 배선 없음 | 모서리 짜냄·솔버 배출은 잔존 → "간혹"이 "드물게"로 줄 뿐. 단독 불충분 |
| D. `CanSeePlayer`에 LOS 레이캐스트 | 벽 뒤 적이 Patrol 복귀 → AI 품질 개선 | 물리적으로 벽 밖에 낀 적을 도로 넣진 못함. 블록 해결의 주 수단 아님 |

### Claude 제안
- **A + B.** A가 근본(결정적 컨테인먼트), B가 보험. C는 defense-in-depth로 여유 시, D는 후속 품질 과제.

### 선택한 방향
- **A + B 채택(유저 승인).**
  - A: `RoomController`에 `RoomSize` + `ClampToInterior(worldPos, bodyRadius)` /
    `IsInsideInterior(worldPos, bodyRadius, slack)`. 한계 = `RoomSize/2 - WallThickness/2 -
    bodyRadius - 0.1`. `EnemyStateMachine`에 `SetRoom` + `MoveTo(worldPos)`(6개 호출부를 여기로
    수렴, 회전은 그대로). `EnemySpawner.SpawnRoom(RoomData, RoomController)`로 시그니처 변경 +
    스폰 좌표 클램프 + `Physics.SyncTransforms()`.
  - B: `EnforceRoomContainment()` in `FixedUpdate` — slack 1유닛(벽 두께 1보다 작음) 넘으면
    스폰 지점 스냅 + 속도 0 + `SyncTransforms` + Idle.

### 선택 이유
- A만이 물리 튜닝·충돌 모드·"모든 미래 이동 코드가 벽 체크를 기억할 것"에 기대지 않고 컨테인먼트를
  **보장**. 방은 축 정렬 박스라 점을 AABB에 클램프하는 건 자명하고 확실.
- B는 프로젝트에서 물리 관통 계열 버그가 2번째(플레이어 2026-09-04)라, 단일 경로 누락이 다시
  클리어 불가 런을 만들지 못하게 하는 값싼 보험.
- C 미채택: A가 결정적이라 불필요. 단, 아트 통합(Phase 4)에서 지오메트리·콜라이더 재조정 시
  적 Continuous 전환은 같이 검토(플레이스홀더 노트에 이미 물리 관통 항목 있음).
- D는 별건(AI 품질)이라 이번 블록 수정 범위에서 제외, 후속.
- **재검토 조건**: 적이 방 사이를 이동하는 설계가 생기면(현재 없음) A의 "문 간격 무시" 클램프를
  문 통로를 예외로 두는 형태로 확장.

### 관련 파일
- `Assets/Scripts/Room/RoomController.cs`(`RoomSize`/`ClampToInterior`/`IsInsideInterior`, `SpawnRoom` 호출 인자)
- `Assets/Scripts/Enemy/EnemyStateMachine.cs`(`SetRoom`/`MoveTo`/`EnforceRoomContainment`, 캡슐 반지름 캐시)
- `Assets/Scripts/Enemy/EnemySpawner.cs`(`SpawnRoom` 시그니처·스폰 클램프·`SyncTransforms`)
- `EnemyChaseState`/`EnemyPatrolState`/`EnemyStunnedState`/`EnemyAttackState`/`MeleeAttackPattern`/
  `RangedKitingBehavior`(`Body.MovePosition` → `machine.MoveTo`), `EnemyMovementFlavor`(주석 정정)

---

## 2026-09-09 — 씬 전환 페이드: 영속 페이드 캔버스 vs 씬별 오버레이 vs 페이드 인만

### 배경 / 문제
- 로드맵 §6 #6 잔여. `GameFlow.LoadScene()`이 `SceneManager.LoadScene`을 동기 호출해
  타이틀↔인게임·인게임 재시작 전환이 전부 하드 컷. 전환 지점 4곳이 모두 `GameFlow`의
  `StartRun`/`RetryRun`/`ReturnToTitle`을 지남.
- 핵심 제약(아키텍처 스킬 `scenes-prefabs-and-object-lifetimes.md` + 코드 확인): 동기
  `SceneManager.LoadScene`은 `DontDestroyOnLoad`가 아닌 모든 오브젝트를 즉시 파괴 → 페이드
  아웃은 로드 전에 끝나야 하고, 페이드 인은 새 씬에서 일어나야 함.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A1 — 영속 페이드 캔버스(`DontDestroyOnLoad` 단일 오브젝트) + `LoadSceneAsync` | 4개 전환점을 구현 1개로 대칭 처리, 튜닝 지점 1곳, 씬별 배선 불필요. async + 1프레임 대기로 새 씬 `BuildFloor` hitch까지 암전으로 가림. 스킬의 "전환 = 상태 머신 + 명시적 부트스트랩"과 일치 | 프로젝트 최초의 `DontDestroyOnLoad` + 부트스트랩 계약. 중복 가드 필요 |
| A2 — 씬별 페이드 인 오버레이 + 호출부 페이드 아웃(영속 오브젝트 없음) | DDOL 없음, "영속 싱글턴 회피" 유지. `FloorTransitionUI` 패턴 재사용 | 페이드 아웃에 살아있는 MonoBehaviour 필요 + 로드를 코루틴 끝으로 미뤄야 → `GameFlow`가 정적인 채로 코루틴을 못 돌림. 오버레이 2개 동기화. 아웃/인이 다른 씬의 다른 코드 경로(비대칭) |
| A3 — 페이드 인만(나갈 때 하드 컷 허용) | 최소 — 씬당 `CanvasGroup` 1개 + 10줄, `GameFlow` 무수정, 영속 오브젝트·정적 코루틴 문제 없음 | 나가는 하드 컷이 보임(동기 로드 stall만큼 정지 후 암전). 양방향 페이드하는 인런 `FloorTransitionUI`와 톤 불일치 |

### Claude 제안
- A1 추천(대칭·일관·async로 hitch 은폐). "영속 싱글턴 회피" 원칙은 *매니저를 여기저기서 만들지
  말자*는 뜻이고, 목적이 명확한 전환 캔버스 하나를 결정적 중복 가드와 함께 부트스트랩하는 건
  스킬이 지지하는 "명시적 부트스트랩". A3를 값싼 폴백으로 제시.

### 선택한 방향
- **A1 채택.** 단, 프리팹/Resources 없이 `SceneTransitionController`가 오버레이를 전부 코드로
  생성(`EnsureExists()`). 페이드 지속시간은 `const`(0.35s in/out). `GameFlow`는 정적 유지 —
  코루틴은 영속 오브젝트가 소유하고, `GameFlow.LoadScene`은 `EnsureExists().FadeAndLoad(name,
  PauseState.ForceResume)` 한 줄로 위임.

### 선택 이유
- 4개 전환점을 대칭·일관되게 처리하는 유일한 안. `LoadSceneAsync` + 1프레임으로 새 씬 준비
  완료까지 암전 유지 → Prototype 진입 시 `StageManager.BuildFloor()` 동기 인스턴스화 hitch도 덮임.
- 코드 생성(프리팹 X): 칠할 게 없는 단색 페이드라 authoring 대상이 아니고, 씬에서 빠뜨리거나
  빌드에서 누락될 실패 모드가 없어짐. 프로젝트의 절차적 생성 선호(`RoomController.Initialize`
  방 지오메트리)와도 일관.
- `PauseState.ForceResume`를 `onBeforeActivate` 콜백으로 넘긴 이유: 페이드 아웃(unscaled) 동안엔
  이전 씬이 pause를 유지하고 새 씬 진입 직전에만 timeScale 복구. `GameFlow`에서 미리 부르면
  페이드 0.35초간 월드가 틱함. 콜백으로 `SceneTransitionController`는 `PauseState`를 모른 채 유지.
- **트레이드오프 / 재검토 조건**: (1) 페이드 시간을 실제로 튜닝하고 싶어지면 → 직렬화 프리팹으로
  전환(범위 국한된 변경). (2) 로딩 시간이 길어지면(아트/씬 규모 ↑) → Phase 5에서 Addressables +
  실제 로딩 화면으로 `FadeAndLoadRoutine` 내부만 교체. (3) 영속 상태가 더 필요해지면 → 여기
  statics를 늘리지 말고 별도 부트스트랩 오브젝트.

### 관련 파일
- `Assets/Scripts/Core/SceneTransitionController.cs` (신규)
- `Assets/Scripts/Core/GameFlow.cs` (`LoadScene` 재배선, `using UnityEngine.SceneManagement` 제거)

---

## 2026-09-08 (8) — 가만히 맞으면 근접 런지마다 뒤로 밀림: 그대로 두기 vs 런지 오버슈트 클램프 vs 치고 빠지기 vs 몸빵 제거 vs 플레이어 넉백 시스템

### 배경 / 문제
- (7) 이후 실플레이: 정지 상태로 근접 적에게 맞으니 "밀고 멈추고" 반복하며 방 뒤로 계속 밀림.
  넉백 시스템으로 오해됐으나 코드상 **플레이어 피격 넉백은 없음**(`HealthComponent.TakeDamage`는
  HP만). 원인은 `MeleeAttackPattern` 런지의 물리 충돌: 추격이 `MaxAttackRange`(1.2)에서 멈추고
  런지 이동량도 `LungeSpeed 8 × LungeDuration 0.15` = 1.2라, 런지가 플레이어 중심까지 관통 →
  솔버 depenetration이 플레이어를 밀어냄. (7)이 idle sway를 걷어내며 이 런지-shove가 드러남.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 그대로 둔다 | 작업 0, 읽히는 박자이고 실플레이어는 구른다 | 유저가 명시적으로 어색하다고 지적함 |
| 런지 전진량을 "플레이어와의 거리 − 접촉버퍼"로 클램프(방향은 고정, 거리만 단축) | 몇 줄, 데이터/프리팹 무변경, 근접 두 변형 공용, 비호밍 유지, 박자는 그대로 두고 관통-shove만 제거 | 접촉버퍼 상수 1개 튜닝 |
| 런지 후 쿨다운 동안 짧게 후퇴(치고 빠지는 근접) | 근접에 리듬감 추가 | 느낌이 크게 바뀜, 코드 큼 — 지금 단계엔 과함 |
| 적↔플레이어 물리 밀어내기 자체 제거(레이어 분리) | shove 원천 제거 | 몸빵(body block) 사라져 적이 플레이어 관통, 아마 비의도 |
| 의도적 플레이어 피격 넉백 시스템 | "제대로 된" 버전, 튜닝 가능 | 이연했던 플레이어 넉백 스탯 작업, 큼 |

### Claude 제안
- 두 번째(클램프)를 즉시 수정으로 제안. 접촉 상태에서 `MovePosition`이 관통해 솔버와 싸우는 게
  근본 원인이라, 런지가 플레이어 앞에서 멈추게만 하면 박자(윈드업→런지→홀드)는 유지한 채
  불도저 느낌만 사라짐. 넉백을 "제대로" 하려면 다섯 번째지만 그건 별도 작업.

### 선택한 방향
- 유저가 두 번째 선택. `MeleeAttackPattern.PhysicsTick` 런지 스텝을
  `min(step, max(0, gap.magnitude − LungeContactBuffer))`로 클램프. `LungeContactBuffer` const
  1.1(양 캡슐 radius 0.5 → 살짝 떨어진 접촉; range 1.2 안이라 런지 종료 데미지 판정 정상).
  이동은 고정 `_lungeDirection`만 따라 → 비호밍 유지. 데이터/프리팹 무변경.
- 플레이어 피격 넉백(다섯 번째)은 **나중에** 플레이어 넉백 스탯과 함께.

### 선택 이유
- 세 번째(치고 빠지기)·다섯 번째(넉백 시스템)는 지금 단계(캡슐 placeholder)에서 굳히기 이르고
  범위가 큼. 네 번째(몸빵 제거)는 부작용이 의도와 반대. 그대로 두기는 유저가 거부.
- 남는 건 "런지가 관통하지 않게"뿐이고, 방향 고정 + 거리 클램프면 비호밍·회피 가능성을 안
  건드리면서 shove만 제거됨 — 최소 변경으로 증상 해소.

### 관련 파일
- `Assets/Scripts/Enemy/MeleeAttackPattern.cs`

---

## 2026-09-08 (7) — 근접 적이 플레이어 주위를 공전: 접촉 거리 정지 게이트 vs 사거리 내 이동을 공격 패턴에만 위임

### 배경 / 문제
- 실플레이에서 근접 타입 전 개체가 플레이어에 붙어서 공격하지 않고 주위를 불규칙하게 원처럼
  돌며 비빔. 원인: (1) `EnemyChaseState`에 정지 거리가 없어 접촉 상태에서도 매 FixedUpdate
  `MovePosition`을 걸고, 오늘 추가한 `EnemyMovementFlavor.ApplyWobble`의 수직 성분만 충돌에
  안 막혀 캡슐 표면을 미끄러짐. (2) `EnemyAttackState`의 쿨다운 중 positional idle sway가
  접촉 상태에서 솔버 depenetration과 싸우며 불규칙 궤도 생성. (3) `MaxAttackRange` 경계에서
  Chase↔Attack 플립플롭.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A. `EnemyData`에 `holdDistance` 필드 추가 → 그 거리 이하면 Chase 전진 중단 + idle sway 스킵 | 최소 변경(필드 1개), 근접 전 타입 일괄 적용 | 튜닝할 숫자 하나 증가, "근접은 사거리 안에서 안 움직인다"를 데이터로 흐리게 표현 |
| B. 사거리(`MaxAttackRange`) 안에서는 이동을 공격 패턴의 런지에만 위임, 그 사이엔 완전 정지(회전만). idle sway는 `HoldStillDistance`(const) 밖 = 원거리 적 쿨다운 시에만 | 접촉 거리에서 `MovePosition`이 아예 사라져 물리 싸움 원천 제거, "윈드업→커밋→리셋" 리듬이 명확, 새 직렬화 필드 없음 | 오늘 추가했던 근접 idle sway를 같은 날 되돌리는 셈(원거리에만 남김), 코드 변경이 A보다 약간 큼 |
| C. idle sway를 라이브 위치 기준 + 플레이어 침범 방지로 매끄럽게 | 기존 연출 유지 | 여전히 작게 공전함 — 유저의 "제자리에 서 있어야" 요구를 못 맞춤 |

### Claude 제안
- A(최소) 또는 B. 접촉 상태에서 `MovePosition` 계열 호출이 depenetration과 싸우는 게 근본
  원인이라, 그 구간에서 위치 이동 호출 자체를 없애는 B가 증상을 원천 제거한다고 설명. C는
  "제자리에 서 있어야"라는 요구와 상충하므로 제외 권고.

### 선택한 방향
- 유저가 B 선택. `EnemyChaseState.PhysicsTick`: 플레이어와의 거리 ≤ `MaxAttackRange`면 전진
  중단(`MoveRotation` 회전만), 그보다 멀 때만 wobble 얹은 이동(접근 구간 wobble 유지).
  `EnemyAttackState`: `ApplyIdleSway` → `HoldOrSway`. 플레이어와 `HoldStillDistance`(const 2.5)
  이하면 positional 이동 없이 회전만 — 근접 range ~1.2라 근접 적은 항상 이 구간, 공격 사이
  완전 정지. 그보다 멀면(원거리 적, range ~6) 기존 sine sway 유지.
- 새 `EnemyData` 필드/데이터·프리팹 변경 없음. `EnemyChaseState.cs` + `EnemyAttackState.cs`.

### 선택 이유
- A의 `holdDistance`는 결국 `MaxAttackRange`와 거의 같은 값이 됨 — 이미 있는 `MaxAttackRange`를
  정지 기준으로 재사용하면 새 필드가 필요 없고, "자기 사거리 안에 들어왔으면 공격 패턴이 이동을
  전담한다"는 규칙이 코드에 그대로 드러남.
- 오늘 추가한 근접 idle sway를 되돌리는 부분은, 그 sway가 노렸던 "살아있는 느낌"이 접촉 거리
  에서는 오히려 depenetration과 싸우며 "비비는" 역효과를 낸다는 실플레이 증거에 따른 것.
  충돌 대상이 없는 원거리 거리에서는 그대로 유효하므로 그 케이스만 남김.
- C는 유저의 명시적 요구("가만히 있는 게 자연스럽다")와 방향이 다름.

### 관련 파일
- `Assets/Scripts/Enemy/EnemyChaseState.cs`, `Assets/Scripts/Enemy/EnemyAttackState.cs`

---

## 2026-09-08 (6) — Benchmark 측정 방법론: MCP 브리지로 Play Mode 구동 vs 유저 수동 조작 + 프레임 버퍼 사후 리드백

### 배경 / 문제
- 계획서 §6.5의 "Enemy N → FPS/GC Alloc Before 표"를 만들려고 `Unity_RunCommand`로
  Play Mode 진입 → 스폰 → Profiler 값 수집을 자동화했더니, 0/50/100/200마리 전 구간에서
  median CPU Time이 ~99.5ms로 **동일하게** 나옴. MCP 브리지의 상수 오버헤드가 프레임에
  올라타서 측정하려는 신호(개체 수에 따른 4~7ms 차이)를 완전히 덮어버림.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 브리지로 계속 구동하되 오버헤드를 빼는 보정값 추정 | 자동화 유지 | 오버헤드가 상수가 아니라 프레임마다 요동 — 보정 불가능, 수치 신뢰도 0 |
| Development Build를 만들어 빌드에서 측정 | Editor·브리지 오버헤드 완전 제거, 가장 정확 | 빌드 파이프라인·자동 계측 훅 작성 비용, Before 표 하나 뽑자고 과함 |
| 유저가 직접 Play/버튼/Stop을 수행(그 구간엔 내가 아무 명령도 안 보냄), Stop 후 `ProfilerDriver.firstFrameIndex~lastFrameIndex` 버퍼(Stop 후에도 유지됨)에서 마지막 200프레임을 읽음 | 브리지가 측정 구간 프레임에 개입 안 함 → median 4~5ms대 깨끗한 수치, 추가 인프라 0 | 수동 조작이라 구간 반복이 느림, 절대값은 여전히 Editor Play Mode 기준 |

### Claude 제안
- 세 번째 대안 제안. 핵심 관찰: 브리지 오버헤드는 "내가 명령을 보내는 동안"만 발생하므로,
  측정 구간에 명령을 아예 안 보내면 사라짐. Profiler 프레임 히스토리 버퍼는 Stop 이후에도
  남아 있어 사후에 읽어도 됨.

### 선택한 방향
- 세 번째 대안 채택. 이번 세션에서 4종 × 50/100/200 Before 표를 이 방식으로 완성.
- 절대값이 Editor 기준이라는 한계는 표에 명시하고, 세 구간 간 상대 비교만 유효하다고 단서.

### 선택 이유
- Before 표의 목적은 "개체 수 증가에 따른 상대적 비용 곡선"이지 빌드 성능 절대값이 아님 —
  Development Build까지 가는 건 그 목적에 과투자. 정확한 절대값이 필요해지는 시점(Phase 6
  본격 최적화)에 빌드 계측으로 올리기로 함.

### 관련 파일
- `Assets/Scripts/Benchmark/BenchmarkSpawner.cs`, `Assets/Scenes/BenchmarkScene.unity`
- `Docs/DevLog.md` 2026-09-08 "이슈/메모"

---

## 2026-09-08 (5) — 각 타입 500/1000마리 스트레스 구간: 지금 측정 vs 스킵(Phase 6로 이연) vs Development Build로 지금

### 배경 / 문제
- 50/100/200 Before 표를 마친 뒤 500/1000까지 구간을 늘려보려 했으나, Melee 1000마리
  스폰 직후 화면 깜빡임 → Unity 강제 종료(직전에 Windows 메모리 부족 경고도 발생). 2회 재현.
- 이 구간을 §6.5 표에 넣을지, 넣는다면 어떻게 측정할지 결정 필요.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 에디터에서 계속 재시도(다른 앱 종료, Deep Profile off 등) | 추가 작업 없음 | 여전히 불안정, 기기 다운 반복 위험, 나온 수치도 Editor+Profiler 왜곡이 200마리 때보다 훨씬 큼 |
| Development Build로 500/1000 측정 | 크래시 위험↓, 상한 수치 확보 | 빌드 파이프라인 작업, 게임 범위(룸당 수십 마리) 밖 수치라 투자 대비 효용 낮음 |
| 측정하지 않고 스킵, §6.5 최적화+After 패스는 Phase 6로 이연(현 헤드룸을 근거로 명시) | 조기 최적화 함정 회피, 시간 절약 | §6.5 "before/after" 3단 중 after가 비게 됨 — "측정했더니 여유 있어 최적화 안 함"이라는 서술로 대체 |

### Claude 제안
- 세 번째 대안 제안. 근거: Profiler 실측상 가장 무거운 BurstRanged 200마리도 median 7.35ms =
  16.67ms 예산의 44%로 정상 상태 병목이 없음. 병목이 없는데 최적화 항목을 억지로 만드는 건
  CLAUDE.md가 경고하는 "placeholder 에셋 상대 조기 최적화". 적이 아직 캡슐이라 아트 통합 후
  프로파일이 통째로 바뀜.

### 선택한 방향
- 유저가 세 번째 대안 선택. 500/1000 미측정. `DevLog.md` 2026-09-08에 크래시 상황 + 원인
  분석 + "§6.5 최적화/After는 Phase 6로 의도적 이연" 근거를 기록.

### 선택 이유
- "먼저 측정했고, 예산 대비 여유가 커서 조기 최적화를 하지 않았다"는 판단 자체가 포트폴리오
  근거가 됨(무비판적 최적화 회피). 실제 병목은 아트/애니/VFX가 들어오는 Phase 6에서 다시
  측정해야 의미가 있음.

### 관련 파일
- `Docs/DevLog.md` 2026-09-08 "이슈/메모 (재부팅 후 후속 세션)"
- `Docs/ProjectPlan.md` §6.5 / Phase 6

---

## 2026-09-08 (4) — 대시 중 적 관통: 데미지 무적만 유지 vs 적 콜라이더를 트리거로 vs 레이어 충돌 무시 + 오버랩 보정 꼬리

### 배경 / 문제
- 실플레이에서 대시(구르기)로 적을 통과하려 하면 물리적으로 막히거나 튕겨나감. 기존엔
  `SetInvulnerable`로 데미지만 무효화했을 뿐 플레이어↔적 물리 충돌은 그대로였음.
- 대시가 적 콜라이더 안에서 끝나는 경우, 충돌을 그냥 복원하면 솔버가 플레이어를 순간
  이동시키듯 밀어냄.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 적 콜라이더를 상시 트리거로 바꿔 플레이어와 물리 충돌 자체를 없앰 | 대시 로직 안 건드려도 됨 | 평상시 적-적/적-벽 밀림, 넉백, 근접 접촉이 전부 깨짐 — 영향 범위 과대 |
| 대시 중 데미지 무적만 유지(현행) + 물리 충돌은 감수 | 구현 없음 | 문제 그대로 — 적 뭉치에 대시가 막힘 |
| 대시 시작~보정 꼬리 동안만 `Physics.IgnoreLayerCollision(Player, Enemy)`, 대시 종료 시 적과 겹쳐 있으면(`CheckCapsule`) 같은 방향으로 저속(×0.35, ≤0.25s) 미끄러져 빠져나온 뒤 충돌 복원 | 평상시 물리는 완전 그대로, 대시만 관통, 복원 시 팝 없음 | 상태(`_isCorrectingOverlap`)와 분기 추가로 `PlayerDodgeRoll` 복잡도↑ |

### Claude 제안
- 세 번째 대안 제안. `IgnoreLayerCollision`은 레이어 쌍 단위 전역 토글이지만 대시 창에만
  걸었다 풀면 부작용 없음. 종료 시 오버랩 보정은 "충돌을 다시 켜되 솔버가 아니라 우리가
  천천히 밀어낸다"는 접근.

### 선택한 방향
- 세 번째 대안 채택. `FixedUpdate`를 `RunDash`/`RunOverlapCorrection`으로 분리,
  `EndDodgeRoll` → `FinishDash`(오버랩 분기) + `CompleteDodgeRoll`(공용 리셋)로 재구성.
- `CancelDodgeRoll`(문/Floor Exit 전환)은 보정 꼬리를 건너뛰고 즉시 복원(텔레포트/숨김 직전
  이라 팝이 안 보임).
- 캡슐 지오메트리 산출은 `WallClamp.GetCapsuleGeometry()`로 추출해 벽 클램프와 오버랩 체크가
  동일 월드 캡슐 공유.

### 선택 이유
- 평상시 물리(적 밀림/넉백/접촉)를 절대 건드리면 안 된다는 게 1순위 — 트리거화는 그걸 깨서
  탈락. 무적만 유지는 문제 미해결. 남는 건 "대시 창에만 국소적으로 끄기"뿐이고, 복원 시 팝은
  별도 보정으로 흡수하는 게 유일하게 깔끔한 조합.

### 관련 파일
- `Assets/Scripts/Player/PlayerDodgeRoll.cs`, `Assets/Scripts/Player/WallClamp.cs`

---

## 2026-09-08 (3) — 플레이어 사망 처리: 전용 컨트롤러 신설 vs `RoomFlowManager` 재사용 / 쓰러지는 연출: 물리 래그돌 vs 스크립트 틱오버

### 배경 / 문제
- 그간 이연되던 Game Over(로드맵 §6 #6 나머지). 플레이어 `HealthComponent.Health.Died`를
  받아 결과 화면을 띄우고, 죽은 티를 내는 연출이 필요.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 사망 처리 전담 `PlayerDeathController` 신설 | 책임 분리 명확 | `RoomFlowManager`가 이미 갖고 있는 참조(PlayerDodgeRoll, PickupNotificationUI, UIManager, stats 빌더)를 상당수 중복 확보해야 함 |
| `RoomFlowManager`에 `HandlePlayerDied` 추가 | `HandleFloorCompleted`와 "런이 방금 끝났다 → 결과 화면" 형태가 동일, 스탯 빌더/구르기 취소/알림 숨김 로직 그대로 공유(`BuildCurrentStats()` 추출) | `RoomFlowManager` 책임이 "방 흐름"에서 "런 종료 전반"으로 살짝 넓어짐 |
| 연출: `isKinematic=false`로 두고 물리 래그돌(임펄스로 넘어뜨림) | "진짜" 물리 반응 | 캡슐 1개짜리 무애니 프리미티브라 래그돌이랄 게 없음, 넘어진 위치·각도 비결정적, 보스 전도 버그와 같은 부류 리스크 |
| 연출: `isKinematic=true` + 캡슐 밑동 피벗 `RotateAround` 스크립트 틱오버(0.45s, 90°) | 결정적, 지면 안 뚫음, `timeScale=0`에서도 `unscaledDeltaTime`으로 동작 | 임시방편 — 실제 캐릭터 애니 들어오면 폐기 |

### Claude 제안
- 두 번째(위치) + 네 번째(연출) 조합 제안. 사망은 승리(FloorCompleted)와 같은 "run ended"
  이벤트의 패배판일 뿐이고, `RoomFlowManager`는 이미 런 종료 순간에 Player 컴포넌트를 만짐.
  연출은 캐릭터가 캡슐인 현 단계에선 물리보다 스크립트가 예측 가능·안전.

### 선택한 방향
- 그대로 채택. `RoomFlowManager.HandlePlayerDied`: 구르기 취소 + 픽업 알림 즉시 숨김 +
  `PlayerInputActions` off + `PauseState.RequestPause` + `PlayerFallOverRoutine` + `ShowGameOver`.
  승/패 공용 `BuildCurrentStats()` 추출. 연출은 캡슐 밑동 피벗 `RotateAround` + `unscaledDeltaTime`.
- `GameOverUI`는 `FloorCompleteUI` 대응 구조(3 `CanvasGroup` 레이어, 버튼은 페이드 후 활성),
  가시성은 프로젝트 공통 `CanvasGroup` 컨벤션(`SetActive` 금지 — 자기-비활성 패널 함정).

### 선택 이유
- 전용 컨트롤러는 참조 중복 비용이 분리 이득보다 큼 — 두 번째 "런 종료 유사 이벤트"가
  이미 있고 형태가 같으니 같은 클래스가 맞음.
- 물리 래그돌은 보스 전도 버그(같은 2026-09-08 로그)에서 이미 데인 부류 — 무애니 캡슐 단계
  에선 스크립트 틱오버가 정답, 캐릭터 아트/애니 교체 시 재검토(CLAUDE.md 플레이스홀더 노트).

### 관련 파일
- `Assets/Scripts/UI/GameOverUI.cs`, `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/Room/RoomFlowManager.cs`
- `Assets/Art/UI/GamePlay/ui_game_fail.png`

---

## 2026-09-08 (2) — 원거리 카이팅: 플레인 Ranged는 그대로 두기 vs 패턴별로 후퇴 로직 복붙 vs 공유 `RangedKitingBehavior` 클래스

### 배경 / 문제
- 후퇴 로직은 `RetreatAndShootPattern`(보스 패턴)에만 있었음. 일반 `RangedAttackPattern`과
  신규 `BurstRangedAttackPattern`은 서 있는 자리에서 그냥 쏴서, 붙어서 밀어붙이면
  원거리 적이 완전히 무력화됨.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 플레인 Ranged/Burst는 후퇴 없이 유지 | 작업 없음 | 러시에 취약한 설계 결함 그대로, 3종 행동이 제각각 |
| 각 패턴 클래스에 후퇴+윈드업 코드를 개별 구현 | 패턴별 미세 조정 자유 | 거의 동일한 상태 머신(후퇴/윈드업/발사 타이밍)을 3벌 복붙 — 버그 3배, `RetreatAndShootPattern`과도 중복 |
| 후퇴/윈드업 타이밍만 담은 공유 `RangedKitingBehavior` 클래스 + 발사는 `onFire` 콜백으로 각 패턴이 소유 | 3종이 예외 없이 동일하게 "가까우면 후퇴 → 벌어지면 윈드업 → 발사", 단발/버스트 차이는 콜백에만 | 순수 C# 헬퍼 클래스 1개 추가, 패턴이 위임 구조가 됨 |

### Claude 제안
- 세 번째 대안 제안. 카이팅의 "리포지션/윈드업"과 "실제 사격(단발 vs 연사)"은 관심사가
  다르니, 전자만 공유 클래스로 빼고 후자는 `Action<Vector3> onFire`로 패턴에 남기면 공유
  클래스가 shot count에 무지한 채로 재사용됨.

### 선택한 방향
- 세 번째 대안 채택. `RangedKitingBehavior(retreatTriggerRange, retreatToRange, windUpDuration)`
  + `Start(machine, onFire)` / `Tick(...)` / `Interrupt()` / `IsBusy`. `RangedAttackPattern`,
  `BurstRangedAttackPattern`, `RetreatAndShootPattern` 전부 이걸로 통일. 후퇴 이동에도
  `EnemyMovementFlavor.ApplyWobble` 적용.

### 선택 이유
- 동일 상태 머신 3벌 복붙은 유지보수상 명백한 악수. 콜백 분리로 "타이밍은 공유, 사격은
  고유"가 되어 향후 4번째 원거리 패턴도 같은 뼈대를 씀.
- 2026-08-18에 `RetreatAndShootPattern`을 만들 땐 원거리 패턴이 2개뿐이라 전용으로 둔 게
  맞았지만, `BurstRanged` 추가로 3개가 되면서 공통화 임계점을 넘음.

### 관련 파일
- `Assets/Scripts/Enemy/RangedKitingBehavior.cs` (신규)
- `Assets/Scripts/Enemy/RangedAttackPattern.cs`, `BurstRangedAttackPattern.cs`,
  `RetreatAndShootPattern.cs`, `EnemyMovementFlavor.cs`

---

## 2026-09-08 (1) — 피격 스태거·넉백 데이터 모델: 전용 필드 세트 신설 vs 기존 `StunDuration` 재활용 + 단일 `KnockbackResistance`

### 배경 / 문제
- 적이 맞아도 반응이 없어 때리는 맛이 없음. 히트 스태거(잠깐 경직)와 넉백(뒤로 밀림)을
  넣되, 개체별로 차등(무거운 적/보스는 거의 안 밀리고 안 흔들림)을 두고 싶음.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| `staggerDuration`, `knockbackDistance`, `knockbackImmune`, `staggerImmune` 등 전용 필드 세트 신설 | 각 축을 독립 튜닝 | `EnemyData`에 필드 4개+ 증식, `stunDuration`(이미 있으나 아무도 안 씀)과 의미 중복 |
| 기존 `stunDuration` 재활용(2s→0.15s 재조정)해 "모든 피격 시 이만큼 경직" + 넉백은 단일 `knockbackResistance`(0~1)만, 거리 = `BaseKnockbackDistance`(1.5, 임시 상수) × (1 - resistance) | `EnemyData` 필드 증가 최소(1개), 죽은 필드 부활, 튜닝 손잡이가 개체당 2개로 단순 | 스태거 길이와 "잠금 저항"을 따로 못 굴림(보스는 stunDuration만 낮추면 됨 — 실용상 충분), 넉백 절대거리가 상수라 나중에 플레이어 스탯과 결합 필요 |

### Claude 제안
- 두 번째 대안 제안. 지금 필요한 차등은 "보스/헤비는 덜 흔들리고 덜 밀린다" 정도라 축 2개
  (경직 시간, 넉백 저항)면 충분. `BaseKnockbackDistance`는 상수로 두되, 플레이어 "넉백 파워"
  스탯이 생기면 그 자리를 대체하도록 주석으로 못박음.

### 선택한 방향
- 두 번째 대안 채택. `EnemyStateMachine`이 `HealthComponent.AnyDamageTaken`(static) 구독 →
  `StunDuration`만큼 `EnemyStunnedState`. 넉백 = `BaseKnockbackDistance × (1 - KnockbackResistance)`.
  `IAttackPattern.Interrupt()` 추가해 윈드업/리포지션 중 스턴 시 안전 취소(쿨다운은 적용).

### 선택 이유
- 전용 필드 세트는 지금 요구(2축 차등)에 과한 데이터 표면적. 안 쓰이던 `stunDuration`을
  되살리는 쪽이 필드 수·개념 수 둘 다 최소.
- 넉백 절대거리 상수화는 "플레이어 넉백 스탯이 아직 없다"는 현 상태에 맞춘 트레이드오프 —
  스탯이 생기는 시점에 `EnemyStateMachine`의 상수 자리를 공식으로 교체하기로 함.

### 관련 파일
- `Assets/Scripts/Enemy/EnemyData.cs`, `EnemyStateMachine.cs`, `EnemyStunnedState.cs`,
  `IAttackPattern.cs`, `EnemyAttackState.cs`

---

## 2026-09-04 (6) — 타이틀 Start 버튼 홀드 시 캐릭터 선택이 즉시 클릭되는 문제

### 배경 / 문제
- `TitleScreenController.HandleStartClicked`가 `startButton.onClick`(포인터 업)에서 **같은 프레임에**
  타이틀 그룹을 끄고 `CharacterSelectUI.Show()`로 `MeleeButton`/`RangedButton`을 커서 바로 아래에
  활성화한다. Start를 누른 그 클릭 제스처가 아직 물리적으로 안 끝났거나(홀드) 리바운스가 있으면,
  방금 나타난 버튼에서 다운→업이 완성되어 `HandleChosen` → `GameFlow.StartRun`이 즉시 실행된다.
  `Button.transition = None`이라 눌림 비주얼이 없어 사용자 눈엔 "시작하자마자 게임 진입"으로 보인다.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| **A (채택). 포인터/키가 떼어질 때까지 melee/ranged 버튼 arm 지연** | 실제 릴리즈 상태에 묶여 결정적(매직넘버 없음). 프로젝트의 CanvasGroup/가드 관례와 일치. 구조 변경 없음, `TitleScreenController` 무수정 | 코루틴 1개 추가 |
| B. 전환 시 1프레임짜리 풀스크린 투명 레이캐스트 블로커 | `CharacterSelectUI` 로직 무수정 | 씬에 블로커 오브젝트 + 수명 관리 코드, 간접적 |
| C. `Show()` 후 ~0.15s 동안 melee/ranged 클릭 무시 | 3줄로 끝 | 매직넘버. 0.15s 이상 홀드하면 여전히 뚫림 → 근본 해결 아님 |

### Claude 제안 / 선택
- Claude가 A/B/C 제시하며 A 권장. 사용자 A 채택.

### 선택한 방향
- `CharacterSelectUI.Show()`: 패널은 켜되 `meleeButton/rangedButton.interactable = false` +
  `EventSystem.SetSelectedGameObject(null)`(키보드 Submit 이월 차단). 코루틴 `ArmAfterInputReleased`가
  `한 프레임 대기 → Mouse.leftButton.isPressed 및 키보드 Enter/NumpadEnter/Space가 안 눌릴 때까지
  대기 → 한 프레임 더 → interactable = true`. `HandleChosen`은 남은 arm 코루틴을 정리.
- hover 흰색 워시(`HoverHighlight`)는 `Selectable.interactable`과 무관해 그대로 동작 — 클릭만 게이트됨.

### 선택 이유 / 트레이드오프
- 타이머(C)는 "충분히 오래 누르면" 뚫리는 확률적 방어라 부적합. 블로커(B)는 오브젝트 수명 관리 필요.
  A는 "그 제스처가 끝났는가"를 입력 상태로 직접 확인하므로 홀드 길이와 무관하게 항상 막힌다.
- `CharacterSelectUI`가 `CharacterSelectPanel`(= `panel` 필드)에 붙은 자기-비활성 구조라 코루틴은
  반드시 `panel.SetActive(true)` 이후 시작해야 함(현재 그렇게 됨). 추후 CanvasGroup 방식 전환 권장
  (`[[feedback_self_deactivating_panel_footgun]]`).

### 관련 파일
- 수정: `Assets/Scripts/UI/CharacterSelectUI.cs`

---

## 2026-09-04 (5) — 캐릭터 선택: 텍스트 버튼 → 일러스트 버튼 + 마우스 오버 시 흰색으로 밝아짐

### 배경 / 문제
- 캐릭터 선택 화면이 텍스트 버튼 2개뿐 — 포트폴리오 첫인상(스크린샷/영상)에 약함. 근접/원거리
  캐릭터 일러스트(`char_melee.png`/`char_ranged.png`, 알파 없는 통짜 세로 3:4)를 버튼으로 쓰고,
  마우스 오버 시 유니티 기능으로 이미지를 흰색으로 밝게 하려는 의도.
- 걸림돌: `Image.color` 틴트는 **곱연산**이라 흰색(1,1,1)을 곱해도 원본 그대로 → 스프라이트를
  흰색으로 "밝게"는 불가능(어둡게만 가능). `Button`의 Color Tint transition으로는 목표를 못 냄.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A. press 없이 hover→즉시 선택, hover는 스케일/그림자로 표현 | 단순 | "흰색으로 밝게" 요구를 못 살림. press 없는 특별처리가 애매 |
| **B (채택). 흰색 오버레이 자식 Image** — 같은 자리 흰색 quad(alpha 0, raycast off)를 hover 시 페이드 인 | `final = sprite×(1-a) + white×a`로 실제 흰색 워시. `Button`/`Selectable` 무수정(opt-in 비주얼). 일러스트가 불투명 사각이라 스프라이트 없는 흰 quad로 충분 | 자식 오브젝트 + 컴포넌트 1개 |
| C. 미리 밝게 처리한 스프라이트를 hover 시 스왑 | 런타임 연산 0 | 아트 2배 관리, 페이드 불가 |

### Claude 제안 / 선택
- Claude가 B(오버레이) 제안. 사용자 B 채택 + "부드러운 페이드", 캡션은 카드 하단
  ("환락 개척자(근거리)" / "기억 개척자(원거리)").

### 선택한 방향
- 신규 `HoverHighlight.cs`(`IPointerEnterHandler`/`ExitHandler`): 직렬화된 `overlay` Graphic의
  alpha를 `0 ↔ hoverAlpha`(기본 0.35)로 `fadeDuration`(기본 0.15s, `unscaledDeltaTime`) 페이드.
  `Awake`/`OnDisable`에서 0으로 스냅(씬 로드/패널 숨김이 hover 도중 걸려도 잔광 방지).
  per-frame step을 `hoverAlpha`로 스케일해 페이드 시간이 alpha 크기와 무관하게 일정.
- `Title.unity` `MeleeButton`/`RangedButton`: Image=일러스트, `Button.transition = None`,
  자식 `Overlay`(스프라이트 없는 흰색 quad, alpha 0, raycast off) + `HoverHighlight` 배선,
  기존 Label을 카드 하단 캡션으로 이동. 카드 540×720. `CharacterSelectUI` 코드 무수정
  (`Button` 인스턴스 유지 → onClick·아키타입 배선 그대로).

### 선택 이유 / 트레이드오프
- 곱연산 틴트의 한계를 오버레이 합성으로 우회하는 표준 기법. 일러스트가 불투명 사각형이라
  스프라이트 매칭 없이 흰 quad로 충분(투명 PNG였다면 `overlay.sprite`에 같은 스프라이트 필요).
- 초기 버그: `overlay.sprite`에 캐릭터 스프라이트를 넣어 흰색×스프라이트 = 변화 없음이었음 →
  `sprite = null`로 수정. 이벤트 디스패치는 정상이었음(레이캐스트/`IPointerEnterHandler` 확인).

### 관련 파일
- 신규: `Assets/Scripts/UI/HoverHighlight.cs`, `Assets/Art/UI/Character/char_melee.png`·`char_ranged.png`
- 수정: `Assets/Scenes/Title.unity`(MeleeButton/RangedButton 재구성)

---

## 2026-09-04 (4) — 보스 HP 바: 보스 전용 스폰 훅 vs 제네릭 스폰 이벤트

### 배경 / 문제
- 로드맵 §6 "당장 넣어볼 UI" #6. 보스 HP 바가 "보스가 나타났다"는 순간과 대상 `HealthComponent`를
  알아야 하는데, 현재 보스는 일반 적과 **완전히 동일한 경로**로 스폰됨(`RoomData.Enemies`의
  `variant: Boss` 엔트리 → `EnemySpawner.SpawnRoom`이 무분기 순회). 스폰 시점 신호가 전혀 없음.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A. `EnemySpawner.BossSpawned(HealthComponent)` — variant==Boss일 때만 발화 | 이벤트+한 줄 | `EnemySpawner`가 "보스"를 앎. 스폰 로직에 보스 개념 유입 |
| **B (채택). `EnemySpawner.EnemySpawned(HealthComponent, EnemyVariantType)` — 모든 적마다 발화** | `EnemySpawner`는 "이걸 스폰했고 variant는 이것"이라는 **사실만** 보고(스폰 텔레메트리), 보스 로직 0. 분기 없음(if 없는 invoke 한 줄). `EnemyAIManager`(#7 tick 분산)·스폰 카운터·벤치마크 등에서 재사용 가능 | 방마다 이벤트 몇 회 더(무시 가능) |
| C. `RoomController`/`StageManager`가 처리 | 방 스코프 | 스폰 리스트에 variant 정보 없음 → 반환형 확장/상관 매칭 필요, 커플링 |

### Claude 제안 / 사용자 반려
- Claude 초안은 A. 사용자가 "일반 몬스터와 다른 보스 스폰이 필요하냐, 스폰은 제네릭하게 두고
  필요하면 기능을 위에 얹자, 나중에 준보스도 특별하게 만들 수 있고"로 반려 → B로 조정.

### 선택 이유 / 트레이드오프
- "보스"라는 해석을 그걸 필요로 하는 쪽(`BossHealthBarUI`가 `variant == Boss` 필터)으로 밀어냄.
  숨김은 UI가 추적 중인 HC의 `Died`로 자체 처리(보스방 문 잠김 → 전투 중 이탈 불가라 사망 트리거 하나면 충분).
- **준보스 미래 경로**(지금 미구현, YAGNI): (1) `EnemyVariantType.MiniBoss` 값 추가 + 풀 매핑, UI가
  Boss/MiniBoss 둘 다 큰 바 표시 / (2) `EnemyData`에 `barStyle` 플래그 추가 + 이벤트에 `EnemyData`도
  실어 UI가 플래그로 판단(준보스 = 플래그 켠 데이터 에셋, 코드 수정 0). 준보스의 실제 요구사항이
  정해질 때 선택.

### 관련 파일
- 수정: `Assets/Scripts/Enemy/EnemySpawner.cs`(`EnemySpawned` 이벤트)
- 신규: `Assets/Scripts/UI/BossHealthBarUI.cs`, `Prototype.unity`의 `Canvas/HUD/BossHealthBar`

---

## 2026-09-04 (3) — 전체 플레이 루프: 타이틀 별도 씬 · 캐릭터 선택 위치 · 씬 라우팅 · 사망 소프트락

### 배경 / 문제
- 로드맵 §6 #6. 진입점이 사실상 "캐릭터 선택"(타이틀 없음), 런이 끝나도 타이틀 복귀 경로가
  없음 = §13상 미완성. 사망 처리는 이번 범위에서 제외.

### 검토한 대안 / 선택

| 쟁점 | 선택 | 대안 / 이유 |
|---|---|---|
| **타이틀: 별도 씬 vs 기존 씬 패널** | 별도 `Title.unity` (사용자: A안) | vs 기존 씬 내 `TitlePanel`: "재시작/타이틀 복귀"가 씬 리로드 없이 런 전체(Player/StageManager/풀) 재초기화를 요구 → `StageManager` 제자리 재빌드가 피하려던 복잡도를 런 규모로 되살림. vs 부트스트랩+DontDestroyOnLoad 매니저: 씬 간 넘길 상태가 (아직) `PendingArchetype` 하나뿐이라 과설계. 별도 씬은 재시작=씬 로드로 단순, Phase 5(Addressables) 진입로도 열림 |
| **캐릭터 선택 위치** | 타이틀 씬으로 이동 (사용자 구상) | 인게임에 남기면 Title→Gameplay 로드 후에도 별도 선택 단계가 필요. 타이틀 씬에서 미리 고르면 선택 결과(`CharacterArchetypeData` SO 참조)를 `GameFlow.PendingArchetype` 정적으로 씬 넘겨 전달 → 인게임 `CharacterSelectController`는 그 값을 소비만 (UI/입력잠금 제거). 에디터 직접 실행용 `fallbackArchetype` 폴백 |
| **씬 라우팅 방식 (사용자: 상세 설명 요청)** | `GameFlow` 정적 클래스 — 씬 이름 상수 + `StartRun`/`RetryRun`/`ReturnToTitle` + `PendingArchetype` | MonoBehaviour 매니저: 버튼 핸들러가 매니저 오브젝트 참조를 들어야 하고 씬마다 배치/중복 관리. SO 씬-config: 이름 오타엔 안전하나 2씬엔 ceremony 과함 → 나중에 승격. 정적은 배치할 GameObject가 없고 어느 씬 버튼에서든 호출 가능. 씬 이름(인덱스 아님)을 쓰는 이유: Build Settings 재정렬 시 인덱스만 밀림 |
| **`PauseState` 씬 전환 처리** | `ForceResume()` 추가 (owner 전부 clear + timeScale=1) | 클리어 화면이 `PauseState`로 timeScale=0. 씬 로드는 남은 owner의 `ReleasePause`를 안 부르고 timeScale도 안 되돌림 → 다음 씬이 얼어서 시작. `GameFlow.LoadScene`이 로드 직전 호출 |
| **사망 시 소프트락 (결정4)** | Player `HealthComponent.destroyOnDeath`=false (임시) | 현재 true → HP 0이면 Player가 `Destroy` → 조작 불가·게임오버 없음 = 데모 중 갇힘. 사망 처리는 이연이지만 이 1필드만 꺼서 "죽어도 HP 0인 채 계속" 상태로 막음. 정식 Game Over 패스에서 되돌림 |
| **씬 전환 연출 (결정5)** | v1 하드컷 | 페이드/비동기 로드+로딩화면은 Phase 5(Addressables) 스토리로 미룸. 씬이 작아 동기 로드 히치 미미 |

### 선택 이유 / 트레이드오프
- `Title.unity` 구성: Camera(URP, solid color) + EventSystem(`InputSystemUIInputModule`,
  인게임 씬과 동일) + Canvas(1920×1080, Scale With Screen Size). `CharacterSelectUI`는 기존
  것을 그대로 옮겨 재사용(패널은 씬에 active 저장 — 자기-비활성 함정 회피,
  `[[feedback_self_deactivating_panel_footgun]]`).
- 3방향 전환 확정: Title→Gameplay(`StartRun`), Gameplay→Gameplay(`RetryRun`, 아키타입 유지),
  Gameplay→Title(`ReturnToTitle`, 아키타입 클리어).

### 관련 파일
- 신규: `Assets/Scenes/Title.unity`, `Assets/Scripts/Core/GameFlow.cs`,
  `Assets/Scripts/UI/TitleScreenController.cs`, `Assets/Art/UI/Title/*.png`(5장)
- 수정: `Player/CharacterSelectController.cs`(선택→소비), `Player/PauseState.cs`(`ForceResume`),
  `UI/FloorCompleteUI.cs`(toTitle 버튼), `UI/UIManager.cs`(charSelect 제거),
  `Player.prefab`(fallbackArchetype, destroyOnDeath=false), `Prototype.unity`(CharacterSelectPanel
  제거, ClearOverlay에 ToTitleButton), `EditorBuildSettings.asset`(Title+Prototype)

---

## 2026-09-04 (2) — 1~3층 방 데이터 JSON 외부화: 파일 구성 · 마지막 층 판정 · 구 에셋 처리 · 손상 폴백

### 배경 / 문제
- 로드맵 §6 #2. `RoomDataAsset`(ScriptableObject) 29개 + 씬 `StageManager.floors` 배선으로
  authoring된 층/방 데이터를, `GameBalance`처럼 텍스트 에디터로 수정 가능한 JSON으로 빼고
  싶다. 요구: 4층 제작 시 Claude/Unity 없이 JSON 파일 하나만 더 만들어 1~3층 파일을 참고해
  저작할 수 있어야 함.

### 검토한 대안 / 선택

| 쟁점 | 선택 | 대안 / 이유 |
|---|---|---|
| **파일 구성** | 층당 파일 1개 (`Balance/Stage/Floors/FloorN.json`), 로더가 `Floor1`부터 없어질 때까지 열거 | vs 단일 `Floors.json`(배열): 4층 추가 시 공유 파일을 편집 → 오타 하나로 전 층 깨짐, "파일 하나만 추가" 요구와 배치. vs 매니페스트+파일: 추가 시 손댈 곳 2군데. → **층당 파일**이 "Floor4.json 하나만 추가"에 정확히 부합, 층별 격리(한 층이 깨져도 나머지 무사) |
| **마지막 층 판정 (사용자: C안)** | 파일 열거 위치가 결정 (`isLastFloor`는 검증용 — 불일치 시 경고만, 열거 우선) | vs (a) 플래그 절대 신뢰: 4층 추가 시 3층 파일도 열어 `false`로 수정해야 함 → 깜빡하면 3층에 갇힘. vs (b) 플래그 완전 무시. → **(c)**: 요구대로 플래그를 데이터에 넣되, 실제 판정은 파일 존재로 → 플래그 실수해도 진행 안 막힘 |
| **구 `RoomDataAsset` 29개 + `ScriptableObjectFloorLayoutProvider` (사용자: A안)** | 삭제 | vs `_legacy/`로 이동 보존: 이중 authoring 경로를 남기지 않는 게 이 작업의 목적. 롤백은 git 히스토리 + 이 로그 |
| **JSON 손상/누락 시 (사용자: B안 + 팝업)** | 그 층만 코드 내장 비상용 3방 층(Start-Combat-Boss)으로 대체 + `IsCorrupted` 플래그 → 진입 시 `SystemNotificationUI` 팝업으로 "N층 데이터 손상" 안내 | vs 하드 에러(런 시작 불가): "잘못된 JSON은 크래시가 아니라 열화" 프로젝트 원칙 위반. vs 콘솔 경고만: 플레이어가 인지 못 함. 팝업은 범용 시스템 알림 핸들러로 분리(향후 다른 시스템 알림 재사용) |

### 선택 이유 / 트레이드오프
- 열거 시 중간 번호가 비고 그 뒤 번호가 있으면(예: Floor2 없는데 Floor3 있음) 경고 후 갭에서
  중단 — "누락"과 "손상"은 구분: 파일이 아예 없으면 열거 중단(갭), 파일은 있는데 파싱 실패면
  그 슬롯만 비상용 대체(층 수 유지).
- `SystemNotificationUI`는 CanvasGroup 방식(자기-비활성 패널 함정 회피, `[[feedback_self_deactivating_panel_footgun]]`).
  메시지 큐 보유 — 손상된 층이 2개면 확인 후 다음 알림.
- enum은 JSON에서 이름 문자열(`"Combat"`, `"Ranged"`, `"North"`)로 저작, `Enum.TryParse`+`Enum.IsDefined`로 파싱 — `GameBalance`가 `PassiveItemType`에 쓰는 방식과 동일. JsonUtility는 `Vector2Int`/`Vector3`를 `{x,y[,z]}`로 왕복.

### 관련 파일
- 신규: `Assets/Scripts/Stage/GameFloors.cs`(로더+DTO), `Assets/Scripts/Stage/FloorLayout.cs`,
  `Assets/Scripts/Room/JsonFloorLayoutProvider.cs`, `Assets/Scripts/UI/SystemNotificationUI.cs`,
  `Assets/StreamingAssets/Balance/Stage/Floors/Floor{1,2,3}.json`
- 삭제: `Assets/Scripts/Room/RoomDataAsset.cs`, `ScriptableObjectFloorLayoutProvider.cs`,
  `Assets/Data/Rooms/*.asset` 29개
- 수정: `StageManager.cs`(`floors` 필드 제거, `GameFloors` 사용, `FloorDataCorrupted` 이벤트,
  `CurrentStageNumber`), `UIManager.cs`(HUD 텍스트 `x-y` 포맷 + 팝업 배선), `IFloorLayoutProvider.cs`(주석)

---

## 2026-09-04 — 대시/걷기의 벽 관통: 벽 콜라이더 두껍게 vs 이동 로직에서 방향 클램프 vs 솔버 설정

### 배경 / 문제
- 스테이지3에서 벽에 붙어 대시(스페이스 홀드)하면 벽을 통과, 이동속도 패시브가 쌓인 상태에선
  걷기만 해도 벽을 통과하는 현상. 격리 테스트로 원인 확정: `Rigidbody.MovePosition`(및 대시가
  재적용하는 `linearVelocity`)을 벽으로 **물리 스텝당 ~0.2유닛 초과**로 명령하면 솔버의
  depenetration을 앞질러 캡슐이 벽 안으로 누적 침투 → 반대편으로 빠져나감. 걷기는
  `CurrentMoveSpeed`가 ~10을 넘어야(이동속도 패시브 ~4개, ≈ 스테이지3) 발현.
- 격리 테스트로 배제한 것: `m_DefaultSolverVelocityIterations` 1→2→4(효과 없음),
  `CollisionDetectionMode`(Discrete/ContinuousDynamic 차이 없음 — 걷기 MovePosition엔 CCD 미적용),
  월드 원점에서의 거리(무관). 순수하게 스텝당 이동거리 문제.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A. 벽 콜라이더를 보이는 것보다 두껍게(예: `WallThickness×3`) | 이동 코드 안 건드림. 단발 대시는 안 뚫릴 확률↑ | 결정론적이지 않음(반복 대시·각도에 따라 여전히 뚫림). **대시 속도가 이동속도 연동이라 "충분한 두께" 기준이 계속 움직임**. 벽=메시=콜라이더 동일 오브젝트라 별도 콜라이더 자식 추가 필요(스윕 클램프보다 코드↑). 방 가용 공간·문 지오메트리 재조정 비용. 근본 원인 미해결(넉백 등에서 재발) |
| B. 대시를 `MovePosition` → `linearVelocity` 구동으로 + `PlayerMovement`/`PlayerDodgeRoll` 둘 다 이동 전 방향을 벽에 대해 클램프(`WallClamp`) | 감지된 벽으로는 속도를 아예 안 넣으니 솔버·CCD·이동속도와 무관하게 관통 불가. 정면=정지·스침=슬라이드 감각. `MovePosition` 이동 패러다임 유지 | 얕은 각도 스침 + 매우 높은 이동속도에선 여전히 조금 누출(방향 프로젝션의 한계). 이동 FixedUpdate마다 `CapsuleCast` 1회 |
| C. `m_DefaultSolverVelocityIterations` 상향 | 전역, 한 줄 | **격리 테스트 결과 효과 없음** (ms 11+에서 그대로 관통) |
| D. 플레이어 이동을 `CharacterController`/full collide-and-slide로 교체 | 근본 해결, 얕은 스침까지 완벽 | 프로토타입 단계에 큰 리팩터. 큐브 플레이스홀더 지오메트리 기준으로 지금 하면 아트 넣고 재조정 시 두 번 일 |

### Claude 제안
- **B 채택**. C는 실측으로 무효 확인. A는 대시 속도 가변화와 상충하고 근본 미해결. D는
  프로토타입 스코프 밖 — 아트 통합 시 콜라이더/히트박스를 어차피 재조정하므로 그때.
- `WallClamp`는 캡슐을 **반지름만큼 뒤로 물린 지점에서** `CapsuleCast` — 정위치 캐스트는
  "시작 시 이미 겹친 콜라이더 무시" 스펙 때문에 '벽에 붙어서' 케이스를 못 잡음.

### 선택한 방향
- **B**. `WallClamp.ClampDirection(capsule, dir, moveThisStep)` 공용 헬퍼, `PlayerMovement`·
  `PlayerDodgeRoll`에서 호출. 대시는 `linearVelocity` 구동. `Player.prefab` Rigidbody
  `CollisionDetection` Discrete → ContinuousDynamic(고속 이동 보험).
- 잔여(얕은 스침 + 극단 이동속도)는 **아트 통합(Phase 4) 시 D로 처리**하는 것으로 이월,
  `ClaudeSessionSummary.md` §5 / `DevLog.md` "플레이스홀더 지오메트리" 목록에 기록.

### 선택 이유
- 정면 벽 충돌(사용자가 신고한 케이스)은 B로 이동속도 5~100 전부 결정론적으로 막힘 —
  실측으로 확인. 잔여 케이스는 좁고(벽 따라 달리며 얕게 파고드는 입력 + 일반 런의 픽업 수를
  한참 넘는 이동속도) 프로토타입 진행을 막지 않음. 큐브 지오메트리에 D를 들이는 건 재작업
  위험이 커서, "정면은 지금 확실히, 나머지는 아트와 함께" 트레이드오프.

### 관련 파일
- `Assets/Scripts/Player/WallClamp.cs`(신규), `PlayerMovement.cs`, `PlayerDodgeRoll.cs`,
  `Assets/Prefabs/Player.prefab`

---

## 2026-09-04 — `autoSyncTransforms` off로 인한 재빌드/순간이동 버그: 전역 설정 vs 지점별 SyncTransforms

### 배경 / 문제
- `ProjectSettings/Physics`의 `Auto Sync Transforms`가 꺼져 있음(`m_AutoSyncTransforms: 0`).
  이 상태에서 (a) `AdvanceToNextFloor()`가 Floor Complete 암전(`timeScale==0`, FixedUpdate 없음)
  중 새 층을 짓고, (b) `RoomTransitionController`가 `player.position`을 Transform으로 직접
  옮기면, 다음 물리 스텝 전까지 콜라이더/`Rigidbody.position`이 물리 씬에 미반영 → 새 층 벽이
  안 막히고, 전환 후 플레이어가 낡은 `Rigidbody.position` 쪽으로 끌려감. 이번 세션 버그 3건의
  공통 뿌리.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A. `Auto Sync Transforms` 전역 ON | 이 클래스의 버그를 통째로 제거. Unity 신규 프로젝트 기본값이 ON | 프레임당 싱크 비용 소폭↑. 전역 동작 변경이라 다른 코드의 암묵적 가정에 영향 가능. 사용자가 당장 필요성을 못 느낌 |
| B. 문제 지점마다 `Physics.SyncTransforms()` 명시 호출 | 영향 범위가 그 경로에만 국한. 무엇을·왜 하는지 코드에 남음 | 새 "재빌드/순간이동" 경로가 생길 때마다 빠뜨릴 수 있음 |
| C. 암전을 `timeScale=1`에서 수행하도록 페이드 연출 재구성 | 다음 FixedUpdate가 자동으로 싱크 | 침습적, 페이드 choreography 회귀 위험 큼 |

### Claude 제안
- **B 채택**하되, "같은 계열 버그가 또 나오면 A로 전환" 조건을 명시. C는 얻는 것 대비 위험 과다.

### 선택한 방향
- **B**. `StageManager.BuildFloor()` 끝 + `RoomTransitionController.TransitionRoutine()`의
  `player.position = arrivalPoint;` 직후에 `Physics.SyncTransforms()`.

### 선택 이유
- 지금 영향받는 경로가 2곳으로 특정되고, 각 지점에 이유를 주석으로 남기면 추적 가능.
  전역 ON은 "필요해지면" 언제든 할 수 있고 B를 깨지 않음. 사용자가 전역 변경에 소극적이라
  최소 침습을 우선.

### 관련 파일
- `Assets/Scripts/Stage/StageManager.cs`, `Assets/Scripts/Room/RoomTransitionController.cs`

---

## 2026-09-04 — 대시 속도: 고정값 vs 현재 이동속도 × 배수, 수치 위치

### 배경 / 문제
- 대시가 `dodgeRollDistance / dodgeRollDuration` 고정(15 u/s)이라 이동속도 패시브를 먹어도
  대시는 그대로 → 기동성 성장이 대시에 반영 안 됨. 사용자가 "이동속도에 따라 대시도 오르게"
  요청.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 거리 스케일(`dodgeRollDistance × 이동속도비`), 지속시간 고정 | 대시 "사거리"가 스탯에 비례 | 배수 모델과 수식만 다를 뿐 결과 동일(지속 고정 시) |
| **"0.2초 동안 현재 이동속도 × N배 버스트"** (`dashSpeed = CurrentMoveSpeed × mult`) | 개념이 단순("대시 = 짧은 고속 이동"). base 스탯에서 5×3=15, 3유닛 → **현재 수치 그대로 보존**. 무적창(지속) 고정이라 방어 가치는 불변, 기동성만 성장 | `dodgeRollDistance` 필드 제거(파생값으로) |
| 지속시간도 스케일 | — | 무적 프레임이 이동속도로 늘어남 → 밸런스 레버가 과함. 비채택 |
| 수치를 `[SerializeField]` 유지 vs `GameBalance` JSON | — | JSON: `moveSpeed` 등과 동일 패턴, 텍스트 튜닝 가능. 파싱은 되는데 키만 누락되면 0으로 채워짐(파일 통째 깨짐과 달리 fallback 안 탐) — `_fieldGuide`에 "0이하면 대시 안 나감" 명시로 완화 |

### 선택한 방향
- **"현재 이동속도 × 3배, 0.2초" 모델.** `PlayerMovement.CurrentMoveSpeed` 프로퍼티 신설(이동속도
  단일 소스). `dashSpeedMultiplier`(3.0)·`dashDuration`(0.2)는 `PlayerBaseStats.json`으로 외부화
  (`GameBalance` 내장 fallback 포함). `dodgeRollDistance`/`dodgeRollDuration` `[SerializeField]`
  및 프리팹 직렬화 값 제거, `cooldown`은 `[SerializeField]` 유지.

### 선택 이유
- 배수 모델이 "대시란 무엇인가"를 가장 직관적으로 표현하고, 배수 3으로 잡으면 기존 수치가
  그대로라 회귀 위험이 없음. 지속시간 고정은 대시의 방어적 가치(무적창)를 밸런스에서 분리해둠.
  JSON 외부화는 최근 `GameBalance` 방향과 통일 — 개별 키 누락 방어는 전 밸런스 파일 공통
  하드닝 사안이라 이번 범위 밖으로 둠.

### 관련 파일
- `Assets/Scripts/Player/PlayerMovement.cs`, `PlayerDodgeRoll.cs`,
  `Assets/Scripts/Balance/PlayerBaseStatsData.cs`, `GameBalance.cs`,
  `Assets/StreamingAssets/Balance/PlayerBaseStats.json`

---

## 2026-09-04 — 문 앞 드랍템 획득 불가: 문 트리거 축소 vs 드랍 위치 보정 vs 전환 시 자동 획득

### 배경 / 문제
- 문 통과 감지 트리거가 `WallThickness × 3`(3유닛) 깊이인데 보이는 문 마커는 1유닛뿐 →
  문 앞 방 안쪽 1.5유닛 × 문폭 6유닛 구간에 아이템이 떨어지면, 주우러 가다 문 트리거를 먼저
  밟아 다음 방으로 전환됨. 아이템 획득 불가.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A. 문 트리거 깊이 축소(`×3` → `×1.5~2`) | 한 줄, 데드존 전반 축소 | **위험**: 대시 속도가 이동속도 연동(극단 60 u/s = 1.2 u/step)이라 트리거가 얇으면 빠른 대시가 프레임 사이로 트리거를 건너뛰어 **문 전환이 안 될** 수 있음. 3유닛 깊이는 이 마진용 |
| B. 드랍 좌표를 문 트리거 밖으로 밀어내기(`ItemDropManager`) | 문 동작 불변(대시 통과 신뢰성 유지). 자체 물리 쿼리라 시그니처 변경 없음. 데드존을 근본적으로 없앰 | 드랍당 `OverlapSphere` 1회. 아이템이 죽은 자리에서 3유닛 당겨짐(여전히 근처) |
| C. 전환 시 근처 `ItemPickup` 자동 획득 | 관대함 | 의도적으로 남긴 아이템까지 획득·엉뚱한 쪽에서 획득 등 마법적. B로 해결되면 불필요 |

### 추가 요청
- 사용자: 보정을 **`Instantiate` 전에** 하여 원래 자리에 잠깐 보였다가 밀리는 게 안 보이게.

### 선택한 방향
- **B**. `ItemDropManager.NudgeClearOfDoors()` — `OverlapSphereNonAlloc`로 근처 `DoorController`
  찾아, 드랍을 문 평면 → `EntryPoint` 방향으로 `DoorClearDistance`(3유닛)까지 밀어냄.
  `SpawnPickup`의 `Instantiate` **전에** 적용(슬라이드 안 보임). 이미 여유 있는 드랍(보장
  보상 포함)은 no-op. 문 트리거는 그대로.

### 선택 이유
- 방금 대시 속도를 이동속도 연동으로 바꿔서 A(트리거 축소)의 통과 미검출 위험이 커진 상태 —
  문 쪽을 건드리지 않고 아이템 쪽에서 "항상 주울 수 있는 자리"를 보장하는 B가 부작용이 가장
  적음. C는 B로 충분하면 과함.

### 관련 파일
- `Assets/Scripts/Combat/ItemDropManager.cs`

---

## 2026-09-03 — 씬 분리 구조: 단일 씬 유지 vs `MainMenu`+`Gameplay` 2씬 vs Bootstrap+Additive

### 배경 / 문제
- 지금까지 모든 작업을 씬 하나(`Prototype.unity`)에서 해 왔고, Build Settings에도 이 씬
  하나만 등록돼 있음. 로드맵 §6의 "UI Scene 신설 → 전체 플레이 루프(타이틀 → 캐릭터 선택 →
  런 → 클리어/게임오버 → 재시작·타이틀 복귀)"를 앞두고, **인게임과 메인 메뉴를 씬으로 나눌지,
  나눈다면 어느 단위로 나눌지** 결정이 필요해짐.
- 결정 전 확인한 현재 구조:
  - 싱글톤도 `DontDestroyOnLoad`도 **전혀 없음**. 모든 연결이 인스펙터 `[SerializeField]` 참조.
  - `SceneManager` 사용처는 `FloorCompleteUI`의 재시작(현재 씬 리로드) 한 곳뿐 →
    **"씬 리로드"가 현재 유일한 런 리셋 메커니즘**.
  - 층은 씬이 아니라 데이터(`FloorDefinition[]` + `RoomDataAsset`)이고, 층 전환은
    2026-09-02 결정대로 씬을 유지한 채 `StageManager.AdvanceToNextFloor()`로 제자리 재빌드.
  - 캐릭터 선택은 별도 화면이 아니라 인게임 씬 안의 패널이고,
    `CharacterSelectController`(Player에 부착)가 `PlayerInputActions.enabled = false`로 입력을 잠금.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A. 단일 씬 유지 — 타이틀/캐릭터선택/결과를 전부 `CanvasGroup` 패널로 처리 | 크로스 씬 배선이 0. 기존 `[SerializeField]` 참조를 하나도 안 건드림. 가장 빠름 | 타이틀 UI가 같은 씬에 들어오면 **씬 리로드가 타이틀까지 되살려서** "재시작"과 "타이틀 복귀"가 같은 동작이 됨 → 지금 공짜로 얻고 있는 리셋을 수동 구현해야 함. 씬 파일이 계속 비대해짐(이미 diff 핫스팟). 타이틀 화면에서 전투 리소스까지 전부 로드. Phase 5(비동기 로딩/로딩 UI)로 갈 길이 없음 |
| B. 2씬 — `MainMenu`(타이틀·캐릭터 선택·재시작/타이틀 복귀) + `Gameplay`(런 전체, 층 1~3 포함) | 씬 리로드 = 런 리셋이 그대로 유지됨. 영속 오브젝트가 최대 1개라 "매니저마다 싱글톤" 함정을 피함. `Gameplay` 씬 **내부** 인스펙터 배선은 손댈 필요 없음. 로드맵의 "UI Scene 신설"과 정확히 일치 | 이 프로젝트 최초의 "씬을 넘는 상태"가 생김(전달값 설계 필요). `CharacterSelectController`가 "UI 띄우고 기다림" → "컨텍스트에서 읽어 장착"으로 바뀌어야 함 |
| C. Bootstrap + Additive — `Bootstrap`(영속 매니저) + `MainMenu`/`Gameplay`를 Additive 로드 | 아키텍처 스토리가 가장 강함. 로딩 화면·Addressables가 자연스럽게 붙음 | **Additive에서는 인스펙터 크로스 씬 참조가 불가**하므로 등록/서비스 조회 또는 이벤트 채널 계층을 지금 도입해야 함. 그런데 지금 영속시킬 매니저가 사실상 없음 — `GameBalance`는 이미 static이고 `StageManager`/`UIManager`는 런 스코프. 비용만 있고 얻는 게 없음 |

### Claude 제안
- **안 B 채택**을 제안하되, "메인 메뉴 UI라서 나눈다"가 아니라 **씬 리로드가 현재 유일한
  리셋 메커니즘이라는 점**이 실질적인 근거임을 먼저 확인. 아울러 **층별 씬 분리는 하지 말 것**을
  같이 제안 — 층은 이미 `RoomDataAsset` 데이터로 구성되므로 씬으로 만들면 같은 메커니즘이 두 벌이 됨.
- 씬 간 전달값은 static `RunContext` 권장(`GameBalance`/`PauseState`처럼 이 프로젝트에 이미
  있는 패턴, `[RuntimeInitializeOnLoadMethod]` 리셋 포함). ScriptableObject 방식은 플레이 모드
  중 변경이 에디터에서 에셋에 남는 함정이 있고, `DontDestroyOnLoad` 오브젝트는 지금 영속시킬
  대상이 없어 과함 — 넘길 값이 `SelectedArchetype` 하나뿐이라는 점이 판단 근거.

### 선택한 방향
- **안 B 채택** (사용자 확정). 씬은 `MainMenu` + `Gameplay` 둘.
- **캐릭터 선택 UI는 `MainMenu` 씬으로 이동**(사용자 지시).
- 층 1~3은 계속 데이터 + 제자리 재빌드(2026-09-02 결정 유지). 층별 씬 없음.
- 결과 화면(`FloorCompleteUI`)은 `Gameplay` 씬 유지가 권장안 — 아직 사용자 확정 대기.

### 선택 이유
- A는 "지금 당장 가장 싸지만" 씬 리로드로 공짜로 얻던 런 리셋을 잃고, 그걸 수동 리셋 코드로
  다시 만들어야 함 — 2026-09-02에 층 전환을 제자리 재빌드로 결정할 때와 정확히 같은 종류의
  트레이드오프이고, 이번엔 방향이 반대(리로드를 **살리는** 쪽이 이득).
- C는 지금 시점엔 영속시킬 매니저가 없어 서비스 로케이터부터 짓는 비용만 남음. 다만 B는 C의
  전제를 깨지 않으므로, **Phase 5(로딩 화면/Addressables)에서 필요해지면 B를 부수지 않고
  앞에 `Bootstrap` 씬만 추가해 C로 승격**할 수 있음 — 그 조건이 생기면 재검토.
- B의 유일한 실질 비용인 "씬을 넘는 상태"는 현재 전달값이 1개(선택한 아키타입)라 매우 작음.
  `Gameplay` 씬 단독 Play 시 fallback 아키타입으로 동작하게 해서 개발 편의는 유지.

### 관련 파일
- 신규 예정: `Assets/Scripts/Core/RunContext.cs`, `SceneNames.cs`/`SceneLoader.cs`,
  `MainMenuController.cs`, `Assets/Scenes/MainMenu.unity`,
  `Assets/Prefabs/UI/CharacterSelectPanel.prefab`
- 수정 예정: `CharacterSelectController.cs`, `UIManager.cs`, `CharacterSelectUI.cs`,
  `FloorCompleteUI.cs`, `PauseState.cs`, `Assets/Scenes/Prototype.unity`(→ `Gameplay.unity`)
- 단계별 실행 계획과 검증 항목은 `DevLog.md` 2026-09-03 "다음 작업" 참조.

---

## 2026-09-02 — 층1→층2 전환: 씬 리로드 vs 제자리 재빌드

### 배경 / 문제
- 층2("ㅂ"자 10개 방) authoring이 끝난 뒤, 층1 클리어 후 층2로 넘어가는
  실제 진행 로직이 필요해짐. 기존 "Retry"는 씬을 통째로 리로드하는
  방식이었는데, 이걸 층 전환에도 그대로 쓸지 다른 방식을 쓸지 결정 필요.
  사용자 요구사항: 층을 넘어가도 패시브/액티브 아이템 진행 상황이 유지돼야
  하고, 캐릭터 선택 화면이 층마다 다시 뜨면 안 됨.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 씬 리로드 + 정적 상태로 "몇 층인지"/"이미 캐릭터 선택했는지" 전달 | 기존 Retry와 동일한 메커니즘이라 새 정리 코드가 거의 불필요 | 리로드 시 Player 오브젝트가 재생성돼 패시브/액티브 아이템 진행 상황이 초기화됨(별도 저장/복원 로직 필요), 캐릭터 선택 재등장을 막으려면 층을 넘어 유지되는 별도 플래그 상태가 필요 |
| 씬 유지, `StageManager`가 현재 층의 방 GameObject만 파괴 후 다음 층 데이터로 재빌드 | Player를 안 건드리므로 패시브/액티브 아이템 상태가 자연히 보존됨. `CharacterSelectController.Awake()`는 Player가 파괴·재생성되지 않는 한 세션당 1회만 실행되므로 캐릭터 선택도 별도 처리 없이 재등장 안 함 | 방/문/미니맵/전환 컨트롤러 각각에 "정리 후 재초기화" 로직을 새로 추가해야 함(`StageManager.TeardownCurrentFloor()`, `MinimapUI.Initialize()` 재호출 가능하게 등) |

### Claude 제안
- 제자리 재빌드안 채택 전, 오브젝트 풀 안전성(적 인스턴스가 방이 아니라
  풀 자신의 자식으로 관리되는지)과 `CharacterSelectController`의 실행 시점을
  코드로 먼저 확인 — 둘 다 안전하다고 확인한 뒤에 제안.

### 선택한 방향
- **제자리 재빌드안** 채택. 마지막 층(현재 층2) 클리어 후의 기존 Retry
  버튼만 예외적으로 씬 리로드 유지 — 이건 "완전히 새로 시작"이 의도이므로
  오히려 리로드가 정확히 맞음.

### 선택 이유
- 이번 요구사항(진행 상황 유지, 캐릭터 선택 억제)을 씬 리로드안으로
  만족시키려면 결국 제자리 재빌드안이 공짜로 얻는 이점(Player 생존)을 힘들게
  재구현해야 함 — 되돌아가는 게 아니라 처음부터 그쪽이 더 단순함.
- 트레이드오프: 향후 층이 늘어나면(3층+) `StageManager`의 파괴/재빌드
  로직이 매번 정확히 정리되는지 계속 신경 써야 함 — 방/미니맵/전환 컨트롤러
  중 하나라도 이벤트 구독 해제를 빠뜨리면 누수. 3층 추가 시 우선 확인할 것.

### 관련 파일
- `Assets/Scripts/Stage/StageManager.cs`(`FloorDefinition[] floors`,
  `AdvanceToNextFloor()`, `TeardownCurrentFloor()`)
- `Assets/Scripts/UI/MinimapUI.cs`(`Initialize()` 재호출 가능하게 수정)
- `Assets/Scripts/UI/FloorCompleteUI.cs`, `FloorTransitionUI.cs`(신규),
  `Assets/Scripts/Room/RoomFlowManager.cs`(전환 시퀀스 지휘)
- 자세한 구현 내용은 `DevLog.md` 2026-09-02 (3) 참고

---

## 2026-09-01 — Enhanced Shot 사거리 분리: 전용 프리팹/풀 vs 공유 프리팹 + rangeOverride 인자

### 배경 / 문제
- 기본 공격 사거리를 3유닛(발사체 수명 0.2초)으로 줄이자, 같은
  `Projectile_Basic` 프리팹/풀을 공유하는 액티브 아이템 "Enhanced Shot"의
  사거리까지 45→3유닛으로 딸려 짧아짐. 사용자가 "Enhanced Shot은 기본 공격과
  별개 스킬로 동작"하도록 요청 (DevLog 2026-08-31 (15)에서 "전용 프리팹/풀
  분리는 별도 논의"로 남겨뒀던 항목).

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| Enhanced Shot 전용 `Projectile` 프리팹 + 전용 풀 GameObject 신설 | 스킬 투사체를 비주얼/거동까지 완전히 독립적으로 튜닝 가능 | 새 프리팹 + 씬에 새 풀 오브젝트 + `ActiveItemContext`에 필드 추가 + 배선 — 투사체 거동(직진·피격·소멸)은 기본 공격과 동일한데 사거리 숫자 하나 때문에 인프라를 복제 |
| 기존 `bonusLifetime` 인자를 재활용해 큰 값 전달 | 코드 변경 최소 | `bonusLifetime`은 패시브 "사거리 증가"(기본 공격 전용) 의미라 의미가 섞임, 총 수명 = base + bonus라 base(0.2초)에 계속 의존 |
| `Projectile.Initialize`에 `rangeOverride`(월드 유닛, 기본 -1) 옵션 인자 추가, `>= 0`이면 base 수명 경로를 완전히 무시 | 프리팹/풀은 그대로 공유(거동 동일), 사거리 숫자만 스킬 데이터(`EnhancedShotActiveItemData.range`)가 소유, 기존 호출부는 무변경 | `Projectile`에 분기 한 줄 추가, 미초기화 시 footgun(값 0 → 즉시 소멸) 방지용 `Awake` 초기화 필요 |

### Claude 제안
- 세 번째 대안. 투사체의 실제 거동이 기본 공격과 동일하므로 전용 프리팹/풀은
  복제 비용만 크고 얻는 게 없음(과설계). 사거리는 "스킬이 자기 데이터로 소유"
  하면 충분하고, `rangeOverride`는 기존 `bonusLifetime` 경로를 안 건드려서
  패시브 사거리 스탯과 의미가 안 섞임.

### 선택한 방향
- `rangeOverride` 옵션 인자 채택. `EnhancedShotActiveItemData.range` 신규,
  `Projectile.Awake()`에서 `-1`로 초기화, 프리팹/풀 공유 유지.
- 스킬 스펙은 같은 날 사용자가 확정: 사거리 25유닛 / 데미지 16(레벨 1) /
  쿨다운 2초. (분리 작업 직후 임시로 45유닛을 뒀다가, 스펙을 제대로 안
  정했다는 사용자 판단으로 25유닛·16데미지로 재지정.)

### 선택 이유
- 지금 필요한 건 "사거리 숫자의 소유권 분리"뿐이고 투사체 종류가 갈라진 게
  아님. 전용 풀은 실제로 거동이 달라지는 스킬(관통/유도/폭발 등)이 생기는
  시점에 도입하는 게 맞음 — 그때 `rangeOverride`는 그대로 두고 전용 프리팹만
  추가하면 됨.

### 관련 파일
- `Assets/Scripts/Combat/Projectile.cs`
- `Assets/Scripts/Combat/EnhancedShotActiveItem.cs`, `EnhancedShotActiveItemData.cs`
- `Assets/Data/Skills/EnhancedShotData.asset`

---

## 2026-08-31 — 방 지오메트리: 방향 조합 프리팹 vs 런타임 절차적 생성

### 배경 / 문제
- Stage 2.5(룸 시스템 전환) 중 각 방의 바닥/벽/문을 어떻게 만들지 결정 필요.
  방마다 문이 열린 방향 조합이 다름(예: 시작방은 1방향만, 중간방은 2방향).

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 방향 조합별 프리팹을 미리 다 만들어둠(2^4=16가지) | 프리팹만 배치하면 끝, 런타임 코드 불필요 | 조합 폭발(최대 16종) — 문/벽 배치를 조합마다 수작업으로 만들어야 함, `RoomData`의 연결 정보와 별도로 프리팹 선택 로직도 필요해 데이터가 두 곳(연결 정보 + 프리팹 종류)으로 갈라짐 |
| `RoomController.Initialize()`가 `RoomData`(연결 방향 목록)를 보고 그 자리에서 바닥/벽/문을 직접 생성 | 조합 폭발 없음 — 이미 있는 `RoomData` 연결 정보를 그대로 재사용, `Room_Base.prefab`은 빈 껍데기 하나로 충분 | 지오메트리 생성 코드를 직접 작성/유지해야 함 |

### Claude 제안
- 두 번째 대안 채택 제안. "여러 방향 조합 프리팹을 따로 만들 필요가 있냐"는
  질문에 대해, 이미 `RoomData`가 문 방향별 연결 리스트를 authoring하고 있으므로
  그 정보를 프리팹 선택이 아니라 지오메트리 생성 입력으로 그대로 재사용하면
  조합 폭발을 피할 수 있다는 점을 근거로 제시.

### 선택한 방향
- 그대로 채택. `Room_Base.prefab`을 `RoomManager`+`RoomController`만 있는 빈
  껍데기로 축소, 바닥/벽/문 지오메트리는 전부 `RoomController.Initialize()`
  에서 코드로 생성.

### 선택 이유
- 데이터(연결 정보)와 비주얼(지오메트리)을 분리하지 않고 하나의 소스에서
  파생시키는 쪽이, 프리팹 조합을 따로 관리하는 것보다 향후 방 크기/모양이
  다양해질 때도 확장 비용이 낮음.

### 관련 파일
- `Assets/Scripts/Room/RoomController.cs`
- `Assets/Prefabs/Room_Base.prefab`
- `Assets/Data/Rooms/Room01_Start~Room05_Boss.asset`

---

## 2026-08-31 — 방-방 이동: 물리적 도보 이동 vs 카메라 연출 전환

### 배경 / 문제
- 문을 통과해 다음 방으로 넘어가는 방식을 실플레이 피드백으로 재검토.
  기존엔 플레이어가 물리적으로 걸어서 다음 방 위치까지 이동해야 했음.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 물리적으로 계속 걸어서 이동(기존 방식) | 구현 이미 되어 있음, 추가 작업 없음 | 방 사이 빈 공간을 걸어서 지나가야 해 템포가 늘어짐, 방 배치 간격(`roomGap`)을 넓게 잡을수록 더 길어짐 |
| 문 트리거 통과 시 플레이어 조작을 잠그고 카메라만 목표 방 입구까지 보간 이동(`SmoothStep`), 도착 후 플레이어 재배치 | 방 사이 이동 템포가 즉시로 줄어듦, 로딩/전환 연출로 방 경계를 자연스럽게 가릴 수 있음 | 전환 중 렌더러/입력/물리(`Rigidbody.isKinematic`) 상태를 임시로 바꿨다가 정확히 원복해야 함 — 상태 복원을 빠뜨리면 버그로 이어짐 |

### Claude 제안
- 두 대안을 제시, 보간 방식 중에서도 즉시 컷 vs 부드러운 이동 중 사용자가
  부드러운 이동(`SmoothStep`)을 선택.

### 선택한 방향
- 카메라 연출 전환 채택. 신규 `RoomTransitionController`(최상위 씬 오브젝트)
  — 문 트리거 통과 시 플레이어 렌더러 비활성화+입력 잠금+`Rigidbody` kinematic
  전환 후 카메라를 0.6초간 `SmoothStep`으로 이동, 도착 시 전부 원복.

### 선택 이유
- 방이 40x40 유닛으로 커지면서 물리 도보 이동의 템포 저하가 더 뚜렷해질
  것으로 판단, 연출 전환이 다음 방으로의 진입감도 함께 살릴 수 있어 선택.

### 관련 파일
- `Assets/Scripts/Room/RoomTransitionController.cs`
- `Assets/Scripts/Room/DoorController.cs`
- `Assets/Scripts/Camera/QuarterViewCameraFollow.cs`

---

## 2026-08-31 — 문 잠금 정책: 방별 사전 잠금 vs 아이작 방식(기본 열림 + 입장 시 잠금)

### 배경 / 문제
- 기존엔 "미클리어 방은 처음부터 잠김, 클리어 시 연결된 문만 해제" 정책이었음.
  사용자가 실플레이 후 이 정책을 뒤집기로 요청.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 기존: 미클리어 방은 authoring 시점부터 문이 잠김 상태로 시작, 클리어 시 연결된 문만 해제 | 구현이 이미 되어 있음 | 아직 들어가지 않은 방의 문도 미리 잠겨 있어, 플레이어가 진입 자체를 시도하기 전부터 "잠김"이 보여서 아이작 특유의 "들어가자마자 갇힌다"는 긴장감이 약함 |
| 아이작 방식: 기본적으로 모든 문은 열림, 미클리어 방에 **입장하는 순간** 그 방의 모든 문이 잠기고, 클리어하면 다시 전부 열림 | 아이작 원작과 동일한 "입장=감금" 긴장감, 변경 지점이 2곳뿐(문 생성 시 기본값, `EnterRoom()`에 `LockAllDoors()` 추가)으로 구현 비용 낮음 | `HandleCleared()`의 기존 `door.IsConnected` 체크가 사실상 무의미해짐(안전장치로만 유지) |

### Claude 제안
- 사용자가 아이작 방식으로 직접 방향을 지정, 변경 지점이 `RoomController.
  BuildSide()`(기본값 열림)와 `EnterRoom()`(입장 시 전체 잠금) 2곳뿐이라는 점을
  확인해 그대로 구현.

### 선택한 방향
- 아이작 방식 채택.

### 선택 이유
- 사용자가 참고 대상으로 삼은 아이작 본가의 룰과 실제 체감을 일치시키는 것이
  목적이었고, 변경 비용도 낮아 트레이드오프가 명확했음.

### 관련 파일
- `Assets/Scripts/Room/RoomController.cs`

---

## 2026-08-31 — 미니맵 안개 시스템: 아이작 방식(전체 공개) vs 엔터 더 겅전 방식(3단계 발견)

### 배경 / 문제
- 미니맵 도입 초기엔 아이작 스타일로 층의 모든 방을 처음부터 다 보여주는
  방식으로 구현했음. 사용자가 재검토를 요청해 "엔터 더 겅전(Enter the
  Gungeon)" 스타일 — 안개 효과 + 방 연결선 + 배경 패널 — 로 재작업.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 아이작 방식: 층 전체 방을 처음부터 다 노출(안개 없음) | 구현 이미 되어 있음, 전체 구조를 한눈에 파악 가능 | 아직 가보지 않은 방의 타입(전투/보스 등)까지 미리 다 알 수 있어 탐험 긴장감이 떨어짐 |
| 엔터 더 겅전 방식: `RevealState` 3단계(`Hidden`→`Discovered`(인접 방문 시 타입 비공개 회색)→`Visited`(실제 입장 시 진짜 색 공개)) + 방 연결선 + 배경 패널 | 인접 발견 방식이 탐험 긴장감을 살림, 연결선으로 미로 구조를 시각적으로 보여줌 | `Dictionary<int, RevealState>` 및 연결선 프리팹(`MinimapConnector`) 추가 관리 필요, 기존 아이콘 prefab의 pivot도 좌표 계산 통일을 위해 재조정 필요 |

### Claude 제안
- 사용자가 엔터 더 겅전 스타일로 직접 방향 지정. `RoomEntered` 이벤트에서
  해당 방을 `Visited`로, 그 방의 모든 인접 방(`Doors[].TargetRoom`)을
  `Discovered`로 승격시키는 방식을 제안해 구현.

### 선택한 방향
- 엔터 더 겅전 방식 채택. `MinimapUI.cs` 전면 재작성, `MinimapConnector.prefab`
  신규, `MinimapIcon.prefab` pivot 재조정.

### 선택 이유
- 사용자가 참고하려는 대상 자체가 바뀌었고("아이작→겅전"), 안개 효과가
  탐험 긴장감이라는 로그라이트 장르 특성과 더 맞는다고 판단.

### 관련 파일
- `Assets/Scripts/UI/MinimapUI.cs`
- `Assets/Prefabs/UI/MinimapIcon.prefab`, `MinimapConnector.prefab`

---

## 2026-08-31 — 패시브 아이템 데이터 모델: 타입별 서브클래스 vs 단일 클래스 + enum 판별자

### 배경 / 문제
- 패시브 아이템(공격력/이동속도/사거리/공격속도 4종) 도입 시, 액티브 아이템은
  `IActiveItem` 구현체를 종류마다 따로 두는 다형성 구조였는데 패시브도 같은
  방식으로 갈지 결정 필요.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 액티브처럼 타입별 서브클래스(`AttackPassiveData`/`MoveSpeedPassiveData`...) + 공통 인터페이스 | 타입마다 동작이 크게 달라져도 확장이 깔끔, 액티브와 구조 일관성 | 4종이 전부 "플레이어 스탯에 값 하나 더하기"로 동일 — 클래스 4개가 사실상 상수만 다른 껍데기, 에셋 생성 메뉴도 4개로 갈라짐 |
| 단일 concrete `PassiveItemData` + `PassiveItemType` enum으로 분기 | 클래스 1개·에셋 메뉴 1개, authoring이 "타입 고르고 수치 입력"으로 단순 | enum에 없는 완전히 새로운 종류의 패시브(예: 조건부 발동)가 생기면 그때 리팩터 필요 |

### Claude 제안
- 두 번째 대안 추천. 4종의 행동이 전부 "스탯 += 값"으로 동형이라 다형성이
  실제로 분기하는 지점이 없고, 서브클래스는 지금 시점엔 상수 테이블을 클래스로
  부풀리는 것뿐이라는 점을 근거로 제시.

### 선택한 방향
- 단일 `PassiveItemData` + `PassiveItemType` enum 채택. 액티브/패시브 공통
  상위 타입으로 추상 `ItemData : ScriptableObject`(`OnAcquired(GameObject)`)
  신설, `ActiveItemData`도 이를 상속하도록 정리해 `ItemPickup`이 종류 분기
  없이 `_item.OnAcquired(...)` 하나로 처리.

### 선택 이유
- "실제로 다르게 동작하는 게 없는데 구조만 나누는" 과설계를 피함. 조건부/특수
  패시브가 실제로 필요해지는 시점에 그 요구를 보고 서브클래스로 승격하기로 함
  — `ItemData` 공통 베이스가 이미 있어 그때 확장 비용도 낮음.

### 관련 파일
- `Assets/Scripts/Combat/ItemData.cs`, `PassiveItemData.cs`, `PassiveItemType.cs`,
  `ActiveItemData.cs`
- `Assets/Scripts/Combat/ItemPickup.cs`, `ItemDropManager.cs`
- `Assets/Scripts/Player/PlayerPassiveStats.cs`
- `Assets/Data/PassiveItems/*.asset`

---

## 2026-08-31 — 패시브 스탯 Pip 바: 아이작 제곱근 소프트캡 공식 vs 개수 기준 캡 + 기본값 포함 비율

### 배경 / 문제
- ESC 장비 메뉴 좌측의 패시브 스탯 표시를 "공격력 증가: +0%" 문장형에서
  바인딩 오브 아이작 스타일의 10칸 Pip 바로 바꿔달라는 요청. 아이작을
  리서치했으나 우리 시스템(아이템당 고정값 + 덧셈, 무제한 스택)과 스탯
  모델이 달라 원작 공식을 그대로 못 씀.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 아이작 원작 공식(`기본 × √(아이템수 × 1.2 + 1)` 제곱근 소프트캡) 이식 | 원작과 동일한 체감 곡선 | 아이작은 곱연산/소프트캡 기반, 우리는 고정값 덧셈이라 공식 전제가 안 맞음 — 이식하려면 스탯 시스템 자체를 갈아야 함 |
| 스탯별로 "몇 개면 만땅"인지를 에셋에 직접 authoring(`capCount`), 채움 = `(기본값 + 보너스) / (기본값 + capCount × 아이템당증가량)` | 드랍 시스템과 같은 소스(`PassiveItemData`)를 공유해 UI에 캡을 중복 보관 안 함, 기본 스탯도 분자·분모에 얹어 아이템 0개에서도 기본 비율만큼 차 있음, 튜닝이 "개수 하나 바꾸기"로 직관적 | 퍼센트/곡선이 아니라 선형 근사(아이작 위키도 자기네 걸 "imprecise scale"이라 표현) |

### Claude 제안
- 두 번째 대안. 아이작도 실제로는 정밀 스케일이 아니라 구간 근사이고,
  `capCount`를 데이터 소스에 두면 UI가 별도 상수를 안 들고 가도 된다는 점을
  근거로 제시. 이후 "기본 스탯이 빠져 있다"는 피드백을 받아 분자·분모에
  기본값을 동일하게 더하는 형태로 보정(기존 튜닝 개수는 그대로 유지되도록 역산).

### 선택한 방향
- 개수 기준 캡 + 기본값 포함 선형 비율 채택. `PassiveItemData.capCount` 필드
  신설, `PlayerStatsPanelUI`가 `PlayerCombat`/`PlayerMovement`/`Projectile`
  기본값을 라이브로 읽어 재계산. 중간에 뒀던 타입별 획득 개수 카운터는 이
  공식으로 안 쓰이게 되어 제거.

### 선택 이유
- 포트폴리오 프로토타입 단계에서 Pip 바는 "대충 얼마나 강해졌나"를 보여주면
  충분하고, 정밀 곡선보다 튜닝 편의(개수 하나로 조정)와 데이터 단일 소스가
  더 중요하다고 판단.

### 관련 파일
- `Assets/Scripts/UI/PlayerStatsPanelUI.cs`
- `Assets/Scripts/Combat/PassiveItemData.cs`, `Projectile.cs`
- `Assets/Scripts/Player/PlayerPassiveStats.cs`, `PlayerMovement.cs`,
  `Assets/Scripts/Player/PlayerCombat.cs`

---

## 2026-08-31 — 일시정지(ESC 메뉴): 플레이어 입력만 차단 vs `Time.timeScale = 0`

### 배경 / 문제
- ESC 장비 메뉴가 열려 있어도 적/발사체가 계속 움직임 — 플레이어 입력만
  잠그고 있었기 때문. 게임 전체를 멈추고 싶다는 요청.
- 2026-08-21 "캐릭터 선택 UI" 결정에서는 즉시성 입력 액션이 얼어붙은 상태를
  남길 수 있다는 이유로 `timeScale=0`을 **일부러 피하고** 입력 비활성화를
  택했었음 — 그 판단을 이 맥락에서 다시 검토.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| 시스템별로 개별 정지(적 AI, 발사체, 스폰 타이머 등을 각각 멈춤) | `timeScale`을 슬로우모션 연출용으로 따로 남겨둘 수 있음 | 멈춰야 할 대상을 매번 빠짐없이 챙겨야 함 — 새 시스템이 생길 때마다 정지 목록에 추가를 잊으면 조용히 안 멈춤 |
| `Time.timeScale = 0` (정확히 0) | 한 줄로 전부 정지, 누락 위험 없음 | Input System 버튼 이벤트(`performed`)는 `timeScale`과 무관하게 계속 발동 → 정지 중 공격/구르기/스킬 버튼이 실행되는 허점 |
| `timeScale`을 0에 가까운 아주 작은 값 | 완전 정지의 부작용을 회피하려는 시도 | 메뉴를 오래 열수록 쿨다운/수명이 미세하게 누적되는 드리프트 버그 |

### Claude 제안
- 정확히 `0` 추천. 코드베이스의 모든 `Time.deltaTime`/`fixedDeltaTime` 사용처가
  곱셈·누적뿐이고 나눗셈이 없어 0으로 멈춰도 0-나누기 위험이 없음을 먼저 확인.
  0이 아닌 값은 드리프트를 남기므로 배제. 남는 허점(버튼 이벤트)은 별도
  가드로 막기로 함.

### 선택한 방향
- `EquipMenuController`가 `Time.timeScale` 0/1을 토글, 입력 비활성화 토글은
  제거. 후속으로 `PauseState`(정적, `IsPaused => Time.timeScale <= 0f`) 신설,
  `PlayerCombat`/`PlayerDodgeRoll`/`ActiveItemController`의 버튼 핸들러 맨
  앞에 `if (PauseState.IsPaused) return;` 가드 추가.

### 선택 이유
- 08-21에 `timeScale=0`을 피했던 이유(즉시성 액션이 얼어붙은 상태값을 남김)는
  이번엔 `PauseState` 가드로 정면 차단할 수 있어 트레이드오프가 뒤집힘.
  "멈출 대상을 빠짐없이 나열"하는 방식은 시스템이 늘수록 유지가 깨지기 쉬워,
  누락 불가능한 전역 정지 + 좁은 예외 가드가 더 안전하다고 판단.

### 관련 파일
- `Assets/Scripts/Player/EquipMenuController.cs`, `PauseState.cs`
- `Assets/Scripts/Player/PlayerCombat.cs`, `PlayerDodgeRoll.cs`
- `Assets/Scripts/Combat/ActiveItemController.cs`

---

## 2026-08-31 — 밸런스 수치 외부화: 파일 포맷(JSON vs CSV/key=value/YAML)과 주입 방식

### 배경 / 문제
- 플레이어 기본 능력치와 패시브 아이템 효과를 매번 Claude/Unity를 거치지 않고
  텍스트 파일만 고쳐 조정하고 싶다는 요청. 포맷과, 그 값을 코드가 어떻게
  읽을지 둘 다 결정 필요.

### 검토한 대안 (포맷)
| 대안 | 장점 | 단점 |
|---|---|---|
| CSV | 표 형태로 한눈에, diff 깔끔 | 중첩 구조 표현이 약함, 값별 설명 주석을 넣을 자리가 없음 |
| key=value(.ini류) | 사람이 쓰기 가장 쉬움 | 파서를 직접 짜야 함, 타입/구조 검증 부재 |
| YAML | 주석 지원, 가독성 좋음 | Unity 기본 파서 없음(외부 라이브러리 필요) |
| JSON + `JsonUtility` | Unity 내장 파서, DTO로 타입 안전, 매칭 안 되는 여분 필드는 무시되므로 `_guide` 필드를 "주석"처럼 넣을 수 있음 | 순수 JSON은 진짜 주석(`//`)이 없음 |

### 검토한 대안 (주입 방식)
| 대안 | 장점 | 단점 |
|---|---|---|
| 로드한 값을 ScriptableObject/`[SerializeField]`에 써넣기 | 기존 인스펙터 워크플로 유지 | 에디터 Play 중 SO를 mutate하면 Play 종료 후에도 안 되돌아갈 위험(`unity63-architecture` 가이드 경고) |
| 각 값의 프로퍼티 getter가 `GameBalance`를 직접 읽음, 원래 `[SerializeField]` 수치 필드는 제거 | SO를 절대 mutate 안 함, 호출부(`RangedWeapon`/`PlayerCombat` 등)는 무수정으로 JSON을 따라감 | `WeaponData.Damage`를 abstract로 바꾸는 등 프로퍼티 구조를 손봐야 함, 인스펙터에서 값이 안 보임 |

### Claude 제안
- 포맷은 JSON + `JsonUtility`(내장 파서 + `_guide` 주석 트릭), 위치는
  `StreamingAssets/Balance/`(빌드 포함 읽기 전용 시드 데이터의 공식 권장
  위치, `unity63-architecture` 스킬 확인). 주입은 프로퍼티 getter 방식 +
  원본 `[SerializeField]` 제거(인스펙터에 남기면 "고쳐도 반영 안 됨" 혼란).
  파싱 실패 시 내장 기본값 폴백.

### 선택한 방향
- 그대로 채택. 신규 `GameBalance`(정적 로더, lazy load +
  `[RuntimeInitializeOnLoadMethod]`로 Play/빌드마다 캐시 강제 리셋),
  `PlayerBaseStatsData`/`PassiveItemBalanceData` DTO,
  `PlayerBaseStats.json`/`PassiveItems.json`. `ProjectileRange`는 JSON에
  "유닛" 단위로 적고 내부에서 초 단위로 자동 환산.

### 선택 이유
- JSON은 이 프로젝트가 유일하게 추가 의존성 없이 타입 안전하게 다룰 수 있는
  포맷이고, `_guide` 필드 트릭으로 "포맷 안 깨는 주석" 요구도 만족. getter
  주입은 SO mutate 금지 가이드를 지키면서 호출부를 하나도 안 건드려도 되는
  유일한 방법이었음.
- 캐시 리셋 훅: lazy 캐시만 두면 이 자동화 환경에서 Stop→Play 후에도 정적
  필드가 리셋 안 되는 경우가 실측돼(= "JSON 고치고 Play만 다시" 약속이
  깨짐), 도메인 리로드 여부와 무관하게 항상 도는
  `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`로 방어.

### 관련 파일
- `Assets/Scripts/Balance/GameBalance.cs`, `PlayerBaseStatsData.cs`,
  `PassiveItemBalanceData.cs`
- `Assets/StreamingAssets/Balance/PlayerBaseStats.json`, `PassiveItems.json`
- `Assets/Scripts/Combat/WeaponData.cs`, `RangedWeaponData.cs`,
  `MeleeWeaponData.cs`, `PassiveItemData.cs`, `Projectile.cs`
- `Assets/Scripts/Player/PlayerMovement.cs`

---

## 2026-08-31 — 패시브 스탯 "공격 속도": 배율(퍼센트) vs 고정값 덧셈

### 배경 / 문제
- 패시브 아이템 4종(공격력/이동속도/사거리/공격속도) 중 공격속도만 배율
  방식(아이템 1개당 +10%, `AttackSpeedMultiplier` 곱연산)으로 구현돼 있었고,
  나머지 3개는 전부 고정값 덧셈이었음. 사용자가 관리 측면에서 이 불일치가
  맞는지 검토를 요청.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A. 배율 방식 유지 | 변경 작업 없음 | 나머지 3개 스탯과 스택 방식이 달라, Pip 바 채움 계산식도 공격속도만 별도 공식(`AttackSpeedRatio`/`AttackSpeedMaxRate`)을 따로 둬야 했음(배율은 base 대비 비율로 표현하면 base가 약분되어 사라지므로, base-상대 공식이 성립하지 않음) — 관리 포인트가 스탯마다 다름 |
| B. 고정값 덧셈으로 전환(초당 공격 횟수를 그대로 더함) | 나머지 3개와 완전히 동일한 `AdditiveRatio` 공식 하나로 통일 가능, 관리 포인트 단일화 | 기존 "0.1=10%" 감각의 수치를 "초당 0.15회"라는 다른 단위로 재계산해서 재설계해야 함(기존 배율 기준으로 튜닝했던 `capCount` 등도 함께 재계산 필요) |

### Claude 제안
- B안 추천. 나머지 3개 스탯이 전부 "base + 고정값 보너스"라는 동일한 모델을
  따르는데 공격속도만 배율이라, Pip 바 공식이 스탯별로 갈라지는 근본 원인이
  바로 이 불일치라는 점을 근거로 제시.

### 선택한 방향
- B안 채택. `PlayerPassiveStats.AttackSpeedMultiplier`(배율, 1.0 시작) →
  `AttackSpeedBonus`(고정값, 0 시작)로 교체, 아이템 1개당 초당 0.15회 증가로
  확정. `PlayerStatsPanelUI`의 전용 특수 공식 삭제, 나머지 3개와 동일한
  `AdditiveRatio` 하나로 통일. `capCount`를 8→24로 재계산(0개 아이템 상태의
  Pip 바가 이전과 동일하게 3/10이 나오도록 새 공식 기준 역산).

### 선택 이유
- 4개 스탯이 전부 같은 "base + 고정값 보너스" 모델을 공유하면 코드(공식
  하나, 특수 케이스 없음)와 데이터(JSON의 `amount` 단위 설명) 양쪽에서 관리
  포인트가 하나로 줄어듦 — 배율을 유지하려면 계속 스탯 하나만 예외로 다뤄야
  했음.

### 관련 파일
- `Assets/Scripts/Player/PlayerPassiveStats.cs`
- `Assets/Scripts/Combat/RangedWeapon.cs`
- `Assets/Scripts/UI/PlayerStatsPanelUI.cs`
- `Assets/Scripts/Balance/GameBalance.cs`
- `Assets/StreamingAssets/Balance/PassiveItems.json`

---

## 2026-08-24 — Stage 2 #6/#7 구조: 적 풀, 스테이지 데이터 공급자, 적-풀 연결 방식

### 배경 / 문제
- Stage 2 남은 두 항목(#6 여러 종류의 일반 Enemy, #7 다중 방 콘텐츠 확장) 구현.
  세 가지 서로 연결된 구조 결정이 필요했음: (a) 여러 종류의 적을 스폰하는 풀
  구조, (b) 스테이지별 적 구성 데이터를 어디서/어떻게 공급할지, (c) 데이터가
  실제 씬의 적 프리팹/풀을 어떻게 가리킬지.

### 검토한 대안 (a) 적 풀 구조
| 대안 | 장점 | 단점 |
|---|---|---|
| A. `GameObjectPool` 인스턴스를 종류별로 여러 개 두고 신규 `EnemySpawner`가 풀 선택만 담당 | 기존 `GameObjectPool`(`UnityEngine.Pool.ObjectPool<T>` 래퍼, 이미 Projectile/Enemy/Boss/Grenade 4곳에서 검증됨) 무수정 재사용 | 적 종류가 늘수록 씬 오브젝트 개수 증가 |
| B. `GameObjectPool` 자체를 확장해 여러 프리팹을 관리하도록 리팩터 | 씬 오브젝트 수 절약 | 이미 4곳에서 공용으로 쓰는 컴포넌트를 건드려야 해 회귀 리스크 |
| C. 전용 스포너가 풀링 로직을 처음부터 재구현 | 유연 | 사용자가 명시한 "여러 오브젝트를 생성하는 타입은 전부 ObjectPool 기반이어야 한다"는 원칙과 충돌, 기존 코드와 중복 |

### 검토한 대안 (b) 스테이지 데이터 공급 방식
| 대안 | 장점 | 단점 |
|---|---|---|
| A. `StageDataAsset`(ScriptableObject)을 `StageManager`/`EnemySpawner`가 직접 참조 | 구현 간단 | 사용자가 이미 갖고 있던 로드맵(중반: 외부 파일 로드, 최종: 서버 데이터)을 반영하려면 그때 가서 `StageManager`/`EnemySpawner` 코드까지 다시 손대야 함 |
| B. `IStageDataProvider` 인터페이스 뒤에 `StageDataAsset` 구현체(`ScriptableObjectStageDataProvider`)를 두고, 나중에 파일/서버 구현체로 교체 | `StageManager`/`EnemySpawner`는 데이터 출처가 바뀌어도 무수정 | 지금 당장은 인터페이스 + 얇은 어댑터 클래스가 추가로 필요 |

### 검토한 대안 (c) 적 종류-풀 연결 방식
| 대안 | 장점 | 단점 |
|---|---|---|
| A. `StageData`가 씬의 `GameObjectPool` 컴포넌트를 직접 참조 | 추가 코드 없음 | 서버/JSON 데이터는 애초에 Unity 씬 오브젝트를 참조할 수 없어, (b)에서 만든 추상화가 무의미해짐 — 마이그레이션 시점에 다시 설계해야 함 |
| B. `EnemyVariantType` enum으로 적 종류를 표현하고, `EnemySpawner`가 enum→풀 매핑을 인스펙터에서 보유 | 서버/JSON에서도 문자열/enum 값 하나로 표현 가능, 데이터와 씬 참조가 분리됨 | enum 값 추가 시 `EnemySpawner`의 매핑도 같이 갱신해야 함(사소) |

### Claude 제안
- (a) A안: 사용자가 "`GameObjectPool`이 Unity 기본 제공 풀링 클래스인지" 먼저
  질문해서, `UnityEngine.Pool.ObjectPool<T>` 래퍼임을 확인시키고 `EnemySpawner`는
  풀 선택만 담당하는 얇은 오케스트레이터로 제안.
- (b) B안: 사용자가 "중반엔 외부 파일, 최종엔 서버 데이터"라는 구체적
  마이그레이션 계획을 이미 갖고 있다고 밝혀, 이 경우의 인터페이스 추상화는
  추측이 아니라 실제 요구에서 나온 것이라 판단해 제안. 기존
  `Health`/`HealthComponent`(순수 C# 로직 + Unity 어댑터 분리) 패턴과 같은
  성격이라는 점도 근거로 제시.
- (c) B안: (b)의 추상화 목적(데이터가 Unity 전용 참조에 묶이지 않게)과 같은
  맥락이라 세트로 제안, 사용자도 enum 방식으로 확정.

### 선택한 방향
- (a) A안, (b) B안, (c) B안 전부 그대로 채택.
- 신규: `EnemyVariantType`, `StageEnemyEntry`, `StageData`,
  `IStageDataProvider`, `StageDataAsset`, `ScriptableObjectStageDataProvider`,
  `StageManager`, `EnemySpawner`.

### 선택 이유
- (a): 기존 컴포넌트 재사용 원칙 유지 + 사용자가 사전에 명시한 요구사항에
  정확히 부합.
- (b): 인터페이스 1개 + 얇은 어댑터 1개 정도의 낮은 비용으로, 실제로 예정된
  마이그레이션(파일→서버) 시점에 `StageManager`/`EnemySpawner`를 다시 건드리지
  않아도 되는 이득이 훨씬 큼 — 추측성 추상화가 아니라 확정된 로드맵에 근거.
- (c): (b)가 의미 있으려면 데이터 자체가 Unity 씬 참조를 전혀 포함하지 않아야
  함 — enum으로 결정.

### 관련 파일
- `Assets/Scripts/Stage/*.cs` (신규 전체)
- `Assets/Scripts/Enemy/EnemySpawner.cs`
- `Assets/Scripts/Room/RoomFlowManager.cs`, `RoomManager.cs`

---

## 2026-08-24 — UIManager 구조: 상속 없는 단일 클래스 vs base+파생(StageUIManager)

### 배경 / 문제
- 적 체력 바 + 스테이지 텍스트 + 기존 UI 5종(`HealthBarUI`/`SkillCooldownUI`/
  `UpgradeSelectionUI`/`EquipSelectionUI`/`CharacterSelectUI`/`ClearOverlayUI`)을
  묶어 관리할 `UIManager` 도입 논의. 사용자가 처음 제안한 방향은 게임 전체
  UI를 관장하는 base `UIManager`와, 이 씬(스테이지)에서만 쓰는 파생
  `StageUIManager`(is-a) — 스테이지 진입 전 로딩 시점에 `StageUIManager`를
  셋업하는 흐름까지 염두에 둠. "최대한 현업 구조로 연습해보고 싶다"는 목적을
  명시.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A. 상속 없는 단일 flat `UIManager` | 이 프로젝트에 지금까지 MonoBehaviour 상속 계층이 하나도 없는 기존 스타일(전부 합성+인터페이스)과 100% 일관, 지금 씬이 하나뿐이라 당장 필요한 만큼만 구현됨 | 씬이 여러 개로 늘어나면 그때 재설계 필요 |
| B. base `UIManager` + 씬 전용 파생 `StageUIManager`(is-a), 로딩 시점 셋업 훅 포함 | "현업 구조" 연습 목적에 부합, 향후 로딩 흐름을 겨냥한 API 모양을 미리 잡아둠 | 지금 씬이 하나뿐이라 base에 넣을 내용을 실제 중복에서 뽑아낸 게 아니라 추측으로 채워야 함. 로딩 씬 자체가 프로젝트에 아직 없어(Phase 5 미착수) 셋업 훅을 걸 실제 지점이 없음 — 지금 만들면 가짜 훅이 됨 |

### Claude 제안
- A안을 우선 제안하되, 사용자의 "현업 구조를 연습해보고 싶다"는 동기 자체는
  이 프로젝트의 포트폴리오 목적(트레이드오프를 이해하고 설명할 수 있는가)에
  정당한 이유라고 인정. 다만 base/파생 분리는 지금 실제 중복 없이 추측으로
  설계되는 점, 로딩 시점 셋업은 그 훅을 걸 로딩 씬 자체가 없다는 점을 각각
  구체적으로 짚어 재검토를 요청.

### 선택한 방향
- 사용자가 직접 재검토 후 A안(상속 없는 단일 `UIManager`)으로 최종 결정,
  로딩 시점 셋업도 완전히 보류하기로 함. 정적 `Instance` 싱글톤 접근점도
  도입하지 않음(지금 당장 멀리서 접근할 필요가 있는 곳이 없음).

### 선택 이유
- 사용자 본인 판단: "먼저 구조를 키우면 나중에 바꾸기 쉽지 않을 것 같다" —
  규모가 실제로 커질 때(두 번째 구체적인 UI 매니저 사례, 예: 별도 메인 메뉴
  씬이 실제로 생길 때) 그때의 진짜 중복을 보고 구조를 키우는 쪽으로 합의.
  로딩 시점 셋업도 같은 논리로, Phase 5(Addressables/로딩 씬)가 실제로 도입될
  때 재검토하기로 함.

### 관련 파일
- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/Room/RoomFlowManager.cs`, `Assets/Scripts/Player/CharacterSelectController.cs`

---

## 2026-08-24 — 적 체력 바: 프리팹 자체 소속 vs UIManager 중앙 관리

### 배경 / 문제
- 적 캐릭터 머리 위에 따라다니는 체력 바 요구사항(사망 시 자동으로 안 보여야
  함). `UIManager` 도입 논의와 맞물려, 이것도 `UIManager`가 관리할지 결정
  필요.

### 검토한 대안
| 대안 | 장점 | 단점 |
|---|---|---|
| A. Enemy 프리팹의 자식으로 World Space `Canvas` 배치, 기존 `HealthBarUI` 재사용 + 신규 `BillboardUI`(카메라 향해 회전) | 위치 추적과 사망 시 숨김이 부모-자식 관계만으로 자동 해결(적 GameObject가 풀에 반환되며 비활성화될 때 자식인 체력 바도 같이 꺼짐) — 별도 코드 불필요. 기존 `HealthBarUI`가 이미 `Health.Changed` 이벤트만으로 동작해 World/Screen Space 무관하게 그대로 재사용 가능 | 프리팹 3종(`Enemy_Basic`/`Enemy_Ranged`/`Boss_Basic`) 각각에 계층 추가 필요 |
| B. `UIManager`가 스폰된 적 목록을 추적하며 매 프레임 `WorldToScreenPoint`로 Screen Space 체력 바 위치를 갱신 | 체력 바를 한 Canvas에서 중앙 관리 | 매 프레임 N마리를 추적하는 코드가 별도로 필요, 사망 시 숨김도 목록에서 수동으로 제거해야 함, 기존 이벤트 기반 패턴(§4 "이벤트 기반 결합도 관리")과 다른 방식이 되어 일관성 저하 |

### Claude 제안
- A안. 기존 `HealthBarUI`가 이미 `Health.Changed` 이벤트만으로 동작한다는
  점, 그리고 이 프로젝트가 이미 채택한 "이벤트 기반 결합도 관리" 원칙과 맞는
  방향이라는 점을 근거로 제시.

### 선택한 방향
- 그대로 채택.

### 선택 이유
- `UIManager`가 매 프레임 N마리를 추적하는 책임을 지지 않아도 됨. 적 사망
  처리(`PooledObject.ReturnToPool()`)가 이미 부모 GameObject를 비활성화하므로
  체력 바 숨김이 "우연히 되는 것"이 아니라 "구조적으로 보장되는 것"이 됨.

### 관련 파일
- `Assets/Scripts/UI/BillboardUI.cs`
- `Assets/Prefabs/Enemy_Basic.prefab`, `Enemy_Ranged.prefab`, `Boss_Basic.prefab`

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
