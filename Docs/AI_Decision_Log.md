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
