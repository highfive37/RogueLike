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

## 2026-09-10

### 진행 내용

**트랙 #2 Phase F — 시드 표시(일시정지/게임오버/층클리어) + 타이틀 시드 입력칸(같은 시드 재생)**
- **시드 5자리 제한으로 전환**: `RunConfig`에 `MaxSeedDigits=5`/`MaxSeedValue=99999` 신규,
  `DisplaySeed`(`"D5"` 포맷, 253→"00253") 프로퍼티 추가. `NewRandom`의 범위를 기존 [1, int.MaxValue)
  (~21억)에서 [0, 99999]로 축소 — 화면에 뜨는 시드를 타이틀의 5자리 입력칸에 그대로 되입력할 수
  있어야 "같은 시드 재생"이 성립하므로, 자동 생성 시드도 입력 가능 범위로 맞춤(유저 확정 사항).
  `DebugOverlayController`(F1)도 `DisplaySeed`로 통일해 표시 포맷 불일치 방지.
- **버튼 재사용 발견**: 씬 조사 중 `GameOverUI`의 재시작/타이틀 버튼이 이미 자체 핸들러가 아니라
  공유 `PauseMenuButtons` 컴포넌트(pause 화면과 동일 컴포넌트, `ButtonsGroup`에 부착)로 배선돼
  있었음을 확인. `FloorCompleteUI`만 유일하게 자체 `HandleRetryClicked`/`HandleToTitleClicked`로
  중복 배선돼 있던 상태 → "교체" 요청을 이 기존 패턴에 맞추는 것으로 해석해 `FloorCompleteUI`의
  중복 핸들러 제거, `ClearOverlayPanel`에 `PauseMenuButtons` 신규 부착(RetryButton/ToTitleButton
  배선). 마지막 층에서만 보이는 기존 `isLastFloor` 표시 조건은 그대로 유지(요청 범위는 배선
  방식 통일이지 표시 조건 변경이 아니라고 판단).
- **`PauseMenuButtons`에 선택적 `seedText` 필드 추가**: 이미 pause/GameOverUI/FloorCompleteUI
  3곳 전부에 이 컴포넌트가 붙어있어(위 재배선 포함), 시드 표시 로직을 3개 화면 클래스에 각각
  추가하는 대신 이 컴포넌트 한 곳에 통합 — Awake()에서 1회 설정(런 중 시드 불변, `DebugOverlayController`
  SEED 표시와 동일 근거). 3개 화면 각각에 `SeedText`(좌상단 anchor, cyan, `SEED: 00253` 포맷)
  신규 생성 후 각 컴포넌트 인스턴스에 배선.
- **타이틀 시드 입력칸**: `RunConfig.TryParseSeedOverride(text, out seed)` 신규(빈값/공백/5자리
  초과/음수/파싱실패 → false, 성공 시 값 그대로 — "253"과 "00253"은 동일 값이므로 별도 변환 불필요).
  `GameFlow.StartRun(archetype, seedOverride=null)` 오버로드, override 있으면 `NewRandom` 대신
  그 값으로 `RunConfig` 생성. `TitleScreenController`에 `seedInputField` 필드 추가, Start 클릭
  시점에 파싱해 캐릭터 선택 콜백에 캡처. 씬: `GameObject/UI (Canvas)/Legacy/Input Field` 메뉴로
  표준 InputField 생성 후 TitleGroup으로 재배치, `contentType=IntegerNumber`/`characterLimit=5`,
  placeholder "시드 (선택, 최대 5자리)". 파싱 위치 설계(대안 비교)는 `AI_Decision_Log.md`
  2026-09-10 참고.

**유저 피드백 반영 1 — FloorCompleteUI 버튼 "교체"의 실제 의미 정정 + 좌상단 배치**
- 1차 구현 때는 "일시정지 UI 버튼으로 교체"를 배선 재사용(공유 `PauseMenuButtons` 컴포넌트)으로
  해석했으나, 유저 확인 결과 실제 의도는 **일시정지 UI 버튼과 동일한 이미지(스프라이트)** 사용이었음.
  `RetryButton`/`ToTitleButton`의 `Image.sprite`를 일시정지·GameOverUI와 동일한
  `btn_restart_normal/over/pressed.png`·`btn_return_title_normal/over/pressed.png`로 교체,
  `Button.transition=SpriteSwap` + `spriteState` 배선. 아트에 라벨이 이미 그려져 있어(GameOverUI
  버튼들과 동일하게 별도 Text 라벨 없음) 기존 "다시하기"/"타이틀로" Text 자식은 삭제.
- 3개 화면 시드 텍스트를 전부 좌상단(anchor/pivot 0,1)에 배치.

**유저 피드백 반영 2 — 버튼 시각 버그 2건 발견/수정 (유저 실플레이 제보)**
- **어두움 + hover/pressed 안 보임**: 원인 하나로 둘 다 설명됨 — `RetryButton`/`ToTitleButton`의
  `Image.color`가 이전 흰 배경 사각형 시절의 잔재 틴트(0.2, 0.2, 0.2, 1)로 남아있었음. `SpriteSwap`은
  스프라이트만 바꾸고 `Image.color`는 안 건드리므로 normal/hover/pressed 전부 20% 밝기로 눌려
  보였던 것 → `Color.white`로 리셋.
- **크기**: 220×60(레거시 값 그대로 방치) → 일시정지 UI와 동일한 300×100 + 좌표(0,-106)/(0,-209)로
  통일.

**유저 피드백 반영 3 — 시드 "?" 표시 버그 근본 원인 + 수정 (유저 실플레이 제보)**
- 원인: `PauseMenuButtons`가 `Awake()`에서 `GameFlow.CurrentRun`을 읽는데, 이 값은
  `StageManager.Awake()`(다른 루트 GameObject)가 `EnsureRun()`을 호출해야 채워짐(타이틀 거치지
  않고 씬 직접 진입 시). `PauseMenuButtons`가 붙은 `ClearOverlayPanel`/`ButtonsGroup`은 씬 로드
  시점부터 항상 active라 그 `Awake()`도 씬 로드 시점에 함께 도는데, **Unity는 서로 다른 루트
  GameObject 간 `Awake()` 상대 순서를 보장하지 않음** — `StageManager.Awake()`보다 먼저 돌면
  `CurrentRun`이 null인 채로 "SEED: ?"가 1회성 설정이라 그대로 고정돼버림.
- 수정: 시드 텍스트 설정 로직만 `Awake()` → `Start()`로 이동(버튼 리스너 등록은 순서 의존이 없어
  `Awake()`에 그대로 둠). Unity는 "씬의 모든 `Awake()`가 어떤 `Start()`보다도 먼저 끝난다"는 건
  보장하므로 확실한 수정.

**기타**
- `Assets/Prefabs/Player.prefab`의 `CharacterSelectController.fallbackArchetype`:
  `MeleeArchetype` → `RangedArchetype` (타이틀 거치지 않고 에디터에서 인게임 씬 바로 실행 시
  기본 캐릭터를 원거리로 변경, 유저 요청). 씬에 별도 오버라이드 없어 프리팹 수정만으로 적용.
- **F5/F6 디버그 키 신규**: `RoomFlowManager.DebugTriggerGameOver()`/`DebugTriggerLastFloorComplete()`
  추가 — F5는 실제 `HandlePlayerDied()`를, F6은 `isLastFloor=true`로 강제한 것 말고는
  `HandleFloorCompleted()`와 동일 로직을 그대로 호출(목업 아님, 실제 코드 경로 재사용이라 화면이
  실제 플레이 결과와 100% 동일 + 드리프트 없음). `DebugOverlayActions.inputactions`에 액션 2개
  추가, 기존 F1/F2와 동일한 always-enabled 액션맵 패턴(`DebugOverlayController`가 구독).
  `PauseState.IsPaused`면 무시해 중복 트리거 방지. 죽거나 마지막 층까지 가지 않고도 게임오버/
  층클리어 UI를 바로 확인 가능 — 위 두 버그 수정 검증에 바로 사용됨.

### 테스트 결과
- [x] 전체 재컴파일(테스트 어셈블리 포함) 콘솔 에러/경고 0 — 이후 각 수정 단계(버튼 비주얼,
      Awake→Start, F5/F6)마다 재확인.
- [x] `RunCommand` 배선 검증: 3개 화면의 `PauseMenuButtons.seedText`/`restartRunButton`/
      `returnToTitleButton` 전부 non-null, `FloorCompleteUI.retryButton`/`toTitleButton`(표시
      토글용) 유지, 버튼 sprite/size/color 값 전부 일시정지 UI와 일치 확인.
- [x] `RunConfig` 동작 검증(`RunCommand`): `DisplaySeed` 253→"00253", `TryParseSeedOverride("253")`
      =`TryParseSeedOverride("00253")`=253, 빈 문자열/6자리(999999) 거부, `NewRandom()` 결과가
      항상 `MaxSeedValue` 이하.
- [x] 타이틀 씬 재로드 후 `seedInputField` 배선 유지, `contentType`/`characterLimit` 값 확인.
- [x] **유저 실플레이 확인 (F5/F6 디버그 키 활용)**: 일시정지/게임오버/층클리어 3화면 좌상단
      시드 정상 표시(더 이상 "?" 아님), FloorCompleteUI 재시작/타이틀 버튼이 일시정지 UI와 동일한
      크기·이미지·hover/pressed 표시로 정상 동작 — 전부 확인 완료.
- [ ] **여전히 미확인**: 타이틀 화면 시드 입력칸에 값을 실제로 입력 → 그 시드로 던전이 재현되는지,
      빈 칸으로 시작 시 기존처럼 랜덤 시드인지 — 이번 세션에서 별도 언급/확인 없었음.

### 이슈/메모
- 시드 공간을 21억 → 10만으로 줄인 것은 유저 명시 요청이지만, 절차적 생성의 실질 다양성에
  영향 없음(레이아웃 다양성은 그래프 성장 알고리즘의 확률적 분기에서 나오지, 시드 공간 크기
  자체에서 나오지 않음 — 10만 종도 사실상 무한히 다른 던전을 만듦).
- **Awake() 순서 레이스 주의**: 이번 세션에서 실제로 걸린 버그 클래스라 일반화해서 남김 — 서로
  다른 루트 GameObject의 `Awake()`가 서로의 상태를 읽는 코드는 Unity가 그 순서를 보장하지 않아
  레이스가 생길 수 있음. 다른 오브젝트의 `Awake()`가 채운 상태를 읽어야 하면 `Start()`에서
  읽을 것(씬 전체 Awake가 모든 Start보다 먼저 끝나는 건 보장됨). `PauseMenuButtons`/`GameFlow`
  외에도 이런 패턴이 또 있을 수 있어 새 static/싱글톤 상태를 만들 때마다 염두에 둘 것.

### 다음 작업
- 타이틀 시드 입력 → 던전 재현 실플레이 확인 (위 미확인 항목)
- 위 항목 통과 시 트랙 #2(절차적 생성) 전체 종료 → §6 다음 순번인 적 경로탐색(A*) + 방 안
  장애물로 이동

---

## 2026-09-09

### 진행 내용

**적 방 이탈 → 진행 불가 버그 수정 (블로킹, 유저 제보) — 방 내부 클램프 + 안전망**
- 증상: 방 진입 시 간혹 스폰된 적이 방 밖에 있거나 밖으로 이동 → 그 적을 못 죽여 방 클리어
  불가 → 진행 정지.
- 원인 (3중):
  1. 적 이동이 전부 생짜 `Rigidbody.MovePosition`(chase/patrol/retreat/knockback/lunge/sway
     6곳), 벽 클램프·스윕 전무. 적을 방에 가두는 건 벽 큐브 콜라이더 충돌뿐인데 적 Rigidbody가
     Discrete(`m_CollisionDetection: 0`, 스윕 없음), 벽 두께 1.
  2. 2026-09-08 넉백: `EnemyStunnedState`가 `MovePosition`으로 절대 목표점(`시작 + 방향×1.5`)을
     ease-out 러프 — 첫 FixedUpdate 스텝 ≈0.46유닛(캡슐 반지름 0.5 육박). 넉백 방향이 벽 쪽이면
     (플레이어와 벽 사이 적을 때리면 흔함) 1~2스텝에 캡슐 중심이 벽 중간면을 넘어가 솔버가 바깥으로
     배출, `MovePosition`이 매 스텝 절대점 재조준이라 솔버 반력 무효(2026-09-04 플레이어 벽 관통과
     동일 계열). 2026-09-08부터 모든 원거리 변형이 `RangedKitingBehavior`로 후퇴까지 해 빈도 증가.
  3. `EnemyStateMachine.CanSeePlayer`가 순수 거리(LOS 없음) → 벽 밖 적도 플레이어를 "봄" →
     Chase/Attack 유지하며 벽 관통 사격, `leashRange 9`(방 40) 안이라 Patrol 복귀도 안 함 → 영구 블록.
- 수정 (A+B, 대안 비교 `AI_Decision_Log.md` 2026-09-09):
  - **A. 방 내부 AABB 클램프**: `RoomController`에 `RoomSize` 노출 + `ClampToInterior(worldPos,
    bodyRadius)` / `IsInsideInterior(worldPos, bodyRadius, slack)`. 내부 한계 =
    `RoomSize/2 - WallThickness/2 - bodyRadius - 0.1`. 문 간격 무시(적은 방 간 이동 안 함).
    `EnemyStateMachine`에 `SetRoom` + 단일 이동 초크포인트 `MoveTo(worldPos)` 추가 — 6개 이동
    호출부를 `Body.MovePosition` → `_machine.MoveTo`로(회전 호출은 그대로). `EnemySpawner.SpawnRoom`
    시그니처 `(RoomData, RoomController)`로, 스폰 좌표도 같은 클램프 + `SetRoom` 주입 + 말미
    `Physics.SyncTransforms()`(autoSyncTransforms off이므로).
  - **B. 안전망**: `EnemyStateMachine.FixedUpdate`에서 `EnforceRoomContainment()` — `_room`
    내부에서 slack 1유닛을 벗어나면(솔버 depenetration·플레이어 밀림 흡수용, 벽 두께 1보다 작음)
    스폰 지점으로 `Body.position` 스냅 + 속도 0 + `SyncTransforms` + Idle. 벽에 박힌 authoring
    좌표나 `MoveTo`를 빠뜨린 미래 이동 경로 대비 보험.
  - `EnemyMovementFlavor` 잘못된 주석("callers pass through WallClamp" — 적은 WallClamp 쓴 적
    없음) → `MoveTo` 언급으로 수정.
- 미채택: C(적 Rigidbody Continuous + 넉백 스텝 상한). A가 컨테인먼트를 결정적으로 보장하므로
  불필요 — 여유 시 defense-in-depth로 추가 가능.

**씬 전환 페이드 (로드맵 §6 #6 잔여) — 영속 페이드 오버레이**
- 문제: `GameFlow.LoadScene()`이 `SceneManager.LoadScene`을 동기 호출 → 타이틀↔인게임,
  인게임 재시작 전환이 전부 하드 컷(이전 씬이 한 프레임 얼었다가 새 씬이 툭 뜸). 전환 지점 4곳
  (`StartRun`/`RetryRun`/`ReturnToTitle`, 그리고 이 셋을 거치는 `FloorCompleteUI`·`GameOverUI`·
  `PauseMenuButtons`의 버튼)이 전부 `GameFlow` 세 메서드를 지나감.
- 설계: A1(영속 페이드 캔버스) 채택 — 대안 비교는 `AI_Decision_Log.md` 2026-09-09.
- 신규 `Assets/Scripts/Core/SceneTransitionController.cs`:
  - 프로젝트 유일의 `DontDestroyOnLoad` 오브젝트. 전체화면 검은 `CanvasGroup` 오버레이.
  - **프리팹/Resources 없이 전부 코드로 생성** — 칠할 게 없는 단색 페이드라 authoring 대상이
    아니고, 코드 생성 오브젝트는 씬에서 빠뜨리거나 빌드에서 누락될 수 없음. `EnsureExists()`가
    `new GameObject("SceneTransition", typeof(RectTransform), Canvas, GraphicRaycaster,
    CanvasGroup, Image, SceneTransitionController)`로 1회 생성, 이후 재사용. 게임을 타이틀로
    바로 부팅할 때는 페이드 인이 없음(맞는 동작 — 페이드해 올라올 대상이 없음).
  - `FadeAndLoad(sceneName, onBeforeActivate)`: 검게 페이드(unscaled 0.35s) → `onBeforeActivate`
    실행 → `LoadSceneAsync(Single)` 완료 대기 → 새 씬 초기화 1프레임 대기 → 페이드 인(0.35s).
    전환 중 재호출은 `_isTransitioning` 가드로 무시(메뉴 버튼 더블클릭 방지).
  - `Canvas.sortingOrder = short.MaxValue`로 모든 인게임 캔버스 위, `GraphicRaycaster` +
    `blocksRaycasts`로 페이드 중 클릭 차단.
- `GameFlow.LoadScene()` 재배선: `SceneManager.LoadScene(name)` →
  `SceneTransitionController.EnsureExists().FadeAndLoad(name, PauseState.ForceResume)`.
  `PauseState.ForceResume`를 `onBeforeActivate`로 넘겨, 페이드 아웃 동안엔 이전 씬이 pause 유지
  (fade는 unscaled)되고 새 씬 진입 직전에만 timeScale 복구 — 씬 로드는 엔진 전역 timeScale을
  리셋하지 않으므로 어딘가에서 풀어줘야 함. `GameFlow`는 정적 유지(코루틴은 영속 오브젝트가 소유),
  미사용이 된 `using UnityEngine.SceneManagement;` 제거. 씬 배선·인스펙터 변경 없음.

**후속 수정(유저 실플레이 피드백)**
- 페이드 인이 안 보임 → Prototype 로드 직후 `StageManager.BuildFloor()` 동기 생성으로 프레임이
  수백 ms 튀고, 페이드 인 코루틴 첫 스텝이 그 delta를 먹어 1프레임 만에 끝남. 수정: `Fade` 루프의
  프레임당 진행량을 33ms로 캡(스파이크가 있어도 최소 ~10프레임 렌더) + 로드 후 정착 프레임 1→2.
  유저 확인: 페이드 인 정상.
- 캐릭터 선택 → 인게임 페이드 아웃이 잘 안 보임 → 원인 2가지. (1) `CharacterSelectUI.HandleChosen`이
  `_onChosen`(씬 전환 시작) 호출 전에 `panel.SetActive(false)`로 선택 UI를 즉시 꺼서, 페이드 아웃이
  이미 비어버린 화면 위에서 재생됨 → 그 `SetActive(false)` 제거(오버레이가 곧바로 입력을 막고,
  Title 씬 언로드가 패널을 파괴하므로 안전). (2) 밝은 화면 → 검정 페이드는 검정 → 씬 페이드인보다
  체감이 약함 → `FadeOutDuration` 0.35→0.5s(페이드 인은 0.35s 유지, 유저가 이미 만족).

**신규 적 변형 2종을 1-2·1-3층에도 배치 (유저 요청)**
- 2026-09-08엔 `Floor1.json`에만 `ErraticMelee`/`BurstRanged`가 있었음. `Floor2.json`(전투방 7개)·
  `Floor3.json`(전투방 10개)의 모든 전투방에 최소 1종씩 고루 배치 — 기존 `Melee`→`ErraticMelee`,
  `Ranged`→`BurstRanged`로 `variant` 문자열만 교체(스폰 좌표·방당 총 적 수·연결·보스방 전부 불변).
  큰 방은 2~3종. `Floor1.json`의 `_fieldGuide` variant 설명도 5종 전부 나열하도록 갱신(기존
  "Melee/Ranged/Boss"만 적혀 있어 실제와 불일치했음).
- 배치 결과(실제 `GameFloors` 로더로 검증): Floor2 Erratic 7 / Burst 7 / Melee 13 / Ranged 10,
  전투방 7/7 신규 변형 포함. Floor3 Erratic 11 / Burst 10 / Melee 18 / Ranged 15, 전투방
  10/10 포함. 3층 모두 `corrupted=False`, 콘솔 파싱 에러/경고 0.

**포트폴리오 트랙 #1 착수 — 자동화 테스트 스위트 Phase 0 (asmdef 도입, A1)**
- 배경: UTF 테스트는 asmdef 어셈블리에 있어야 하고 asmdef는 `Assembly-CSharp`를 참조 못 함 →
  테스트 대상 게임 코드가 asmdef로 들어와야 함. 현 프로젝트 asmdef 0개. A1(전체를 단일 asmdef로
  래핑) 채택 — 대안 비교 `AI_Decision_Log.md` 2026-09-09 (2).
- 선행 조사: `Assets/Scripts/` 116개 `.cs` 전부 대상. 쓰는 패키지 네임스페이스는 `UnityEngine.InputSystem`
  (`InputAction`/`InputActionAsset`/`Keyboard`/`Mouse`/`Pointer` — 전부 `Unity.InputSystem` 본체) +
  `UnityEngine.UI`/`EventSystems`(`UnityEngine.UI`) 뿐. `UnityEditor` 사용처 0 → Editor asmdef 불필요.
  `Assets/TutorialInfo/`의 템플릿 잔재 스크립트 2개는 `Assembly-CSharp`에 남되 게임 코드가 참조
  안 하므로 무해(별도 정리 대상).
- 작업: `Assets/Scripts/RogueLike.asmdef` 신규 (name `RogueLike`, references `["Unity.InputSystem",
  "UnityEngine.UI"]`, `autoReferenced: true`). 스크립트 코드 변경 0.

**테스트 스위트 Phase 1 — 테스트 인프라 + 첫 EditMode 테스트**
- 테스트 어셈블리 2개: `Assets/Tests/EditMode/RogueLike.Tests.EditMode.asmdef`
  (`includePlatforms:[Editor]`, refs `RogueLike`/`UnityEngine.TestRunner`/`UnityEditor.TestRunner`,
  `precompiledReferences:["nunit.framework.dll"]`, `defineConstraints:["UNITY_INCLUDE_TESTS"]`),
  `Assets/Tests/PlayMode/RogueLike.Tests.PlayMode.asmdef`(Phase 3용, 지금 생성만).
- 테스트 가능하게 순수 로직 2개 추출 (동작 불변):
  - `KnockbackMath.Distance(resistance)` = `BaseKnockbackDistance(1.5) × (1 - Clamp01(resistance))`.
    `EnemyStateMachine.HandleDamageTaken` 인라인 + `BaseKnockbackDistance` const를 신규
    `Assets/Scripts/Enemy/KnockbackMath.cs`로.
  - `RoomBounds.InteriorHalfExtent / ClampLocal / IsInsideLocal` (전부 방-로컬 공간, 순수).
    `RoomController`의 `InteriorLimit`/`InteriorSkin`/클램프 수식 → 신규
    `Assets/Scripts/Room/RoomBounds.cs`로. `RoomController.ClampToInterior`/`IsInsideInterior`는
    world↔local 변환만 하고 이 순수 함수 호출.
- 첫 EditMode 테스트 19개:
  - `HealthTests` 7 — 데미지 감소/0 클램프/`Died` 1회만/무적 시 무시/0·음수 무시/`Changed` 인자/`ResetHealth`
  - `KnockbackMathTests` 5 — 저항 0/0.5/1/음수/1초과 클램프
  - `RoomBoundsTests` 7 — 한계 수식/작은 방 음수 방지/밖 점 클램프/안 점 불변/Y 불변/slack 밖 False/slack 밴드 True

**테스트 스위트 Phase 2 — 데이터 로더 (`GameFloors` / `GameBalance`)**
- `Assets/Scripts/AssemblyInfo.cs` 신규 — `[assembly: InternalsVisibleTo("RogueLike.Tests.EditMode"/".PlayMode")]`
  (public API 안 늘리고 internal 시크릿 테스트).
- `GameFloors` 리팩터(동작 불변):
  - `LoadFloor(path)` → 파일 읽기 / 파싱 분리. 신규 `internal ParseFloorJson(json, n, isLast)`,
    `internal LoadAllFloorsFrom(dir)` — 테스트가 실제 StreamingAssets 안 건드리게.
  - **신규 검증 추가(`ValidateGraph`)**: 모든 connection의 targetRoomId가 실존해야 하고, start
    방에서 BFS로 전 방에 도달해야 함. 어긋나면 기존 구조 결함과 동일 처리(→ 비상층 + `IsCorrupted`).
    이전엔 dangling/단절 그래프가 "정상"으로 로드돼 런 소프트락 → 이제 진입 시 시스템 팝업.
    실제 `Floor1/2/3.json`(방 6/10/13) 전부 `IsCorrupted=False` 유지 확인.
- `GameBalance` 리팩터(동작 불변): `LoadPlayerBaseStats` 파싱 단계를 `internal ParsePlayerBaseStats(json)`
  로 분리(null/공백/파싱실패 → 내장 기본값, throw·null 없음). 나머지 3개 로더(passive/drop table/
  active skills)도 같은 시크릿 필요 — Phase 2 후속으로 남김.
- 테스트 20개: `GameFloorsTests` 16(정상 로드/파일순서 우선/비-JSON·null·빈 방·start 없음·boss
  없음·startId 불일치·중복 id·잘못된 roomType·잘못된 variant·**dangling connection**·**단절 방**
  → 전부 emergency+`IsCorrupted`; 2파일 카운트/빈 폴더→emergency/갭 정지+경고),
  `GameBalanceTests` 4(정상 값/`null`·garbage·공백 → 기본값).

**테스트 스위트 Phase 4 — CI (내 몫: 워크플로 작성 / 유저 몫: 라이선스·시크릿)**
- 사전 확인: GameCI `unityci/editor` Docker 이미지에 **`6000.3.22f1` 존재 확인**(Docker Hub 태그
  조회 — ubuntu/windows 다수 타겟). 초기에 우려했던 "6000.3 이미지 부재" 리스크 해소.
- `.github/workflows/ci.yml` 신규: push/PR(main) + `workflow_dispatch` → `actions/checkout@v4` →
  `actions/cache@v4`로 `Library/` 캐시(콜드 임포트 ~15분 → 웜 ~2분) → `game-ci/unity-test-runner@v4`
  (`unityVersion: 6000.3.22f1`, `testMode: all`, `checkName`으로 커밋에 체크런) → 결과 아티팩트 업로드.
  `concurrency`로 같은 ref 중복 실행 취소.
- **활성화 방식 변경 반영**: `game-ci/unity-request-activation-file`는 v4에서 폐지(`.alf` 왕복
  불필요). GameCI v4 Personal 라이선스는 **로컬 Unity Hub가 이미 만든 `.ulf`**(Windows:
  `C:\ProgramData\Unity\Unity_lic.ulf`)를 그대로 시크릿에 넣으면 됨 + `UNITY_EMAIL`/`UNITY_PASSWORD`
  3개. 그래서 임시 `activation.yml`은 만들었다가 삭제, `ci.yml` env에 3개 시크릿 참조.
- `.gitignore`에 `/artifacts/`, `TestResults*.xml`, `[Tt]est[Rr]esults/`, `/CodeCoverage/`,
  `*.alf`/`*.ulf` 추가.
- **다음(유저)**: (1) 커밋+푸시 → (2) Unity 계정 이메일/비번을 `UNITY_EMAIL`/`UNITY_PASSWORD`
  시크릿으로 등록(방법 B — GameCI v4 Personal 활성화가 자격증명만으로 됨; 실패 시 수동 활성화
  `.ulf`를 `UNITY_LICENSE`로) → (3) 재실행 후 초록불 확인. 이후 내가 README 배지 추가.
- 1차 실패(2026-09-09): Docker 이미지는 정상 pull됐으나 러너 디스크 부족(`no space left on
  device`, il2cpp 이미지가 ~14GB 러너를 초과). Unity 6/신 라이선싱이라 `.ulf` 파일이 로컬에
  없음(`UnityEntitlementLicense.xml` 방식) → 방법 B(계정 이메일/비번만) 우선. `ci.yml`에 안 쓰는
  사전설치물(android SDK/dotnet/ghc/CodeQL 등) 제거 스텝 추가.
- 2차: 디스크·라이선스·**테스트 전부 통과 (CI에서 49/49: EditMode 39 + PlayMode 10)**. 마지막
  결과 게시 스텝만 실패 — `GITHUB_TOKEN`이 `checks: write` 없음(신규 저장소 기본 읽기전용).
  워크플로에 `permissions: {contents: read, checks: write}` 추가로 해결.
- 3차: **완전 초록 🟢**. `README.md` 신규 + CI 배지. **포트폴리오 트랙 #1(테스트+CI) 완료**.
- §6 #6(전체 플레이 루프)도 유저 1주파로 **완료 처리**(2026-09-09) — 신규 적 타입 2·3층 육안
  확인만 이후 다른 작업 시 이월.

**다음 작업 계획 — 포트폴리오 트랙 #2: 결정론적 시드 절차적 층 생성 (설계 계획, 미착수)**
- 심 위치: `GameFloors.Floors`(FloorLayout 리스트 생산지)에 `IFloorSource` 추상 → `JsonFloorSource`
  (현행 래핑) / `ProceduralFloorSource`(신규). `StageManager` 소비 코드 거의 불변(FloorLayout 형태 동일).
  `IFloorLayoutProvider` 심은 애초에 이 교체용(isaac-room-system 메모리).
- 생성 알고리즘: **아이작식 큐 성장** 채택 방향(중앙 시작 → 큐에서 랜덤 방 꺼내 인접에 확률+
  "이웃 1개" 제약으로 추가 → 목표 N → 후처리로 보스=시작 BFS 최원거리 데드엔드, 아이템=다른
  데드엔드). 대안: 스패닝트리+추가엣지(A로 수렴), 템플릿 조립(과함, 기각).
- 유저 결정 대기 서브 항목: (1) 시드 출처 — 랜덤+오버라이드 추천 (2) 절차화 범위 — 그래프+타입
  +적 구성까지 추천, 스폰 위치는 `RoomBounds` 안 지터 (3) JSON provider — 디버그 대안 모드로 유지
  (4) 결정성 범위 — per-system 스트림(layout/enemies/drops), 전투 미세 랜덤 제외 (5) 난이도 스케일
  — 층 시드 = master+N.
- 작업 단계: A) RNG 아키텍처(`RngStreams` + `RunConfig`를 GameFlow로 전달) B) `IFloorSource` 심
  추출(동작 불변, 49 테스트 유지) C) `ProceduralFloorGenerator` D) 절차적 적 구성 E) 프로퍼티
  테스트(같은 시드→동일 그래프 / 항상 연결+Start+Boss / 방 수 범위 / 보스방=데드엔드) + 기본을
  절차적으로 전환 F) 시드 노출(일시정지/게임오버). 규모 ~3~4 세션. 상세는 대화 2026-09-09.
- 아트 리소스 적용(무료 CC0 3D 1세트)을 이 즈음 병행 — 생성기/카메라를 회색 캡슐 아닌 실제 크기로.

**트랙 #2 Phase A+B 착수 — RNG 아키텍처 + `IFloorSource` 심 (동작 불변)**
- 서브 항목 5개 유저 확정: 알고리즘 A / 시드 c(랜덤+오버라이드) / 범위 ii(그래프+타입+적 구성) /
  JSON provider 대안 모드로 유지 + 손상 폴백 재활용 / 난이도는 `ProceduralRules.json`(층별 이산 표,
  곡선 함수 아님; F3 아이템 방은 확률). 상세 `AI_Decision_Log.md` 2026-09-09 (3).
- **Phase A — RNG** (`Assets/Scripts/Core/`):
  - `IRng` — 결정적 난수 스트림 인터페이스(`NextUInt`/`Range(int/float)`/`Next01`/`Chance`).
  - `DeterministicRng` — 자체 PRNG: xorshift128(Marsaglia) + SplitMix64 시딩. `System.Random`은
    .NET 런타임 버전에 따라 시퀀스가 바뀔 수 있어(공유 시드 무의미) 자체 구현. `Range(int)`는
    거부 표본추출로 무편향.
  - `StableHash.Fnv1a` — `string.GetHashCode()`가 최신 .NET에서 프로세스마다 랜덤이라 스트림
    이름 해시가 재현 안 됨 → FNV-1a 고정.
  - `RngStreams` — 마스터 시드 → 이름별 독립 서브스트림(`Stream("layout")` / `Stream("enemies")`).
    하나 뽑아도 다른 게 안 흔들림. `drops`는 범위 iii 확장 시.
  - `RunConfig`(시드 + `FloorSourceMode` {Procedural, Json}) + `RunConfig.NewRandom`.
  - `GameFlow`: `CurrentRun` 추가, `StartRun`/`RetryRun`이 `NewRandom` 생성(RetryRun은 새 시드 =
    새 던전, "같은 시드 재생"은 후일), `ReturnToTitle`이 clear, 에디터 직접 실행용 `EnsureRun()`.
    현재 `DefaultFloorSource = Json`(생성기 나오면 한 줄 flip).
- **Phase B — `IFloorSource` 심** (`Assets/Scripts/Stage/`):
  - `IFloorSource { IReadOnlyList<FloorLayout> Floors }`. `JsonFloorSource`가 `GameFloors.Floors`
    래핑(손상 폴백 포함). `StageManager.Awake`가 `GameFloors.Floors` 직접 참조 → `BuildFloorSource()`
    (`RunConfig.FloorSource` switch, 지금은 Json만). `ProceduralFloorSource`는 Phase C에서 합류.
- **검증**: EditMode **52개**(기존 39 + 신규 `RngTests` 13: 같은 시드→동일 시퀀스 / 다른 시드→다름 /
  `Range` 경계·양끝 도달·max 미도달·예외 / `Next01` 구간 / `Chance` 0·1·0.5(4만 표본 ±0.02) /
  스트림 동일이름 동일·다른이름 독립·인스턴스 간 재현 / `StableHash` 고정값) 전부 통과. PlayMode
  **10개** 그대로 통과(심 리팩터 동작 불변). `RunCommand` 새너티: RngStreams 재현성, layout/enemies
  독립, `EnsureRun` 기본값(mode Json), `JsonFloorSource` 3층(6/10/13) 정상. 콘솔 에러 0.
**트랙 #2 Phase C — `ProceduralFloorGenerator` (아이작식 큐 성장)**
- `FloorGraph.Validate(rooms, startId)` — Phase 2에서 `GameFloors`에 넣었던 그래프 검증(dangling
  connection + start에서 BFS 연결성 + 중복 id + startRoomId 존재)을 공용 static으로 추출.
  `GameFloors.ParseFloorJson`이 이걸 호출하도록 변경(동작 불변), 생성기도 self-check로 사용.
- `FloorGenParams`(struct) — 층별 노브 `MinRooms`/`MaxRooms`/`ItemRoomChance`. 지금은
  `ProceduralFloorSource`에 하드코딩 기본값(F1 6~8, F2 9~11, F3 12~14 / 아이템 확률 1/1/0.5),
  Phase D에서 `ProceduralRules.json` 로더로 교체.
- `ProceduralFloorGenerator.Generate(floorNumber, isLast, params, RngStreams)`:
  - 격자 중앙(0,0)에 시작방 → 반복 홍수 패스: 배치된 셀들을 `(y,x)` 정렬 순회, 각 빈 직교
    이웃이 "기존 방 이웃 정확히 1개"면 50% 확률로 추가. 목표 방 수 도달 or 성장 불가까지.
  - 데드엔드(완성 후 문 1개, start 제외) 계산 → 보스 = start에서 BFS 최원거리 데드엔드(거리
    동률은 `(y,x)` 순), 아이템 = 남은 데드엔드 중 `ItemRoomChance`로 하나, 나머지 Combat.
  - 방 id = start에서 BFS 순서(고정 이웃 순 N/E/S/W) → start 항상 `floorNumber*1000+1`. 방 리스트는
    id 오름차순으로 방출(`Dictionary` 열거 순서 비보장 회피 — 그래프뿐 아니라 리스트도 결정적).
  - 문 = 인접한 두 배치 셀 사이 양방향. 방 크기 타입별(Start 16 / Item 20 / Combat·Boss 40).
    적은 빈 배열(Phase D).
  - `(masterSeed, floorNumber, attempt)`로 스트림 파생 → 제약 미충족(방 부족/데드엔드 없음/그래프
    무효) 시 다음 attempt로 재시도(최대 40, 각 attempt 저렴). 전부 실패 시(사실상 불가) trivial
    start-combat-boss 라인 폴백 + 경고.
- `ProceduralFloorSource(masterSeed) : IFloorSource` — 3개 층 생성(층당 파생 스트림, N번째만 isLast).
  `StageManager.BuildFloorSource` switch에 `FloorSourceMode.Procedural` case 추가. **기본은 여전히
  `Json`**(`GameFlow.DefaultFloorSource`) — 절차적은 도달 가능하지만 아직 기본 아님.
- **검증**: EditMode **66개**(기존 52 + 신규 14: `ProceduralFloorGeneratorTests` 9 — 같은 시드→동일
  레이아웃 / 다른 시드→다름 / 항상 유효 그래프+Start1+Boss1 / 방 수 범위 / 보스=데드엔드 /
  id 층 네임스페이스 / 문 양방향 / 아이템 확률 0→없음·1→있음. `ProceduralFloorSourceTests` 5 —
  3층+isLast / 전부 유효·비손상 / 같은 마스터시드 전 층 재현 / 다른 시드 다름 / 후반 층 방 수 증가)
  전부 통과. PlayMode **10개** 그대로. `RunCommand` ASCII 미리보기: 시드 20260909 → 7/11/12방,
  아이작형 스프롤, 보스 원거리 데드엔드, 같은 시드 재현 True. 콘솔 에러/폴백 경고 0.
**트랙 #2 Phase D — `ProceduralRules.json` 로더 + 절차적 적 구성**
- **`ProceduralRules.json`** (`Assets/StreamingAssets/Balance/Stage/`) 신규 — 층별 난이도 규칙:
  `roomCount{min,max}` / `itemRoomChance` / `enemiesPerCombatRoom{min,max}` / `variantWeights
  {melee,ranged,erraticMelee,burstRanged}`. `_guide`/`_fieldGuide` 포함. `floors` 배열, 층 수
  초과 시 마지막 규칙 반복.
- **`ProceduralRules`** 로더 — `GameFloors`/`GameBalance`와 동일 패턴(정적, lazy, 세션 리셋,
  `internal Parse` 시크릿, 파일 없음/깨짐 → 내장 기본값 + 경고). `RuleAt(rules, N)`(1-based, 배열
  범위로 clamp)은 순수라 테스트 가능.
- **`FloorRule`**(구 `FloorGenParams` 대체·확장) struct — 6필드. 생성자가 out-of-range 값 sanitize
  (min>max, 음수 가중치, 확률>1). `VariantWeights`(struct) — 4가중치 + `Pick(IRng)` 가중 추첨
  (전부 0이면 Melee).
- **`ProceduralFloorGenerator`** 적 구성 추가:
  - Combat 방: `enemyRng.Range(min, max+1)`마리, 각각 `VariantWeights.Pick`, 위치는 방 크기의
    `RoomBounds.InteriorHalfExtent`의 45~75% 반경 링에 균등 각도 + 지터 배치(`enemies` 스트림).
  - Boss 방: `EnemyVariantType.Boss` 1마리 `(0, 1.5, 6)`.
  - Start/Item 방: 적 0.
  - `layout`(attempt별) / `enemies`(층별) 스트림 분리 — 적 수 튜닝이 그래프를 안 흔들고 그 역도.
  - `RoomBounds.WallThickness` 상수를 `RoomController`에서 `RoomBounds`로 이동(생성기·방 지오메트리·
    클램프가 한 상수 참조). `RoomController.WallThickness = RoomBounds.WallThickness`.
- `ProceduralFloorSource`가 `DefaultParamsFor` → `ProceduralRules.ForFloor(n)`.
- **검증**: EditMode **75개**(기존 66 + 신규 9: `ProceduralRulesTests` 6 — 정상 파싱/floors 없음·
  garbage → throw/out-of-range sanitize/`RuleAt` clamp/기본값 3층 상승 곡선. 생성기 적 테스트 3 —
  Combat 방 적 수 범위·Boss 1마리·Start/Item 0 / 가중치 하나만 non-zero면 전부 그 변형 /
  스폰 위치가 방 내부). PlayMode **10개** 그대로. `RunCommand` 미리보기(시드 20260909):
  `ProceduralRules.json` 정상 로드(기본값 아님), Floor1/2/3 = 7/11/12방·13/42/56마리(난이도 상승),
  변형 믹스가 후반에 Ranged/Burst 쪽으로 이동, 같은 시드 = 적까지 동일. 콘솔 에러/폴백 경고 0.
**트랙 #2 Phase E — 절차적 생성을 기본으로 전환 + 프로퍼티 테스트 + 시드 표시**
- **`GameFlow.DefaultFloorSource` = `Json` → `Procedural`** (한 줄). 이제 모든 실제 런
  (`StartRun`/`RetryRun`)과 에디터 직접 실행(`EnsureRun`)이 시드로부터 던전을 생성. `Json`
  모드는 `RunConfig`에 `FloorSourceMode.Json`을 주면 여전히 도달 가능(디버그/레퍼런스용, 아직
  아무도 안 함).
- **시드 표시 + 디버그 재시작** (실플레이 편의):
  - F1 오버레이에 시드 표시. 처음엔 FPS 텍스트에 줄 추가했으나 그 UI 박스가 높이 30px +
    `Truncate`라 2번째 줄이 잘려 안 보임(유저 제보) → `1-SHOT` 인디케이터와 같은 런타임 생성
    방식으로 **별도 `SeedIndicator` 텍스트**(FPS 박스 바로 아래, overflow ON). 포맷 `SEED: {시드}`.
  - **시드를 양수 전용으로** — `RunConfig.NewRandom`이 `Random.Next(1, int.MaxValue)`(이전엔
    `int.MinValue~int.MaxValue`라 음수 나옴). 읽기/공유/나중에 타이틀 입력하기 쉽게. 여전히 ~21억 종.
  - **F1 켠 상태에서 R키 → 런 재시작**(`GameFlow.RetryRun`, 새 시드·새 던전). 시드 확인 반복
    테스트용. `_visible`일 때만 동작(`Keyboard.current.rKey.wasPressedThisFrame` 폴링, `CharacterSelectUI`와
    같은 패턴). 실측: 양수 시드 2000개 전부 > 0·유일, F1 → `SEED: 1511630634` 표시 확인. R
    재시작 end-to-end는 유저 실플레이로 확인(자동화 세션 도메인 리로드 노이즈로 검증 불안정).
  - 일시정지/게임오버 화면 시드 표시 + 타이틀 시드 입력칸은 Phase F.
- **방 바닥 체크 패턴** (유저 요청) — `RoomController.BuildGeometry`의 바닥 Plane에 코드로 만든
  2×2 흑백 체커 텍스처(Point/Repeat) + URP Unlit 머티리얼. 칸 하나 2유닛 고정, 방 크기(16/20/40)별로
  `mainTextureScale`만 다른 공유 머티리얼 1개씩 캐시(MPB `_BaseMap_ST` 오버라이드는 URP에서
  불안정해 안 씀). 실측: 8방 전부 `RoomFloorChecker_{size}` 적용, 탑다운 렌더 샘플에서 흑백 교대
  확인. EditMode 82 + PlayMode 10 그대로 통과.
  - **`Prototype.unity`의 정적 `Ground` 오브젝트 삭제**(유저 승인) — Stage 1 시절의 30×30 Plane
    (y=0)이 룸 시스템의 방별 바닥 Plane(역시 y=0, 시작방은 원점)과 동일 평면이라 Z-fighting
    유발(체커 넣으니 눈에 띔). 룸 시스템이 완전 대체: 방 바닥도 `Ground` 레이어(그레네이드
    레이캐스트) + MeshCollider, 스크립트 `[SerializeField]` 참조 없음, 플레이어는 `useGravity=false`라
    바닥 콜라이더 불필요. 삭제 후 실측: `Ground` 없음, 플레이어 (0,1,0) 유지, y=0 동일평면 렌더러
    0개(Z-fight 해소), 콘솔 에러 0.
- **프로퍼티 테스트** — 신규 `ProceduralInvariantsTests` 7개: 120시드 × 3층 전부에 대해
  유효 그래프·비손상 / Start 1·Boss 1(데드엔드)·Item ≤1(데드엔드) / 방 id·격자 셀 유일 /
  방 수가 실제 사용된 규칙 범위 내(= trivial 폴백 안 걸림) / 적 스폰 방 내부 / 문 양방향 /
  같은 시드 전 층 바이트 단위 재현.
- **씬 실측** (`Prototype.unity` Play Mode): `CurrentRun` mode=Procedural, seed 랜덤. 8방
  (Combat 5 / Boss 1 / Item 1 / Start 1) 생성, `StageManager` 1-1 정상, Floor Exit는 보스방에
  배치됨, Player는 시작방(0,1,0), 콘솔 에러/경고 0.
- **검증**: EditMode **82개**(기존 75 + 7). PlayMode **10개** 그대로. 전부 통과.
- **다음 = 유저 실플레이 (트랙 #2의 핵심 체크포인트)**: 타이틀→런 시작 시마다 다른 던전 /
  F1로 시드 확인 후 재시작 → 시드·던전 바뀜 / 1→2→3층 방 수·적 수 증가 체감 / 방-방 전환·
  미니맵·보스방 Floor Exit·아이템방 드랍이 절차적 레이아웃에서도 정상인지. 코드상 스모크는
  통과했으나 여러 레이아웃(4방향 허브·긴 체인·루프)에서의 카메라/미니맵 체감은 사람 확인 필요.
- 이후: Phase F — 시드를 일시정지/게임오버 화면에 표시 + 타이틀 화면 시드 입력칸(같은 시드 재생).

**테스트 스위트 Phase 3 — PlayMode 테스트**
- `Assets/Tests/PlayMode/` 신규. `TestHelpers.cs`(`TestObjects`) — 비활성 GO에 컴포넌트 add →
  `[SerializeField] private` 필드를 리플렉션으로 주입 → 활성화하는 패턴. `MakeEnemyData`(빈
  `attackPatterns` + 범위 지정), `BuildEnemy`(Rigidbody 실프리팹처럼 no-gravity/Y·회전 고정),
  `BuildRoom`(`Initialize`로 `RoomSize` 세팅 + 지오메트리), `MakePlayer`(Player 태그).
- 테스트 10개 (`[UnityTest]` IEnumerator, `WaitForFixedUpdate`로 물리 스텝):
  - `PoolTests` 4 — `PooledObject.ReturnToPool` 2회 안전(2026-08-31 이중반환 가드), `NotifyTakenFromPool`
    재장전, `GameObjectPool.Get` 활성·부모 세팅, Release→Get 동일 인스턴스 재사용
  - `EnemyContainmentTests` 3 — **2026-09-09 방 이탈 블로킹 버그 회귀 잠금**: `MoveTo`(방 밖 좌표)
    → 내부 클램프 / `MoveTo`(안) → 그대로 / 방 밖으로 순간이동 → `EnforceRoomContainment`가 스폰
    지점으로 스냅
  - `EnemyFsmTests` 3 — 플레이어 DetectionRange 안 → Idle→Chase / 멀면 Idle 유지 / 피격 → Stunned

**적 발사체 ↔ 아군 발사체 = 서로 통과 (상쇄 없음) (유저 요청)**
- 이전 동작: `Projectile.OnTriggerEnter`가 "발사자(_owner)가 아닌 콜라이더에 처음 닿으면" 무조건
  `ReturnToPoolOnce()`로 소멸. 발사체 프리팹끼리도 트리거 콜라이더(SphereCollider isTrigger,
  kinematic Rigidbody)라 적 탄과 플레이어 탄이 겹치면 둘 다 그 자리에서 사라짐(2026-09-09 조사에서
  실측: 마주 오는 두 발사체 → active 0). 아이작/건전 계열엔 "탄 상쇄"가 기본이 아님.
- 수정: `OnTriggerEnter`에서 `other`가 `Projectile` 컴포넌트를 가진 경우 `_owner` 체크 직후
  즉시 return — 데미지 없음, 소멸 없음, 서로 그냥 지나감. `IDamageable`(적/플레이어/파괴 가능
  오브젝트)·벽 등 그 외 충돌 처리는 전부 그대로. 나중에 의도적인 상쇄/블랭크 메커닉을 넣을 여지는
  주석으로 남김. 프리팹·레이어·물리 설정 변경 없음(코드 6줄 추가).
- 신규 테스트 `Assets/Tests/PlayMode/ProjectileTests.cs` 1개 — 마주 겹친 두 발사체(다른 owner)를
  프리팹처럼 구성(트리거 SphereCollider r=0.5 + kinematic Rigidbody) → 물리 스텝 몇 번 후 둘 다
  여전히 active. 이 동작(추후 상쇄 메커닉 추가 시 같이 갱신)을 회귀 잠금.

### 테스트 결과

**적 발사체 ↔ 아군 발사체 통과**
- [x] EditMode **82개** + PlayMode **11개**(기존 10 + 신규 `ProjectileTests` 1) 전부 통과
      (`TestRunnerApi` EditMode/PlayMode 각각 실행 → `state=Passed pass=93 fail=0`). 재컴파일
      콘솔 에러 0. `RogueLike.Tests.EditMode`·`RogueLike.Tests.PlayMode` 어셈블리 정상 빌드.
- [x] 신규 `ProjectileTests.TwoOpposingProjectiles_Overlapping_BothPassThroughAndStayActive`
      PlayMode 통과 — 마주 겹친 적/플레이어 발사체 둘 다 소멸 안 됨(수정 전이면 둘 다 소멸).

**테스트 스위트 Phase 3**
- [x] PlayMode 테스트 **10개 전부 통과** (`TestRunnerApi` PlayMode 실행 → `TestResults.xml`
      `total="10" passed="10" failed="0"`). MCP로 PlayMode 실행도 정상 작동(play mode 진입→실행→종료).
- [x] EditMode + PlayMode 합계 **49개 전부 통과** (39 + 10). 콘솔 에러 0.

**테스트 스위트 Phase 2**
- [x] EditMode 테스트 **39개 전부 통과** (`TestRunnerApi`: `started cases=39` →
      `FINISHED status=Passed pass=39 fail=0 skip=0 incon=0`). Phase 1의 19 + Phase 2의 20.
- [x] `ValidateGraph` 회귀: 실제 `Floor1/2/3.json` 로드 → 방 6/10/13, `IsCorrupted=False` 전부,
      `CORRUPTED COUNT: 0` (Play Mode `GameFloors.Floors`).
- [x] `GameFloors`/`GameBalance` 리팩터 후 전체 재컴파일 콘솔 에러 0.

**테스트 스위트 Phase 1**
- [x] EditMode 테스트 **19개 전부 통과** (`TestRunnerApi` 실행: `started cases=19` →
      `FINISHED status=Passed pass=19 fail=0 skip=0 inconclusive=0`). 이 세션에서 EditMode 테스트
      실행은 MCP로 정상 작동(Play Mode 프레임 고정과 무관).
- [x] 추출 리팩터 회귀 확인(`RunCommand`): `KnockbackMath.Distance` 0/0.5/1/2 → 1.5/0.75/0/0
      (인라인과 동일). `RoomBounds` 한계 18.9, 클램프 (18.90, 5, -3)(Y 불변), slack 밴드 True.
- [x] 전체 재컴파일 콘솔 에러 0.

**테스트 스위트 Phase 0 (asmdef)**
- [x] `RogueLike.asmdef` import 후 재컴파일 콘솔 에러 0. 참조는 `Unity.InputSystem` + `UnityEngine.UI`
      2개로 충분(사전 조사대로 — 추가 참조 캐스케이드 없음).
- [x] Play Mode `RunCommand` 검증: 게임 타입 9종(`EnemyStateMachine`/`RoomController`/`GameFloors`/
      `Health`/`GameFlow`/`SceneTransitionController`/`PlayerMovement`/`UIManager`/`GameObjectPool`)
      전부 `RogueLike` 어셈블리 소속. `RogueLike` 타입 149개(116 파일). 참조 어셈블리 =
      `netstandard, Unity.InputSystem, UnityEngine.CoreModule/JSONSerializeModule/PhysicsModule/
      TextRenderingModule/UIModule, UnityEngine.UI`.
- [x] `Assembly-CSharp`는 잔존(타입 7개 = `Assets/TutorialInfo/`의 템플릿 Readme 스크립트) — 게임
      코드 무참조, 무해. 별도 정리 대상으로 남김.
- [x] 스크립트 `.cs` 변경 0줄. 런타임 동작 불변(어셈블리 이름만 부여).

**적 방 이탈 수정**
- [x] 변경 8파일(`RoomController`/`EnemySpawner`/`EnemyStateMachine`/`EnemyChaseState`/
      `EnemyPatrolState`/`EnemyStunnedState`/`EnemyAttackState`/`MeleeAttackPattern`/
      `RangedKitingBehavior`/`EnemyMovementFlavor`) `ValidateScript` + 재컴파일 콘솔 에러 0.
- [x] `ClampToInterior` 수치 검증(Play Mode `RunCommand`): 방2(size 40, center (50,0,0)) 기준
      `(150,0,100)` → local `(18.90, 18.90)` = `20 - 0.5 - 0.5 - 0.1` 정확히 일치. 내부 점은 불변.
      `IsInsideInterior` 밖 False / 안 True.
- [x] 이동 초크포인트: 6개 이동 호출부 전부 `MoveTo` 경유(grep 확인). 직접 `MovePosition`는
      회전(`MoveRotation`)만 남음.
- [ ] **런타임 동작(넉백이 벽에서 멈춤 / 후퇴가 방 안 유지 / 밖으로 나간 적 스냅백 / 전투 회귀
      없음)은 사람 플레이 확인 필요** — 이 자동화 세션은 Play Mode `Time.frameCount`가 3130에
      고정돼 FixedUpdate가 안 돌아 런타임 검증 불가(룸/벤치 작업 때도 겪은 환경 한계). 코드 결함 아님.

**씬 전환 페이드**
- [x] `Unity_ValidateScript`(standard) 두 파일 0건, 컴파일 에러/경고 0건(`SceneTransitionController`,
      `GameFlow` 재컴파일 후 콘솔 클린 — 무관한 `[RelayService]` 경고만 있음).
- [x] Play Mode + `Unity_RunCommand` 구조 검증: `EnsureExists()` 2회 호출 → 동일 인스턴스 1개
      (`first == second`, 씬 내 STC 카운트 1), 루트 오브젝트, `scene == "DontDestroyOnLoad"`,
      `Canvas ScreenSpaceOverlay / sortingOrder 32767`, `GraphicRaycaster` 존재,
      `CanvasGroup alpha 0 / blocksRaycasts false`, `Image` 불투명 검정, RectTransform 전체 스트레치.
- [x] 전체 전환 1회 실측(Title→Prototype): `FadeAndLoad` 호출 → ~1초 뒤 active scene = Prototype,
      `timeScale` 1로 복구(`ForceResume` 발화 확인), STC 인스턴스가 Single 로드 후에도 생존·중복 0,
      `alpha` 0으로 페이드 인 완료, `blocksRaycasts` 해제, Player 정상 존재.
- [x] 재진입 가드: 한 프레임에 `FadeAndLoad("Title")` + `FadeAndLoad("Prototype")` → 2번째 무시
      (씬은 한 전환만 진행).
- [x] Stop 후 누수 0: STC 인스턴스 0, 정적 `Instance` 해제, `timeScale` 1, 에디트 모드 씬 무변경.
- [ ] **연속 전환의 페이드 코루틴 완주는 사람 플레이 확인 필요** — 자동화 Play Mode에서 첫 전환은
      완주했으나 이후 `yield return null` 코루틴이 멈추는 알려진 환경 한계(2026-08-18/08-31에도 관측).
      코드 결함 아님. 아래 체크리스트로 유저 확인.

### 이슈/메모
- **미세 트레이드오프(수용)**: `PauseState.ForceResume`가 페이드 아웃 시작 시점이 아니라 로드
  직전(`onBeforeActivate`)에 발화하도록 했으므로, 이전 코드(동기 로드) 대비 이전 씬이 추가로 도는
  시간은 없음. 반대로 만약 `ForceResume`를 `GameFlow`에서 먼저 부르면 페이드 아웃 0.35초 동안
  월드가 틱함 — 그래서 콜백으로 넘김.
- 페이드 지속시간은 `const`(0.35s). 코드 생성 오브젝트라 인스펙터가 없음. 단색 페이드라 시각
  이터레이션 대상이 아니라고 판단, 튜닝은 한 줄 수정. (인런 `FloorTransitionUI`는 극적 연출이라
  1s 유지 — 씬/메뉴 전환은 더 빠른 게 나음.)
- 이 오버레이는 의도적으로 프로젝트 유일의 `DontDestroyOnLoad`. 목적이 명확한 단일 전환 표면 +
  결정적 중복 가드 + 단일 생성 경로(`GameFlow`)라, 아키텍처 스킬이 경고하는 "모든 매니저를 DDOL"
  안티패턴이 아니라 권장하는 "명시적 부트스트랩 경로"에 해당. 추가 영속 상태가 필요해지면 여기
  statics를 늘리지 말고 별도 부트스트랩으로.

### 다음 작업
- 적 방 이탈 수정 — 유저 실플레이 체크리스트:
  - 1층 전투방에서 근접 적을 벽 쪽으로 몰아 여러 번 때려도 벽 밖으로 안 나가는지(넉백 관통)
  - 원거리 적이 후퇴하다 벽에 닿으면 벽 앞에서 멈추는지(관통·미끄러짐 없음)
  - 혹시라도 밖으로 나간 적이 즉시 스폰 지점으로 복귀하는지
  - 전투 회귀: 적 추격/순찰/공격, 근접 런지 명중, 방 클리어 정상, 여러 방 연속 주파
- 씬 전환 페이드 — 유저 실플레이 체크리스트(1주파):
  - 타이틀 → 캐릭터 선택 → 런: 양방향 페이드, 아키타입 전달
  - 3층 풀 주파: 층1→2, 2→3 인런 페이드(회귀 확인), 마지막 층 도달
  - 마지막 층 클리어 → `FloorCompleteUI` 재시작/타이틀 — 둘 다 페이드
  - 사망 → `GameOverUI` 재시작/타이틀 — 둘 다 페이드
  - 런 중 ESC → 재시작/타이틀 — 둘 다 페이드
  - JSON 외부화 확인: 1~3층이 `Floor{1,2,3}.json`에서 로드(방 6/10/13), 손상 폴백
  - 재시작 후 런 상태 초기화, 타이틀 복귀 시 아키타입 클리어
- 위 통과 시 로드맵 §6 #6 종료 → 남은 건 Phase 4(아트)/5(Addressables)/7(문서화).

---

## 다음 세션 인수인계 (2026-09-09 세션 종료 시점 · 다음 작업일 2026-09-10 예정)

> 새 세션에서 이 로그를 처음 읽는 경우를 위한 정리. 위 2026-09-09 항목들이 이 세션에서
> 한 일, 아래가 "다음에 할 일". 상세 근거는 `ClaudeSessionSummary.md` §6 / `AI_Decision_Log.md`.

### 0. 커밋 대기 (이 세션 결과물, 아직 미커밋)

- 워킹 트리에 다음이 스테이징 안 된 채 남아 있음. 유저가 Fork GUI로 직접 커밋한다.
  커밋 메시지(subject/description)는 세션 대화에서 이미 제공함 — 필요하면 다시 만들어 줄 것.
  - 코드: `Assets/Scripts/Combat/Projectile.cs`, `Assets/Scripts/Core/{GameFlow,RunConfig}.cs`,
    `Assets/Scripts/Room/RoomController.cs`, `Assets/Scripts/UI/DebugOverlayController.cs`,
    `Assets/Scenes/Prototype.unity`(레거시 `Ground` 삭제)
  - 신규: `Assets/Tests/EditMode/ProceduralInvariantsTests.cs`, `Assets/Tests/PlayMode/ProjectileTests.cs`
  - 문서: `Docs/{DevLog,ClaudeSessionSummary,AI_Decision_Log}.md`
- 내용 = (1) 절차적 던전 생성을 기본 모드로 전환 + 시드 표시/양수화/R재시작/체커 바닥/Ground 삭제,
  (2) 적·아군 발사체 상호 통과. 한 커밋으로 묶는 것을 권장(파일이 깔끔히 나뉘면 2커밋도 가능).

### 1. 트랙 #2 (절차적 생성) 마무리 — 최우선

- **유저 1주파 실플레이 검증 (핵심 체크포인트, 미완)**:
  - 런 시작마다 다른 던전 / 같은 시드 = 같은 던전
  - F1로 시드 확인 → R로 재시작 → 시드·던전 바뀜
  - 1→2→3층 방 수·적 수 증가, 변형 믹스가 후반에 원거리/버스트로 이동
  - 방-방 전환·미니맵·보스방 Floor Exit·아이템방 드랍이 여러 레이아웃(4방향 허브·긴 체인·루프)
    에서도 정상인지 — 코드 스모크는 통과, 카메라/미니맵 체감은 사람 확인 필요
- **Phase F (미착수)**: 시드를 일시정지·게임오버 화면에도 표시 + 타이틀 화면에 시드 입력칸
  (같은 시드 재생). `DebugOverlayController`의 `SEED` 표시 패턴 재사용 가능. 타이틀 입력은
  `RunConfig`에 시드 오버라이드를 넣고 `GameFlow.StartRun` 경로에서 읽는 형태.

### 2. 이월된 유저 실플레이 (이전 세션 미완, 위 "다음 작업"의 체크리스트 참고)

- 씬 전환 페이드 1주파 (타이틀↔선택↔런, 층 1→2→3 인런, 클리어/게임오버/ESC 전부 페이드)
- 적 방 이탈 수정 체크리스트 (근접 넉백·원거리 후퇴 벽 앞 정지·밖으로 나가면 복귀)
- 신규 적 변형(ErraticMelee/BurstRanged)이 2·3층에 실제로 섞여 나오는지 육안 확인
- 대시 무적 프레임 최종 확인 (`ClaudeSessionSummary.md` §6 #3)
- 발사체 상호 통과: **유저 실플레이 확인 완료** — 재검증 불필요. 추후 의도적 상쇄/블랭크
  메커닉을 넣게 되면 `Projectile.OnTriggerEnter` 가드 한 곳 + `ProjectileTests`만 고치면 됨.

### 3. 포트폴리오 기술 트랙 — 확정 순서 (`ClaudeSessionSummary.md` §6)

1. ~~테스트 스위트 + CI~~ ✅ 완료
2. **절차적 층 생성** — 대부분 완료. 위 1번(Phase F + 실플레이)만 남음
3. **적 경로탐색 (그리드 A* 권장) + 방 안 장애물** — 절차적 방이 생겨 가치 상승(장애물이
   생성기의 일부). `EnemyChaseState` 이동만 교체. 프로파일러 계측(원래 #7)을 여기 흡수. ~4~5 세션
4. **입력 버퍼링 + 프레임레이트 독립성** — 건전식 타이트 조작. ~1 세션. 게임필 우선이면 3과 교체 가능
5. **커스텀 에디터 툴링 — SO 검증 패스** (+ 여유 시 층 그래프 비주얼라이저) — 노력 대비 신호 큼, ~1 세션
6. **세이브/로드 — 버전드 직렬화 + 런 이어하기** — 언블록 안 함, 가장 나중 또는 마감 시 스킵

### 4. 아트 리소스 적용 시점

- 무료 CC0 / AI 생성 3D 1세트 — **2번(절차적 생성) 착수 직전~진행 중 병행 권장 = 지금 즈음**.
  회색 캡슐 대신 실제 크기 모델로 바꿔야 생성기/카메라 튜닝이 플레이스홀더에 안 물림.

### 5. 그 뒤 (아트 통합 이후로 의도적 이연)

- Stage 3 최적화 (`EnemyAIManager` AI Tick 분산, `GetComponent` 캐싱) + §6.5 After 측정
  (Before 표·벤치 인프라는 완료, 현재 실측 병목 없음)
- Phase 4 아트 교체 / Phase 5 Addressables(아트 후 축소 범위) / Phase 7 포트폴리오 문서화
- Stage 2.5 잔여: 아이템 방 세부 밸런스, 아이콘 스와치 → 실제 픽토그램

---

## 2026-09-08

### 진행 내용

**적 AI/행동패턴 보강 2차 — 히트 스태거/넉백, 원거리 카이팅, 보스 물리 버그 수정**
- 피격 스태거: `EnemyStateMachine`이 `HealthComponent.AnyDamageTaken`(static)을 구독,
  `EnemyData.StunDuration`(기존에 있었지만 아무도 안 쓰던 필드 재활용, 2s→0.15s로 재조정)만큼
  `EnemyStunnedState`로 전환. 넉백은 신규 `EnemyData.KnockbackResistance`(0~1) 하나로만 산출
  (`거리 = BaseKnockbackDistance(1.5, 임시 상수) × (1 - resistance)`) — 추후 플레이어 넉백력
  스탯이 생기면 이 상수 자리를 대체할 예정. `IAttackPattern.Interrupt()` 추가해 윈드업/리포지셔닝
  중 스턴 걸려도 안전하게 취소(쿨다운은 정상 적용).
- 원거리 카이팅: `RangedAttackPattern`/`BurstRangedAttackPattern`/`RetreatAndShootPattern` 전부
  신규 공유 클래스 `RangedKitingBehavior`로 통일 — 예외 없이 전 개체가 "너무 가까우면 후퇴 →
  벌어지면 윈드업 → 발사".
- 쿨다운 중 유휴 흔들림: `EnemyAttackState`가 공격 종료 지점을 기준으로 작은 sine sway 적용
  (기존 Wobble 값 재사용, 신규 필드 없음). ※ 같은 날 후속 수정으로 근접 접촉 거리에서는
  이 sway를 끄고 회전만 하도록 게이트(아래 "근접 적 비비기/공전 수정" 참고) — 원거리 적
  쿨다운 시에만 유지.
- 이동 시 비직선 흔들림: 신규 공유 헬퍼 `EnemyMovementFlavor.ApplyWobble()` — 직선 방향에
  수직 sine 파를 얹어 여러 개체가 같은 자로 그은 듯한 경로로 몰려오지 않게 함. `EnemyChaseState`/
  `EnemyPatrolState`/`RetreatAndShootPattern`(및 `RangedKitingBehavior`의 후퇴)에서 호출,
  `EnemyData`에 `wobbleFrequency`/`wobbleAmplitude` 신규 필드. 평균 방향·속도는 불변이라
  `WallClamp` 통과나 거리 기반 FSM 전환에는 영향 없음. 개체별 위상 시드
  (`EnemyStateMachine.Awake`에서 `RandomPhaseSeed()`)로 동기화되지 않게 함. (위의 `EnemyAttackState`
  유휴 sway와는 별개 시스템 — 이쪽은 이동 경로, 저쪽은 정지 중 미세 요동.)
- **신규 적 변형 2종 추가**: `EnemyVariantType`에 `ErraticMelee`/`BurstRanged` 추가.
  - `ErraticMelee`(`Enemy_ErraticMelee.prefab` + `EnemyErraticMeleeData.asset`): 공격 패턴
    애셋은 **일반 Melee와 동일한 `MeleeAttack.asset`을 재사용**(신규 애셋 없음). 차별화는
    `EnemyData` 값만 — `wobbleFrequency 4.5`/`wobbleAmplitude 0.6`(일반 대비 크게)로 갈지자
    접근, `moveSpeed 2`. 이동 코드 수정 없이 데이터만으로.
  - `BurstRanged`(`Enemy_BurstRanged.prefab` + `EnemyBurstRangedData.asset` + 신규
    `BurstRangedAttack.asset`): 신규 `BurstRangedAttackPattern`/`BurstRangedAttackPatternData` —
    윈드업(조준 고정) 후 `BurstCount`발을 `BurstInterval` 간격으로 같은 방향에 연사하는
    "샷건형" 원거리. 단발 `RangedAttackPattern`이 못 덮던 자리. 카이팅은 `RangedKitingBehavior`
    공유(사격 자체만 패턴이 소유, `onFire` 콜백으로 분리).
  - `Floor1.json` 실배치: 방2를 두 신규 변형(ErraticMelee+BurstRanged)만으로 교체, 방3/방4는
    일부 Melee/Ranged를 신규 변형으로 치환 — 실전 방에서 섞여 나오도록.
- **보스 물리 버그**: 유저가 실전에서 "보스가 넘어져서 제자리 경련하며 혼자 죽는" 현상 발견 →
  원인 2가지. (1) `Boss_Basic.prefab`만 다른 모든 개체와 달리 Rigidbody `useGravity=1,
  constraints=0`으로 잘못 설정(플레이어/일반 몹은 `useGravity=0, constraints=84`) — 스태거로
  상태 전환이 잦아지며 토크가 누적돼 캡슐이 전도. (2) `Projectile.OnTriggerEnter`에 발사 주체
  제외 로직이 없어서(FlamethrowerActiveItem은 있음) 넘어진 캡슐의 늘어난 충돌 형태에 자기
  발사체가 재진입 → 자해. 둘 다 수정 후 30연속 피격 스트레스 테스트로 재현 안 됨 확인.

**플레이어 사망 → Game Over 흐름 (로드맵 §6 #6 나머지 — 그간 이연되던 항목)**
- 신규 `GameOverUI` — `FloorCompleteUI`의 패배판 대응물. `CanvasGroup` 3개(`dimGroup`은
  0→`dimMaxAlpha`로 완전 검정 아래 제한, `contentGroup`(fail 이미지 + 런 스탯)·`buttonsGroup`은
  0→1)를 **전부 같은 `fadeDuration` 타임라인**에서 페이드. `buttonsGroup`은 페이드가 끝난
  뒤에만 `interactable`/`blocksRaycasts` on(버튼도 같이 페이드되지만 완전히 뜨기 전엔 클릭
  불가). 가시성은 프로젝트의 다른 결과 화면과 동일하게 `CanvasGroup` 컨벤션 — `SetActive`
  안 씀(자기-비활성 패널 함정 회피). 표시 스탯은 `FloorCompleteUI.FloorCompleteStats`
  재사용(클리어한 방/패시브 수/액티브 이름/생존 시간).
  ※ 최초 구현은 버튼을 별도 `buttonsFadeDuration` 코루틴으로 콘텐츠 페이드 **후에** 시작했으나,
    유저 피드백(버튼 페이드가 살짝 늦게 시작)으로 단일 타임라인에 병합, `FadeInButtonsRoutine`·
    `buttonsFadeDuration` 필드 제거.
- `UIManager.ShowGameOver(stats)` 파사드 메서드 추가.
- `RoomFlowManager`가 플레이어 사망도 담당(전용 컨트롤러 신설 대신) — `HealthComponent.Health.
  Died` 구독. 처리: 진행 중 구르기 취소(`CancelDodgeRoll`) + 픽업 알림 즉시 숨김 +
  `PlayerInputActions` 비활성 + `PauseState.RequestPause` + 캡슐 쓰러지는 연출 코루틴 +
  `ShowGameOver`. 스탯 빌드는 승/패 공용 `BuildCurrentStats()`로 추출.
- 사망 연출: 물리 아닌 **스크립트 틱오버**(`DeathFallDuration` 0.45s, `DeathFallAngle` 90°).
  `isKinematic=true` 후 캡슐 밑동(`position - up*height/2`)을 피벗으로 `RotateAround` —
  transform 원점(캡슐 중심) 기준으로 돌리면 지면을 뚫고 회전하므로. `Time.unscaledDeltaTime`
  사용(`PauseState`가 이미 `timeScale=0`). 아직 캐릭터가 무애니 프리미티브 캡슐이라 임시 —
  실제 캐릭터 아트/애니 교체 시 재검토(CLAUDE.md 플레이스홀더 지오메트리 노트).
- 아트: `Assets/Art/UI/GamePlay/ui_game_fail.png`(GPT 생성) 추가.

**대시 시 적 관통 리워크 (실플레이 피드백 — 적한테 물리적으로 튕겨나감)**
- 선행: `Enemy` **레이어** 신설(`TagManager.asset`, 기존엔 `Enemy` 태그와 `Ground` 레이어만
  있었음). 적 프리팹/스폰 개체를 이 레이어로 두고, 아래 레이어 충돌 무시가 이걸 대상으로 함.
- `PlayerDodgeRoll`: 대시 중 플레이어 레이어 ↔ `Enemy` 레이어 충돌을 `Physics.IgnoreLayerCollision`
  으로 끔(기존엔 `SetInvulnerable`로 데미지 무효만 있었고 물리 충돌은 그대로라 적한테 막히거나
  튕김). 대시가 적 콜라이더 안에서 끝나면 충돌을 즉시 복원할 때 솔버가 플레이어를 순간적으로
  밀어내므로, `IsOverlappingEnemy()`(`Physics.CheckCapsule`, `Enemy` 레이어)면 **오버랩 보정
  꼬리 상태**로 진입 — 같은 방향으로 `CorrectionSpeedMultiplier`(0.35) 속도로 최대
  `MaxCorrectionDuration`(0.25s) 미끄러져 빠져나온 뒤 충돌 복원. `CancelDodgeRoll`(문/Floor
  Exit 전환)은 이 보정을 건너뛰고 즉시 복원(어차피 텔레포트/숨김 직전).
- 리팩터링: `FixedUpdate` → `RunDash`/`RunOverlapCorrection` 분리, `EndDodgeRoll` →
  `FinishDash`(오버랩 판정 분기) + `CompleteDodgeRoll`(공용 리셋). 대시 캡슐 지오메트리 산출을
  `WallClamp.GetCapsuleGeometry()`로 추출해 `WallClamp.ClampDirection`과 오버랩 체크가 동일한
  월드 캡슐을 쓰게 함(중복 제거).

**Benchmark Baseline 인프라 (계획서 §6.5 대응) — Before 표까지 완성**
- `Assets/Scenes/BenchmarkScene.unity` 신규: Room/Floor/StageManager 없이 Ground(60×60) +
  Player + 7개 GameObjectPool(적 4종+Boss+Projectile+Grenade, capacity/maxSize 200마리
  기준으로 증설)만 있는 격리된 스트레스 테스트 씬.
- `Assets/Scripts/Benchmark/BenchmarkSpawner.cs` 신규: `SpawnWave(variant, count)`—아레나
  전체에 그리드+지터로 분산 배치(겹침 방지), `EnemySpawner.SpawnRoom()`과 동일하게
  `SetProjectilePool`→`SetSpawnPosition` 순서 준수. `ClearAll()`로 웨이브 간 정리.
  Play Mode에서 50/100/200 전부 정상 스폰·동작·정리 확인(에러 0건).
- **`Player.prefab` 렌더링 버그 발견/수정**: BenchmarkScene 실행 시 바닥만 보이고 플레이어가
  안 보인다는 제보 → 스카이박스(Default-Skybox, Built-in RP 전용이라 URP와 비호환)를 먼저
  의심해 카메라 Clear Flags를 Solid Color로 바꿨으나 무관했음. 실제 원인은 `Player.prefab`의
  `MeshFilter.m_Mesh`가 애초에 `fileID: 0`(널)이었던 것 — Prototype 씬에 이미 배치된 인스턴스만
  캡슐 메시가 씌워진 오버라이드를 갖고 있었고 그게 프리팹 에셋에 한 번도 Apply된 적이 없었던
  기존 버그. `PrefabUtility.LoadPrefabContents`로 프리팹 자체에 캡슐 메시를 채워 넣어 수정.
  부수적으로, 이번 세션 초반에 `get_components`로 Player를 조회하면서 `MeshFilter.mesh`(공유
  아님, 인스턴스) 접근이 발생해 이미 배치해둔 BenchmarkScene 쪽 인스턴스에 빈 메시 오버라이드가
  같이 눌러앉아 있던 것도 별도로 발견해 직접 재배선함. Play Mode 라이브 인스턴스에서
  `sharedMesh=Capsule, vertexCount=550`으로 확정 확인. 유저 육안 확인 완료.
- **BenchmarkScene 편의 기능 추가**: 바닥에 체크 패턴 머티리얼(`Assets/Art/Debug/
  BenchmarkGroundChecker.png/.mat`, 칸 하나 4유닛 — 스케일 참고용) 적용. 화면 우측 상단에
  UGUI 버튼 패널을 신설해 `BenchmarkSpawner`의 public 메서드에 `Button.onClick` 퍼시스턴트
  리스너로 연결(기존 `[ContextMenu]` 우클릭 방식은 그대로 유지, 병행 가능). Canvas를 처음에
  World Space 기본값으로 만드는 바람에 100×100 유닛짜리 사각형이 씬 원점에 벽처럼 박히는
  실수를 했었고, `Canvas.renderMode`를 Screen Space - Overlay로 명시 설정해 수정.
  이후 유저 요청으로 스폰 방식을 개편: 적 타입 선택 버튼 4개(Melee/Ranged/ErraticMelee/
  BurstRanged, Boss 제외) + **추가(additive)** 스폰 버튼(Add 50/100, `[ContextMenu]`엔
  Add 500/1000도, 기존 절대치 Spawn 50/100/200에서 변경)으로 재구성. 그리드 배치도 `count`
  단위로 매번 재계산하던 방식에서 고정 32×32(=1024셀) 그리드를 `ClearAll()` 전까지 순차로
  채워나가는 방식(`_nextGridIndex`)으로 바꿔, Add를 연달아 눌러도 겹치지 않고 이어서 퍼지도록
  함(한 사이클 ~1000마리까지, 초과 시 셀 재사용). 화면 우측 중단에 현재 선택된 타입의 누적
  스폰 수를 보여주는 텍스트도 추가(`variantCountLabel`, 타입 전환 시 즉시 그 타입의 카운트로
  갱신). Play Mode에서 타입 전환 + Add 50 + Add 100(총 150, 겹침 0건)까지 실제 검증.

**근접 적 "비비기/공전" 수정 — 사거리 안에서는 공격 패턴만 이동을 소유 (실플레이 피드백)**
- 증상(유저): 근접 타입 전 개체가 플레이어에 붙어서 공격하지 않고, 주위를 불규칙하게 원처럼
  돌며 비빔. 매우 가까울 땐 제자리에 서 있는 게 자연스러움.
- 원인 3가지 복합: (1) `EnemyChaseState`에 정지 거리가 없어 접촉 상태에서도 매 FixedUpdate
  플레이어 쪽으로 `MovePosition` — 오늘 넣은 `ApplyWobble`의 **수직 성분**만 충돌에 안 막히고
  남아 캡슐 표면을 따라 미끄러짐. (2) `EnemyAttackState`의 쿨다운 중 positional idle sway가
  고정 앵커로 `MovePosition`하는데, 접촉 상태라 솔버의 depenetration(방사형 밀어내기)과
  "밀림↔되돌림"으로 싸우며 불규칙 궤도 발생. (3) `MaxAttackRange` 경계에서 Chase↔Attack
  플립플롭 시 매번 wobble 이동 재유입.
- 수정(옵션 B — `AI_Decision_Log.md` 2026-09-08 (7)): 사거리 안에서는 이동을 공격 패턴의
  런지에만 맡기고 그 사이엔 정지.
  - `EnemyChaseState.PhysicsTick`: 플레이어와의 거리가 `MaxAttackRange` 이하면 전진 중단
    (회전만 `MoveRotation`으로 추적). 그보다 멀 때만 wobble 얹은 이동 — 접근 구간 wobble은 유지.
  - `EnemyAttackState`: `ApplyIdleSway` → `HoldOrSway`로 개편. 플레이어와 `HoldStillDistance`
    (const 2.5) 이하면 positional 이동 없이 회전만(근접 range ~1.2라 근접 적은 항상 여기 —
    공격 사이 완전 정지). 그보다 멀면(원거리 적 쿨다운, range ~6) 기존 sine sway 유지 —
    거리가 있어 충돌 대상이 없으니 jitter 없음.
- 새 `EnemyData` 필드/데이터·프리팹 변경 없음. `EnemyChaseState.cs` + `EnemyAttackState.cs` 2파일.
  `EnemyPatrolState`/원거리 카이팅은 손대지 않음.

**근접 런지 오버슈트 클램프 — 가만히 맞으면 런지마다 뒤로 밀리는 문제 (실플레이 피드백)**
- 증상(유저): 1층 첫 방에서 근접 적에게 아무것도 안 하고 맞아보니 "밀고 멈추고" 반복. 넉백
  시스템인지 물었으나 — **플레이어 피격 넉백은 코드상 아예 없음**(`HealthComponent.TakeDamage`는
  HP만, `AnyDamageTaken` 구독자는 적뿐). 원인은 `MeleeAttackPattern`의 **런지 물리 충돌**:
  추격이 `MaxAttackRange`(=근접 range 1.2)에서 멈추는데 런지 이동량이 `LungeSpeed 8` ×
  `LungeDuration 0.15` = 1.2유닛이라 정확히 플레이어 중심까지 파고들어 관통 → 솔버가 겹침을
  풀며 플레이어(제약 없는 다이나믹 리지드바디)를 뒤로 밀어냄. 밀려서 사거리 밖 → Chase 재접근
  → 또 런지. B 수정이 sway를 걷어내며 이 런지-shove가 순수 신호로 드러난 것.
- 수정(옵션 2 — `AI_Decision_Log.md` 2026-09-08 (8)): `MeleeAttackPattern.PhysicsTick`의 런지
  전진량을 매 tick `min(스텝, max(0, 플레이어와의 직선거리 − LungeContactBuffer))`로 클램프.
  `LungeContactBuffer` const 1.1(양 캡슐 radius 0.5 → 살짝 떨어진 접촉, 쿨다운 홀드 중 상시
  접촉 그라인딩 없음. range 1.2 안이라 런지 종료 시 데미지 판정은 그대로 성립). 이동은 고정
  `_lungeDirection`을 따라서만 → 비호밍 유지(옆으로 피하면 런지가 그대로 지나쳐 헛침).
- 데이터/프리팹 변경 없음(`MeleeAttack.asset`엔 lunge 필드 미직렬화 = 코드 기본값 사용,
  일반 Melee·ErraticMelee 공용이라 한 번에 적용). `MeleeAttackPattern.cs` 1파일.
- 플레이어 피격 넉백은 별도 시스템으로 **나중에**(플레이어 넉백 스탯과 함께) — 유저 결정.

### 테스트 결과
- [x] 보스 Rigidbody 수정 후 스트레스 테스트(연속 피격 30회) — 전도/자해 재현 안 됨.
- [x] BenchmarkSpawner로 50/100/200 스폰 — 그리드 분산 확인(X/Z 스프레드가 arenaHalfExtent
  근처까지 도달), 콘솔 에러 0건, `ClearAll()` 후 활성 EnemyStateMachine 0개 확인.
- [x] `Player.prefab` 메시 수정 — 유저 육안 확인 완료(캡슐 정상 표시).
- [x] UGUI 스폰 버튼 — Play Mode에서 `Button.onClick.Invoke()`로 직접 검증(0→50마리), 유저도
  화면에서 클릭으로 스폰 확인.
- [x] **Enemy N → FPS/GC Alloc Before 표(계획서 §6.5 포맷) — 완성, 4종 전부.** 아래 참고.
- [x] 신규 변형 2종(`ErraticMelee`/`BurstRanged`) — 실전 방(`Floor1.json` 방2/3/4) 배치·동작
  유저 확인 완료. (이동 wobble도 이 과정에서 함께 육안 확인.)
- [x] 플레이어 사망 → Game Over 전체 흐름 — 감지·캡슐 틱오버 연출·`GameOverUI` 표시 +
  버튼(재시작/타이틀) 동작까지 유저 확인 완료. (버튼 배선 자체는 이번에 완료 — 이월 아님.
  타이틀/일시정지 UI 레이아웃 미세조정만 포트폴리오 마감 후로 이연.)
- [x] `GameOverUI` 페이드 단일 타임라인 병합(dim/content/buttons 동시, 버튼은 페이드 후 클릭
  가능) — 유저 확인 완료.
- [x] 대시 적 관통 + 오버랩 보정 꼬리 — 유저 확인 완료.
- [x] 근접 적 사거리 내 정지(옵션 B) + 런지 오버슈트 클램프(옵션 2) — 1층 첫 방 실플레이로
  유저 확인 완료. 근접 적이 플레이어를 밀지 않음(공전/불도저 shove 없음). 코드 검증도
  `Unity_ValidateScript` 0건 + 콘솔 컴파일 에러 없음.

**Before (BenchmarkScene, 단일 종별 개별 측정, Unity Editor Play Mode, Deep Profile 끔)**

Median GC Alloc은 4종·전 구간에서 전부 0 bytes(스폰/공격 순간에만 20~35KB대 스파이크, 개체 수와
뚜렷한 상관관계는 없음) — 표에서는 Median CPU Time과 예산(16.67ms) 초과 프레임 비율만 기재.

| Enemy 수 | Melee | Ranged | ErraticMelee | BurstRanged |
|---|---|---|---|---|
| 50 | 4.77 ms (1.0%) | 3.79 ms (3.5%) | 3.48 ms (3.0%) | 4.39 ms (4.0%) |
| 100 | 5.11 ms (0.5%) | 3.89 ms (3.5%) | 3.89 ms (4.0%) | 5.33 ms (4.5%) |
| 200 | 4.43 ms (4.0%) | 6.44 ms (5.5%) | 5.78 ms (4.5%) | 7.35 ms (7.5%) |

가장 무거운 조합은 **BurstRanged 200마리, median 7.35ms**(16.67ms 예산의 약 44%) — 카이팅
리포지셔닝 + 버스트 발사체 스폰이 겹쳐서 4종 중 가장 비싼 건 예상대로였지만, 여전히 예산 절반
이하라 안전권.

### 이슈/메모
- **MCP 브리지 오버헤드 문제 해결**: 이전 시도(`Unity_RunCommand`로 Play Mode를 직접 구동)에서는
  0/50/100/200마리 전부 median CPU Time이 ~99.5ms로 동일하게 나와 브리지 자체의 상수
  오버헤드가 신호를 덮어버렸었음. **유저가 직접 Play/버튼 클릭/Stop을 수행하고(내 쪽에서는 그
  구간 동안 아무 명령도 안 보냄), Stop 후 Profiler의 프레임 히스토리 버퍼(`ProfilerDriver.
  firstFrameIndex~lastFrameIndex`, Stop 이후에도 유지됨)에서 그 세션의 마지막 200프레임 구간을
  읽어오는 방식으로 전환하니 median 4~5ms대의 깨끗한 수치가 나옴** — 브리지가 실제로 프레임에
  개입하지 않으면 문제없다는 뜻. 이 방식이 이번 세션에서 확립한 최종 측정 방법론.
- 위 표의 수치는 여전히 Unity Editor Play Mode 기준(Development Build 아님) — Editor 자체
  오버헤드는 포함되어 있으니, 실제 빌드 성능과는 절대값이 다를 수 있음. 다만 브리지 오버헤드
  문제는 해소됐고, 세 구간(50/100/200) 간 상대 비교는 유효함.

### 다음 작업
- 4종 전부 200마리까지 여유로워서(가장 무거운 BurstRanged도 median 7.35ms, 예산의 44%) 최적화
  작업이 시급하진 않음 — 필요 시 §6.5 항목(AI tick distribution, GetComponent 캐싱 등) 중
  실측 병목(스폰/공격 순간의 GC 스파이크) 위주로 접근.
- §6.5의 "최적화 → After" 패스는 **Phase 6(아트 통합 이후)로 의도적 이연** — 지금 병목이
  없고, 캡슐 placeholder 상대로 최적화하면 아트 들어올 때 재작업. Before 표 + 벤치 인프라로
  §6.5의 이번 단계 분량은 종료(`AI_Decision_Log.md` 2026-09-08 (5)).
- 4종을 동시에 섞어 스폰하는 "혼합 구성" 시나리오는 아직 안 해봄 — 필요하면 BenchmarkSpawner에
  다중 변형 웨이브 기능을 추가해서 시도.
- 사망 연출 / 대시 적 관통 / 신규 변형 2종 실전 배치는 유저 Play Mode 확인 완료(위 "테스트
  결과"). 남은 건 `GameOverUI` 버튼(재시작/타이틀) 배선 — UI Scene 플레이 루프 작업에서 함께.

### 이슈/메모 (재부팅 후 후속 세션) — 500/1000마리 스트레스 테스트 중 에디터 크래시

**상황(유저 진술)**
- 위 50/100/200 측정을 마친 뒤, 측정 중 **Windows "메모리 부족" 알림**이 떠서 PC를 1차 재부팅.
- 재부팅 후 Unity 재실행, 각 타입 500/1000마리까지 구간을 늘려볼 계획으로 Melee 1000마리를
  스폰(`Add 1000` 1회). 스폰 직후 적들이 정지 상태의 플레이어에게 몰려들어 **캡슐끼리 충돌이
  대량 발생**하는 상태였음.
- 몇 초 뒤 **화면이 한 번 깜빡였다가 Unity "강제 종료" 팝업**이 떠서, 유저가 급히 Unity를 종료하고
  PC를 2차 재부팅. 이후 세션이 끊겨 이 대화(다음 세션)로 이어짐.
- 데이터 손실 없음: 50/100/200 Before 표는 이 세션 이전에 이미 본 DevLog에 기록 완료. 스폰 방식
  개편分까지 반영된 `BenchmarkSpawner.cs`(Add 500/1000 ContextMenu 포함)도 디스크에 온전.
  크래시 시점(2차 재부팅 직전)에 Unity가 `Assets/_Recovery/0.unity`(BenchmarkScene 백업)를
  남겼으나, 정상 저장본이 별도로 있어 다음 실행 시 복구 팝업은 Discard 예정.

**원인 분석(측정 없이 정황 + 코드/프리팹 근거 기반, 확정 아님)**
- "Unity가 오브젝트 1000개를 못 버틴다"가 아니라 **이 프리팹 × 이 측정 환경**이 겹친 결과. 실제
  게임(룸당 10~30마리, 시간차 스폰, 정상 풀링)에서는 재현되지 않는 조건.
- 적 프리팹 1개의 실측 비용: `Enemy_Basic.prefab`은 루트 컴포넌트 8개 + **자식에 개별 월드스페이스
  Canvas(HealthBarCanvas) + Fill/Background**. 1000마리면 dynamic Rigidbody 1000 + CapsuleCollider
  1000 + **월드스페이스 캔버스 1000개**(드로우콜/캔버스 리빌드 1000배) + 상태머신/상태객체 ×1000.
- 충돌 기여분: 근접 공격 판정 자체는 물리 충돌이 아님(`MeleeWeapon.cs`가 `OverlapSphere` 계열
  쿼리로 `IDamageable` 탐색 → 컨택트 페어 미생성). 다만 **1000개 dynamic 캡슐이 정지한 플레이어
  주위로 뭉치면서 서로 밀어내는 물리 컨택트 페어가 국소 밀집도의 제곱 가까이 폭증** → 수만 쌍
  동시 활성 → PhysX 컨택트 매니폴드 버퍼(네이티브 메모리) + 솔버 비용 급증. 유저가 본 "충돌
  대량 발생"이 이 부분.
- 동시 작용한 다른 요인: (1) `SpawnWave`가 버튼 1클릭에 **1000× Instantiate를 한 프레임 동기
  실행** → 거대 히치. (2) 풀 capacity/maxSize ~200이라 **1000 중 800은 풀 오버플로우 =
  Instantiate/Destroy 처링**(메모리 파편화 + GC 압박). (3) **Profiler 레코딩 중**이라 1000개
  오브젝트의 계층 샘플 + GC 스파이크를 프레임마다 버퍼에 축적 — 스트레스 테스트가 에디터를
  OOM시키는 흔한 주범. (4) 에디터 Play Mode 자체 오버헤드 + **직전에 이미 RAM 여유가 거의 없던
  상태**(1차 메모리 부족 경고).
- "화면 깜빡임 → 강제 종료" 신호는 RAM OOM과는 다른 축: 렌더 스레드가 1000 메시 + 1000 캔버스에
  눌려 응답 시간을 초과하거나 VRAM이 부족하면 **Windows 그래픽 드라이버 TDR(타임아웃 복구)
  리셋** → 디바이스 로스트 → Unity 크래시. RAM 압박 + GPU 압박이 함께 터진 캐스케이드로 추정.

**결론 / 조치**
- 500/1000 구간은 **측정하지 않고 스킵**(유저 결정). 게임 범위(룸당 수십 마리) 밖이고, 에디터 +
  Profiler 환경이 그 규모를 담지 못함. ≤200 구간이 유효 데이터이며, 가장 무거운 BurstRanged
  200마리도 예산의 44%라 최적화 비긴급이라는 위 "다음 작업" 결론 유지.
- 향후 고카운트 측정이 꼭 필요해지면: 에디터 아닌 **Development Build**, Profiler는 counters-only,
  스폰을 여러 프레임에 분산, 풀 maxSize 상향(처링 제거), 필요 시 적 레이어 self-collision 해제.

### 다음 작업 후보 — 포트폴리오 기술 요소 제안 (Claude 제안, 미결정 · 검토/논의 후 진행 예정)

**맥락**: 전체 플레이 루프 마감(씬 전환 페이드 + 3층 주파 검증)만 하면 §6 항목 6이 닫히는데,
그 뒤 남은 계획 항목(Stage 3 최적화/AI Tick, 엘리트 적 1종, 사운드·이펙트)이 포트폴리오
관점에서 기술 신호가 약하다는 유저 판단. `ProjectPlan.md` §9·§10·§12 논지("AI 보조 개발 +
비판적 리뷰 + 기술적으로 방어 가능한 설계")를 실증할 **검증 가능한 엔지니어링 산출물**이
부족함. 아래는 Claude가 제안한 후보. **다음 작업일에 유저가 검토 → 추가 논의 → 가능한 것만
`ProjectPlan.md`에 추가 후 착수.** 지금은 결정된 것 없음.

**추천 상위 3**
1. **자동화 테스트 스위트(EditMode + PlayMode) + CI** — UTF. EditMode: 순수 로직(`Health`
   데미지, 넉백 거리 공식, RNG 결정성, 층 그래프 연결성, JSON 밸런스 파싱+폴백). PlayMode:
   적 FSM 전이, 룸 클리어 판정, 풀 재사용 상태 누수 없음. GitHub Actions(GameCI)로 push마다
   테스트+빌드. → §12 "How did you validate AI-generated code?"의 직접적 답. 범위 작음/점진적
   (CI 라이선스 설정만 fiddly). 과설계 위험 거의 없음. **1순위**.
2. **적 경로탐색(NavMesh 또는 그리드 A*)** — 현재 적은 직선 `MovePosition` + wobble +
   `WallClamp`뿐이라 장애물·모서리를 못 돔(§6 항목 5 "방 안 장애물" 이연의 실제 이유).
   룸별 `NavMeshSurface` 베이크 → `NavMesh.CalculatePath` 결과를 기존 `EnemyChaseState`가 소비,
   또는 룸 그리드 커스텀 A*. → A*/NavMesh 내부, steering↔pathing 분리, "NavMesh vs 커스텀 왜".
   범위 중간(커스텀 A*는 중대형). FSM 구조에 `Chase`만 교체로 들어감.
3. **결정론적 시드 기반 절차적 층 생성** — 이미 있는 `IFloorLayoutProvider` 뒤에 시드 RNG
   서비스 + 절차적 룸 그래프 생성기(연결성 보장, 방 타입 배치 규칙). 같은 시드=같은 런.
   → RNG 아키텍처(per-system 스트림), 결정론적 시뮬레이션, 절차적 생성 알고리즘, "인터페이스
   설계가 갚인 사례"(애초에 이 교체를 의도해 만든 추상화). 범위 중간(결정성 감사가 subtle).
   메모리상 유저가 원래 계획한 방향.

**그 외 후보 (작거나 보조적)**
4. **세이브/로드 — 버전드 직렬화 + "런 이어하기"** (§3·Phase 3 "Save structure" 미구현):
   런 상태 직렬화, 스키마 버전+마이그레이션, 원자적 쓰기(temp+rename), 손상 폴백, 타이틀
   "이어하기". → `[SerializeReference]` 다형 아이템 리스트, `BinaryFormatter` 회피 이유,
   save-anywhere vs checkpoint. 범위 중간.
5. **커스텀 에디터 툴링** — 층 그래프 비주얼 에디터, 또는 SO 검증 패스(`AssetPostprocessor`/
   `EditorWindow`가 `EnemyData`·패턴 애셋의 널 참조·범위 밖 값을 런타임 전에 검출). →
   `EditorWindow`/`PropertyDrawer`/`SerializedProperty`. 범위 작음~중간, 노력 대비 신호 큼.
6. **입력 버퍼링 + 프레임레이트 독립성** — 대시/공격 입력 Update 샘플→FixedUpdate 소비(일부
   됨) + 이르게 누른 입력 큐잉 + coyote-time류 관용. → 게임필 엔지니어링, Update/FixedUpdate
   분리 근거(§12). 범위 작음, 기존 코드 점진.
7. **프로파일러 계측을 "시스템"으로** — `ProfilerRecorder`/`ProfilerMarker`를 AI tick·스폰·
   경로탐색에 상시, 디버그 오버레이에 프레임 예산 트래커. 이연된 AI Tick 타임슬라이싱을 마커로
   이득 증명하며 제대로. → `ProfilerRecorder` API, AI LOD/타임슬라이싱, 증거 기반 GC 헌팅.
   단, 병목이 없어 아트 전까진 다소 학술적.

**과설계 위험 — 넣는다면 신중히**
- **Addressables**(§6.4/Phase 5): §12가 명시적으로 묻는 주제라 토킹 포인트는 진짜지만 애셋이
  적어 실익은 인위적. 넣으면 "적+보스+VFX만" 범위로 축소해 정직하게.
- **이벤트 버스 정식화**: 지금도 `static event` 몇 군데 있음. 소규모에서 과하면 마이너스 —
  "여긴 C# 이벤트, 저긴 SO 채널, 이유는…"이라는 판단 스토리 자체가 값어치(§10).

**Claude 판단**: 1(테스트+CI)은 위험 없이 논지와 정확히 맞아 거의 무조건. 2(경로탐색)가 기능
공백+기술 깊이 둘 다 채움. 3(절차적 생성)은 "설계가 갚인" 서사가 강함. 상세 논의는
2026-09-08 대화 참고.

---

**2026-09-09 결정 — 장르 명시 + 우선순위 확정**

유저가 장르를 명시 선언: **"3D 바인딩 오브 아이작 / 엔터 더 건전" 계열**(문 연결 공간형 방,
절차적 생성 층, 타이트한 트윈스틱식 이동/회피, 런 기반). `ProjectPlan.md` §2 Genre / `ClaudeSessionSummary.md`
§1 에 반영. 앞으로 작업 제안·범위 판단의 기준점.

이 선언으로 위 "추천 상위 3"에서 **2(경로탐색) ↔ 3(절차적 생성) 순서가 뒤바뀜** — 아이작=절차적
생성이 장르 정체성이라, 고정 3층이 "선언한 장르"와 현재 구현의 가장 큰 간극. 확정 순서:

| # | 항목 | 규모 | 이 위치인 이유 |
|---|---|---|---|
| 0 | §6 #6 마감(씬 페이드 + 3층 주파 검증) | — | 루프를 반쯤 검증된 채 두지 않기. 포트폴리오 항목 아님 |
| 1 | 자동화 테스트 + CI | 2~3세션 | 과설계 위험 0, §12 직접 답, 뒤 작업(2·3, 둘 다 subtle)의 안전망. 이 환경 Play Mode `frameCount` 고정 우회(GameCI=실제 헤드리스 에디터) |
| 2 | 결정론적 시드 절차적 층 생성 | 3~4세션 | 장르 정체성 간극. `IFloorLayoutProvider` 심 회수(소비자 0줄 수정 diff = §10 서사). per-system 시드 RNG 아키텍처. 층 수 3 고정, 레이아웃만 절차적, JSON provider는 고정시드 디버그 폴백 유지 |
| 3 | 적 경로탐색(그리드 A* 권장) + 방 장애물 | 4~5세션 | 절차적 방이 생긴 뒤 가치↑(장애물=생성기 일부). `EnemyChaseState` 이동만 교체. 프로파일러 계측(원래 #7) 흡수 |
| 4 | 입력 버퍼링 + 프레임레이트 독립성 | 1세션 | 건전식 타이트 조작. 게임필 우선 시 3과 순서 교체 가능 |
| 5 | 커스텀 에디터 툴링(SO 검증 패스) | 1세션 | 노력 대비 신호 큼, 절차적 생성 출력 검증과 궁합 |
| 6 | 세이브/로드(버전드 직렬화 + 런 이어하기) | 2~3세션 | 아무것도 언블록 안 함. 가장 나중 또는 마감 시 스킵 |

**아트 리소스 적용(무료 에셋 / AI 생성) 시점 — 권장: 2번(절차적 생성) 착수 직전 ~ 진행 중 병행.**
- **1번보다 뒤인 이유**: 테스트+CI는 순수 인프라라 아트와 무관. 아트를 먼저 넣으면 CI 빌드 파이프라인만
  무거워지고 얻는 게 없음.
- **6번까지 미루지 않는 이유**: (a) 절차적 생성·경로탐색은 회색 프리미티브 영상에선 임팩트가 약함 —
  포트폴리오 스크린샷/영상이 이 시점부터 찍히기 시작함. (b) 콜라이더·히트박스·스폰 오프셋·카메라
  구도는 아트 들어오면 어차피 재조정(플레이스홀더 지오메트리 노트, `ClaudeSessionSummary.md` §5) —
  절차적 생성/경로탐색을 회색 캡슐 기준으로 튜닝하면 아트 후 재작업. 아트를 2번과 겹치면 생성기
  튜닝을 처음부터 실제 크기·비주얼로 함. (c) Stage 3 최적화 After(Phase 6)와 Addressables(Phase 5)가
  아트에 게이팅되어 있어, 아트가 이르면 그 트랙들도 일찍 열림.
- **범위 주의**: 일관된 스타일 1세트(플레이어/적 3~5종/보스/방 타일/문/기본 VFX)로 한정. 팩 섞지 말 것
  (`ProjectPlan.md` Phase 4). AI 생성은 캐릭터 일러스트·UI엔 이미 씀(`Assets/Art/UI/`), 3D 메시는
  무료 에셋(Kenney, Synty 무료분, itch.io CC0)이 현실적. 스타일 통일이 최우선 판단 기준.

---

## 2026-09-04 (3)

### 진행 내용

**전체 플레이 루프 골격 — 타이틀 씬 신설 (로드맵 §6 #6, 사망 처리는 제외)**
- `Assets/Scenes/Title.unity` 신규 (Camera / EventSystem(InputSystemUIInputModule) / Canvas).
  배경(`title_bg`) + 로고(`title_logo`) + Start 버튼(`btn_start_normal/over/push` SpriteSwap).
  캐릭터 선택을 **인게임 씬에서 타이틀 씬으로 이동**: Start → `TitleGroup` 숨김 → 기존
  `CharacterSelectUI` 재사용해 근접/원거리 선택 → `GameFlow.StartRun(archetype)`.
- 타이틀 레이아웃: 로고/Start 버튼을 배경 오른쪽 절반(앵커 0.75)의 중상단/중하단에 배치.
  Start → **배경·로고·버튼 전부 비활성** + 캐릭터 선택 패널(불투명 검정)로 전환 —
  타이틀 위 오버레이가 아니라 "다음 UI로 넘어가는" 방식(임시 검정 배경, 추후 전용 화면).
- **UI 아트 리소스 적용(GPT 생성, `Assets/Art/UI/`)**: `ui_main_menu_btn.png`(3상태 1장)는
  Sprite Multiple + 자동 슬라이스(투명 경계) → `btn_gamestart_normal/over/pressed`로 리네임 →
  타이틀 StartButton SpriteSwap 재배선. 나머지는 개별 PNG로 받아 그대로 사용.
- **HUD #2·#3·#4·#1 구현** (인게임 씬, §6 "당장 넣어볼 UI"):
  - #3 크로스헤어 `CrosshairUI` — 마우스 추적(스크린→로컬), 플레이 중 OS 커서 숨김, 일시정지 시 복원. `hud_crosshair`.
  - #2 런 타이머 `RunTimerUI` — `StageManager.ElapsedTime` → `mm:ss`, HUD 상단(1-1 아래). `hud_timer_frame` + Text.
  - #4 체력 바 리스킨 — 기존 `HealthBarUI`(fill/텍스트 로직 그대로) 스프라이트만 교체: 프레임 `hud_hp_frame`, fill `hud_hp_gauge`(Filled/Horizontal). 코드 변경 없음.
  - #1 일시정지 메뉴 버튼 `PauseMenuButtons` — ESC 장비 패널(`EquipMenuController`가 이미 일시정지)에
    "Restart Run"/"Return to Title" 버튼 추가, `GameFlow.RetryRun`/`ReturnToTitle` 배선. 6장 SpriteSwap(`btn_restart_*`/`btn_return_title_*`).
  - #6 보스 HP 바 — **구현 완료**. `EnemySpawner`에 **제네릭** `event Action<HealthComponent, EnemyVariantType> EnemySpawned`
    추가(보스 분기 없음 — 스폰 사실만 보고, `SpawnRoom` 루프에서 적마다 1회 invoke). 신규 `BossHealthBarUI`가
    구독 → `variant == Boss`일 때만 자기를 켬(CanvasGroup alpha) → 그 HC의 `Health.Changed`로 fill 추적 →
    `Died`에 숨김. 보스방 진입 시 문이 잠기므로 "전투 중 이탈" 없음 → 숨김 트리거는 사망 하나로 충분.
    HUD에 `BossHealthBar`(`hud_boss_hp_frame` + `Fill`=`hud_hp_gauge` 재활용 Filled/Horizontal + "BOSS" 라벨).
    준보스 확장 경로는 `AI_Decision_Log.md` 2026-09-04 (4) 참고(지금은 미구현, YAGNI).
  - RunTimer는 보스 바(상단 중앙)와 겹쳐서 좌상단(HP 바 아래)으로 이동.

### 테스트 결과 — 보스 HP 바 (Play Mode, 보스방 RoomController.EnterRoom() 강제 호출 + HealthComponent.TakeDamage)
- [x] 런 시작 시 bar alpha 0(숨김).
- [x] 보스 스폰 → `EnemySpawned(hc, Boss)` 발화 → bar alpha 1, fill 1.0. (보스 전용 스폰 코드 없이 제네릭 이벤트로 반응 확인)
- [x] 보스 40% 피격 → fill 0.60, 여전히 표시. 55%/62% 피격도 fill 정확.
- [x] 보스 사망 → fill 0.00 → bar alpha 0(숨김).
- [x] 콘솔 에러 0.
- [ ] 프레임 내 게이지 인셋(아트에서 눈으로 측정), 바 크기/위치, "1-1" StageText와 보스 네임플레이트 겹침: 실플레이 육안 조정 이월.

### HUD 후속 조정
- RunTimer: 미니맵 UI 바로 아래(우상단)로 이동.
- 크로스헤어: 44→66 (150%).
- **플레이어 HP 게이지 압축 이슈 해결** — 원인: fill RectTransform이 5.8:1(208×36)로 잡혀 있어
  게이지 콘텐츠(실측 7.22:1)가 세로로 눌려 보였음. `hud_hp_frame`의 트랙 홈을 알파로 실측
  (x[0.193..0.927] y[0.398..0.653], 8.6:1) → `HealthBarFill` 앵커를 트랙에 정확히 맞춤.
  구조/코드는 그대로: `Image.type=Filled / Horizontal / Origin=Left` + `HealthBarUI`의
  `fillAmount = current/max`(무수정). 검증: HP 100/50/10% → fillAmount 1.00/0.50/0.10,
  fill 월드 rect(220×26, 8.6:1)는 HP 변화와 무관하게 불변(RectTransform 리사이즈·프레임 이동
  없음), 왼→오 방향으로 보이는 영역만 감소.
- **우하단 "노란/청록 사각형 + 0" = 버그 아님** — `EquipPanel`은 정상 비활성 확인. 그건
  `SkillCooldownSlot`(액티브 아이템 쿨다운, 노랑 스와치)/`DashCooldownSlot`(대시, 청록) 플레이스홀더
  아이콘. "0"은 준비 상태 표시로 0.5초 hold 후 사라짐(캡처가 그 창을 잡았던 것). 다만 폴리시로:
  `ActiveItemCooldownUI`에 빈 슬롯 처리 추가 — **장착 아이템이 없으면 슬롯 아이콘을 alpha 0.25로
  디밍 + 쿨다운 텍스트 숨김**(빈 능력 슬롯처럼 보이게). 장착 시 alpha 1.0 복귀. `DashCooldownSlot`은
  대시가 상시 가능이라 그대로. `emptySlotAlpha` 직렬화 필드(기본 0.25). 검증: 미장착 alpha 0.25 /
  EnhancedShot 장착 시 alpha 1.0 + 이름 표시.
- HP 게이지가 프레임 채널을 안 채우던 문제 조정: `hud_hp_gauge`는 자기 캔버스의 세로 ~41%
  (y 0.30~0.71)만 실제 빨간 콘텐츠라 fill rect를 채널에 딱 맞추면 위아래 어두운 틈이 남음.
  → fill RectTransform을 프레임 세로 거의 전체(y 0.02~0.98) × x 0.185~0.955로 확대해 콘텐츠
  (중앙 41%)가 채널을 꽉 채우게 함. `Filled/Horizontal/Left` + `fillAmount=current/max` 그대로,
  rect는 HP 변화와 무관하게 231×96 불변(검증 100/50/10% → 1.00/0.50/0.10).
- Title "Game Start" 버튼: 합본 `ui_main_menu_btn.png` 삭제됨 → 개별 3파일
  `btn_gamestart_normal/over/pressed.png`로 재배선(Sprite Single import + SpriteSwap).
- **캐릭터 선택: 텍스트 버튼 → 일러스트 버튼 + hover 흰색 워시**. `Assets/Art/UI/Character/`의
  `char_melee.png`/`char_ranged.png`(세로 3:4, 알파 없는 통짜 일러스트, 나중 캐릭터 아트 모음
  폴더). 신규 `HoverHighlight.cs`(opt-in, `Button`/`Selectable` 무수정): 자식으로 같은 스프라이트의
  흰색 오버레이(alpha 0, raycast off)를 두고 `IPointerEnterHandler`/`ExitHandler`로 alpha를
  0↔`hoverAlpha`(기본 0.35)로 `fadeDuration`(기본 0.15s, unscaled) 페이드 → 곱연산 틴트로는
  불가능한 "흰색으로 밝아짐". `Title.unity`의 `MeleeButton`/`RangedButton`: Image 스프라이트 교체,
  `Button.transition = None`, 자식 `Overlay` + `HoverHighlight` 배선, 기존 Label을 카드 하단
  캡션으로 이동("환락 개척자(근거리)" / "기억 개척자(원거리)"). 카드 360×480, 좌우 x±235.
  `CharacterSelectUI` 코드 무수정(`Button` 인스턴스 유지 → onClick·아키타입 배선 그대로).
  검증: 오버레이 hover-enter 시 alpha 0→0.35 6프레임 그라데이션, exit 시 0 복귀, 카드 클릭 →
  `GameFlow.PendingArchetype=MeleeArchetype` → `Prototype` 로드. 콘솔 에러 0.
- **hover가 안 밝아지던 버그 수정**: 오버레이 Image에 캐릭터 스프라이트를 넣었던 게 원인 —
  `Image.color`는 곱연산이라 흰색(1,1,1)×캐릭터스프라이트 = 캐릭터 그림 그대로(alpha만 0.35)라
  시각 변화 없음. 이벤트 디스패치는 정상이었음(레이캐스트 → MeleeButton 프론트 히트,
  `IPointerEnterHandler`가 `HoverHighlight`로 해석됨 확인). 일러스트가 투명 없는 통짜라 →
  오버레이를 **스프라이트 없는 순수 흰색 quad**로 교체하니 hover 시 카드가 흰색으로 워시됨(캡처 확인).
- 카드 1.5배 확대: 360×480 → **540×720**, 좌우 x±310, Prompt y 420으로.
- 인게임 일시정지(ESC) 패널의 `ConfirmButton`("확인") → `btn_close_*` 이미지 버튼으로 교체
  (`Assets/Art/UI/Common/btn_close_normal/over/pressed.png`, RGB·알파 없음, "Close" 텍스트 포함).
  RestartRunButton과 동일 방식(SpriteSwap), 320×80, y −150. 기존 `ConfirmLabel`("확인") 비활성.
  `ActiveItemEquipUI.confirmButton`은 같은 `Button` 인스턴스라 `HandleConfirm` → `_onConfirmed`
  (`EquipMenuController.Close`) 배선 그대로 → 클릭 시 패널 닫힘·게임 복귀 동작 불변. 검증:
  클릭 → `EquipPanel.activeInHierarchy` True→False. 코드 무수정.
- **일시정지(ESC) 패널 버튼 레이아웃 재배치** — 화면을 세로로 갈라 오른쪽 절반, 상단 아이템
  표시 영역(장비 슬롯 3개)과 겹치지 않게 버튼 3개를 **오른쪽 영역 하단에 정렬**하고 세로 간격을
  이전 대비 ~50% 축소. `Canvas/EquipPanel/ItemsPanel`(우측 절반 컨테이너, anchor min(0.5,0)
  max(1,1), rect 665×748) 자식 RectTransform만 조정(코드 무수정):
  - `RestartRunButton` (0,-90 / 300×100) — 위
  - `ReturnToTitleButton` (0,-200 / 300×100) — 중간
  - `ConfirmButton`(Close) (0,-300 / 300×75) — 아래
  - 장비 슬롯은 상단 그대로(`EquipSlot3` 0,201 / `EquipSlot2` 0,268 / `EquipSlot1` 0,335).
  - 전부 anchor·pivot (0.5,0.5). 순서 위→아래 Restart / Title / Close.
  세로 간격 2차 축소: 이전 118px 등간격(여백 ~18px) → 1차 중심간 100~110px(여백 ~10~12px) →
  **2차 여백 ~3px**(중심간 Close→Title 103, Title→Restart 103; Restart 0,-106 / Title 0,-209 /
  Close 0,-300). 여백 기준 1차 대비 다시 ~70% 축소. 검증(에디터, EquipPanel 임시 활성 후
  GetWorldCorners): 버튼 3개 x[848..1148] 중앙 x≈998(1330 폭 화면 우측 절반 중심), y —
  Restart[218..318] / Title[115..215] / Close[37..112], 인접 여백 3px, Close 하단 마진 ~37px.
  장비 슬롯(EquipSlot3 y[547..603])과 Restart 상단(y 318) 사이 ~229px 여유로 아이템 영역과 분리.
  카메라 캡처로 3버튼 세로 열 + Close 버튼이 새 알파 배경 아트(투명 프레임, Restart/Title과 통일)로
  교체된 것 확인. (이전 턴 메모였던 Close 검정 사각 배경 문제는 유저가 알파 버전으로 재출력해 해소.)
  이후 유저 요청으로 3버튼 간격 한 번 더 축소: 여백 ~10px → **~3px**(Restart 0,-106 / Title
  0,-209 / Close 0,-300, 월드 y Restart[218..318]/Title[115..215]/Close[37..112]).
- **타이틀 Start 버튼 홀드 → 캐릭터 선택이 즉시 클릭되던 버그 수정** (A안: 릴리즈까지 arm 지연).
  원인: `TitleScreenController.HandleStartClicked`가 `onClick`(포인터 업)에서 **같은 프레임에**
  타이틀 그룹을 끄고 `CharacterSelectUI.Show()`로 `MeleeButton`/`RangedButton`을 커서 바로 아래에
  활성화 → Start를 누른 그 제스처(홀드/리바운스)의 다운 상태가 방금 나타난 버튼에서 클릭으로
  완성 → `HandleChosen` → `GameFlow.StartRun` 즉시 실행. (`Button.transition = None`이라 눌림
  비주얼이 없어 "그냥 바로 시작"으로 보임.)
  수정(`CharacterSelectUI.cs`, `TitleScreenController` 무수정): `Show()`에서 패널은 켜되
  `meleeButton/rangedButton.interactable = false` + `EventSystem.SetSelectedGameObject(null)`,
  코루틴 `ArmAfterInputReleased`가 `한 프레임 → Mouse.leftButton.isPressed && 키보드
  Enter/NumpadEnter/Space 안 눌릴 때까지 대기 → 한 프레임 → interactable = true`. `HandleChosen`은
  arm 코루틴을 정리. hover 흰색 워시는 `interactable`과 무관하게 유지(클릭만 게이트).
  검증(Play Mode, `Show(null)` 직접 호출 + `EditorApplication.Step`): Show 직후 두 버튼
  interactable=False, 입력 미보유 상태로 6프레임 후 True 복귀. 컴파일 에러 0.

### 테스트 결과 (Play Mode, RunCommand + EditorApplication.Step)
- [x] 타이틀 StartButton: `btn_gamestart_normal` 표시, SpriteSwap over/pressed 배선 확인(캡처).
- [x] 인게임 HUD: Crosshair 생성(커서 숨김 동작 확인, 실제 추적은 마우스 필요), RunTimer `00:00` 표시,
  HP 바 프레임/게이지 스프라이트 적용 + `100/100` 유지, ESC 패널에 Restart Run/Return to Title 버튼
  표시 + `PauseMenuButtons` 참조 배선 확인(캡처).
- [x] 콘솔 에러 0.
- [ ] 실플레이 육안: 크로스헤어 마우스 추적, 타이머 카운트업, 저체력 시 게이지 감소, ESC 버튼 클릭 →
  씬 전환, HUD 레이아웃(HP 게이지 인셋/일시정지 버튼 위치는 placeholder 수치) 미세조정: 이월.
- `Assets/Scripts/Core/GameFlow.cs` 신규 (정적): 씬 이름 상수 2개(`Title`/`Prototype`) +
  `PendingArchetype`(씬 로드를 넘겨 전달되는 유일한 상태, SO 참조라 안전) +
  `StartRun`/`RetryRun`/`ReturnToTitle`. 로드 전 `PauseState.ForceResume()`로 timeScale 정리.
- `PauseState.ForceResume()` 추가 — 씬 teardown용(리로드가 owner의 ReleasePause를 안 부르고
  timeScale도 안 되돌리므로).
- `CharacterSelectController`(Player): UI 표시/입력 잠금 로직 제거 → `Start()`에서
  `GameFlow.PendingArchetype ?? fallbackArchetype`로 무기 장착 + 입력 활성화. `fallbackArchetype`
  (에디터에서 인게임 씬 직접 실행용, MeleeArchetype) 직렬화 필드 추가, `uiManager` 참조 제거.
- `UIManager`: `characterSelectUI` 필드 + `ShowCharacterSelect` 제거(타이틀로 이동).
- `FloorCompleteUI`: 마지막 층 클리어 화면에 `toTitleButton` 추가(retry와 함께 노출).
  retry → `GameFlow.RetryRun()`(아키타입 유지, 씬 리로드), toTitle → `GameFlow.ReturnToTitle()`
  (아키타입 클리어, 타이틀 씬). 라벨 "Retry"→"다시하기", 신규 "타이틀로".
- Build Settings: `Title`(index 0, 시작 씬) + `Prototype`(index 1).
- 결정4: Player `HealthComponent.destroyOnDeath` → false (사망 시 소프트락 방어. 정식 Game Over는
  이연). 결정5: 씬 전환 v1 하드컷(비동기+로딩화면은 Phase 5).
- Title art: `Assets/Art/UI/Title/` (bg/logo + 버튼 3상태). Sprite import, mipmap off,
  bg maxSize 2048 / 버튼 1024.

### 테스트 결과 (Play Mode, RunCommand + EditorApplication.Step — 에디터 비포커스라 프레임 수동 스텝)
- [x] Title: 로드 시 TitleGroup 표시 / CharacterSelectPanel Awake로 숨김. Start 클릭 →
  TitleGroup 숨김 + CharacterSelectPanel 표시.
- [x] Melee/Ranged 클릭 → `GameFlow.PendingArchetype` 설정 → `Prototype` 씬 로드. 인게임에서
  Player 존재, 입력 활성화, 무기 장착(선택한 아키타입), CharacterSelectPanel 없음, StageManager
  1층 6방 빌드, HUD `1-1`.
- [x] 마지막 층(3층) 클리어 화면: `RetryButton`("다시하기") + `ToTitleButton`("타이틀로") 둘 다
  활성, timeScale=0.
- [x] "타이틀로" → timeScale=1, PendingArchetype null, `Title` 씬 로드, 초기 상태 복귀.
- [x] "다시하기" → timeScale=1, PendingArchetype 유지(Ranged), `Prototype` 리로드, 1층 6방,
  HUD `1-1`, 입력 활성 → **루프 3방향 전환 전부 확인**.
- [x] 콘솔 에러 0. 한글(로고/프롬프트) 빌트인 폰트로 정상 렌더(scene view 캡처 확인).
- [ ] 실플레이로 1→2→3층 실제 전투 주파 후 클리어 화면 도달: 이월.
- [ ] 타이틀 UI 레이아웃 육안 미세조정(로고/버튼 위치는 placeholder 수치): 이월.

### 이슈/메모
- 사망 처리(Game Over 화면/사망 연출) 미구현 — 이연. 지금은 죽어도 HP 0인 채 계속 플레이.
- 씬 rename(`Prototype`→`Gameplay`)은 게임 구색 갖춰진 뒤로 보류(결정3).

### 다음 작업
- **최우선: 실플레이 1주파** — Title→3층 클리어→루프까지 눈으로 확정(오늘 배선은 전부
  RunCommand + `EditorApplication.Step` 간접 검증만 됨).
- 이후 #6 나머지(사망 처리/Game Over 흐름) 또는 #7(Profiler 최적화).
- 플레이스홀더 아트 상대 UI 레이아웃 수치는 실제 아트 들어오기 전까지 동결, 자잘한 조정은 배치 처리.

---

## 2026-09-04 (2)

### 진행 내용

**1~3층 방 데이터 JSON 외부화 (로드맵 §6 #2)**
- `RoomDataAsset`(SO) 29개 + 씬 `StageManager.floors` 배선 → `Assets/StreamingAssets/Balance/
  Stage/Floors/Floor{1,2,3}.json` 3개 파일로 이관. 현재 authoring 값 그대로 익스포트 스크립트로
  덤프 후 한글 `_guide`/`_fieldGuide` 주석 추가(필드 상세는 Floor1.json에만, 2·3은 참조).
- 신규 `GameFloors`(정적 로더, `GameBalance` 패턴): 지연 로드 + `RuntimeInitializeOnLoadMethod`
  세션 리셋, enum은 이름 문자열 + `Enum.TryParse`/`IsDefined`, `_guide` 키 무시.
  `Floor1`부터 없어질 때까지 열거해 층 수 결정, 마지막 층은 파일 위치로 판정(`isLastFloor`
  플래그는 불일치 시 경고만). 파일 누락=갭에서 중단, 파싱 실패=그 층만 코드 내장 비상용
  3방 층(Start-Combat-Boss)으로 대체 + `IsCorrupted` 플래그.
- `JsonFloorLayoutProvider : IFloorLayoutProvider`(구 `ScriptableObjectFloorLayoutProvider` 대체),
  `FloorLayout`(런타임 층 데이터). `StageManager`: `floors` 필드 제거, `GameFloors.Floors` 사용,
  `FloorDataCorrupted` 이벤트 추가, `FloorDefinition` 클래스 삭제.
- 삭제: `RoomDataAsset.cs`, `ScriptableObjectFloorLayoutProvider.cs`, `Assets/Data/Rooms/*.asset` 29개.
- 결정 근거(파일 구성/마지막 층 판정/구 에셋/손상 폴백)는 `AI_Decision_Log.md` 2026-09-04 (2).

**시스템 알림 팝업 UI 신규**
- `SystemNotificationUI`: 전체 화면 딤 + 메시지 박스 + 확인 버튼 하나. CanvasGroup 방식
  (자기-비활성 패널 함정 회피), 메시지 큐 보유. 씬에 `Canvas/SystemNotificationPanel` 추가,
  `UIManager`에 배선.
- 층 JSON 손상 시 그 층 진입 순간 `StageManager.FloorDataCorrupted` → `UIManager` →
  팝업에 "1-2 층 데이터(Floor2.json)가 손상되었습니다. 파일을 수정해 주세요." 표시.

**HUD 층 표시를 스테이지-층 포맷으로**
- 중상단 텍스트 `StageN` → `x-y` (x=스테이지 인덱스, y=층 인덱스). 현재 스테이지 1개뿐이라
  `StageManager.stageNumber`(직렬화 필드, 기본 1)로 두고 `1-1`/`1-2`/`1-3` 표시.

### 테스트 결과 (Unity RunCommand 구조 검증 — 실플레이 주파는 이월)
- [x] `GameFloors.Floors` = 3층. 방 수 6/10/13, 타입 분포·연결 수(10/20/28)·적 수(M/R/B
  6·5·1 / 20·17·1 / 29·25·1 = 3층 전투적 54+보스1)가 이관 전 에셋과 일치.
- [x] `StageManager.AdvanceToNextFloor()` 1→2→3 정상, HUD `1-1`→`1-2`→`1-3`, 3층에서
  `IsLastFloor=true`, `FloorExit` 생성. 마지막 층에서 재호출 시 경고 + no-op.
- [x] Floor2.json의 `roomType`을 잘못된 값으로 바꿔 재생성 → Floor2만 비상용 3방 층
  (`corrupted=True`, `startRoomId=2001`), Floor1·3 무사. 2층 진입 시 팝업 표시
  (`canvasGroup.alpha=1`, 메시지 문구 정확), 확인 버튼 클릭 시 `alpha=0`으로 사라짐. 콘솔에
  `GameFloors: ... unknown roomType 'Broken_TEST'` 에러 1건(의도된 안내).
- [x] 파일 복원 후 클린 로드 시 콘솔 에러·경고 0.
- [ ] 실제 플레이로 1→2→3층 전투 주파 + 미니맵/문/스폰 육안 확인: 이월.
- [ ] MessageText의 한글이 빌트인 폰트에서 정상 렌더되는지 육안 확인: 이월(Windows 폰트
  폴백으로 대개 정상이나 미확인).

### 이슈/메모
- `AdvanceToNextFloor()`를 RunCommand로 연속 호출 시 이전 층 방 GameObject가 남아 보이는 건
  `Destroy()` 지연 + 에디터 비포커스로 프레임이 안 도는 테스트 환경 아티팩트(teardown 코드
  자체는 미변경). 실플레이에선 매 프레임 flush됨.

### 다음 작업
- 로드맵 §6 #3 대시 무적 판정 확인, #4 대시 아이콘 UI(이미 완료), #5~ 이어서.

---

## 2026-09-04

### 진행 내용

**스테이지3 맵 제작 (로드맵 §6 #1)**
- 방 데이터 에셋 13개 신규: `Assets/Data/Rooms/Room3_*.asset` (id 301~313 — Start / CombatA~J /
  Item / Boss). 층2와 동일한 `RoomDataAsset` 포맷·authoring 방식(방/문 메커니즘 변경 없음).
- 맵 구조는 바인딩 오브 아이작에서 차용: 시작방 중앙(3방향 문), 보스방 = 시작에서 가장 먼
  단문(單門) 데드엔드, 아이템방 = CombatF(307) 뒤 데드엔드 스퍼, 본체 루프 2개
  (`301-306-309-308-301`, `301-302-303-304-305-307-310-308-301`), 선택 전투 데드엔드(311).
  전투 적 총 54마리 + 보스 1.
- `StageManager.floors`에 3번째 `FloorDefinition` 배선(`SerializedObject`로 인메모리 수정 후
  `SaveScene`), `startRoomId: 301`. `IsLastFloor`가 `floors.Length` 기반 데이터 구동이라
  **코드 수정 없이** 층2가 카운트다운→자동 전환, 층3이 Retry 층이 됨.
- 구조 검증 스크립트: id·좌표 중복 0, 전 연결 상호대응·1칸 인접, 시작방 BFS 13/13 도달,
  데드엔드 3개.

**대시 리워크 + 밸런스 JSON 외부화 (사용자 요청)**
- 대시 속도를 이동속도 연동으로 변경: `dashSpeed = PlayerMovement.CurrentMoveSpeed ×
  dashSpeedMultiplier`, `dashDuration` 동안 유지. 기본 스탯에서 15 u/s·3유닛(기존값 보존),
  이동속도 패시브 먹으면 대시도 같이 빨라짐. 무적창(지속시간)은 고정.
- `PlayerMovement.CurrentMoveSpeed` 프로퍼티 신설(base + `MoveSpeedBonus`, 이동속도 단일
  소스). `FixedUpdate`도 이걸 사용.
- `dashSpeedMultiplier`(3.0), `dashDuration`(0.2)를 `PlayerBaseStatsData`/`GameBalance` 내장
  기본값/`PlayerBaseStats.json`(+`_fieldGuide`)로 외부화. `PlayerDodgeRoll`의
  `dodgeRollDistance`/`dodgeRollDuration` `[SerializeField]` 및 `Player.prefab` 직렬화 값
  제거(`cooldown`은 유지).
- 대시 구동을 `MovePosition` → `linearVelocity`로 전환(아래 벽 관통 버그 참고). `EndDodgeRoll()`
  추출(타임아웃·취소 공용, 종료 시 velocity 0).

**F2 디버그 — 적 즉사 데미지 토글 (사용자 요청)**
- `DebugDamage` 정적 클래스 신규: `EnemyOneShot` bool, `OneShotAmount = 99999`,
  `[RuntimeInitializeOnLoadMethod]`로 세션마다 리셋.
- `HealthComponent.TakeDamage` 맨 앞:
  `if (DebugDamage.EnemyOneShot && amount > 0 && !CompareTag("Player")) amount = 99999;` —
  최종값만 교체(스탯 불변), 로그·`AnyDamageTaken`·`Health` 전부 교체값 사용.
  `!CompareTag("Player")` 사용 — 일반 적은 Untagged, 보스만 `Enemy` 태그라
  `CompareTag("Enemy")`로는 보스만 잡힘. F2 끄면 그동안 먹은 공격력 픽업 반영된 정상
  데미지로 복귀(스탯을 안 건드렸으므로 복원할 게 없음).
- `DebugOverlayActions.inputactions`에 `ToggleEnemyOneShot` → `<Keyboard>/f2`.
  `DebugOverlayController`가 토글 처리 + 콘솔 로그 + FPS 텍스트 왼쪽에 런타임 생성 "1-SHOT"
  인디케이터(노랑, F1 오버레이와 독립적으로 F2 상태만 반영).

**스테이지3 플레이테스트에서 발견한 버그 4건 수정**

1. **재빌드된 층(2·3층)의 벽이 플레이어를 안 막음.**
   - 원인: 프로젝트 설정 `Physics.autoSyncTransforms = false` + `AdvanceToNextFloor()`가 Floor
     Complete 암전(`timeScale == 0`) 중 실행 → FixedUpdate 없음 → 자동 물리 싱크 없음 → 새
     층 콜라이더가 플레이어가 움직이기 전까지 물리 씬에 미반영.
   - 수정: `StageManager.BuildFloor()` 끝에 `Physics.SyncTransforms()`.

2. **방 전환 후 조작 없이 특정 방향으로 등속 드리프트 (대시로 문 통과 시 심함).**
   - 초기 가설(isKinematic 토글 넘어 잔류 속도 복원)은 격리 테스트 + 사용자가 넣은 진단
     로그(`mag` 항상 0)로 반증 → 그 대응(속도 0으로) 되돌림.
   - 실제 원인: `RoomTransitionController`가 `player.position = arrivalPoint`(Transform만)로
     순간이동 → autoSync off라 `Rigidbody.position`이 낡은 값 → `PlayerMovement.FixedUpdate`가
     매 틱 `_rigidbody.position`을 읽어 그쪽으로 `MovePosition` → 입구에서 낡은 위치(들어온
     방향 반대)로 끌려감. 대시는 `PlayerDodgeRoll`이 팬 중에도 kinematic 바디를 몰아 간격을
     더 벌림.
   - 수정: `TransitionRoutine`에서 `player.position = arrivalPoint;` 직후
     `Physics.SyncTransforms()`. + 전환 시작 시 진행 중인 구르기 취소
     (`PlayerDodgeRoll.CancelDodgeRoll()` 신설).

3. **Floor Exit를 대시로 진입 시 다음 층 시작방에서 조작 불가·드리프트.**
   - 원인: `PlayerDodgeRoll.FixedUpdate`(구르기 종료 처리 지점)가 Floor Complete
     일시정지(`timeScale=0`)로 얼어붙음 → `timeScale=1` 복구 후 새 층에서 낡은 방향으로 재개.
   - 수정: `RoomFlowManager.HandleFloorCompleted()`가 `ShowFloorComplete`(=일시정지) 전에
     `CancelDodgeRoll()` 호출.

4. **벽에 붙어 대시(홀드)하면 벽 관통 / 스테이지3에서 이동속도 높으면 걷기만 해도 벽 관통.**
   - 원인: `MovePosition`(및 재적용되는 `linearVelocity`)을 벽으로 물리 스텝당 ~0.2유닛 초과로
     명령하면 솔버 depenetration을 앞질러 캡슐이 벽을 통과. 걷기는 이동속도 패시브로
     `CurrentMoveSpeed`가 ~10을 넘어야(≈ 스테이지3) 발현. 격리 테스트로 확인: 솔버
     iteration·충돌검출 모드·월드 원점 거리 전부 무관, 순수 스텝 이동거리 문제.
   - 수정: 공용 `WallClamp.ClampDirection(capsule, dir, moveThisStep)` 신설 — 캡슐을 **반지름만큼
     뒤로 물린 지점에서** `CapsuleCast`(밀착 시작도 감지되도록 — 정위치 캐스트는 초기 겹침을
     무시함), 히트 시 `ProjectOnPlane`로 벽 방향 성분 제거(정면 = 정지, 스침 = 슬라이드).
     `PlayerMovement`(걷기)·`PlayerDodgeRoll`(대시) 둘 다 사용. 대시는 `linearVelocity` 구동으로
     전환. `Player.prefab` Rigidbody `CollisionDetection` Discrete → ContinuousDynamic.
   - 검토·기각한 대안: 벽 콜라이더를 보이는 것보다 두껍게(대시 속도 가변이라 "충분한 두께"
     기준이 계속 움직임 + 문 지오메트리 재조정 비용 + 근본 원인 미해결),
     `m_DefaultSolverVelocityIterations` 상향(격리 테스트 결과 효과 없음). 상세:
     `AI_Decision_Log.md` 2026-09-04.
   - 잔여 한계: 벽을 얕은 각도로 스치며 파고드는 이동을 매우 높은 이동속도(일반 런의 픽업 수를
     한참 넘는)에서 하면 여전히 조금 새어나감. 완전 해결은 플레이어 이동을 `MovePosition`에서
     제대로 된 스윕 캐릭터 이동으로 교체해야 함 — 아트 통합 시로 이월.

**자잘한 수정 2건**
- **픽업 알림이 층 전환을 넘어 표시됨.** `PickupNotificationUI.HideAfterDelay`가 스케일 타임
  `WaitForSeconds`라 Floor Complete 일시정지 중 얼어붙어 다음 층 페이드인에도 라벨이 떠 있음.
  → `HideImmediate()` 신설, `RoomFlowManager.HandleFloorCompleted()`에서 대시 취소와 같이 호출.
- **문 바로 앞에 떨어진 드랍템을 못 주움.** 문 통과 감지 트리거가 `WallThickness * 3`(3유닛)
  깊이라 보이는 문(1유닛)보다 방 안쪽으로 넓음 → 그 안에 떨어진 아이템을 주우러 가면 문
  트리거가 먼저 밟혀 다음 방으로 이동. → `ItemDropManager.NudgeClearOfDoors()`:
  `OverlapSphereNonAlloc`로 근처 `DoorController` 찾아 드랍 좌표를 문 안쪽 방향(`EntryPoint`
  쪽)으로 `DoorClearDistance`(3유닛)만큼 밀어냄. `SpawnPickup`의 `Instantiate` **전에** 적용 →
  원래 자리에 잠깐 보였다가 밀리는 것 없이 최종 위치에 바로 생성. 이미 여유 있는 드랍(보장
  보상 포함)은 no-op. 문 트리거 자체는 빠른 대시 통과 신뢰성 때문에 그대로 둠. 대안 비교:
  `AI_Decision_Log.md` 2026-09-04.

### 테스트 결과
- [x] 스테이지3 방 13개 임포트 + 구조 검증 통과(중복 0, 상호대응·인접, BFS 13/13)
- [x] 전 스크립트 컴파일 에러·경고 0 (`Unity_ReadConsole`)
- [x] `GameBalance.Player.dashSpeedMultiplier=3`/`dashDuration=0.2` JSON 파싱, 기본 대시
      15 u/s·3유닛(현재값 보존) 확인
- [x] `DebugDamage` 토글·`OneShotAmount=99999`, F2 액션 `<Keyboard>/f2` 바인딩, 비플레이어
      `HealthComponent`에 즉사 적용·Player 태그는 스킵(100→93) 확인
- [x] `WallClamp` 격리 시뮬레이션: 정면 걷기 벽 관통 ms 5~100 전부 막힘
- [x] `NudgeClearOfDoors` 라이브 문 검증: 문 평면~2.9유닛 드랍 → 3유닛으로 이동(트리거 밖),
      여유 있는 드랍은 불변
- [x] `RoomTransitionController`/`RoomFlowManager`의 `PlayerDodgeRoll`·`PickupNotificationUI`
      참조가 씬에서 정상 해석됨, Player Rigidbody `collisionDetectionMode = ContinuousDynamic`
      프리팹 전파 확인
- [ ] (사용자 실플레이 필요) 층1→2→3 자동 전환 시퀀스, 스테이지3 정면 벽 충돌, 대시로
      문/Floor Exit 통과 회귀, F2 인게임 동작, 픽업 알림·문 앞 드랍 획득, 대시 감각(3유닛)·
      이동속도 패시브 후 대시 체감
- 자동화 Play가 `Time.frameCount` 2에서 고정되는 한계로 코루틴(방/층 전환)·Update 기반
  동작은 이번에도 자동 검증 불가 — 정적/기하 검증까지만 자동으로 하고 나머지는 실플레이로.

### 이슈/메모
- 이번 세션 버그 4건 중 3건(재빌드 층 벽 / 전환 드리프트 / Floor Exit 대시)의 공통 뿌리는
  `Physics.autoSyncTransforms = false` + `timeScale=0` 중 씬 재빌드/순간이동. 전역으로 켜는
  안은 사용자가 필요성을 못 느껴 보류 — 대신 `SyncTransforms()` 호출을 지점마다 삽입. 향후
  같은 계열 버그가 또 나오면 전역 설정 재고. 상세: `AI_Decision_Log.md` 2026-09-04.
- **플레이스홀더 지오메트리 — 아트 통합(Phase 4) 시 함께 처리할 충돌 이슈** (지금은 큐브/캡슐
  기준이라 그때 콜라이더·히트박스·스폰 오프셋을 어차피 재조정해야 함):
  1. `WallClamp` 잔여: 벽 얕은 각도 스침 + 매우 높은 이동속도에서 관통 (근본 해결 =
     MovePosition 이동 → 스윕 캐릭터 이동 교체)
  2. 문 통과 트리거가 보이는 문보다 깊음 (드랍 nudge로 우회 중, 근본은 지오메트리)
  3. 벽에 밀착한 두 오브젝트 사이 근접 발사 시 발사체가 벽 판정으로 소멸(데미지 안 들어감)
- 자잘 이슈에 세션 시간이 많이 쓰인 세션 — 사용자 판단으로 위 3건은 아트 리소스 적용 후
  일괄 처리로 스코프 컷.

### 다음 작업
- 위 [ ] 실플레이 검증 일괄
- 로드맵 §6: #1(3층 제작) 완료 → #2(1~3층 방 데이터 JSON 외부화) / #3(대시 무적 판정 확인) /
  #4(대시 아이콘·쿨다운 UI) 순서로
- 씬 분리(`MainMenu` + `Gameplay`)는 3층까지 다 나온 뒤로 (2026-09-03 계획 유지)
- 미커밋 상태: 스크립트 14개 수정 + 신규 2개(`WallClamp`/`DebugDamage`) + `Player.prefab` +
  `DebugOverlayActions.inputactions` + `PlayerBaseStats.json` + `Prototype.unity` + 스테이지3
  방 에셋 26개 — 커밋 구성 결정 필요

---

## 2026-09-03

### 진행 내용
- **구현 없음 — 씬 분리 설계 검토/결정만 진행.** 지금까지 단일 씬(`Prototype.unity`) 하나로
  작업해 온 구조를 여러 씬으로 나눌지 검토하고, **`MainMenu` + `Gameplay` 2씬 구성(안 B)**
  으로 결정. 검토한 대안 3개와 선택 근거는 `AI_Decision_Log.md` 2026-09-03 항목 참조.
- 사용자 지시로 **캐릭터 선택 UI는 신설할 `MainMenu` 씬으로 이동**하는 것까지 확정.
- 결정에 앞서 씬/프리팹 파일을 직접 파싱해 현재 오브젝트·컴포넌트 구조를 확인함(아래 이슈/메모).

### 테스트 결과
- 해당 없음 — 코드/씬 변경 없음.

### 이슈/메모
- 씬 안의 프리팹 인스턴스는 `Player`, `Boss_Basic` 둘뿐이고 나머지는 전부 씬 직속 오브젝트.
- **`PlayerPassiveStats` / `EquipMenuController` / `PickupNotificationUI`가 `Player.prefab`
  본체에는 없고, 씬의 Player 인스턴스에 "추가된 컴포넌트" 오버라이드로만 붙어 있음**
  (`PickupNotificationCanvas` 자식도 마찬가지). 씬 분리를 막지는 않지만(Player는 `Gameplay`
  씬에 그대로 남음), 씬이 늘어나는 시점에 프리팹 본체로 올리는 게 맞음 — **별도 작업으로 분리**.
- **`PauseState`에 세션 리셋 훅이 없음.** `_owners`(static `HashSet`)는 씬 로드로 초기화되지
  않으므로, 파괴된 오브젝트가 오너로 남으면 `Count`가 0이 되지 않아 `Time.timeScale`이 0으로
  고착될 수 있음. 지금은 `FloorCompleteUI`가 로드 직전 `ReleasePause()`를 부르는 단일 경로뿐이라
  발현되지 않지만, 씬 전환 경로가 늘어나면 위험. `GameBalance`의 `[RuntimeInitializeOnLoadMethod]`
  리셋 패턴을 그대로 적용하면 됨.
- **`CharacterSelectUI.Awake()`가 `panel.SetActive(false)` 방식** — 2026-08-24 / 2026-09-02 (2)에
  두 번 터진 "자기-비활성 패널 함정"과 같은 구조. `MainMenu` 씬으로 옮기면서 `FloorCompleteUI`와
  동일한 `CanvasGroup` 방식으로 함께 전환할 것.
- **미결정 2건** (작업 착수 전 사용자 확인 필요):
  1. 결과 화면(`FloorCompleteUI`)을 `Gameplay`에 남길지 `MainMenu`로 옮길지 — **권장: Gameplay
     유지**. 살아있는 `StageManager` / `PlayerPassiveStats` / `PlayerActiveItemInventory` 참조로
     통계를 만들기 때문에, 옮기면 통계 전달 데이터를 새로 설계해야 함. 아래 계획은 이 가정 기준.
  2. 게임오버(플레이어 사망) 흐름을 이번 범위에 포함할지 — 현재 사망 시 처리 자체가 정의돼 있지 않음.

### 다음 작업 — 씬 분리 (안 B: `MainMenu` + `Gameplay`)

우선순위상 이 작업을 먼저 진행하고, 2026-09-02 (3)의 "다음 작업" 목록(3층 맵 제작, 방 데이터 JSON
외부화, 대시 무적 확인, 대시 쿨다운 UI, Stage 2.5 잔여 항목)은 그 뒤에 이어서 진행한다.

**0단계 — 선행**
- 현재 미커밋 변경(스크립트/데이터 30개 이상, `Prototype.unity` 포함)을 **먼저 커밋**한다.
  씬 rename/분리 diff와 섞이면 되돌리기가 어려워짐.

**1단계 — 새 스크립트 3종**
- `RunContext` (static, `Assets/Scripts/Core/`): `SelectedArchetype`(`CharacterArchetypeData`).
  `MainMenu` → `Gameplay`로 넘길 값은 현재 이것 하나뿐. `GameBalance`와 동일하게
  `[RuntimeInitializeOnLoadMethod]`로 세션마다 리셋.
- `SceneNames` + `SceneLoader` (static): 씬 이름 상수와 `LoadMainMenu()` / `LoadGameplay()`.
  로드 직전 `Time.timeScale` 복구를 한 곳에서 보장하고, `buildIndex` 대신 이름으로 로드
  (씬이 늘어나면 인덱스가 흔들림).
- `MainMenuController` (`MainMenu` 씬): 타이틀 → Play → 캐릭터 선택 표시 →
  `RunContext.SelectedArchetype` 설정 → `SceneManager.LoadSceneAsync(Gameplay)`.

**2단계 — 기존 스크립트 수정 5종**

| 파일 | 변경 |
|---|---|
| `CharacterSelectController.cs` | `UIManager` 참조 제거. `Start()`에서 UI를 띄우고 기다리는 대신 `RunContext.SelectedArchetype`을 읽어 즉시 `EquipWeapon` + 입력 활성. **컨텍스트가 비어 있으면(= `Gameplay` 씬만 단독 Play) 인스펙터 기본 아키타입으로 fallback** — 개발 중 Gameplay 씬만 눌러 테스트하는 흐름을 유지하기 위해 필수. |
| `UIManager.cs` | `characterSelectUI` 필드와 `ShowCharacterSelect()` 제거(`MainMenu` 씬으로 이동). |
| `CharacterSelectUI.cs` | `MainMenu` 씬으로 이동. `panel.SetActive()` 토글 → **`CanvasGroup` 토글로 전환**, 오브젝트는 항상 active 유지(자기-비활성 패널 함정 재발 방지). |
| `FloorCompleteUI.cs` | 마지막 층의 `SceneManager.LoadScene(buildIndex)` → `SceneLoader.LoadGameplay()`. **"타이틀로" 버튼 추가** → `SceneLoader.LoadMainMenu()`. 두 경로 모두 `PauseState.ReleasePause()` 선행. 층 전환 카운트다운/`AutoAdvanceRequested` 경로는 그대로 유지(층 전환은 여전히 씬 안에서 제자리 재빌드). |
| `PauseState.cs` | `[RuntimeInitializeOnLoadMethod]` 리셋 추가(`_owners.Clear()` + `timeScale = 1f`). |

**3단계 — 씬 작업 (Unity 에디터 / MCP)**
1. `Prototype.unity` → `Gameplay.unity` rename (git mv로 히스토리 유지).
2. `CharacterSelectPanel`을 프리팹으로 추출 → `Assets/Prefabs/UI/CharacterSelectPanel.prefab`
   (씬 간 오브젝트 이동은 프리팹 경유가 참조 유실이 가장 적음).
3. 새 씬 `MainMenu.unity` 생성: Camera + EventSystem + Canvas(TitlePanel + CharacterSelectPanel
   인스턴스) + `MainMenuController`.
4. `Gameplay.unity`에서 `CharacterSelectPanel` 제거, `UIManager` 인스펙터 정리,
   `ClearOverlayPanel`에 "타이틀로" 버튼 추가 후 배선.
5. Build Settings: `0 = MainMenu`, `1 = Gameplay`.

**4단계 — 검증 (확인할 증거)**
- [ ] 컴파일 클린 (`Unity_ReadConsole`로 확인)
- [ ] `MainMenu`에서 Play → 캐릭터 선택 → `Gameplay` 진입 → 선택한 무기가 실제로 장착됨
- [ ] **`Gameplay` 씬 단독 Play** → fallback 아키타입으로 정상 진행(개발 편의 회귀 확인)
- [ ] 층1 클리어 → 카운트다운 → 페이드 → 층2 시퀀스가 씬 분리 후에도 그대로 동작
- [ ] 마지막 층 클리어 → 결과 화면 → ① 재시작 ② 타이틀 복귀, 각각 진입 직후 `Time.timeScale == 1`
- [ ] **ESC 장비 메뉴를 연 채로 타이틀 복귀** → 재진입 시 게임이 멈춰 있지 않음(`PauseState` 리셋 검증)
- [ ] 미니맵 / HUD / 아이템 픽업 등 `Gameplay` 씬 내부 배선이 rename 후에도 유지됨

**5단계 — 문서**
- `DevLog.md`(작업 결과), `AI_Decision_Log.md`(구현 중 추가 결정 발생 시),
  `ClaudeSessionSummary.md` §씬 구성/클래스 책임 표 갱신, `ProjectPlan.md` 로드맵 반영.

**이번 범위에서 제외**
- 층별 씬 분리(층은 계속 `RoomDataAsset` 데이터 + 제자리 재빌드 유지), Bootstrap 씬 / Additive
  로딩, Addressables·로딩 화면(Phase 5), 게임오버 흐름, 3층 제작.
- `StageManager.TotalRoomsCleared` / `TotalElapsedTime`(이미 추적 중, UI 미노출)을 이용한
  **런 전체 요약 화면**은 결과 화면을 `MainMenu`로 옮기게 될 경우의 후속 과제로만 남겨둠.

---

## 2026-09-02 (3)

### 진행 내용
- **Floor Exit 콜라이더 버그 수정**: `FloorExit.prefab`의 `BoxCollider.m_Size`(3.5, 2, 3.5)가
  오브젝트의 `localScale`(3, 0.1, 3)에 곱해져 실제 트리거가 (10.5, 0.2, 10.5)유닛으로 잡혀
  있던 것을 발견 — 발판(3×3유닛)과 한참 떨어진 곳에서부터 결과 UI가 뜨던 원인. `m_Size`를
  (1.1, 2, 1.1)로 수정해 실제 트리거를 (3.3, 0.2, 3.3)유닛으로 맞춤(발판 크기와 거의 일치).
- **층2("ㅂ" 모양) 방 배치 authoring**: 로드맵 §3/§6의 "다음 층 제작" 착수.
  - 1차로 3×3 격자(9개 방, 좌상단 시작/우하단 보스/정중앙 아이템, 가운데 행만 동서 연결
    없음) 구성 → 실플레이 확인 중 사용자가 지적한 두 가지 회귀:
    - 미니맵에서 시작방(현재방, 노란 표시)이 안 보임
    - 시작방→207 "직행"처럼 보이는 현상, 204방에서 몬스터 스폰
    두 증상 모두 원인은 하나였음(아래 참고).
  - 이후 사용자가 정확한 ASCII 도안으로 모양을 재지정: 시작방 좌상단 한 칸 더 돌출(0,3),
    아이템방을 (1,2)로 이동(기존 CombatB 자리 대체), 기존 CombatB 완전 삭제, 우상단(2,3)에
    신규 방 추가, 전투방 이름을 CombatA~G로 빈틈없이 재정렬. 최종 10개 방, Top 정사영
    캡처로 실루엣 확인.
  - 몬스터 구성은 룸별 랜덤이 아니라 고정값으로 authoring(사용자 지시 — 추후 JSON
    밸런스 파일로 뺄 예정, 이번 세션 범위 아님).
- **플레이어 시작 위치 버그 발견/수정**: 위 "204에서 스폰" 현상의 진짜 원인. `StageManager.
  BuildFloor()`가 시작방에 대해 `EnterRoom()`은 호출하지만, 플레이어의 실제 월드 좌표를
  시작방 위치로 옮기는 코드가 어디에도 없었음 — 플레이어는 씬에 저장된 좌표(원점,
  (0,1,0))에 그대로 있었고, 이게 층1의 시작방(grid 0,0=월드원점) 위치와 우연히 같아서
  지금까지 드러나지 않았던 잠복 버그. 층2의 시작방은 grid(0,2)라 원점과 다른데, 원점 자리는
  하필 층2의 다른 방(구 204)이라 "엉뚱한 방에서 시작 + 그 방의 진짜 문으로 이동"하는
  현상으로 나타났던 것. `RoomTransitionController.PlacePlayerAt(Vector3)` 신설(기존 문
  전환 때 쓰던 `player` 참조 재사용), `StageManager.BuildFloor()`가 층 빌드 직후 호출하도록
  수정.
- **미니맵 오프스크린 버그 발견/수정**: 위 "노란 방 안 보임" 현상의 진짜 원인. `MinimapUI.
  GridToLocal`이 `grid.y`를 그대로 더하는 방식이었는데, 패널이 화면 우상단 고정(anchor
  (1,1))이고 아이콘도 패널 좌상단 기준이라, grid.y가 커질수록(북쪽 방일수록) 패널/화면
  "위쪽 바깥"으로 밀려나는 구조였음 — 층1은 모든 방이 grid.y=0(가로 일직선)이라 한 번도
  발현되지 않았던 잠복 버그. Play Mode 라이브로 실제 화면 좌표(1330×748 기준 Y=778, 화면
  밖)까지 찍어서 확인. `GridToLocal`을 floor의 최대 행/최소 열 기준 상대 좌표로 재계산하게
  수정 + 패널 세로 크기 90→150으로 확장.
- **미니맵을 로컬 스크롤 뷰포트 방식으로 전면 개편**(사용자 요청): 기존엔 층 전체를 고정
  패널에 한 번에 다 그리는 방식 → 현재 방이 항상 패널 중앙에 오도록 매 방 입장마다 재계산,
  `RectMask2D`로 뷰포트 밖은 잘라서 안 보이게 변경. 아이콘/커넥터를 담는 `Content` 자식을
  런타임에 만들어 방 입장마다 `Content.anchoredPosition`만 재계산하는 방식(개별 아이콘
  재배치 불필요). 패널 크기 160×160 고정 뷰포트로 축소.
- **층1→층2 자동 진행 구현**(로드맵 §6 "UI Scene" 작업의 일부를 앞당김, 전체 타이틀/게임오버
  루프는 아직 아님): 층1 클리어 시 기존 Retry 버튼 대신 "3초 후에 다음 층으로 이동" 카운트다운
  → 0초에 자동으로 층2로 전환. 마지막 층(현재는 층2) 클리어 시에만 기존 Retry(씬 리로드)
  유지.
  - 설계 검토: 씬 리로드로 층 전환(정적 상태로 층 인덱스 전달)도 검토했으나, 리로드 시
    Player 오브젝트가 재생성돼 패시브/액티브 아이템 진행 상황이 초기화되고 캐릭터 선택도
    재등장 억제용 별도 로직이 필요해짐 — 대신 **씬 유지, StageManager가 방들만 파괴 후
    재생성**하는 방식 채택. 오브젝트 풀은 적 인스턴스를 방이 아니라 풀 자신의 자식으로
    관리해서 방 파괴가 안전함을 확인, `CharacterSelectController.Awake()`는 Player가
    파괴·재생성되지 않는 한 세션당 1회만 실행됨을 코드로 확인 후 결정.
  - `StageManager`: `roomAssets[]`/`startRoomId` 단일 필드 → `FloorDefinition[] floors`로
    교체. `AdvanceToNextFloor()`(현재 층 방 파괴+이벤트 해제 → 다음 층 재빌드 → 플레이어
    재배치) 신설. `IsLastFloor`/`CurrentFloorNumber` 프로퍼티 추가. `RoomsClearedCount`/
    `ElapsedTime`은 층마다 리셋(결과 UI 표시용), 별도로 `TotalRoomsCleared`/
    `TotalElapsedTime`(누적, 아직 UI 미노출)을 내부적으로 계속 추적.
  - `MinimapUI.Initialize()`를 재호출 가능하게 수정(이전 층 아이콘/커넥터/구독 정리 후
    재빌드) — 층 전환마다 새로 호출됨.
  - `FloorCompleteUI`: 마지막 층 여부에 따라 Retry 버튼 또는 카운트다운 텍스트 노출.
    카운트다운 3→2→1→0 (0 표시 직후 전환 이벤트 발생, `Time.unscaledDeltaTime`/
    `WaitForSecondsRealtime` 기반이라 일시정지 중에도 진행). `Hide()`/`ReleasePause()` 신설.
  - `FloorTransitionUI` 신규: 화면 전체를 덮는 검은 페이드(1초 아웃 → 0.5초 유지 중 실제
    층 재빌드 실행 → 1초 인), 전 구간 언스케일 타임.
  - `RoomFlowManager`가 카운트다운 완료 이벤트를 받아 페이드 전환 지휘, 페이드인 완료 후에만
    일시정지 해제.
  - **카운트다운 버그 발견/수정**: 초기 구현은 "1초..."까지만 텍스트를 갱신하고 바로
    페이드를 시작해서, 페이드 진행 1초 내내 "1초 후에..." 텍스트가 화면에 남아있어 "0초를
    못 보고 1에서 바로 암전되는" 것처럼 보였음(사용자 실플레이로 발견). 전환 이벤트 발생
    직전에 텍스트를 "0초 후에..."로 갱신하는 한 줄 추가로 해결, 실플레이로 재확인 완료.
- **HUD 자잘한 수정 2건**(사용자 실플레이 피드백):
  - 상단 "Stage" 텍스트가 방ID를 그대로 표시해서 층2에서 "Stage210"처럼 보이던 것 →
    `StageManager.CurrentFloorNumber`(1-based)를 대신 표시하도록 수정, 기존 방ID 기반
    `StageChanged` 이벤트/데이터는 그대로 유지.
  - 액티브 아이템 쿨다운 아이콘(화면 중하단) 아래에 현재 장착 아이템 이름 텍스트 신규 추가
    (`ActiveItemCooldownUI`가 `PlayerActiveItemInventory.EquippedItem.ItemName`을 매 프레임
    반영).

### 테스트 결과
- [x] Floor Exit 트리거 크기 수정 반영 확인
- [x] 층2 최종 방 배치(10개, ㅂ자) 구조 검증 스크립트로 확인(중복 좌표/ID 없음, 모든 연결
      상호대응·1칸 인접, 전 방 시작방에서 BFS 도달 가능) + Top 정사영 캡처로 실루엣 육안 확인
- [x] 플레이어 시작 위치 수정 후 Play Mode 라이브 확인: 플레이어 위치가 시작방 위치와
      거리 1(서 있는 높이)로 정확히 일치
- [x] 미니맵 오프스크린 수정 후 라이브 확인: 9개 아이콘 전부 `onScreen=true`, 시작방 노란
      아이콘 정상 표시
- [x] 미니맵 로컬 스크롤 전환 라이브 확인: 현재 방 아이콘이 뷰포트 정중앙에 정확히 위치,
      인접 방만 뷰포트 안에 보임
- [x] (사용자 실플레이) 층2 아이템 방 경유 최단 루트로 보스방까지 클리어 문제없음
- [x] (사용자 실플레이) 층1 클리어 → 카운트다운 → 페이드 → 층2 시작방 전체 시퀀스 정상,
      페이드인 끝난 후에만 조작 가능, 층2에서도 패시브/액티브 아이템 유지·캐릭터 선택
      재등장 없음, 층2(마지막 층) 클리어 시 Retry로 층1부터 완전 재시작 정상
- [x] (사용자 실플레이) 카운트다운 0초 표시 버그 수정 후 재확인 완료
- [ ] 상단 Stage 라벨/액티브 아이템 이름 텍스트는 데이터 경로만 스크립트로 확인(자동화
      세션 프레임 고정으로 화면 갱신 자체는 못 봄) — 사용자가 "확인했다"고 답해 실질적으로는
      확인된 것으로 처리

### 이슈/메모
- 이번 세션에서 발견된 버그 3건(Floor Exit 콜라이더, 플레이어 시작 위치, 미니맵
  오프스크린)이 전부 "층1에서는 좌표가 우연히 안전한 값이라 한 번도 발현되지 않았던
  잠복 버그"라는 공통점이 있었음 — 층1만으로 검증하는 것의 구조적 한계로 기록해둘 것.
  앞으로 새 층 authoring 시 이 세 가지(콜라이더 스케일 곱, 플레이어 스폰 좌표, 미니맵
  좌표계)는 재발 여부를 우선 의심할 것.
- `Unity_ManageAsset`의 `Rename` 액션이 오작동해 애셋이 `Assets/`(하위 폴더 아님) +
  확장자 없는 경로에 남는 문제 발생(`Room2_CombatC.asset` → `Assets/Room2_CombatB`). 이후
  파일 이동은 Bash `mv`로 직접 처리하는 방식으로 우회.
- 자동화 Play Mode의 `Time.frameCount` 고정 한계(기존에도 여러 번 기록된 문제)가 이번엔
  카운트다운 코루틴 검증에서 재발 — 벽시계로 59초가 지나도 프레임 카운트 2에서 정지,
  코루틴이 전혀 진행 안 됨. 정적 배선 검증(참조 null 체크, `Show()` 직후 분기 상태)까지만
  자동으로 하고 나머지는 실플레이로 확인하는 패턴을 계속 따름.

### 다음 작업 (사용자 지정, 우선순위 순)
1. 3층 맵 제작
2. 1~3층 방 위치/속성 등 데이터를 JSON 파일로 외부화(수정 가능하게) — 최근 도입한
   `GameBalance`/`ItemDropTable.json` 패턴과 통일감 있게 검토
3. 스페이스바 대시 동작이 이동 중 무적인지 확인
4. 화면 우하단에 대시 아이콘 UI 배치 + 액티브 아이템 쿨다운과 같은 방식으로 대시 쿨다운 표시
5. 이후 Stage 2.5 잔여 항목 재개(아이템 방 세부 밸런스, 잔여 정리 등)

---

## 2026-09-02 (2)

### 진행 내용
- **버그 수정 (회귀): 보스 클리어 → Floor Exit 진입 시 조작 불가 + 결과
  화면 안 뜸.** 2026-08-24에 이미 고쳤던 "`ClearOverlayPanel` 자기-비활성
  패널 함정"이 Stage 2.5 / FloorExit 작업 중 재발한 것.
  - 원인: 씬의 `ClearOverlayPanel`(`&689983790`, 현재 `FloorCompleteUI`
    컴포넌트 호스트)이 `m_IsActive: 0`으로 저장돼 있었음. `FloorCompleteUI`는
    `Awake()`에서 `panel.SetActive(false)`로 **자기 자신**을 끄는데, 오브젝트가
    비활성으로 시작하면 `Awake()`가 지연되다가 `Show()`의 첫
    `panel.SetActive(true)` 도중 동기 실행되며 방금 켠 패널을 다시 꺼버림.
    그 다음 줄들(`playerInputActions.enabled = false`,
    `PauseState.RequestPause`)은 그대로 실행 → 입력 잠김 + timeScale 0 +
    화면은 안 보이는 소프트락. (`AI_Decision_Log`/`ClaudeSessionSummary` §4에
    이 패턴 상시 경고 노트 있음 — 세 번째 재발)
  - 수정 (Option B-2, CanvasGroup 방식): GameObject active를 토글하지 않고
    `CanvasGroup`으로만 표시/숨김. 컴포넌트의 GameObject는 씬 내내 활성 유지 →
    `Awake()`가 로드 시 1회만 정상 실행됨(버그 없던 다른 패널들과 동일 상태).
    - `FloorCompleteUI.cs`: `[SerializeField] GameObject panel` →
      `CanvasGroup canvasGroup`. `ApplyVisible(bool)` 추가
      (`alpha`/`interactable`/`blocksRaycasts` 토글). `Awake()`는
      `ApplyVisible(false)`, `Show()`는 텍스트 채우기 → `ApplyVisible(true)` →
      입력잠금 → `PauseState.RequestPause` 순으로 정리(`SetActive` 호출 제거).
    - `Prototype.unity`: `ClearOverlayPanel` `m_IsActive: 0 → 1`,
      `CanvasGroup` 컴포넌트(`&689983795`, alpha 0 / interactable 0 /
      blocksRaycasts 0) 추가, `FloorCompleteUI.canvasGroup` 배선. 딤 `Image`
      (`&689983793`)는 그대로 — 숨김 시 alpha 0 + blocksRaycasts 0이라 안
      보이고 클릭도 안 먹음. (Play Mode 중 손댄 디스크 수정본은 에디터가
      스테일 씬을 들고 있어 미반영 → `Unity_RunCommand`로 인메모리 씬에 직접
      적용 후 `EditorSceneManager.SaveScene`으로 저장하는 방식으로 처리)
  - 미처리(별건): 스테일 `m_EditorClassIdentifier: ...ClearOverlayUI`
    (씬 재저장 시 자동 정정, 하위 스크립트는 guid로 해석되므로 무해),
    삭제된 `ClearOverlayUI.cs.meta` git 잔재.

### 테스트 결과
- [x] 스크립트 재컴파일 에러/경고 0건 (`Unity_ReadConsole`, Play Mode 종료 후)
- [x] `Unity_RunCommand` 구조 검증: `ClearOverlayPanel.activeSelf = True`,
      `CanvasGroup` 존재(alpha 0), `FloorCompleteUI.canvasGroup` 필드가 그
      CanvasGroup을 가리킴, 씬 저장됨
- [x] `Unity_RunCommand` 기능 검증: 에디트 모드에서 `FloorCompleteUI.Show(더미
      stats)` 호출 → `canvasGroup.alpha = 1` / `interactable = True` /
      `blocksRaycasts = True` / `Time.timeScale = 0` / `PauseState.IsPaused =
      True` 확인, 이후 상태 원복(저장 안 함)
- [ ] 실플레이: 보스 클리어 → Floor Exit 진입 → 결과 패널이 실제로 보이고 4개
      값(rooms/passive/active/time) 채워짐 + 입력잠금 + Retry 클릭 시 씬
      리로드·timeScale 복구 — 미확인/예정
- [ ] 실플레이: Play 진입 직후 화면 안 어두움 + 클릭 안 막힘 — 미확인/예정

### 이슈/메모
- Play Mode 중 씬 파일을 직접 편집하면 에디터 인메모리 씬과 어긋남 —
  스크립트는 재컴파일돼도 씬은 리로드 안 됨. 이번엔 디스크 hand-edit 대신
  `RunCommand`로 인메모리 씬에 적용 후 저장해서 단일 소스로 정리.
- 이 "자기 자신을 `Awake()`에서 `SetActive(false)` 하는 패널" 함정이 세 번째
  재발(`ClearOverlayPanel` 8/24, 그리고 `ActiveItemEquipUI`/
  `PickupNotificationUI` 사례). B-2처럼 CanvasGroup로 가면 구조적으로 사라지므로
  나머지 패널도 같은 방식 이관을 검토할 것.

### 다음 작업
- 위 [ ] 실플레이 2건 확인
- 아이템 방(#3) 설계 및 구현
- 다음 층 제작 (층 2·3 데이터 authoring, 목표 총 3층)
- 씬의 비활성 `Boss_Basic` 인스턴스 삭제

---

## 2026-09-02

### 진행 내용
- 코드/데이터 변경 없음 — Stage 2.5 이후 로드맵만 개정한 세션.
- **잔여 정리 항목 정리 방법 확정**: 씬(`Prototype.unity`)에 남은 비활성
  `Boss_Basic` 인스턴스(`PrefabInstance &5312692809645635092`, 위치
  `(-10, 1.5, 10)`, `m_IsActive: 0`)를 삭제하기로 함. 근거 확인:
  - `BossPool`(`GameObjectPool`)은 프리팹 자산(`Boss_Basic.prefab`,
    guid `1b7355ec…`)을 직접 참조하며, 이 씬 인스턴스는 씬 루트 정렬
    목록 외 어디서도 참조되지 않음 — 삭제해도 스폰 경로에 영향 없음.
  - 이 인스턴스에만 `EnemyStateMachine.data`(→ `BossData.asset`),
    `projectilePool`, `maxHealth: 300` 오버라이드가 걸려 있어, 과거
    (2026-08-18) 프리팹 자산의 결함을 가려 `BossPool` 스폰 시
    NullReferenceException 원인 파악을 방해한 전력이 있음.
  - `projectilePool` 오버라이드가 씬 풀 오브젝트를 물고 있어, 나중에 씬
    정리 시 조용히 깨진 참조가 될 수 있음.
  - 정리 방법: 씬에서 해당 GameObject만 삭제 후 저장, 프리팹 자산은 유지.
    삭제 후 보스 방 진입 → 정상 스폰/처치 1회 확인.
- **로드맵 개정 (사용자 지시)**: 기존 "단일 테스트 층의 방 개수를 5~7개로
  확장" 계획을 폐기하고 다음으로 대체:
  1. **다음 층 제작 — 목표 총 3층**. 현재 테스트 층 1개에 층 2·3을
     `RoomDataAsset` / `FloorLayout` 데이터로 추가(방/문 메커니즘 변경 없는
     데이터 authoring 작업).
  2. **3층 완성 후 UI Scene 신설 → 전체 플레이 루프 구성**: 타이틀 →
     캐릭터 선택 → 런(3층 주파) → 클리어/게임오버 → 재시작·타이틀 복귀를
     하나의 루프로 연결. 미정 상태였던 **Game Over 흐름 / 런 종료 이후
     흐름**을 이 작업에 통합해서 설계하기로 함.
- `ClaudeSessionSummary.md` §3(Stage 2.5 남은 작업) / §5(미결정 항목) /
  §6(앞으로 해야 할 작업, 우선순위 재번호) / 헤더 갱신일에 위 내용 반영.

### 테스트 결과
- 해당 없음 (문서 개정 세션, 코드/씬 변경 없음).

### 이슈/메모
- 실제 작업(Boss 인스턴스 삭제, 층 2·3 authoring, UI Scene)은 이번 세션에서
  착수하지 않음 — 계획 확정까지만.

### 다음 작업
- 아이템 방(#3) 설계 및 구현
- 다음 층 제작 (층 2·3 데이터 authoring, 목표 총 3층)
- UI Scene + 전체 플레이 루프 (3층 완성 후)
- 씬의 비활성 `Boss_Basic` 인스턴스 삭제

---

## 2026-09-01

### 진행 내용
- 사용자가 2026-08-31 작업분(룸 시스템 전환 + 아이템/밸런스/디버그 관련
  전반)을 직접 플레이하면서, 그동안 자동화 환경 한계로 "직접 호출/물리 스텝
  강제 실행"으로만 검증됐던 항목들을 실플레이로 최종 확인. 코드/데이터 변경
  없음(검증 전용 세션).

### 테스트 결과 (사용자 실플레이)
- [x] 방-방 카메라 전환 연출이 끝까지 자연스럽게 완료됨 — 문 통과 → 카메라
      보간 이동 → 도착 후 플레이어 재배치 및 입력·물리·렌더러 원복까지 전
      구간 정상 (2026-08-31 (2) 항목에서 자동화 한계로 미검증이던 코루틴
      완주 부분)
- [x] 시작방(16유닛)과 일반 방(40×40유닛) 크기, 기존 `QuarterViewCameraFollow`
      (단순 오프셋 추적, 방 경계 재프레이밍 없음) 구도가 실제 플레이에서
      적절함 — 방 경계 재프레이밍/Cinemachine 도입은 당장 불필요로 판단
- [x] 미니맵(겅전 스타일 3단계 안개 + 방 연결선 + 배경 패널)이 실제 화면
      (우상단)에서 가독성·배치 모두 양호
- [x] `BillboardUI` 빌보딩이 자연스러움 — 데미지 숫자 팝업 / 적 HP바 / 픽업
      알림이 이동·전환 중에도 항상 카메라를 향함
- [x] ESC 일시정지 메뉴가 실제 ESC 키로 정상 개폐되고, 좌(패시브 스탯 Pip
      바 4종) / 우(장비 슬롯) 분할 레이아웃과 Pip 바 채움 표시가 화면에서
      괜찮음
- [x] 패시브 아이템 픽업이 지면에 사선으로("누운 캡슐") 잘 놓임 — 회전 강제
      초기화 / 공중 스폰 버그 수정((18) 항목)이 실제로 반영됨을 확인
- [x] 기본 공격 사거리 3유닛(발사체 유지시간 0.2초 × 15유닛/초)과 공격 속도
      체감(초반 느림 → 스택 시 빨라짐)이 적절함

### 이슈/메모
- 위 항목 전부 "문제 없음"으로 확인됨 — 관련 미확인 꼬리표들을 정리.
- 아직 남은 확인/결정(전부 실플레이 블로커 아님): Enhanced Shot 액티브
  아이템 사거리도 같이 짧아진 게 의도한 바인지, `FlamethrowerActiveItem.
  CooldownRemaining` 항상 0 반환 갭, 아이콘 스와치 → 실제 픽토그램 교체,
  F1 오버레이 FPS 텍스트 위치, 씬의 비활성 `Boss_Basic` 플레이스홀더 정리

### 다음 작업
- 아이템 방(#3) 설계 및 구현 (바닥 드롭 인프라 재사용 + "전투 없이 보장된
  픽업 배치" 추가)
- 테스트 층 5개 방 → 5~7개 이상으로 확장 (메커니즘 검증 완료, 데이터 애셋만
  추가하면 됨)
- Game Over / 런 종료 이후 흐름 추가
- Stage 3(최적화) 잔여 항목 재개 시점 논의

---

## 2026-09-01 (2)

### 진행 내용
- Stage 2.5 잔여 정리 2건 처리 (위 실플레이에서 확인됐던 미결정 항목):
  1. **Enhanced Shot 사거리를 기본 공격과 분리** — 기존엔 액티브 아이템
     "Enhanced Shot"이 기본 공격과 같은 `Projectile_Basic` 프리팹/풀을 쓰면서
     사거리를 `GameBalance.Player.projectileLifetime`(기본 공격용으로 0.2초 =
     3유닛으로 줄인 값)에 그대로 얹어 받고 있었음. 이제 스킬이 자기 데이터
     기준으로 독립 동작:
     - `Projectile.Initialize(...)`에 `rangeOverride`(월드 유닛, 기본 -1)
       옵션 인자 추가. `>= 0`이면 `수명 = rangeOverride / speed`로 계산하고
       `lifetime + bonusLifetime`(기본 공격 + 패시브 사거리 경로)은 무시.
       `Awake()`에서 `-1`로 초기화(재사용/미초기화 시 즉시 소멸하는 footgun
       방지)
     - `EnhancedShotActiveItemData`에 `range` 필드 신규(월드 유닛).
       `EnhancedShotActiveItem`이 `Initialize(..., rangeOverride: _data.Range)`
       로 전달
     - 프리팹/풀은 그대로 공유(투사체 거동은 동일, 사거리 숫자만 스킬 소유).
       패시브 "사거리 증가"는 원래대로 기본 공격에만 적용되고 Enhanced Shot엔
       무영향(스코프 변화 없음)
     - **같은 날 스킬 스펙 확정(사용자 지정)**: 사거리 **25유닛**, 데미지
       **16**(레벨 1 기준), 쿨다운 2초는 유지. `range`/`damage` 코드 기본값과
       `EnhancedShotData.asset` 모두 이 값으로 맞춤
  2. **Flamethrower 스킬에 쿨타임 3초 부여** — `FlamethrowerActiveItem.
     CooldownRemaining`이 항상 0을 반환해 사실상 무한 연발이 가능하던 갭을
     메움:
     - `FlamethrowerActiveItemData`에 `cooldown` 필드 신규(기본 3초)
     - 분사 종료(버튼 릴리스 / `maxDuration` 소진 둘 다) 시점에
       `EndActivation()`이 `_cooldownRemaining = _data.Cooldown` 설정,
       `Tick()`이 비활성 상태에서 쿨다운 감소, `OnActivatePressed()`가
       `_cooldownRemaining > 0f`면 재분사 차단
     - `CooldownProgress`: 분사 중 1, 쿨다운 중 `remaining / cooldown`,
       준비 완료 0 (`IActiveItem` 인터페이스 계약대로) — `ActiveItemCooldownUI`
       가 이제 화염방사기에도 정상적으로 쿨다운 스윕을 표시함
  - 데이터 애셋에도 값 명시: `EnhancedShotData.asset`에 `damage: 16` /
    `range: 25`, `FlamethrowerData.asset`에 `cooldown: 3` (코드 기본값과 동일,
    명시성)

### 테스트 결과
- [x] 스크립트 리임포트 후 재컴파일 에러/경고 0건 (`Unity_ReadConsole` 확인)
- [x] `RunCommand`로 확정 스펙 확인: `EnhancedShotData` `Damage`=16,
      `Range`=25유닛, `Cooldown`=2초. `projectileSpeed`=15 기준 자체 수명
      25/15≈1.67초로, 기본 공격 수명 0.2초(3유닛)와 완전히 분리됨
- [x] `RunCommand`로 `FlamethrowerActiveItem` 상태 머신 검증:
      분사 종료 즉시 `CooldownRemaining`=3.00 / `CooldownProgress`=1.00 →
      `Tick(1s)` 후 2.00 / 0.67 → **쿨다운 중 `OnActivatePressed()` 호출해도
      재분사 안 됨**(`Tick(1s)` 후 1.00 / 0.33으로 계속 감소, 1.00으로 안
      튐) → `Tick(2s)` 후 0.00 / 0.00(준비) → 준비 상태에서 누르면 다시
      분사(progress 1.00)
- [ ] 실플레이에서 Enhanced Shot이 실제로 방을 가로지르는 사거리로 날아가는지,
      화염방사기 쿨다운 스윕이 UI에 뜨는지 육안 확인 — 미확인/예정

### 이슈/메모
- `Unity_RunCommand` / `Unity_ManageAsset`가 "Unity not detected (no fresh
  discovery files found)"를 간헐적으로 반환하는 현상 이번에도 반복 —
  1~2회 재시도로 정상 동작(이전 세션들과 동일한 일시적 디스커버리 파일
  타이밍 이슈).
- `RunCommand`의 `result.Log`는 `{0}`/`{1}`만 치환하고 `{2:F2}` 같은 포맷
  지정자/3번째 이상 인자는 리터럴로 출력함 — 검증 값은 `ToString("F2")`로
  미리 포맷해서 단일 문자열로 넘길 것.

### 다음 작업
- (위 [ ] 항목) 실플레이 육안 확인
- 남은 잔여 정리: 아이콘 스와치 → 실제 픽토그램, F1 오버레이 FPS 텍스트
  위치, 씬의 비활성 `Boss_Basic` 플레이스홀더 정리

---

## 2026-08-31

### 진행 내용
- **Stage 2.5(룸 시스템 전환) 착수** — Stage 3(최적화)는 잠시 보류하고, 기존
  "웨이브 반복" 방식이던 Stage 진행을 아이작(Binding of Isaac) 스타일의
  공간형 방/문 시스템으로 전환. 설계는 대화로 먼저 합의: 맵 구조는 고정
  authoring 그래프(런타임 랜덤 없음, 추후 랜덤 분기 대비해 방의 "다음 방"을
  단일 필드가 아니라 문 방향별 연결 리스트로 authoring), 방 배치는 층 전체를
  그리드에 미리 인스턴스화(백트래킹 가능), 보상 UI는 기존 전체화면 모달 유지.
  - 신규 스크립트 9개(`Assets/Scripts/Room/`): `RoomType`, `DoorDirection`,
    `RoomDoorConnection`, `RoomData`(플레인 POCO, 기존 `StageData`와 동일하게
    엔진 비의존), `RoomDataAsset`(SO, `StageDataAsset`과 동일 패턴),
    `IFloorLayoutProvider`/`ScriptableObjectFloorLayoutProvider`(기존
    `IStageDataProvider` 패턴 재사용 — 나중에 절차적 생성기로 교체 가능),
    `DoorController`(문 잠금/해제), `RoomController`(방 하나의 생명주기:
    지연 스폰, 클리어 추적, 문 배선)
  - 기존 스크립트 변경: `RoomManager.BeginRoom()`이 태그 전역 검색 대신
    명시적 `IEnumerable<HealthComponent>`를 받도록 변경(방마다 별도
    `RoomManager` 인스턴스가 필요해졌기 때문), `EnemySpawner.SpawnStage`→
    `SpawnRoom`으로 교체(스폰 좌표를 방 로컬 기준으로 해석해 방의 월드
    트랜스폼으로 변환), `StageManager` 전면 재작성(층의 방 그래프를 빌드하고
    현재 방을 추적 — 클래스명은 씬 컴포넌트 참조/GUID 파손을 피하려 그대로
    유지, 책임만 "스테이지 진행"에서 "층/방 그래프 관리"로 바뀜),
    `RoomFlowManager` 재작성(더 이상 스폰을 직접 하지 않고, `StageManager`가
    발행하는 방 클리어 이벤트에 맞춰 업그레이드/장착 UI 시퀀싱과 입력 잠금만
    담당)
  - 삭제: `StageData`/`StageDataAsset`/`IStageDataProvider`/
    `ScriptableObjectStageDataProvider`(4개 스크립트, 휴지통 이동) 및
    `Assets/Data/Stages/Stage01~10.asset`(10개, `RoomData` 계열로 완전 대체)
  - `Assets/Prefabs/Room_Base.prefab` 신규 — RunCommand로 절차적 생성(40x40
    유닛 방, 4방향에 벽 2세그먼트+문 게이트, 문 폭 6유닛, 트리거 콜라이더로
    "플레이어 진입" 감지). 연결이 authoring되지 않은 방향은 문 게이트가
    영구적으로 잠긴 채 남아 사실상 벽 역할을 함(비주얼만 살짝 다름, 화이트박스
    단계라 우선순위 낮음으로 보류)
  - `Assets/Data/Rooms/Room01_Start~Room05_Boss.asset` 5개 신규 authoring —
    선형 연결(Start→CombatA→CombatB→CombatC→Boss), 기존 Stage01/02/05/10
    애셋의 적 배치 패턴을 재사용해 2→3→6마리로 난이도 상승 + 보스 1마리
  - 씬 재배선: `StageManager` 필드 교체(`roomAssets` 5개, `startRoomId=1`,
    `roomPrefab`, `enemySpawner`, `roomWorldSize=40`, `roomGap=10`), 기존
    "RoomManager" 최상위 GameObject를 "RoomFlowManager"로 개명하고 그 위의
    `RoomManager` 컴포넌트는 제거(방별 `RoomManager`가 이제 각 Room 프리팹
    인스턴스 안에 있음)

### 테스트 결과
- [x] 전체 스크립트 컴파일 에러/경고 없음 (Unity MCP 콘솔 확인)
- [x] Play Mode 진입 시 5개 방이 그리드(50유닛 간격)에 정상 인스턴스화 —
      좌표 (0,0,0)/(50,0,0)/(100,0,0)/(150,0,0)/(200,0,0) RunCommand로 확인
- [x] 시작방(Room1, 적 없음)이 자동으로 클리어 처리되고 문이 즉시 열림을 확인
- [x] Room2/3/4 진입 시 authoring한 적 수·변종이 정확히 스폰됨(2/3/6마리),
      스폰 월드 좌표가 "방 월드 위치 + 로컬 오프셋"과 정확히 일치함을
      RunCommand로 계산 대조(예: Room2 월드(50,0,0) + 로컬(8,1,0) =
      월드(58,1,0), 실제 스폰 위치와 일치)
- [x] 문 잠금 동작: 연결이 없는 방향(N/S)은 게이트가 항상 활성(잠김),
      연결된 방향(E/W)은 클리어 전 잠김→적 전멸 후 자동 해제됨을
      `DoorController` 게이트 오브젝트의 활성 상태로 확인
- [x] 클리어→업그레이드 선택→장착 선택→입력 재활성화 흐름 정상 동작 —
      실제 버튼 `onClick.Invoke()`로 재현, Room2/3/4 세 번 반복 확인
- [x] 백트래킹: 이미 클리어한 Room2를 다시 `EnterRoom()` 호출해도 적이
      재스폰되지 않고(풀 활성 개수 0 유지) 보상 UI도 재트리거되지 않음을 확인
- [x] 보스방(Room5) 클리어 → `ClearOverlayPanel` 활성 + 입력 잠금, 업그레이드
      UI는 뜨지 않음(보스 클리어는 보상 없이 바로 런 종료)을 확인
- [x] 화면 상단 `StageText`가 현재 방 ID를 따라 "Stage 5"까지 정상 갱신됨을
      최종 확인
- [ ] 실제 사람 플레이로 방 경계 트리거를 걸어서 통과하는지, 문이 열린 뒤
      물리적으로 지나갈 수 있는지 — 미확인/예정 (자동화 세션에서는
      `RoomController.EnterRoom()`을 직접 호출해 트리거를 시뮬레이션함;
      트리거 콜라이더 자체의 물리 동작은 검증하지 않음)
- [ ] 방이 커진 것(40x40)이 실제 플레이 카메라 앵글에서 어떻게 보이는지,
      기존 `QuarterViewCameraFollow`(단순 오프셋 추적, 방 경계 재프레이밍
      없음)로 충분한지 — 미확인/예정

### 이슈/메모
- `RoomController.EnterRoom()`을 자동화 세션에서 직접 호출해 "플레이어가
  방에 걸어 들어옴"을 시뮬레이션했다 — 물리 트리거(`OnTriggerEnter`) 자체는
  거치지 않았으므로, 트리거 콜라이더 크기/레이어가 실제로 플레이어 캡슐과
  올바르게 상호작용하는지는 별도로 실플레이 확인이 필요함.
- `StageManager` 클래스명을 유지한 이유: 파일을 리네임하거나 새로 만들어
  기존 파일을 지우면 스크립트 GUID가 바뀌어 씬에 이미 배선된 컴포넌트
  참조가 전부 깨짐. 내용만 전면 교체하고 이름은 그대로 둠 — 클래스 상단
  주석에 "책임이 바뀌었다"는 점을 남겨둠.
- 문/벽 처리를 단순화함: 4방향 전부에 항상 "문 모양" 게이트가 있고, 연결이
  없는 방향은 그 게이트가 영구히 닫힌 채로 남는 방식(진짜 벽 메쉬로 안
  바꿈). 기능상 문제는 없지만 비주얼이 살짝 부자연스러울 수 있음 —
  화이트박스 프로토타입 단계라 우선순위 낮음으로 보류.
- 카메라는 방 경계에 맞춘 재프레이밍 로직을 넣지 않고 기존 단순 추적
  방식을 그대로 뒀음 — 방이 40x40으로 커졌을 때 실제로 부족한지는 실플레이
  후 판단하는 게 낫다고 보고 이번 범위에서는 제외함.

### 다음 작업
- 실제 플레이로 물리적 도어 통과/방 진입 트리거 검증
- 테스트 층(5개 방)을 원래 목표인 5~7개 이상으로 확장 authoring 검토
  (메커니즘 자체는 검증 완료 — 데이터 애셋만 추가하면 됨, 코드 변경 불필요)
- 카메라 방 경계 재프레이밍 필요 여부 판단
- Stage 3(최적화: `EnemyAIManager`, `GetComponent` 캐싱, `BenchmarkSpawner`,
  Profiler Before/After) 재개 시점 논의

---

## 2026-08-31 (2)

### 진행 내용
- 사용자 실플레이 피드백 3건 반영 (같은 날 Stage 2.5 후속 작업)
  1. 시작방(Room1)을 작게: `RoomData`/`RoomDataAsset`에 `roomSize` 필드 추가,
     방마다 크기를 다르게 authoring 가능하게 함. `Room01_Start.asset`만
     16유닛(기존 40유닛 대비)으로 축소, 나머지는 기본값 40 유지
  2. 방 지오메트리를 프리팹에 미리 굽는 방식에서 **런타임 절차적 생성**으로
     전환 — `RoomController.Initialize()`가 `RoomData`(크기+연결 방향)를 보고
     그 자리에서 바닥/벽/문을 직접 생성. 연결이 authoring된 방향에만 문(틈+
     게이트)을 만들고, 연결이 없는 방향은 처음부터 **구멍 없는 완전한 벽**을
     만듦 — 이전에는 "잠긴 문"과 "막힌 방향"이 똑같이 생겨서 Room2부터
     입구/출구가 안 보이던 문제의 원인이었음. `Room_Base.prefab`은 빈 껍데기
     (`RoomManager`+`RoomController`만)로 축소, 지오메트리는 전부 코드에서
     생성. "여러 방향 조합 프리팹을 따로 만들 필요가 있냐"는 질문에는
     조합 폭발(2^4=16가지)을 피하고 이미 있는 `RoomData` 연결 정보를
     재사용하는 이 방식을 추천해 채택
  3. **방-방 이동을 물리적으로 걷는 방식에서 카메라 연출 전환 방식으로 변경**:
     신규 `RoomTransitionController`(최상위 씬 오브젝트) — 문의 트리거를
     통과하면(잠금 해제 상태에서만) 플레이어 렌더러 비활성화+입력 잠금+
     Rigidbody kinematic 전환 후, 카메라가 현재 위치에서 목표 방의 입구
     지점까지 0.6초간 `SmoothStep` 보간 이동(사용자가 부드러운 이동 방식
     선택), 도착 시 플레이어를 그 지점에 재배치하고 렌더러/입력/물리 원복.
     `DoorController`에 `PassedThrough` 이벤트, `EntryPoint`(문 안쪽 진입
     지점) 추가. `QuarterViewCameraFollow`에 `Offset` public 게터 추가(전환
     중 `enabled=false`로 끔). `StageManager`가 층 빌드 완료 후
     `RoomTransitionController.Initialize()`로 전 도어의 이벤트를 구독시킴

### 테스트 결과
- [x] 컴파일 에러/경고 없음
- [x] Room1(시작방) 바닥 스케일이 (1.6,1,1.6) = 16유닛으로 축소됨을 확인
- [x] 방마다 문/벽 구조가 데이터와 정확히 일치: Room1·Room5는 문 1개+벽
      세그먼트 5개, Room2~4는 문 2개(E/W)+벽 세그먼트 6개, 연결 없는 방향(N/S)은
      항상 완전한 벽으로 생성됨(구멍 없음)을 RunCommand로 확인
- [x] **물리 트리거 실제 발화 확인** — Player를 Room1 East 문 트리거 콜라이더
      중심으로 직접 이동시킨 뒤 `Physics.Simulate()`로 물리 스텝을 한 번
      강제 실행, `DoorController.OnTriggerEnter`→`PassedThrough`→
      `RoomTransitionController` 코루틴 진입까지 실제로 발화됨을 확인
      (플레이어 렌더러 비활성화, `PlayerInputActions.enabled=False`,
      `Rigidbody.isKinematic=True`로 전환된 것을 코루틴 실행 직후 상태로 확인)
- [ ] 카메라 보간 완주(문 진입 지점까지 이동) 및 최종 상태 복원(플레이어
      재배치, 다음 방 활성화, 렌더러/입력/물리 원복) — **미확인**. 이번
      세션에서 `Time.frameCount`가 1에서 고정되는 알려진 자동화 한계
      (2026-08-18 이슈/메모 참고)로 코루틴이 첫 `yield` 이후 진행되지 않음.
      트리거 발화와 전환 시작 로직은 확인됐으나, 실제 카메라 이동 연출과
      도착 처리는 사람이 직접 플레이해서 확인 필요
- [ ] 방 크기가 작아진 시작방이 실제로 체감상 괜찮은지, 카메라 앵글에서
      비율이 맞는지 — 미확인/예정

### 이슈/메모
- 문/벽 가시성 버그의 정확한 원인: 이전 구현은 4방향 전부에 "문 모양"을
  만들어두고 연결 없는 방향은 게이트를 영구 잠금 상태로만 남겼는데, 잠긴
  문과 벽이 동일한 큐브라 시각적으로 구분이 안 됐음. 지금은 연결 없는
  방향에 아예 문 지오메트리 자체를 생성하지 않고 완전한 벽 하나만 만들어서
  근본적으로 해결.
- 코루틴 완주를 이번 세션에서 확인하지 못한 것은 `RoomTransitionController`
  코드 문제가 아니라 자동화 세션의 `Time` 고정 한계 때문 — `Physics.Simulate()`
  로 물리 콜백(트리거 발화, 코루�ine 진입)까지는 강제로 검증 가능했지만,
  `yield return null`로 다음 프레임을 기다리는 부분은 실제 에디터 업데이트
  루프가 돌아야 진행되므로 이 환경에서는 강제할 수 없었음.

### 다음 작업
- 실제 플레이로 방-방 카메라 전환 연출 전체(보간 이동→도착→상태 복원) 확인
- 시작방 크기가 실제로 적절한지 플레이 후 판단, 필요시 다른 방들도 개별
  크기 조정 검토
- 위 확인 후 문제 없으면 테스트 층을 5개→7개 이상으로 확장

---

## 2026-08-31 (3)

### 진행 내용
- 지형(바닥/벽)은 추후 다른 무료 애셋으로 교체될 예정이라는 전제 하에,
  문(출입구) 표현 방식 변경 — 벽 모양과 무관하게 항상 존재하는 반투명
  마커 오브젝트로 상태를 표시하도록 함
  - `DoorController` 재작성: 기존 "게이트 큐브 SetActive로 잠금/해제" 방식을
    폐기, 대신 **물리 차단용 투명 콜라이더**(잠금 시에만 활성)와 **시각용
    반투명 마커**(항상 존재, `MaterialPropertyBlock`으로 알파만 변경)를
    분리. 잠금 시 검정 알파 0%(완전 투명), 해제 시 검정 알파 70%
  - `RoomController.BuildSide()`: 문마다 콜라이더 2개(트리거 1개+물리
    차단용 1개) + 마커 큐브(콜라이더 제거, 렌더러만) 생성. `doorMarkerMaterial`
    필드를 `[SerializeField]`로 추가해 `Room_Base.prefab`에 배선
  - 신규 `Assets/Materials/DoorMarker.mat`: URP Lit 셰이더를 Transparent
    서페이스로 스크립트에서 직접 설정(`_Surface`/`_Blend`/`_SrcBlend`/
    `_DstBlend`/`_ZWrite`/렌더큐 등), 기본 색은 검정 알파 0
  - `Room_Base.prefab` 재생성(빈 껍데기 + `doorMarkerMaterial` 참조 배선)

### 테스트 결과
- [x] 컴파일 에러/경고 없음
- [x] Play Mode에서 각 문의 마커 알파값을 `MaterialPropertyBlock`으로 직접
      조회해 확인 — Room1(시작방, 즉시 클리어)의 East 문만 알파 0.70,
      나머지 미클리어 방(Room2~5)의 모든 문은 알파 0.00으로 정확히 일치
- [x] 물리 차단 콜라이더도 같은 규칙으로 확인 — 잠긴 문은 차단 콜라이더
      활성(1개), 해제된 문은 비활성(0개)
- [x] `Unity_Camera_Capture`로 Room1 스크린샷 확인 — 열린 문 위치에 배경보다
      어두운 영역이 존재. 다만 압축된 프리뷰 이미지만으로는 톤이 의도한
      만큼 뚜렷한지 확신하기 어려워 **정확한 시각적 느낌은 실제 플레이에서
      재확인 필요**

### 이슈/메모
- 알파 변경은 `renderer.material`(인스턴스 생성) 대신 `MaterialPropertyBlock`
  사용 — 방마다 문이 여러 개 절차적으로 생성되므로 머티리얼 인스턴스가
  누적되는 것을 피하기 위함
- URP Lit 셰이더의 Transparent 서페이스 설정을 스크립트로 직접 구성함
  (에디터의 Surface Type 토글 대신 `_Surface`/`_Blend`/블렌드 모드/ZWrite/
  렌더큐/키워드를 코드로 재현) — 셰이더 버전에 따라 프로퍼티명이 달라질 수
  있어 향후 URP 업그레이드 시 깨질 수 있는 지점으로 기억해둘 것

### 다음 작업
- 방-방 카메라 전환 연출 실플레이 검증 (이전 항목에서 계속 이월)

### 추가 수정 (같은 날, 알파값 반전)
사용자가 헷갈렸다며 알파값을 반대로 정정: **잠김 = 검정 알파 100%(완전 불투명),
열림 = 검정 알파 30%**로 변경 (`DoorController.LockedAlpha`/`UnlockedAlpha` 상수만
수정). Play Mode에서 `MaterialPropertyBlock` 재조회로 확인 — Room1의 열린 문만
알파 0.30, 나머지 잠긴 문 전부 알파 1.00.

### 추가 수정 (같은 날, 문 잠금 정책을 아이작 방식으로 변경)
기존엔 "미클리어 방은 처음부터 잠김, 클리어 시 연결된 문만 해제"였는데, 사용자
요청으로 정책을 뒤집음: **기본적으로 모든 방의 모든 문은 열림 상태**, 플레이어가
**미클리어 방에 입장하는 순간 그 방의 모든 문이 잠기고**, 클리어하면 다시 전부
열림. 변경 지점 2곳뿐: `RoomController.BuildSide()`에서 문 생성 직후
`door.Lock()` → `door.Unlock()`으로 교체(기본 열림), `EnterRoom()`에 적 스폰
직전 `LockAllDoors()` 호출 추가(입장 즉시 감금). `HandleCleared()`의
`door.IsConnected` 체크는 이제 항상 true라 사실상 무의미해졌지만 안전장치로 유지.

테스트: Play Mode에서 방 빌드 직후(전부 열림) → `Room2.EnterRoom()` 호출(Room2만
잠김, 나머지 그대로 열림) → 강제 전멸시켜 클리어(Room2 다시 전부 열림)까지
로그로 전 구간 확인, 의도대로 동작.

### 참고: 무관한 기존 버그 발견 및 수정
같은 세션에서 사용자가 직접 플레이하는 동안 콘솔에
`InvalidOperationException: Trying to release an object that has already
been released to the pool.` (`Projectile.OnTriggerEnter` → `PooledObject.
ReturnToPool()` → `GameObjectPool.Release()`)가 반복 발생하는 것을 확인,
사용자 요청으로 수정 (이번 Stage 2.5 방 시스템 작업과는 무관한 기존 버그).

- **원인**: `Projectile.OnTriggerEnter`가 트리거를 맞을 때마다 무조건
  `ReturnToPool()`을 호출했는데, 투사체가 같은 물리 스텝에 콜라이더 여러
  개(예: 서로 붙어있는 적 2마리)와 동시에 겹치면 `OnTriggerEnter`가 그
  프레임 안에서 여러 번 호출됨. 또한 수명 만료(`Update()`)와 피격이 같은
  프레임에 겹치는 경우도 마찬가지로 중복 호출 가능. `PooledObject`/
  `GameObjectPool` 어느 쪽에도 중복 반환을 막는 안전장치가 없어서 Unity의
  `ObjectPool<T>`가 `collectionCheck: true` 설정 때문에 두 번째 반환 시
  예외를 던짐
- **수정**: `Projectile`에 `_hasReturned` 플래그 추가, 피격/수명만료 양쪽
  경로를 `ReturnToPoolOnce()`로 통합해 최초 1회만 실제로 반환되도록 함.
  추가로 `PooledObject` 자체에도 `_isReleased` 플래그로 일반적인 중복 반환
  가드를 넣어 Projectile 외 다른 풀링 오브젝트(적/보스/그레네이드)에서도
  같은 클래스의 버그가 발생하지 않도록 함. `GameObjectPool.OnGetFromPool`이
  꺼내줄 때마다 `PooledObject.NotifyTakenFromPool()`로 가드를 리셋하므로
  재사용 자체는 막히지 않음

### 테스트 결과 (버그 수정)
- [x] 컴파일 에러/경고 없음
- [x] Play Mode에서 `ProjectilePool`로 인스턴스를 하나 꺼내 `ReturnToPool()`을
      연속 2회 호출 — 첫 호출은 정상 반환, 두 번째 호출은 예외 없이 조용히
      무시됨을 확인 (수정 전이었다면 `InvalidOperationException` 발생)
      풀에서 다시 꺼낸 뒤 재반환도 정상 동작(가드가 재사용 시 리셋됨) 확인

---

## 2026-08-31 (4)

### 진행 내용
- 아이작 참고 후보 3가지(미니맵/방 안 장애물/아이템 방) 중 미니맵 먼저 진행
  하기로 합의 (장애물은 지형 애셋 교체 시점으로, 아이템 방은 미니맵 이후
  — 바닥 드롭 아이템 로직 자체가 아직 없어서 그때 정의하기로 함)
- **미니맵 구현** — `RoomTransitionController`와 동일한 배선 패턴 재사용
  - 신규 `Assets/Scripts/UI/MinimapUI.cs`: `StageManager.BuildFloor()`가
    `Initialize(_roomsById.Values)`로 전체 방 목록을 넘겨주면, 각 방의
    `RoomEntered`/`Cleared` 이벤트를 직접 구독해 아이콘 색을 갱신.
    `Dictionary<int, Image>`로 방 ID→아이콘 매핑만 관리(별도 `MinimapIcon`
    클래스는 만들지 않음). 안개 효과 없음 — 지금은 고정 선형 5개 방이라
    가릴 게 없고, 나중에 랜덤 분기 맵이 들어올 때 같이 추가하기로 결정
  - `RoomController`에 `GridPosition` public 프로퍼티 추가(기존엔
    `RoomData` 내부에만 있었음)
  - `StageManager`에 `minimapUI` 필드 추가, 층 빌드 완료 후
    `minimapUI.Initialize(...)` 호출 한 줄만 추가
  - 씬: `Canvas/HUD/Minimap`(좌상단 앵커, `MinimapUI` 컴포넌트) +
    `Assets/Prefabs/UI/MinimapIcon.prefab`(28x28 단색 Image) 신규,
    `StageManager.minimapUI` 배선
  - 색상: 미방문=회색, 미방문 시작방=파란빛, 미방문 보스방=붉은빛,
    현재 위치=노랑(방문/클리어 여부와 무관하게 최우선), 클리어됨(현재 위치가
    아닐 때만)=초록

### 테스트 결과
- [x] 컴파일 에러/경고 없음
- [x] Play Mode에서 아이콘 5개가 40px 간격으로 정확히 생성됨 확인
      (anchoredPosition 0/40/80/120/160)
- [x] 초기 상태: Room1(시작방, 즉시 클리어)=노랑(현재), Room2~4=회색,
      Room5(보스)=붉은빛 — 정확히 일치
- [x] `Room2.EnterRoom()` 호출 후: Room1이 초록(클리어, 더 이상 현재 아님)으로
      전환, Room2가 노랑(현재)으로 전환, 나머지는 그대로 — 확인
- [x] Room2 강제 전멸(클리어) 후: Room2는 여전히 노랑 유지(현재 위치 우선
      규칙이 의도대로 동작 — 클리어돼도 플레이어가 아직 그 방에 있으면
      "현재" 표시가 "클리어" 표시보다 우선함) — 확인

### 다음 작업
- 미니맵 시각적 배치(좌상단 위치, 아이콘 크기 40px 간격)가 실제 화면에서
  괜찮은지 실플레이 확인
- 아이템 방(#3) 착수 — 바닥 드롭 아이템 로직 정의부터 시작
- 방 안 장애물(#2)은 지형 애셋 교체 시점까지 보류
- 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

### 추가 수정 (같은 날, 엔터 더 건전 스타일로 미니맵 재작업)
사용자 요청으로 미니맵을 우상단으로 옮기고, "엔터 더 건전(Enter the Gungeon)"
미니맵 시스템(안개 효과 + 방 연결선 + 배경 패널)으로 재작업.

- **위치**: `Minimap` 앵커/피벗을 (0,1)(좌상단) → (1,1)(우상단)으로 변경
- **안개 효과(3단계 `RevealState`)**: `Hidden`(아예 안 보임) →
  `Discovered`(방문한 방과 인접하면 타입 정보 없이 흐린 회색으로만 노출) →
  `Visited`(실제로 들어가야 진짜 색 공개). `RoomEntered` 이벤트에서 해당
  방을 `Visited`로, 그 방의 모든 `Doors[].TargetRoom`을 `Discovered`로
  승격시키는 방식 — 아이작 버전에서 "처음부터 전부 다 보여주기"로 했던 것과
  가장 크게 달라진 지점
- **방 연결선**: 방 쌍마다 `Image` 하나를 회전시켜 두 아이콘 중심을 잇는 얇은
  막대로 표현, 두 끝 방이 모두 `Hidden`이 아닐 때만 활성화. 중복 연결선
  방지를 위해 `(min(a,b), max(a,b))` 키로 방 쌍 하나당 하나만 생성
  - 신규 `Assets/Prefabs/UI/MinimapConnector.prefab` (pivot 0.5,0.5 회전용)
  - 기존 `MinimapIcon.prefab`도 pivot을 (0,1)→(0.5,0.5)로 재생성(연결선과
    좌표 계산 방식을 통일하기 위함)
- **배경 패널**: `Minimap` 루트 자체에 반투명 검정 `Image`(알파 0.5) 추가
- **랜드마크 색 유지**: 시작방/보스방은 `Visited`+클리어 이후에도 일반
  전투방과 다른 색(파란빛/붉은빛)을 영구적으로 유지하도록 함 — "현재 위치"
  일 때만 예외적으로 노랑이 우선함
- `MinimapUI.cs` 전면 재작성 (`Dictionary<int, RevealState>` 추가,
  `BuildConnectors()`/`BuildIcons()` 분리, `container` 필드 제거 후
  자기 자신의 `transform`을 컨테이너로 재사용하도록 단순화)

### 이슈/메모 — 작업 중 발견한 세션 이상 상황
씬 재배선 도중 Unity 에디터가 **Play 모드 상태**인 것을 뒤늦게 발견 — 마지막
Stop 호출 이후 Claude가 다시 Play를 누른 적이 없어 원인 불명(사용자 확인
결과 사용자가 직접 켠 것도 아니라고 함). Play 모드에서 만든 씬 오브젝트
(Minimap 재배치, 임시 프리팹 소스 오브젝트 등)는 Stop 시 전부 자동 폐기되는
것을 이용해 사용자 확인 후 즉시 Stop, 이후 순수 Edit 모드에서 씬 작업을
처음부터 다시 수행함. 이 과정에서 확인된 사실: `RunCommand`의
`result.DestroyObject()`는 Edit 모드에서는 즉시 반영되지 않을 수 있음
(`Object.Destroy`가 다음 프레임까지 지연되는데, 포커스 없는 자동화 환경은
프레임이 잘 안 도는 것으로 추정) — 반면 프리팹 저장(`PrefabUtility.
SaveAsPrefabAsset`)은 Play/Edit 모드와 무관하게 디스크에 정상 저장됨을
확인(재작업 중 기존에 저장된 프리팹 2개를 그대로 재사용해서 검증). Edit
모드에서 오브젝트를 확실히 지워야 할 때는 삭제 직후 `find`로 재조회해서
실제로 없어졌는지 검증하는 절차를 거칠 것.

### 테스트 결과 (Gungeon 스타일 재작업)
- [x] 컴파일 에러/경고 없음, 순수 Edit 모드에서 씬 재배선 완료 후 Play
      테스트로 검증
- [x] 초기 상태: Room1=노랑(현재), Room2=흐린 회색(발견됨, 타입 비공개),
      Room3~5=비활성(숨김) — 정확히 일치. 연결선도 1-2만 활성, 나머지 비활성
- [x] `Room2.EnterRoom()` 호출 후: Room1이 파란빛(시작방 랜드마크, 클리어)로
      전환, Room2=노랑(현재), **Room3가 새로 발견되어 흐린 회색으로 등장**,
      연결선 2-3도 새로 활성화됨 — 안개 효과가 인접 발견 방식으로 정확히
      동작함을 확인

---

## 2026-08-31 (5)

### 진행 내용
- 사용자 요청으로 방 클리어 후 보상 흐름을 전면 변경
  1. **"클리어 후 3개 스킬 중 선택" UI(`UpgradeSelectionUI`) 완전 폐기** —
     스킬 습득 방식은 추후 아이템 방(#3) 작업에서 바닥 드롭 픽업으로
     재정의할 예정이라 지금은 그냥 제거. 관련 스크립트
     `UpgradeSelectionUI.cs`/`UpgradeSlotView.cs` 삭제, 씬의 `UpgradePanel`
     오브젝트도 삭제. `PlayerSkillInventory.Acquire()`를 호출하던 유일한
     경로가 사라져 스킬 습득 경로가 당분간 없어짐 — 의도된 상태(다음 아이템
     작업에서 재정의)
  2. **장비(장착) UI는 ESC 키로 열고 닫는 방식으로 변경** — 신규
     `EquipMenuController`(Player에 배치)가 자체 `InputActionAsset`
     (`Assets/InputActions/EquipMenuActions.inputactions`, Menu 맵의
     `ToggleEquip` 액션, `<Keyboard>/escape` 바인딩)으로 토글 관리.
     `PrototypeSkillSwitcher`의 디버그 키(1/2/3)와 동일한 이유로 별도
     InputActionAsset을 씀 — 열려있는 동안 `PlayerInputActions`(이동/조준/
     공격/구르기 전체)를 잠그는데, 토글 키 자체가 그 안에 있으면 잠근 순간
     닫을 수 없게 되기 때문
  3. **결과적으로 방 클리어가 더 이상 게임을 중단시키지 않음** —
     `RoomFlowManager`에서 `RoomClearedForReward` 구독과 관련 핸들러
     (`HandleRoomClearedForReward`/`OnUpgradeChosen`/`OnEquipConfirmed`)
     전부 제거, `FloorCompleted`(보스 클리어 → Clear 오버레이) 처리만 남김.
     `playerInventory`/`playerInputActions` 필드도 더 이상 안 써서 제거
  - `EquipSelectionUI`에 `Hide()` 메서드 추가(패널만 닫음, 확인 로직 없이
    취소 용도), `UIManager`에 `HideEquipSelection()` 포워딩 메서드 추가,
    `ShowUpgradeSelection()`/`upgradeSelectionUI` 필드는 UIManager에서 제거
  - `StageManager.RoomClearedForReward` 이벤트 자체는 그대로 남겨둠(지금은
    구독자가 없어 발행만 되고 아무 일도 안 일어나지만, 다음 아이템 방
    작업에서 "전투방 클리어 시점"에 아이템을 드롭시키는 신호로 그대로
    재사용할 계획이라 미리 지우지 않음)

### 테스트 결과
- [x] 컴파일 에러/경고 없음
- [x] Room2를 강제 전멸시켜 클리어 — 클리어 직후 UI가 전혀 뜨지 않고
      `PlayerInputActions.enabled`가 계속 `True`로 유지됨을 확인(게임이
      끊기지 않음)
- [x] **실제 Input System을 통한 ESC 키 입력 시뮬레이션**
      (`InputSystem.QueueStateEvent`+`InputSystem.Update()`로 키보드 상태
      이벤트를 진짜로 흘려보냄, 메서드 직접 호출이 아님)으로 토글 왕복 검증:
      1차 ESC → `EquipPanel` 활성화 + 입력 잠금, 2차 ESC → `EquipPanel`
      비활성화 + 입력 재활성화. 완전한 엔드투엔드 검증 성공

### 다음 작업
- 아이템 방(#3) — 바닥 드롭 아이템 로직 정의, `RoomClearedForReward`를
  드롭 트리거로 재사용하는 방향 검토
- 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (6)

### 진행 내용
- 액티브 아이템(기존 스킬 3종을 이렇게 명명) 바닥 드롭/획득 시스템 신규 구현
  - 신규 `ActiveItemPickup`(`Assets/Scripts/Combat/`) — 바닥 픽업 오브젝트.
    트리거로 Player 충돌 감지 → `PlayerSkillInventory.Acquire()` 호출 +
    `PickupNotificationUI.Show()`로 이름 표시 + 자기 자신 `Destroy()`
  - 신규 `ActiveItemDropManager`(`Assets/Scripts/Combat/`) — 드롭 확률(10%,
    Inspector 노출) 관리 + **한 번 드롭된 아이템은 풀에서 제거되어 다시
    드롭되지 않음**(`List<SkillItemData>`에서 뽑을 때마다 제거)
  - 신규 `PickupNotificationUI`(`Assets/Scripts/UI/`) — Player 자식으로 달린
    월드 스페이스 캔버스+`BillboardUI`, 획득 시 "{아이템명} 획득" 텍스트를
    1.5초간 표시
  - `EnemySpawner.SpawnRoom()`에 적 사망 시 드롭 굴림 로직 추가 —
    자기 자신을 즉시 구독 해제하는 로컬 함수로 처리(적이 GameObjectPool로
    재사용되는 구조라, 매번 새로 구독하고 안 지우면 재사용될 때마다 중복
    구독이 쌓여 사망 한 번에 드롭이 여러 번 굴러가는 버그가 생길 수 있어서)
  - 씬: `ActiveItemDropManager` 오브젝트 신규(액티브 아이템 3종 SO 참조 +
    드롭 확률 0.1 + `ActiveItemPickup.prefab` 참조), `EnemySpawner`에 배선.
    Player에 `PickupNotificationCanvas`(월드 스페이스, `BillboardUI`,
    `PickupNotificationUI`) 신규 추가
  - 신규 `Assets/Prefabs/ActiveItemPickup.prefab` — 납작한 원기둥("둥근
    단추" 모양), 트리거 콜라이더, 노란색 머티리얼
  - **플레이어 시작 상태 변경**: `PlayerSkillInventory.startingSkill`,
    `SkillController.defaultSkill` 둘 다 씬에서 None으로 변경(코드는 이미
    null 가드가 있어 필드 값만 비움) — 이제 아무것도 소지/장착하지 않은
    채로 시작. 액티브 아이템 습득 경로가 이제 드롭 픽업 하나뿐이라 자연스럽게
    맞물림
  - "액티브 아이템"은 사용자 요청대로 용어(명명)만 반영 — `SkillItemData`/
    `ISkillItem`/`SkillController` 등 기존 클래스명은 그대로 유지(실제
    사용처가 많아 전면 리네임은 요청 범위를 넘어선다고 판단, 필요하면 후속
    작업으로 별도 진행)

### 이슈/메모 — 개발 중 발견 및 수정한 버그
- **`PickupNotificationUI`를 못 찾던 버그**: `ActiveItemPickup.OnTriggerEnter`
  에서 처음엔 `other.TryGetComponent<PickupNotificationUI>()`를 썼는데,
  `PickupNotificationUI`는 Player의 **자식** 오브젝트(`PickupNotificationCanvas`)
  에 있어서 `TryGetComponent`(같은 오브젝트만 검색)로는 못 찾음.
  `other.GetComponentInChildren<PickupNotificationUI>(true)`로 교체
  (부모→자식 검색 + 비활성 포함) — 여기서 `includeInactive: true`가 필수인
  이유는 그 다음에 발견한 두 번째 버그와 연결됨
- **비활성 자기자신을 찾지 못하던 버그**: `PickupNotificationUI.Awake()`가
  `panel.SetActive(false)`로 **자기 자신이 붙어있는 오브젝트**를 비활성화하는
  패턴인데, `GetComponentInChildren`는 기본값(`includeInactive: false`)이라
  비활성 상태인 그 오브젝트 자체를 검색에서 제외해버려서 항상 못 찾았음.
  `includeInactive: true`를 명시해서 해결. 이 프로젝트에서 "자기 자신을
  Awake에서 비활성화하는 UI 패턴"이 이미 한 번 문제(2026-08-24,
  `ClearOverlayPanel` 첫 활성화 타이밍 버그)를 일으킨 적이 있어서, 앞으로도
  이 패턴을 쓰는 새 UI를 외부에서 `GetComponentInChildren`으로 찾을 때는
  `includeInactive: true`를 기본으로 챙길 것

### 테스트 결과
- [x] 컴파일 에러/경고 없음
- [x] 시작 시 `SkillController.CooldownProgress=0`, `PlayerSkillInventory.
      Owned.Count=0`, `EquippedSkill=null` — 완전히 빈 상태로 시작 확인
- [x] **드롭 확률/중복 방지 검증**: `ActiveItemDropManager.TryDrop()`을
      200회 강제 호출 — 정확히 3개(액티브 아이템 종류 수)만 스폰되고 이후
      100회 추가 호출에도 더 이상 스폰되지 않음(콤보 확률상 200회 중
      3종 모두 최소 1번씩 성공할 확률이 사실상 100%에 가까움을 이용한 검증)
- [x] **획득 플로우**: 플레이어를 스폰된 픽업 위치로 이동 + 물리 스텝 강제
      실행 → `PlayerSkillInventory.Owned.Count`가 1 증가, 실제 습득한
      아이템명 확인, 픽업 위치의 알림 UI가 활성화되고 "{이름} 획득" 텍스트
      정확히 표시됨을 확인
- [x] **기존 장비 UI와의 연동**: 새로 획득한 아이템이 ESC 장비 메뉴의
      슬롯에 나타나고, 실제 클릭+확인으로 장착까지 정상 동작, 장착 후
      `SkillController`가 정상적으로 해당 스킬을 인식함(쿨다운 0으로 시작)
      확인

### 다음 작업
- 아이템 방(#3) 구체 설계 — 이번에 만든 `ItemPickup`/`ItemDropManager`를
  그대로 재사용해 "전투 없이 보장된 픽업 배치"만 추가하면 될 것으로 보임
- 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (7)

### 진행 내용
- 패시브 아이템(능력치 상승) 도입을 앞두고, "스킬" 하나만 전제하던 기존
  클래스명 전체를 액티브/패시브 공존 가능한 이름으로 리네임
  - `SkillItemData`→`ActiveItemData`, `ISkillItem`→`IActiveItem`,
    `SkillController`→`ActiveItemController`, `PlayerSkillInventory`→
    `PlayerActiveItemInventory`, `OwnedSkill`→`OwnedActiveItem`,
    `SkillContext`→`ActiveItemContext`, `PrototypeSkillSwitcher`→
    `PrototypeActiveItemSwitcher`, `EquipSelectionUI`→`ActiveItemEquipUI`,
    `SkillCooldownUI`→`ActiveItemCooldownUI`, `EquipSlotView`→
    `ActiveItemEquipSlotView`, `Grenade/EnhancedShot/FlamethrowerSkill(Data)`→
    `.../ActiveItem(Data)`, `SkillLeveling`→`ActiveItemLeveling`
  - 지난 세션에서 만든 `ActiveItemPickup`/`ActiveItemDropManager`는 반대로
    "Active" 접두를 떼고 `ItemPickup`/`ItemDropManager`로 리네임 — 사용자
    요청 이유: 패시브 아이템도 액티브와 동일한 드롭/픽업 방식을 쓸 예정이라,
    드롭·픽업 인프라 자체는 아이템 종류에 중립적인 이름이어야 함(내부 로직은
    아직 `ActiveItemData`만 다루고, 실제로 패시브 타입을 받아들이도록
    일반화하는 건 패시브 설계가 나온 뒤 별도 작업으로 미룸 — 지금 미리
    다형성 구조를 만들어두지 않음)
  - `ActiveItemData.SkillName`→`ItemName`, `CreateSkill()`→
    `CreateActiveItem()`, `PlayerActiveItemInventory.EquippedSkill`→
    `EquippedItem` 등 공개 프로퍼티/메서드명도 함께 정리
  - **직렬화된 `[SerializeField]` 필드 이름은 전부 그대로 유지**(예:
    `ActiveItemData.skillName`, `PlayerActiveItemInventory.startingSkill`) —
    필드 이름을 바꾸면 Unity가 기존 씬/에셋에 저장된 값을 못 찾아 데이터가
    날아가므로, 공개 API(프로퍼티/메서드)만 새 이름으로 감싸고 내부 직렬화
    필드는 건드리지 않는 방식으로 안전하게 처리
  - 파일 이동은 `Unity_ManageAsset`의 `Move` 액션으로 처리(전부 "에러"
    메시지를 반환했지만 실제로는 정상 처리됨 — 이 도구의 알려진 오보고 버그,
    매번 `Glob`으로 실제 파일 위치를 재확인해서 검증함). 이 방식은 delete+
    recreate와 달리 스크립트 GUID를 보존해서, 씬에 이미 배선된 컴포넌트
    참조(Player, EquipPanel, SkillCooldownSlot 등)가 안 깨짐

### 이슈/메모 — 이번 세션 최대 사고: 재컴파일이 멈춘 사건
파일 리네임 직후 콘솔에 `Assets/Scripts/Combat/SkillItemData.cs could not
be found` 에러가 계속 떠서, 처음엔 "그냥 오래된 캐시 메시지겠거니" 하고
`AssetDatabase.Refresh`/`RequestScriptCompilation`/`RequestScriptReload`를
여러 번 호출했지만 전혀 반응이 없었음(`IsCompiling`이 계속 `False`). 원인을
좁히려고 **`Library/ScriptAssemblies` 폴더를 Bash로 직접 삭제**하는 실수를
저질렀는데, 이게 오히려 Unity 실행 중인 프로세스의 증분 빌드 상태를 완전히
꼬이게 만들어서 이후로는 스크립트 API를 통한 어떤 재컴파일 요청도 먹히지
않는 상태가 됨(`RunCommand`로 새 타입(`ActiveItemController`)을 직접
참조해보면 "타입을 찾을 수 없음" 컴파일 에러로 실제 Assembly-CSharp이
전혀 재빌드되지 않았음을 확인). `EditorApplication.UnlockReloadAssemblies()`/
`StopAssetEditing()` 등도 시도했지만 전부 무의미했음(오히려
`StopAssetEditing` 쪽은 "매칭되는 Start 없음" 에러만 반복 — 즉 애초에
에셋 편집 배치 잠금 문제도 아니었음). 결국 **사용자가 직접 Unity
프로젝트 창에서 우클릭 → Reimport All**을 눌러서 Unity 자체를 재시작시켜
해결됨. **교훈**: Unity가 실행 중인 상태에서 `Library/` 캐시 폴더를
외부 프로세스로 직접 지우는 건(사람이 직접 Unity Editor UI를 통해서 하는
게 아니라면) 하지 말 것 — 재컴파일 문제가 의심되면 스크립트 API 호출로
먼저 시도하고, 그래도 안 되면 사용자에게 직접 Reimport All을 요청할 것.
(참고: `Unity_GetConsoleLogs`가 이미 해결된 옛날 에러를 계속 다시 보여주는
캐시 문제가 있다는 것도 이번에 확인 — `Unity_ReadConsole`(Clear 액션 지원)
쪽이 더 신뢰할 수 있는 실시간 콘솔 상태를 보여줌. 이후로는 콘솔 확인 시
`Unity_ReadConsole`을 우선 사용할 것)

### 테스트 결과
- [x] 재컴파일 완료 후 컴파일 에러/경고 없음 (`Unity_ReadConsole`로 확인)
- [x] `RunCommand`에서 `ActiveItemController`/`PlayerActiveItemInventory`
      타입을 직접 참조해 정상 컴파일됨을 확인 — 리네임이 실제로 반영됨
- [x] 씬 컴포넌트 보존 확인: `Player`의 `ActiveItemController`가 기존과
      동일한 `instanceID`로 남아있고 `PlayerActiveItemInventory.Owned`
      데이터도 그대로 유지됨(GUID 보존 방식이 의도대로 동작)
- [x] 기존 3개 액티브 아이템 데이터 에셋(`GrenadeData`/`EnhancedShotData`/
      `FlamethrowerData.asset`)이 `ActiveItemData` 타입으로 정상 로드되고
      저장된 이름값(`ItemName`)도 그대로 보존됨을 확인
- [x] Play Mode 전체 흐름 재검증: 캐릭터 선택 → 드롭 200회 굴림(3개 스폰) →
      픽업 획득(Owned 1 증가, "Enhanced Shot 획득" 알림 정상 표시) → ESC
      장비 메뉴 → 슬롯 클릭+확인 → 장착 확인("Equipped: Enhanced Shot") →
      메뉴 닫힘까지 리네임 이전과 동일하게 전부 정상 동작

### 다음 작업
- 패시브 아이템 설계 착수 시, `ItemPickup`/`ItemDropManager`가 `ActiveItemData`
  뿐 아니라 패시브 데이터도 다루도록 일반화하는 작업 필요(공통 베이스
  타입/인터페이스 설계 포함)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (8)

### 진행 내용
- **패시브 아이템 시스템 신규 구현** — 액티브 아이템과 동일하게 적 처치/
  아이템 방 드롭으로 획득, 충돌 즉시 적용(장비 UI 불필요), 획득해도 드롭
  풀에서 제거되지 않아 같은 종류를 여러 번 먹고 스택 가능하게 설계.
  - 공통 베이스 `ItemData : ScriptableObject`(추상 `OnAcquired(GameObject)`)
    신설 — `ActiveItemData`가 이를 상속하도록 변경(기존 `OnAcquired`
    구현은 `PlayerActiveItemInventory.Acquire()` 호출로 이동), 신규
    `PassiveItemData`(단일 concrete 클래스 + `PassiveItemType` enum
    판별자: 액티브처럼 타입별 서브클래스가 필요할 만큼 행동이 다르지
    않고 전부 "스탯에 값 더하기"라 다형성 대신 enum 분기로 단순화)
  - `PassiveItemType` 4종과 배율: 공격력 증가(+2.5), 이동 속도 증가(+1.5),
    발사체 이동 거리 증가(+0.3초), 공격 속도 증가(+10%) — 각각
    `Assets/Data/PassiveItems/*.asset`으로 authoring, 타입별 고유 색상 지정
  - `ItemPickup`을 `ActiveItemData` 전용에서 `ItemData` 다형 처리로 일반화
    (`_item.OnAcquired(other.gameObject)` 호출로 액티브/패시브 분기 없이
    동작), 패시브인 경우 `visualRenderer.material.color`를 데이터의 색상으로
    틴트. `ItemDropManager`는 액티브(1회성, 뽑히면 풀에서 제거)/패시브
    (무제한, 절대 제거 안 함) 두 풀을 유지하고 매 성공 롤마다 "남은 액티브 수
    + 패시브 수" 범위에서 균등 추첨(고정 비율 대신 자연스러운 가중치)
  - 신규 `PlayerPassiveStats`(Player에 부착) — 누적 스탯만 들고 있는 순수
    저장소: `AttackDamageBonus`/`MoveSpeedBonus`/`ProjectileLifetimeBonus`/
    `AttackSpeedMultiplier`. `PlayerMovement.moveSpeed`, `PlayerCombat`→
    `WeaponData.CreateWeapon()`(신규 3번째 인자로 전달), `MeleeWeapon`/
    `RangedWeapon`이 각각 읽어서 반영
  - 원거리 기본 공격에 연사 속도 제한이 아예 없던 걸 발견 — 패시브 "공격
    속도 증가"가 의미를 가지려면 필요해서 `RangedWeaponData.fireCooldown`
    (기본 0.3초) 신규 추가, `RangedWeapon`에 쿨다운 게이트 로직 추가,
    `IWeapon`에 `Tick(float)` 신설(매 프레임 쿨다운 감소용), `PlayerCombat`에
    `Update()`를 새로 추가해 매 프레임 `_weapon.Tick()` 호출. 근접 무기는
    `Tick()`이 아무 동작 없는 no-op
  - `Projectile.Initialize`/`ProjectileSpawner.Spawn`에 `bonusLifetime`
    옵션 인자 추가(기본값 0) — 액티브 아이템(`EnhancedShotActiveItem`)의
    기존 3-인자 호출부는 그대로 두어도 컴파일/동작 그대로 유지되도록
    일부러 옵션 인자로 설계(패시브 사거리 증가는 플레이어 기본 공격에만
    적용, 액티브 아이템 스킬에는 영향 없음)
  - 패시브 픽업 비주얼: "누워있는 캡슐" — `Assets/Prefabs/
    PassiveItemPickup.prefab` 신규(Capsule 프리미티브, Z축 90도 회전,
    트리거 콜라이더, 베이스 머티리얼은 `SetItem()` 시점에 인스턴스 컬러로
    틴트). 액티브 아이템 픽업(`ActiveItemPickup.prefab`, 원판 모양)은
    그대로 유지

### 이슈/메모
- `Unity_ManageGameObject`의 `set_component_property`로 컴포넌트 참조
  필드(예: `passiveStats`)를 배선할 때, 문서에 나온 `{"find": "...",
  "component": "..."}` 형태와 순수 `instanceID` 정수 둘 다 이 필드에서는
  "Property 'passiveStats' not found. Did you mean: passiveStats?"라는
  자기 자신을 추천하는 이상한 에러로 실패함. `{"instanceID": N}` 형태로
  감싸서 보내니 정상 동작 — 컴포넌트 참조 필드 배선 시 이 형태를 우선
  시도할 것

### 테스트 결과
- [x] 컴파일: 수정/신규 스크립트 전부 `Unity_ReadConsole` 기준 에러 0건
- [x] Play Mode: 패시브 아이템 5개(공격력 증가 x2 + 나머지 3종 각 1개)를
      플레이어 위치에 스폰 후 트리거 충돌 강제 발생 → `PlayerPassiveStats`
      값이 정확히 `AttackDamageBonus=5, MoveSpeedBonus=1.5,
      ProjectileLifetimeBonus=0.3, AttackSpeedMultiplier=1.1`로 누적(스택
      정상 동작), 획득 알림 UI 컴포넌트 정상 확인
- [x] Play Mode: `RangedWeapon`을 직접 인스턴스화해 `Attack()`/`Tick()`
      검증 — 연속 `Attack()` 2회 호출 시 두 번째는 쿨다운에 막혀 발사체
      생성 안 됨(활성 발사체 개수 불변), `Tick(1f)`로 쿨다운 해소 후
      `Attack()` 호출 시 정상 발사(개수 +1)
      확인. 데미지 보너스 반영 확인: 기본 8 + 공격력 보너스 5 = 13 데미지
      발사체 3개가 같은 지점에 겹쳐 타겟에 적중 → 체력 100 → 61(39 =
      13 x 3) — 계산과 정확히 일치
- [ ] 실제 사람 입력(마우스 클릭)으로 여러 프레임에 걸친 발사체 이동→
      명중까지의 풀 플로우는 이 자동화 환경의 알려진 한계(Time.deltaTime/
      Update()가 실제 프레임 경계 없이는 진행되지 않음)로 미검증 — 로직
      자체는 위 직접 호출 테스트로 검증됨, 실플레이 확인 필요
- [ ] 패시브 픽업의 "누워있는 캡슐" 비주얼과 타입별 색상 틴트를 에디터
      화면으로 직접 눈으로 확인하지 않음(트리거/스탯 값만 코드로 검증) —
      실플레이 시 육안 확인 필요

### 다음 작업
- 실제 플레이로 패시브 아이템 낙하 확률(10%, 액티브 풀과 공유)과 비주얼
  확인
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (9)

### 진행 내용
- 사용자 피드백 반영 1: 패시브 아이템 드랍 확인 요청 — `ItemDropManager`/
  `EnemySpawner` 로직 재검토 결과 특정 아이템만 막히는 버그는 없음을 확인.
  현재 층 총 적 12마리 × 드랍 확률 10% = 기댓값 1.2회이므로, 이미 2개
  획득 후 추가 드랍이 없는 건 정상적인 확률 편차로 결론(코드 수정 없음)
- 사용자 피드백 반영 2: 패시브 아이템 픽업이 "사선으로 누운" 모양이 아니라는
  피드백 — `Assets/Prefabs/PassiveItemPickup.prefab`의 회전을
  `(0, 0, 90)`→`(0, 45, 90)`으로 수정(눕는 축은 유지하고 Y축 45도 추가
  회전으로 축 정렬이 아닌 대각선으로 보이게 함). `Unity_SceneView_
  CaptureMultiAngleSceneView`로 탑뷰 스크린샷 찍어 시각 확인
- ESC 장비 메뉴(`EquipPanel`) 레이아웃을 좌/우 분할로 개편: 좌측에 플레이어
  누적 패시브 스탯(공격력/이동속도/발사체 사거리/공격속도 4종, 패시브
  아이템 종류와 1:1 대응) 표시, 우측은 기존 장비 슬롯+확인 버튼 레이아웃
  그대로 유지
  - 신규 `PlayerStatsPanelUI`(단일 `Text`에 4줄 포맷 문자열 출력,
    `Refresh(PlayerPassiveStats)`) — 개별 스탯마다 별도 `Text`를 두는 대신
    가장 단순한 형태로 구현(스탯 4개 전부 항상 같이 보여지고 개별 스타일링
    요구가 없어서 다중 컴포넌트로 나눌 이유가 없음)
    `EquipMenuController`에 `playerPassiveStats` 필드 추가 →
    `UIManager.ShowEquipSelection(inventory, passiveStats, onConfirmed)` →
    `ActiveItemEquipUI.Show(inventory, passiveStats, onConfirmed)`로 전달,
    열 때마다 `statsPanel.Refresh()` 호출
  - 씬 구조: `EquipPanel` 아래 `StatsPanel`(좌, anchorMin(0,0)~anchorMax
    (0.5,1))과 `ItemsPanel`(우, anchorMin(0.5,0)~anchorMax(1,1)) 신설,
    기존 `EquipSlot1~3`/`ConfirmButton`은 `ItemsPanel`로 재배치(앵커가
    이미 (0.5,0.5) 중심 고정이라 `SetParent(parent, false)`만으로 상대
    위치 그대로 유지됨 — 별도 좌표 재계산 불필요)

### 이슈/메모
- `Unity_RunCommand`가 가끔 "Unity not detected (no fresh discovery
  files found)" 에러를 일시적으로 반환하는 현상 재확인(이번에도 즉시
  재시도하니 정상 동작) — Library 삭제 사고와는 무관한 단순 일시적 디스커버리
  파일 타이밍 이슈로 보임, 재시도로 해결되면 그냥 넘어갈 것
- 작업 도중 사용자가 직접 Play 모드로 들어가 테스트하고 있었음
  (`IsPlaying: true`를 예상치 못하게 감지) — 사용자가 실행 중인 세션을
  임의로 Stop하지 않고, 필요한 검증(프리팹 회전 확인용 임시 인스턴스 생성)만
  마치고 정리한 뒤 사용자가 테스트를 마칠 때까지 씬 저장은 보류함

### 테스트 결과
- [x] 컴파일 에러 0건
- [x] Play Mode: ESC 입력 시뮬레이션(`InputSystem.QueueStateEvent`)으로
      `EquipPanel` 정상 오픈, `StatsPanel`/`ItemsPanel` 둘 다 활성화 확인,
      `StatsText`가 실제 누적된 값("공격력 증가: +5\n이동 속도 증가:
      +1.5\n발사체 이동 거리 증가: +0초\n공격 속도 증가: +0%")을 정확히
      출력함을 확인
  - [x] `StatsPanel`/`ItemsPanel`의 `RectTransform` 월드 좌표 확인 —
      좌측 40~645px, 우측 665~1330px로 겹침 없이 분리됨
- [ ] 실제 화면 렌더링(Screen Space - Overlay Canvas라 카메라 캡처로는
      찍히지 않음)으로 육안 확인은 못함 — RectTransform 수치 계산과 텍스트
      내용으로만 검증, 실플레이 시 최종 육안 확인 필요

### 다음 작업
- 실플레이로 ESC 메뉴 좌/우 분할 레이아웃 육안 확인
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (10)

### 진행 내용
- 사용자 피드백: 좌측 스탯 표시를 "공격력 증가: +0%" 같은 문장형 텍스트가
  아니라 바인딩 오브 아이작 스타일로 간결하게 하고 싶다는 요청 → 리서치 후
  계획 수립, 승인받아 구현
  - 리서치: 아이작의 일시정지 메뉴 스탯 바는 퍼센트가 아니라 스탯별로
    미리 정해둔 "구간표"를 기준값에 대충 맞춰 칸을 채우는 방식(위키 표현
    "an imprecise scale"). 실제 데미지 자체는 `기본데미지 × √(증가아이템수
    ×1.2+1)` 제곱근 소프트캡 공식을 쓰는데, 이건 우리 시스템(아이템당 고정
    + 값, 무제한 스택)엔 그대로 적용 불가 → 아이디어만 차용해서 재설계
  - 결정: 10칸 Pip 바 + "아이템 몇 개면 만땅인지"를 아이템 종류별로 직접
    authoring하는 개수 기준 캡(퍼센트 계산 아님) — 공격력 8개/이동속도
    6개/사거리 6개/공속 8개= 만땅으로 설정
  - `PassiveItemData`에 `capCount` 필드 추가(에셋별 authoring, 드랍 시스템과
    같은 소스 오브 트루스를 공유 — UI 쪽에 별도로 캡 값을 중복 보관하지
    않음), `PlayerPassiveStats`에 타입별 획득 개수 카운터 추가
    (`AttackPowerCount` 등, 기존 누적 보너스 값과 별도로 관리)
  - `PlayerStatsPanelUI` 전면 재작성: 스탯 4종 각각 "색상 아이콘(추후 실제
    픽토그램으로 교체 예정, 지금은 해당 패시브 아이템의 색과 동일한 색
    스와치) + 10칸 Pip 바"로 표시. Pip 개수/컨테이너를 씬에 미리 심어두는
    대신 `Awake()`에서 코드로 생성(이 프로젝트의 기존 관례 — 미니맵
    아이콘/커넥터도 같은 방식)
  - 씬 구조: `StatsPanel`에 `VerticalLayoutGroup` 추가, 기존 `StatsText`
    제거, `AttackPowerRow`/`MoveSpeedRow`/`ProjectileRangeRow`/
    `AttackSpeedRow` 4개 신설(각각 `HorizontalLayoutGroup` + `Icon` +
    `PipsContainer`), `PlayerStatsPanelUI`의 4개 `StatRow`(data/icon/
    pipsContainer)를 `SerializedObject` 점(dot) 경로로 배선

### 이슈/메모
- `Unity_RunCommand`에서 `UnityEngine.UI.Image`를 `using Image = ...;`
  별칭이나 `using UnityEngine.UI;`로 끌어와 짧게 `Image`라고만 쓰면
  `CS0118: 'Image' is a namespace but is used like a type` 에러 발생 —
  원인 불명(이 자동실행 스크립트 환경에서만 재현, 실제 프로젝트 스크립트
  파일에서는 문제 없음). 매번 `UnityEngine.UI.Image`로 완전히 명시하니
  정상 컴파일됨 — 이후 `RunCommand`로 UI 코드 짤 때는 `Image` 별칭/using
  없이 전체 네임스페이스로 쓸 것

### 테스트 결과
- [x] 컴파일 에러 0건
- [x] Play Mode: `PlayerPassiveStats.Apply()`를 캡보다 적거나 정확히
      맞거나 초과하는 다양한 개수로 직접 호출(공격력 4/8, 이동속도 3/6,
      사거리 6/6, 공속 2/8) → ESC로 메뉴 연 뒤 각 행의 Pip `Image.color.a`
      값을 읽어 채워진 칸 수 확인: 5/10, 5/10, 10/10, 2/10 — 전부 기대한
      비율과 정확히 일치(사거리는 캡을 정확히 채워 10/10 만땅 확인)

### 다음 작업
- 실플레이로 Pip 바 색상/채움 상태를 화면에서 육안으로 확인(Screen Space
  Overlay라 카메라 캡처 불가, 지금까지는 컬러값/알파값으로만 검증)
- 아이콘 스와치를 실제 픽토그램(칼/발자국/화살/시계 등)으로 교체 — 사용자가
  아이콘 제작하면 `PlayerStatsPanelUI.BuildRow()`의 `icon.color` 대신
  `icon.sprite` 배선으로 변경 필요
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (11)

### 진행 내용
- 사용자 피드백: Pip 바가 패시브 보너스만 반영하고 "기본 스탯"은 빠져있다는
  지적 → 기본 스탯 + 보너스를 합친 실제 값 기준으로 바 채움을 계산하도록
  재설계
  - 핵심 아이디어: 기존에 튜닝해둔 `CapCount`(공8/이속6/사거리6/공속8) 값을
    그대로 재사용하면서 기본 스탯만 얹는 공식으로 변경 —
    `(기본값 + 보너스) / (기본값 + CapCount × 아이템당증가량)`. 이러면
    아이템 0개 상태에서도 기본 스탯 비율만큼 이미 칸이 채워져 보이고,
    이전과 똑같은 개수를 모았을 때 만땅이 되는 타이밍은 그대로 유지됨
    (분자·분모에 기본값을 동일하게 더한 것뿐이라 기존 밸런스가 안 깨짐)
  - 기본값 소스 배선: `PlayerMovement.BaseMoveSpeed`(기존 `moveSpeed`
    필드 노출), `Projectile.BaseLifetime`(기존 `lifetime` 필드 노출),
    `PlayerCombat.EquippedWeaponDamage`(신규 — `EquipWeapon()` 호출 시
    받은 `WeaponData`를 저장해뒀다가 데미지 값 노출, 근접/원거리 등 현재
    장착된 무기에 따라 실시간으로 달라짐), 공격 속도는 배율 개념이라 기본값
    1.0을 그냥 상수로 사용(외부 배선 불필요)
  - 더 이상 개수 비율로 채우지 않으므로 `PlayerPassiveStats`의 타입별 개수
    카운터(`AttackPowerCount` 등, 직전 세션에 추가했던 것)는 안 쓰이게 되어
    제거 — 죽은 필드를 남겨두지 않음
  - `PlayerStatsPanelUI`가 이제 `PlayerCombat`/`PlayerMovement`/
    `Projectile` 프리팹 참조를 직접 들고 `Refresh()` 때마다 기본값을
    라이브로 읽어옴(장비 UI 열 때마다 최신 장착 무기 기준으로 재계산됨)

### 이슈/메모
- `Unity_RunCommand`가 "Unity not detected (no fresh discovery files
  found)"를 연속 2번 반환한 뒤 3번째 재시도에 성공하는 경우가 있었음 —
  이전에 확인한 일시적 디스커버리 파일 타이밍 이슈와 동일 현상으로 보임,
  `Unity_ManageEditor GetState`로 에디터가 정상 응답하는 것 확인 후 재시도로
  해결

### 테스트 결과
- [x] 컴파일 에러 0건
- [x] Play Mode: 원거리 무기 장착(기본 데미지 8) 상태에서 아이템 0개일 때
      Pip 바가 이미 3/10(공격력), 4/10(이동속도), 6/10(사거리), 6/10(공속)
      만큼 채워져 있음을 확인(전부 기본 스탯 비율과 정확히 일치하는 계산값)
  - [x] 이어서 공격력 4개/이동속도 3개/사거리 6개/공속 2개 획득 후 —
      6/10, 7/10, 10/10(정확히 만땅), 7/10으로 갱신됨을 확인, 전부 수식
      계산값과 일치

### 다음 작업
- 실플레이로 Pip 바 색상/채움 상태 육안 확인 (계속 이월 중)
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (12)

### 진행 내용
- 사용자 피드백: "본인조차 헷갈릴 수 있다"며 (1) 각 스탯 줄 위에 무슨
  능력치인지 텍스트로 표시, (2) 스탯 영역을 좌측 패널 안에서 좀 더
  오른쪽 아래로 이동 요청
  - 기존 `AttackPowerRow`/`MoveSpeedRow`/`ProjectileRangeRow`/
    `AttackSpeedRow`(아이콘+Pip 바만 있던 가로 줄) 각각을 새
    `...RowGroup`(세로 `VerticalLayoutGroup`)으로 감싸고, 그 안에
    "Label"(텍스트: 공격력/이동속도/사거리/공격속도) → 기존 Row 순서로
    배치. `PlayerStatsPanelUI`가 들고 있던 `Icon`/`PipsContainer` 참조는
    Row를 통째로 재부모화한 것뿐이라 전혀 안 깨짐(그대로 유효)
  - `StatsPanel`의 `VerticalLayoutGroup.childAlignment`을
    `UpperLeft`→`LowerRight`로 변경, 패딩도 조정 — 좌측 패널 안에서
    스탯 4줄 전체가 오른쪽 아래로 몰리도록 함(좌/우 분할 자체는 그대로
    유지, 그 안에서의 정렬만 변경)

### 이슈/메모
- **자동화 환경 한계 재확인**: `RunCommand`로 UI 계층 구조를 변경한 직후
  같은 스크립트 안에서 자식들의 `RectTransform.anchoredPosition`을 읽으면
  전부 기본값 `(0,0)`/100x100으로 보였음(레이아웃 재계산이 아직 한 프레임도
  안 돈 상태) — `Canvas.ForceUpdateCanvases()` +
  `LayoutRebuilder.ForceRebuildLayoutImmediate(statsPanelRect)`을 명시적으로
  호출해야 그 자리에서 바로 올바른 좌표가 나옴. 실제 게임 플레이에서는
  `SetActive(true)` 시 Unity가 자동으로 레이아웃을 재계산하므로 문제
  없지만, **에디터 자동화로 UI 레이아웃을 만들고 나서 그 자리에서
  검증하거나 값을 확정해서 씬에 저장하려면 항상 이 강제 리빌드 호출을
  거칠 것** — Time.frameCount가 멈춰있는 이 환경의 알려진 한계와 같은
  계열의 증상

### 테스트 결과
- [x] 컴파일 에러 0건
- [x] Play Mode: ESC 메뉴에서 4개 라벨("공격력"/"이동속도"/"사거리"/
      "공격속도")이 각 줄 위에 정확히 표시됨을 확인
- [x] 강제 리빌드 후 `RectTransform.anchoredPosition` 확인: 4개 줄이
      전부 x=535(패널 우측 정렬), y가 -228/-348/-468/-588로 120px씩
      일정하게 아래로 쌓여 겹침 없이 정렬됨을 확인
- [ ] 실제 화면 렌더링 육안 확인은 못함(Screen Space Overlay, 계속 이월)

### 다음 작업
- 실플레이로 라벨+Pip 바 좌/우 분할 및 우하단 정렬 육안 확인 (계속 이월 중)
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (13)

### 진행 내용
- 우하단 정렬이 사용자 기준 "너무 중앙에 있다"는 피드백 → 1차로 우측
  패딩을 20→140으로 늘려 좌측으로 살짝 밀었다가, 곧이어 아예 좌측 패널
  안에서 "가운데 정렬"로 최종 변경 요청받아 반영
  - `StatsPanel`의 `VerticalLayoutGroup.childAlignment`을
    `LowerRight`→`MiddleCenter`로, `padding`을 좌우 대칭(20,20,20,20)으로
    변경 — 좌/우 화면 분할 구조는 그대로 유지, 좌측 구역 안에서 스탯 4줄
    블록이 가로/세로 모두 중앙 정렬되도록 함

### 이슈/메모
- 레이아웃 그룹이 자식의 `anchorMin`/`anchorMax`/`pivot`을 정렬 방식에 맞게
  내부적으로 재작성한다는 것을 확인 — 그래서 재빌드 후 `anchoredPosition`
  숫자만 보면 오해하기 쉬움(예: 가운데 정렬인데 `anchoredPosition.x`가
  패널 절반 폭과 같은 302.5로 나와서 "안 먹혔나?" 싶었음). **앞으로 UI
  위치 재배치를 검증할 때는 `anchoredPosition`이 아니라 `RectTransform.
  position`(월드 좌표)으로 확인할 것** — anchor 재작성 여부와 무관하게
  항상 정확함

### 테스트 결과
- [x] 컴파일 불필요(레이아웃 프로퍼티만 변경, 코드 변경 없음), 콘솔 에러 0건
- [x] `RectTransform.position`(월드 좌표) 확인: 4줄 전부 x=342.5로
      `StatsPanel`의 월드 중심 x와 정확히 일치(가로 중앙 정렬 확인),
      y값 554/434/314/194가 `StatsPanel` 중심 y=374를 기준으로 정확히
      대칭(세로 중앙 정렬 확인)

### 다음 작업
- 실플레이로 최종 가운데 정렬 배치 육안 확인 (계속 이월 중)
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (14)

### 진행 내용
- 사용자 요청: 패시브 아이템 드랍율을 50%로 인상
  - 기존엔 액티브/패시브가 하나의 `dropChance`(10%) 롤을 공유하고, 성공
    시 "남은 액티브 + 전체 패시브" 풀에서 균등 추첨하는 구조였음 — 이
    구조로는 액티브와 패시브에 서로 다른 확률을 줄 수 없어서, 액티브용
    `dropChance`(10%, 필드명 유지 — 기존 씬에 저장된 값 보존)와 패시브용
    `passiveDropChance`(신규, 50%)를 분리하고 `TryDrop()`에서 각각
    독립적으로 롤하도록 변경(`TryDropActive`/`TryDropPassive` 분리) — 한
    번의 처치에서 액티브/패시브가 둘 다 나올 수도, 둘 다 안 나올 수도
    있는 구조로 바뀜(기존엔 상호 배타적 단일 롤)

### 이슈/메모
- 테스트용으로 임시 `ItemDropManager`를 `AddComponent`로 즉석 생성했을 때,
  `allActiveItems` 배열을 채우기 전에 `Awake()`가 이미 동기적으로 실행되어
  `_remainingActiveItems.AddRange(null)`에서 `ArgumentNullException` 발생
  — 실제 씬에서는 인스펙터 값이 이미 로드된 상태로 `Awake()`가 돌기 때문에
  발생하지 않는, 테스트 스크립트 순서 문제였음(임시 오브젝트 생성 후 값
  배선 전에 절대 `TryDrop`/`Awake` 트리거하지 말 것 — 정식 확인은 씬에 이미
  있는 `ActiveItemDropManager`로 재검증해서 문제 없음 확인)

### 테스트 결과
- [x] 컴파일 에러 0건
- [x] Play Mode: 실제 씬의 `ActiveItemDropManager`로 100회 `TryDrop` 호출
      — 액티브는 풀 한도(3개)까지 정확히 소진, 패시브는 53/100(약 53%)
      드랍되어 목표 50%와 정상 오차 범위 내에서 일치

### 다음 작업
- 실플레이로 패시브 드랍 체감 빈도 확인 (계속 이월 중)
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (15)

### 진행 내용
- 사용자 피드백: 발사체 기본 사거리가 너무 길어서(기존 유지시간 3초 ×
  속도 15유닛/초 = 45유닛, 방 크기 40×40유닛보다 커서 방 안에서 사실상
  안 없어짐) 사거리 능력치가 있으나 마나 하게 체감됨 → 기본 사거리를
  3유닛으로 줄여달라는 요청
  - "사거리 스탯"의 실체가 유지시간(초)이라 사용자가 원하는 "유닛" 단위로
    바로 못 받는 문제가 있어서, 먼저 유닛(거리) ↔ 초(유지시간) 환산
    관계(거리 = 속도 × 유지시간)를 설명하고 "몇 유닛으로 하고 싶은지"로
    알려달라고 요청 → "3유닛"으로 확정받음
  - `Assets/Prefabs/Projectile_Basic.prefab`의 `lifetime`을 3초→0.2초로
    변경(0.2 × 15 = 정확히 3유닛). 스크립트 코드 변경 없이 프리팹 값만
    조정 — `Projectile.BaseLifetime`을 읽는 `PlayerStatsPanelUI`의 사거리
    Pip 바 계산도 자동으로 이 새 기본값을 반영하게 됨(추가 배선 불필요)

### 이슈/메모
- `EnhancedShotActiveItem`도 같은 `Projectile_Basic` 프리팹/풀을 공유해서
  쓰기 때문에, 이번 기본 유지시간 변경이 액티브 아이템 "Enhanced Shot"
  스킬의 사거리에도 동일하게 적용됨(45유닛→3유닛). 패시브 "사거리 증가"
  보너스 자체는 스코프상 기본 공격에만 적용되고 액티브 스킬에는 안 붙지만,
  "기본 유지시간"이라는 프리팹 공유 값 자체는 두 시스템이 같이 쓰고
  있다는 점은 참고할 것(필요 시 Enhanced Shot 전용 프리팹/풀로 분리하는
  건 별도 논의 필요)

### 테스트 결과
- [x] 컴파일 불필요(프리팹 값만 변경), 콘솔 에러 0건
- [x] Play Mode: 갓 스폰한 발사체의 `BaseLifetime`=0.2s, 속도 15 기준
      예상 이동거리=3유닛 정확히 확인
- [x] Play Mode: ESC 메뉴에서 사거리 Pip 바가 아이템 0개 상태에서 1/10만
      채워짐을 확인 — (기본 0.2s + 0) / (기본 0.2s + 6×0.3s=1.8s) =
      0.2/2.0=10% 계산값과 정확히 일치, 시작부터 사거리 여유가 거의 없어
      보이도록 의도한 대로 동작

### 다음 작업
- 실플레이로 실제 발사체가 3유닛만 날아가고 사라지는 걸 육안으로 확인
  (계속 이월 중)
- Enhanced Shot 스킬 사거리도 같이 짧아진 게 의도한 바인지 확인 필요
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (16)

### 진행 내용
- 사용자 피드백: 공격 속도가 초반치고 너무 빠름 → "인게임 최대 수치 10
  기준 3칸 정도"로 조정 요청(= Pip 바 기준 아이템 0개 상태에서 3/10 정도)
  - **설계상 문제 발견**: 기존 공격 속도 Pip 바 공식은 `(기본 1.0 + 보너스)
    / (기본 1.0 + CapCount×증가량)`이었는데, 공격 속도 보너스는 다른
    3개 스탯과 달리 코드에서 배율(곱셈)로 적용됨(`effectiveCooldown =
    baseCooldown / multiplier`) — 이 경우 base가 분자·분모에 똑같이
    곱해지는 구조라 실제 `fireCooldown` 값을 아무리 바꿔도 Pip 채움
    비율이 절대 안 바뀌는 구조적 문제였음(수학적으로 base가 완전히
    상쇄됨). 그래서 사거리 때처럼 "기본값을 바꾸면 그대로 반영"되는
    방식이 공격 속도에는 안 통함
  - 해결: 공격 속도만 "절대 기준치" 방식으로 전환 — 실제 초당 공격 횟수
    (`배율 / 기본 발사 간격`)를 계산해서, authoring된 절대 기준
    `AttackSpeedMaxRate`(5회/초 = Pip 바 100% 기준점, 하드캡 아님 — 더
    많이 쌓으면 기준치를 넘어설 수도 있음)와 비교하는 공식으로 교체.
    이러면 실제 `fireCooldown`을 튜닝하면 Pip 바에도 정확히 반영됨(사거리
    스탯과 동일한 "능력치=실제 값" 원칙으로 통일)
  - `PlayerCombat.EquippedFireCooldown` 신규 추가(장착 무기가
    `RangedWeaponData`일 때만 유효, 근접 무기는 0 → 빈 바)
  - `Assets/Data/Weapons/RangedGun.asset`의 `fireCooldown`을 0.3초→0.65초로
    변경(초당 약 3.33회 → 약 1.54회로 감속) — 목표였던 "0개 아이템일 때
    3/10 Pip"에 정확히 맞춰 역산한 값

### 이슈/메모
- 추가/증가(additive) 방식 스탯(공격력/이동속도/사거리)과 배율
  (multiplicative) 방식 스탯(공격속도)은 "기본값 상대적 계산"과 "절대
  기준치 계산" 중 서로 다른 공식이 필요하다는 걸 확인 — 배율형 보너스는
  base가 분자/분모에서 상쇄되어 상대적 계산으로는 base 변경이 전혀
  반영되지 않음. 이후 새로운 배율형 스탯을 추가할 일이 있으면 이 패턴을
  재사용할 것

### 테스트 결과
- [x] 컴파일 에러 0건
- [x] Play Mode: 아이템 0개 상태에서 공격 속도 Pip 바 정확히 3/10 확인
      (기본 발사 간격 0.65초 → 초당 1.538회 → 1.538/5 = 30.8% ≈ 3/10)
- [x] Play Mode: 공속 아이템 5개 획득(배율 1.5) 후 Pip 바 5/10으로 정상
      증가 확인(1.538×1.5/5 ≈ 46.2% ≈ 5/10)
- [x] Play Mode: 실제 쿨다운 게이팅 로직(`RangedWeapon`)은 변경 없이 그대로
      `기본 발사간격 / 배율` 공식 사용 중임을 재확인(0.65/1.5=0.4333초로
      정확히 계산됨) — Pip 바 리디자인이 실제 전투 로직에 영향 없음

### 다음 작업
- 실플레이로 실제 공격 속도 체감(초반엔 느리게, 스택 쌓을수록 빨라지는
  느낌) 확인 (계속 이월 중)
- 실플레이로 실제 발사체가 3유닛만 날아가고 사라지는 걸 육안으로 확인
  (계속 이월 중)
- Enhanced Shot 스킬 사거리도 같이 짧아진 게 의도한 바인지 확인 필요
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (17)

### 진행 내용
- 사용자 요청: 이동속도 스탯도 공격속도와 같은 "기본값 변경이 Pip 바에
  반영 안 되는" 문제가 있는지 검증
  - 코드 검토: `PlayerMovement`의 이동속도 보너스는 배율이 아니라 덧셈
    방식(`speed = moveSpeed + MoveSpeedBonus`)이라 공격속도(배율 방식)와
    다른 카테고리 — 상쇄 문제 구조적으로 없음을 확인
  - Play Mode 라이브 검증: 기본 이동속도 5(Pip 4/10) → 임시로 2로 낮춤
    → Pip 2/10으로 정확히 반응(계산값과 일치) → 원래 값(5)으로 복구
  - 결론: 이동속도는 문제없음, 코드 수정 불필요

### 테스트 결과
- [x] Play Mode: 기본 이동속도 변경(5→2→5)에 따라 Pip 바가 4/10→2/10→
      4/10으로 정확히 반응함을 확인 — 별도 조치 불필요

### 다음 작업
- 실플레이로 실제 공격 속도 체감(초반엔 느리게, 스택 쌓을수록 빨라지는
  느낌) 확인 (계속 이월 중)
- 실플레이로 실제 발사체가 3유닛만 날아가고 사라지는 걸 육안으로 확인
  (계속 이월 중)
- Enhanced Shot 스킬 사거리도 같이 짧아진 게 의도한 바인지 확인 필요
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (18)

### 진행 내용
- 사용자 피드백: 패시브 아이템이 요청한 대로 사선으로 눕지 않고 여전히
  공중에 똑바로 서있음 → 원인만 먼저 조사(작업 보류 요청), 이후 수정 승인
  받아 반영
  - **원인 1**: `ItemDropManager.SpawnPickup()`이
    `Instantiate(prefab, position, Quaternion.identity)`로 호출되고
    있어서, 프리팹 자체에 저장된 회전값(패시브의 사선 `0,45,90`)과 무관하게
    항상 무회전으로 강제 스폰됨(액티브 픽업은 우연히 프리팹 회전이
    `(0,0,0)`이라 이 버그가 안 보였을 뿐, 똑같이 영향받고 있었음)
  - **원인 2**: 드랍 위치로 `enemyTransform.position`을 그대로 쓰는데,
    적 스폰 좌표가 `Room02_CombatA.asset` 등에서 `y: 1`로 authoring되어
    있어(캡슐 피벗 관례) 적 사망 위치도 y≈1 — 지면(y=0)이 아니라 사람
    키 높이에 그대로 스폰되고 있었음
  - 수정: `SpawnPickup()`에서 `Quaternion.identity` 대신
    `prefab.transform.rotation`(프리팹 자체 회전 유지) 사용, 신규
    `dropHeight`(기본 0.15, 지면 위로 살짝 띄운 값) 필드로 Y좌표를
    고정해서 지면 근처로 정규화

### 테스트 결과
- [x] 컴파일 에러 0건
- [x] Play Mode: y=1(적 피벗 높이) 위치에서 `TryDrop` 20회 호출 → 스폰된
      픽업 17개 전부 `position.y=0.15`(지면 높이)로 정규화됨을 확인
- [x] Play Mode: 패시브 픽업은 `eulerAngles=(0,45,90)`(사선 눕힌 회전)로,
      액티브 픽업은 `eulerAngles=(0,0,0)`(기존 원판 회전)로 각각 프리팹에
      저장된 값 그대로 스폰됨을 확인 — 회전 강제 초기화 버그 해결 확인

### 다음 작업
- 실플레이로 패시브 아이템이 실제로 사선으로 누워 지면에 걸쳐 보이는지
  육안 확인
- 실플레이로 실제 공격 속도 체감 확인 (계속 이월 중)
- 실플레이로 실제 발사체가 3유닛만 날아가고 사라지는 걸 육안으로 확인
  (계속 이월 중)
- Enhanced Shot 스킬 사거리도 같이 짧아진 게 의도한 바인지 확인 필요
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (19)

### 진행 내용
- 사용자 피드백: ESC로 여는 UI(이제부터 "일시정지 UI"로 지칭)를 켜면
  플레이어 조작만 막히고 적/발사체 등은 계속 움직임 → 시스템 하나하나
  개별로 멈추기보다 `Time.timeScale`을 0으로 만들어 전체를 얼리는 방식을
  제안받아 검토 후 승인, 반영
  - 검토(코드베이스 확인 + `unity63-architecture` 스킬 "Update/FixedUpdate/
    Time" 레퍼런스 참고): 이 프로젝트의 모든 `Time.deltaTime`/
    `Time.fixedDeltaTime` 사용처가 곱셈·누적(`+=`)뿐이고 나눗셈으로 쓰는
    곳이 전혀 없어서 `timeScale=0`으로 완전히 멈춰도 0-나누기 같은 위험이
    없음을 확인 — "최대한 느리게(0에 가까운 값)"보다 **정확히 0**을
    추천(0이 아니면 메뉴를 오래 열어둘수록 미세하게 시간이 계속 흘러
    쿨다운/수명 등이 조금씩 누적되는 드리프트 버그 가능성이 생김)
  - 사용자 질문에 답변: `Time.timeScale=0`은 옛날 Unity 버전부터 계속
    있던 표준 API이며, 버전이 오래돼서 못 쓰는 제약은 없음(다만 확답은
    아니지만 과거 특정 물리/NavMesh 이슈로 인한 방어적 회피, 또는
    `timeScale`을 슬로우모션 연출 용도로 따로 쓰고 진짜 일시정지는 별도
    플래그 방식으로 구현했을 가능성 등을 참고로 언급)
  - `EquipMenuController.Open()`/`Close()`에서 `playerInputActions.enabled`
    토글을 완전히 제거하고 `Time.timeScale = 0f`/`1f`로 교체 — 더 이상
    안 쓰는 `playerInputActions` 필드 자체도 제거
  - Input System의 액션 콜백(`ToggleEquip` 등)은 `Time.timeScale`과
    무관하게 계속 동작하므로 ESC로 다시 닫는 것도 정상 작동함을 확인

### 이슈/메모 — 잔존하는 작은 허점 발견 (사용자에게 알림, 아직 안 고침)
- `Time.timeScale=0`으로도 못 막는 게 있음을 실측으로 확인: Input System의
  버튼 이벤트(`performed` 콜백)는 `timeScale`과 무관하게 그대로 발동되기
  때문에, 일시정지 중에 공격 버튼을 누르면 `PlayerCombat.OnAttackPerformed`
  → `RangedWeapon.Attack()`이 실제로 실행되어 발사체가 생성됨(그 자리에
  얼어붙어 있다가 일시정지 해제 즉시 날아감 — "공짜 예약 샷" 같은 자잘한
  익스플로잇). 이동은 `FixedUpdate` 자체가 멈춰서 문제없지만, 공격/닫지
  롤/스킬처럼 버튼 이벤트로 직접 실행되는 액션은 전부 같은 유형의 위험을
  안고 있음 — 사용자에게 보고만 하고 수정은 보류(필요하면 각 액션 핸들러
  시작부에 `Time.timeScale > 0f` 체크를 추가하는 정도로 간단히 막을 수 있음)

### 테스트 결과
- [x] 컴파일 에러 0건
- [x] Play Mode: ESC로 열면 `Time.timeScale` 1→0, 닫으면 0→1로 정확히
      전환됨을 확인
- [x] Play Mode: 일시정지 중 공격 버튼 입력 시 발사체가 실제로 생성됨을
      확인(0개→1개) — 위 이슈 항목의 익스플로잇을 실측으로 재현

### 다음 작업
- 일시정지 중 공격/스킬/구르기 등 버튼 이벤트가 여전히 실행되는 문제를
  막을지 결정 필요(사용자 확인 대기)
- 실플레이로 패시브 아이템이 실제로 사선으로 누워 지면에 걸쳐 보이는지
  육안 확인
- 실플레이로 실제 공격 속도 체감 확인 (계속 이월 중)
- 실플레이로 실제 발사체가 3유닛만 날아가고 사라지는 걸 육안으로 확인
  (계속 이월 중)
- Enhanced Shot 스킬 사거리도 같이 짧아진 게 의도한 바인지 확인 필요
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (20)

### 진행 내용
- 사용자 승인받아 이전 항목의 잔존 허점(일시정지 중 버튼 이벤트로 공격/
  구르기/스킬이 그대로 실행되는 문제) 수정
  - 신규 `PauseState`(정적 클래스, `IsPaused => Time.timeScale <= 0f`)를
    `Assets/Scripts/Player/`에 추가 — 3곳에서 동일한 체크가 필요해서
    매번 `Time.timeScale <= 0f`를 반복 작성하는 대신 한 곳에 모음
  - `PlayerCombat.OnAttackPerformed`, `PlayerDodgeRoll.
    OnDodgeRollPerformed`, `ActiveItemController.OnActivatePressed`/
    `OnActivateReleased` 각각 핸들러 맨 앞에 `if (PauseState.IsPaused)
    return;` 가드 추가

### 테스트 결과
- [x] 컴파일 에러 0건
- [x] Play Mode: 평상시 공격은 정상 작동(발사체 1개 생성) 확인
- [x] Play Mode: 일시정지 중 공격 시도 시 발사체가 생성되지 않음을 확인
      (이전 항목에서 재현했던 익스플로잇이 정상적으로 막힘)
- [x] Play Mode: 메뉴 닫으면 `Time.timeScale` 정상적으로 1로 복구됨을 재확인

### 다음 작업
- 실플레이로 패시브 아이템이 실제로 사선으로 누워 지면에 걸쳐 보이는지
  육안 확인
- 실플레이로 실제 공격 속도 체감 확인 (계속 이월 중)
- 실플레이로 실제 발사체가 3유닛만 날아가고 사라지는 걸 육안으로 확인
  (계속 이월 중)
- Enhanced Shot 스킬 사거리도 같이 짧아진 게 의도한 바인지 확인 필요
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (21)

### 진행 내용
- 사용자 피드백: 사거리 증가 패시브 아이템 1개당 증가율이 너무 높음 →
  실제 수치 질문받아 계산해서 답변(기존 +0.3초=+4.5유닛, 기본 3유닛 대비
  +150%로 확실히 큰 편이었음 확인) → 2유닛으로 낮춰달라는 요청 받아 반영
  - `Assets/Data/PassiveItems/ProjectileRangeData.asset`의 `amount`를
    0.3초→0.1333...초(2유닛÷15유닛/초)로 변경

### 테스트 결과
- [x] 컴파일 불필요(에셋 값만 변경), 콘솔 에러 0건
- [x] Play Mode: 1개 획득 시 `ProjectileLifetimeBonus`가 정확히 0.1333초
      (=2유닛)로 적용되고, 총 사거리가 3→5유닛으로 정확히 늘어남을 확인.
      Pip 바도 새 계산값(0/10 상태 2/10 → 1개 후 3/10)에 맞게 정상 반영됨

### 다음 작업
- 실플레이로 패시브 아이템이 실제로 사선으로 누워 지면에 걸쳐 보이는지
  육안 확인
- 실플레이로 실제 공격 속도 체감 확인 (계속 이월 중)
- 실플레이로 실제 발사체가 3유닛만 날아가고 사라지는 걸 육안으로 확인
  (계속 이월 중)
- Enhanced Shot 스킬 사거리도 같이 짧아진 게 의도한 바인지 확인 필요
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (22)

### 진행 내용
- 사용자 요청: 밸런스 수치(플레이어 기본 능력치, 패시브 아이템 효과)를
  매번 Claude/Unity를 거치지 않고 텍스트 파일만 고쳐서 조정할 수 있게
  해달라는 대규모 요청 — 설계안 검토(대안 포맷 비교: CSV/key=value/YAML
  vs JSON, 장단점 비교 후 JSON으로 결정) → 파일당 가이드 주석 요청까지
  받아 구현 완료
  - `unity63-architecture` 스킬의 "Data Ownership"/"Persistence" 레퍼런스
    참고: `StreamingAssets`가 "빌드에 포함되는 읽기 전용 시드 데이터"의
    공식 권장 위치, ScriptableObject 에셋을 런타임에 직접 mutate하지
    말라는 가이드(에디터 Play 중 되돌아가지 않을 위험) 확인
  - 신규: `Assets/Scripts/Balance/`에 `GameBalance`(정적 로더, 최초 접근
    시 lazy load), `PlayerBaseStatsData`/`PassiveItemBalanceData`
    (JsonUtility DTO)
  - 신규: `Assets/StreamingAssets/Balance/PlayerBaseStats.json`,
    `PassiveItems.json` — 각 파일에 `_guide`/`_fieldGuide` 필드로 수치별
    설명을 JSON 안에 내장(JsonUtility는 매칭 안 되는 여분 필드를 그냥
    무시하므로 순수 JSON 문법을 안 깨면서 "주석"처럼 동작)
  - 밸런스 넘버는 ScriptableObject/MonoBehaviour의 `[SerializeField]`를
    직접 mutate하지 않고, 각 값의 프로퍼티 getter가 `GameBalance`를
    직접 읽도록 변경(`WeaponData.Damage`를 abstract로 바꿔 `RangedWeaponData`/
    `MeleeWeaponData`가 오버라이드, `PlayerMovement.BaseMoveSpeed`,
    `PassiveItemData.Amount`/`CapCount`, `Projectile`의 speed/lifetime은
    풀링된 인스턴스라 안전하게 직접 오버라이드) — 덕분에 `RangedWeapon`/
    `MeleeWeapon`/`PlayerCombat`/`PlayerPassiveStats`/`PlayerStatsPanelUI`는
    호출부 변경 전혀 없이 자동으로 JSON 값을 따라가게 됨
  - `ProjectileRange` 패시브의 JSON `amount`는 "유닛" 단위로 적도록 하고
    (`PlayerBaseStats.json`의 `projectileSpeed`로 나눠 내부 "초" 단위로
    자동 환산) — 사용자가 항상 유닛 단위로만 생각하면 되게 함
  - 기존 `[SerializeField]` 수치 필드들(`PlayerMovement.moveSpeed`,
    `WeaponData.damage`, `RangedWeaponData.fireCooldown`,
    `MeleeWeaponData.range`, `Projectile.speed`/`lifetime`,
    `PassiveItemData.amount`/`capCount`)은 이제 JSON이 유일한 권한이라
    전부 제거(인스펙터에 남겨두면 "값을 바꿔도 반영 안 됨"으로 헷갈릴
    수 있어서)

### 이슈/메모 — 발견한 진짜 버그와 수정
- **정적 캐시가 Stop→Play 사이에 리셋 안 되는 문제 발견**: `GameBalance`를
  최초에 "최초 접근 시 lazy load, 한 번 로드되면 캐시"로만 구현했는데,
  실측해보니 `EditorSettings.enterPlayModeOptionsEnabled=False`(도메인
  리로드가 기본값)임에도 불구하고 이 자동화 환경에서 Stop 후 Play를
  다시 눌러도 정적 필드가 리셋되지 않는 경우가 실제로 재현됨(JSON 파일을
  일부러 지워도 이전 세션에 캐시된 값이 그대로 나옴). 이대로면 "JSON
  고치고 Play만 다시 누르면 반영"이라는 이 기능의 핵심 약속이 깨질 수
  있어서, `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`로 캐시를
  강제 리셋하는 훅을 추가 — 이 어트리뷰트는 도메인 리로드 여부와 무관하게
  Play 세션/빌드 실행마다 항상 호출되므로 훨씬 안전함
  - 수정 후 재검증: JSON의 `moveSpeed`를 99로 바꾸고 Play 재진입 →
    `GameBalance.Player.moveSpeed=99` 정상 반영 확인(수정 전이었다면 이전
    캐시값이 나왔을 상황)
- `Unity_ReadConsole`이 `Debug.LogWarning` 호출을 일부 못 잡아내는 것으로
  보이는 현상 재확인(반환값 자체는 폴백 로직이 정확히 작동함을 증명하는
  값이었는데 경고 로그는 안 보임) — 이전에도 있었던 콘솔 조회 도구의
  신뢰성 이슈와 같은 계열로 보임, 값 기반 검증을 우선할 것

### 테스트 결과
- [x] 컴파일 에러 0건
- [x] Play Mode: `GameBalance.Player`의 7개 필드 전부 JSON과 정확히 일치
      확인, `RangedWeaponData`/`MeleeWeaponData`의 `Damage`/`FireCooldown`/
      `Range`가 GameBalance 값을 정확히 반영함을 확인
- [x] Play Mode: 패시브 아이템 4종의 `Amount`/`CapCount` 전부 JSON과 일치
      확인, `ProjectileRange`의 유닛→초 자동 환산(2유닛→0.1333초) 정확히
      동작 확인
- [x] Play Mode: JSON 파일을 실제로 지운 상태에서 폴백 기본값(전부 이전
      튜닝값과 동일하게 하드코딩)이 정확히 나오는 것 확인
- [x] Play Mode: JSON 값을 실제로 수정(moveSpeed→99)한 뒤 재시작 시 정상
      반영되는 것 확인(캐시 리셋 버그 수정 검증)
- [x] Play Mode: 최종 회귀 테스트 — 이동속도/무기 데미지/쿨다운/패시브
      적용까지 전체 흐름이 리팩터 이전과 동일하게 정상 동작함을 확인

### 다음 작업
- 실플레이로 JSON 값을 직접 수정해서 Unity/Claude 없이 밸런스 조정이
  실제로 되는지 최종 확인
- 실플레이로 패시브 아이템이 실제로 사선으로 누워 지면에 걸쳐 보이는지
  육안 확인
- 실플레이로 실제 공격 속도 체감 확인 (계속 이월 중)
- 실플레이로 실제 발사체가 3유닛만 날아가고 사라지는 걸 육안으로 확인
  (계속 이월 중)
- Enhanced Shot 스킬 사거리도 같이 짧아진 게 의도한 바인지 확인 필요
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (23)

### 진행 내용
- 사용자가 새 JSON 밸런스 워크플로를 곧바로 직접 사용 — `PassiveItems.json`의
  `AttackPower.amount`를 2.5→5로 직접 수정함(Claude/Unity 안 거치고 텍스트
  에디터로 바로 조정 — 의도한 워크플로가 실제로 작동함을 사용자가 직접
  확인한 셈)
- 사용자 요청: "정확한 테스트"를 위한 디버그 오버레이 신설 — 디버그 키(F1,
  직접 지정) 토글 시 우측 상단 FPS 표시 + 플레이어가 데미지를 입거나
  가할 때 데미지 수치가 잠시 떴다 사라지는 팝업 표시, 다시 누르면 전부
  숨김
  - 신규 `Assets/InputActions/DebugOverlayActions.inputactions`("Debug"
    맵, "ToggleOverlay" 액션, F1 바인딩) — 기존 ESC/디버그 스킬 전환 키와
    동일하게 항상 활성화된 별도 InputActionAsset이라 다른 입력 상태와
    무관하게 반응함
  - `HealthComponent`에 정적 이벤트 `AnyDamageTaken(HealthComponent, float,
    Transform)` 신설 — 무효화/사망/0 이하 데미지로 실제로 안 먹힌 경우는
    발동 안 함. 정적이라 새로 스폰되는 적/풀링된 적이 언제 생기든 상관없이
    한 번만 구독하면 전부 잡힘(적마다 개별 구독할 필요 없음)
    - 신규 `DebugOverlayController`(UI 스크립트): F1으로 FPS 텍스트
      활성화/비활성화, `Time.unscaledDeltaTime` 기반 FPS 계산(일시정지
      중에도 정확하게 표시됨), `AnyDamageTaken` 구독해서 피해 대상이
      플레이어면 빨간색으로 플레이어 위에, 가해자가 플레이어면(=플레이어가
      적에게 입힌 피해) 흰색으로 그 적 위에 각각 월드 스페이스 텍스트를
      코드로 즉석 생성 후 0.8초 뒤 자동 파괴(픽업 알림 UI와 동일하게
      쿼터뷰 카메라의 고정 각도에 맞춰 회전 없이(`Quaternion.identity`)
      배치 — 이 프로젝트에서 이미 검증된 "고정 각도 카메라 + 무회전 월드
      스페이스 캔버스" 관례를 그대로 재사용)
  - FPS 텍스트는 미니맵과 겹치지 않게 그 왼쪽에 배치(HUD 하위, 우측 상단
    영역, 미니맵 좌측 경계 바로 옆)

### 테스트 결과
- [x] 컴파일 에러 0건
- [x] Play Mode: F1 누르면 FPSText 활성화, 다시 누르면 비활성화되는 것
      확인
- [x] Play Mode: 플레이어가 피해를 입는 상황(가짜 소스로 7 데미지)과
      플레이어가 적에게 피해를 주는 상황(가짜 타겟에 13 데미지) 각각
      시뮬레이션 → 정확히 2개의 팝업 생성, 텍스트("7"/"13")와 색상
      (빨강/흰색) 모두 의도한 대로 정확히 일치
  - [x] 오버레이 OFF 상태에서는 피해가 발생해도 팝업이 안 생기는 것 확인
      (2개→2개, 새로 안 늘어남)

### 다음 작업
- 실플레이로 F1 디버그 오버레이 육안 확인(팝업 위치/페이드 느낌 등)
- 실플레이로 JSON 값을 직접 수정해서 Unity/Claude 없이 밸런스 조정이
  실제로 되는지 최종 확인
- 실플레이로 패시브 아이템이 실제로 사선으로 누워 지면에 걸쳐 보이는지
  육안 확인
- 실플레이로 실제 공격 속도 체감 확인 (계속 이월 중)
- 실플레이로 실제 발사체가 3유닛만 날아가고 사라지는 걸 육안으로 확인
  (계속 이월 중)
- Enhanced Shot 스킬 사거리도 같이 짧아진 게 의도한 바인지 확인 필요
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (24)

### 진행 내용
- 사용자가 `PassiveItems.json`(공격력 2.5→5)과 `PlayerBaseStats.json`
  (원거리 데미지 8→12)을 직접 수정 — 새 JSON 워크플로가 실사용되고 있음
  확인, 그대로 유지
- 사용자 피드백: 데미지 숫자 팝업과 적 HP바 UI가 빌보딩이 안 된 것 같음 →
  원인 확인 후 진짜 빌보딩 구현
  - 원인: 기존엔 실제 빌보딩 로직이 전혀 없었고, "고정 각도 쿼터뷰
    카메라라 월드 스페이스 캔버스를 무회전(`Quaternion.identity`)으로만
    둬도 대충 맞아 보인다"는 근사에 의존하고 있었음(`PickupNotificationUI`
    주석에 "BillboardUI와 같은 패턴"이라 적혀 있었지만 실제로는 그런
    컴포넌트 자체가 존재하지 않았음). 이 근사는 플레이어 바로 위치에서만
    그럭저럭 맞고, (1) 플레이어에서 떨어진 위치(방 안의 적)일수록
    원근 왜곡으로 어긋나고, (2) 적 HP바는 적 트랜스폼의 자식이라 적이
    이동/추적 중 회전하면 그 회전을 그대로 상속받아 같이 돌아가버리는
    문제가 있었음
  - 신규 `Assets/Scripts/UI/BillboardUI.cs` — `LateUpdate()`에서 매 프레임
    자신의 회전을 메인 카메라의 회전과 정확히 일치시킴(카메라와 항상
    평행한 평면 유지, 부모 회전이나 월드 위치와 무관하게 항상 정확)
  - 적용 대상: `Enemy_Basic`/`Enemy_Ranged`/`Boss_Basic` 프리팹의
    `HealthBarCanvas`, 플레이어의 `PickupNotificationCanvas`, 디버그
    데미지 숫자 팝업(`DebugOverlayController.SpawnDamageNumber()`에서
    생성 시 자동으로 컴포넌트 추가)

### 이슈/메모
- 컴포넌트 추가 도중 일시적으로 `PickupNotificationCanvas`에
  `BillboardUI`가 중복 추가된 것을 발견(둘 다 같은 프레임에 붙어있었음
  — MCP 툴 쪽의 일시적 중복 디스패치로 추정, 원인 특정은 안 됨) → 하나
  제거해서 정리. 이후 프리팹 3개에 추가할 때는 미리 "이미 있으면 건너뛰기"
  가드를 넣어서 안전하게 처리(실제로 이 스크립트 최초 실행 시점에 이미
  다 붙어있다는 로그가 나와서 똑같은 중복 현상이 있었던 것으로 보이나,
  가드 덕분에 최종 상태는 전부 정확히 1개씩으로 확인됨)
- 이 자동화 환경은 `LateUpdate()` 같은 실제 프레임 기반 콜백이 자동으로
  틱 되는 걸 관찰하기 어려운 알려진 한계가 있어서(실프레임 경계 없음),
  `BillboardUI`의 로직 자체를 수동으로 재현해서 검증함(회전 알고리즘은
  정확함을 확인, 실제 엔진이 `LateUpdate`를 매 프레임 호출해주는 것은
  실플레이로만 최종 확인 가능)

### 테스트 결과
- [x] 컴파일 에러 0건
- [x] 프리팹/씬 컴포넌트 확인: `HealthBarCanvas` 3개 프리팹 전부, 플레이어
      `PickupNotificationCanvas` 전부 `BillboardUI` 정확히 1개씩 존재
- [x] Play Mode: 부모(플레이어) 회전을 90도로 바꾼 뒤 `BillboardUI`와
      동일한 로직(`transform.rotation = Camera.main.transform.rotation`)을
      적용하면 부모의 회전과 무관하게 카메라 회전(59.04, 0, 0)으로 정확히
      돌아오는 것을 확인 — 알고리즘 정확성 검증됨

### 다음 작업
- 실플레이로 데미지 팝업/HP바가 실제로 항상 카메라를 향하는지 육안 확인
  (특히 적이 이동 중 회전할 때, 방 안에서 플레이어와 멀리 떨어져 있을 때)
- 실플레이로 F1 디버그 오버레이 육안 확인
- 실플레이로 JSON 값을 직접 수정해서 Unity/Claude 없이 밸런스 조정이
  실제로 되는지 최종 확인
- 실플레이로 패시브 아이템이 실제로 사선으로 누워 지면에 걸쳐 보이는지
  육안 확인
- 실플레이로 실제 공격 속도 체감 확인 (계속 이월 중)
- 실플레이로 실제 발사체가 3유닛만 날아가고 사라지는 걸 육안으로 확인
  (계속 이월 중)
- Enhanced Shot 스킬 사거리도 같이 짧아진 게 의도한 바인지 확인 필요
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-31 (25)

### 진행 내용
- 사용자 피드백: 패시브 "공격 속도 증가"가 배율(퍼센트, 0.1=10%) 방식이라
  나머지 3개 스탯(전부 고정값 덧셈)과 결이 다르고 관리하기 불편함 → 검토
  후 의견 요청받아 "고정값 덧셈(B안: 초당 공격 횟수를 그대로 더함)"으로
  통일할 것을 추천 → 승인받아 반영, 아이템 1개당 초당 0.15회 증가로 확정
  - `PlayerPassiveStats.AttackSpeedMultiplier`(배율, 1.0 시작) →
    `AttackSpeedBonus`(고정값, 0 시작)로 교체, `Apply()`도 나머지 3개와
    동일하게 단순 덧셈
  - `RangedWeapon.Attack()`: `쿨다운 = 기본쿨다운 / 배율` 나눗셈 공식을
    `유효초당횟수 = (1/기본쿨다운) + 보너스; 쿨다운 = 1/유효초당횟수`로
    교체
  - `PlayerStatsPanelUI`: 공격속도 전용 특수 공식(`AttackSpeedRatio`/
    `AttackSpeedMaxRate`)을 완전히 삭제하고 나머지 3개와 동일한
    `AdditiveRatio` 하나로 통일(관리 측면 개선 효과 실제로 확인됨)
  - `GameBalance`/`PassiveItems.json`의 AttackSpeed 기본값을 0.1(배율)→
    0.15(초당 횟수)로, `capCount`를 8→24로 재계산(0개 아이템 상태에서
    Pip 바가 이전과 동일하게 3/10이 나오도록 새 공식 기준으로 역산 —
    사용자가 이전에 승인한 "초반엔 3칸 정도" 튜닝 의도를 그대로 유지)

### 이슈/메모 — 사용자가 별도로 보고한 버그 조사 (수정은 보류)
- 사용자 보고: 실플레이 중 ESC를 안 눌러도 일시정지 UI가 나타남 → 조사
  결과, **실제 게임 버그가 아니라 이 세션의 자동 검증 작업이 원인**이었음
  을 확인
  - 증거 1: `ActiveItemEquipUI.Show()`를 호출하는 코드 경로는 전체
    코드베이스에서 `EquipMenuController.Open()` 단 한 곳뿐임(검색으로
    확인) — 즉 정상적인 게임 로직상으로는 ESC 없이 절대 열릴 수 없음
  - 증거 2: 확인 당시 패널은 떠 있었는데 `Time.timeScale`은 1(정상
    흐름)이었음 — 진짜 ESC로 열렸다면 `Open()`이 항상 `timeScale=0`도
    같이 세팅하므로, 이 불일치 자체가 "정상 경로로 연 게 아니다"라는
    증거
  - 원인 확정: Pip 바 갱신을 확인하려고 ESC 키 입력 시뮬레이션 대신
    `ActiveItemEquipUI.Show()`를 코드에서 직접 호출한 테스트가 있었는데,
    이게 `EquipMenuController`를 거치지 않고 UI만 강제로 띄운 것 — 발견
    즉시 `Hide()`로 정리함
  - 추가로 확인된 사실: 조사 도중 에디터가 이미 Play 모드에서 빠져나와
    있는 것을 발견(제가 명시적으로 Stop을 누르기 전에 이미 정지 상태) —
    사용자가 이상한 UI를 보고 직접 Stop을 눌렀을 가능성이 높아 보임(같은
    라이브 에디터를 이 세션과 사용자가 동시에 들여다보고 있었던 정황과
    일치)
  - 결론: 코드 수정 불필요. 다만 이후 자동화 테스트에서 UI를 직접
    `Show()` 호출하는 방식은 실제 트리거 경로(ESC 입력)를 안 타므로
    `EquipMenuController`의 내부 상태(`_isOpen`)와 실제 UI 표시 상태가
    어긋날 수 있음 — 앞으로는 가능하면 실제 입력 시뮬레이션 경로로만
    검증할 것

### 테스트 결과
- [x] 컴파일 에러 0건
- [x] Play Mode: 0개 아이템 상태에서 `AttackSpeedBonus=0`, Pip 바
      정확히 3/10(신규 capCount=24 기준 재계산값과 일치) 확인
- [x] Play Mode: 공속 아이템 3개 획득 후 `AttackSpeedBonus=0.45`
      (0.15×3) 정확히 확인
- [ ] 3개 스택 후 Pip 바 갱신 육안 확인은 이 자동화 환경에서 ESC 키
      시뮬레이션이 같은 세션 내 반복 호출 중 불안정해져서 재확인 못함
      (버그 아님 — 데이터 자체는 위에서 이미 정확함을 확인했고, Pip
      바 공식은 나머지 3개 스탯과 동일한 이미 검증된 `AdditiveRatio`를
      그대로 재사용하므로 로직 정확성은 간접적으로 보장됨) — 실플레이로
      최종 육안 확인 권장

### 이슈/메모 — ESC 눌러도 일시정지 UI가 안 뜨는 버그 (수정 완료)
- 사용자 재보고: 위 항목 수정 후 재플레이 시 그 문제는 안 보였으나, **ESC를
  눌렀을 때 `Time.timeScale`(deltaTime 정지)만 적용되고 정작 일시정지 UI
  자체는 나타나지 않는** 새 증상 발견
- 1차 원인 파악: `PlayerStatsPanelUI`가 비활성 상태로 시작하는 조상
  (`EquipPanel`) 아래에 있어 `Awake()`(Pip 바 생성)가 최초 `SetActive(true)`
  시점까지 지연됨 → `ActiveItemEquipUI.Show()`가 그 `SetActive(true)`보다
  먼저 `Refresh()`를 호출하고 있어서 Pip 배열이 아직 `null`인 상태로 접근 →
  예외 발생 → `Show()`가 `panel.SetActive(true)`에 도달하기 전에 중단되어
  UI 전체가 안 뜸 (timeScale만 바뀌고 UI는 안 보이는 것과 정확히 일치)
  - 1차 조치(승인받아 적용, 유지): `BuildRow`/`ApplyFill` 호출부를
    `SafeBuildRow`/`SafeRefreshRow`로 감싸 행(row) 단위로 예외를 격리하고
    `Debug.LogError`로 남기도록 함 — 전체 크래시는 막았으나 Pip 바가 여전히
    비어 있는 근본 원인은 그대로였음
  - 2차 조치(1차만으로는 재현 지속): `Show()` 내부에서 `panel.SetActive(true)`
    를 `Refresh()`보다 먼저 호출하도록 순서 변경 — 그런데도 재현이 계속돼서
    추가 조사
- 진짜 근본 원인 확정: `ActiveItemEquipUI` 컴포넌트가 **자기 자신의
  GameObject인 `EquipPanel`을 `panel` 필드로 자기 참조**하고 있었고,
  `Awake()`가 `panel.SetActive(false)`(= 자기 자신 끄기)를 호출하고 있었음.
  `EquipPanel`은 저장된 씬 상태 자체가 이미 비활성(`activeSelf=false`)이라
  이 `Awake()`가 한 번도 실행된 적이 없었고, `Show()`의 최초 1회
  `panel.SetActive(true)` 호출이 바로 이 지연된 `Awake()`를 그 활성화
  캐스케이드 안에서 동기적으로 트리거함 → 그 `Awake()`가 즉시 자기 자신을
  다시 꺼버려서, 캐스케이드가 자식(`StatsPanel`)의 `Awake()`/`BuildRow()`에
  도달하기 전에 중단됨 (Edit 모드에서 `EquipPanel.activeSelf=False`로 저장된
  실제 씬 상태를 직접 확인해 검증 — 테스트 잔여물이 아니라 실제로 항상
  재현되는 버그였음)
  - 최종 수정: `ActiveItemEquipUI.Awake()`에서 `panel.SetActive(false)` 줄
    제거 — 애초에 씬에 이미 비활성 상태로 저장돼 있으므로 불필요했고, 바로
    이 줄이 최초 오픈을 스스로 방해하고 있었음
- 테스트 결과(Play Mode, `Show()` 직접 호출로 검증):
  - [x] `Show()` 이후 `EquipPanel.activeSelf=True` 유지(자기 재비활성 없음)
  - [x] `StatsPanel.activeSelf=True`, `activeInHierarchy=True`
  - [x] 4개 행 × 10 Pip = 40개 Pip 오브젝트 정상 생성 확인
  - [x] 콘솔 에러 0건(`SafeBuildRow`/`SafeRefreshRow` 에러 로그도 안 찍힘 —
        정상 경로로 완주했다는 뜻)
- 조사 중 추가로 남겼던 임시 진단 로그(`[TEMP]` 등)는 결론 후 전부 제거함

### 다음 작업
- 실플레이로 ESC 메뉴가 실제 입력으로도 정상적으로 열리는지 최종 육안 확인
  (이번 수정은 `Show()` 직접 호출로 검증됨, 실제 ESC 키 경로 재확인 권장)
- 실플레이로 공속 아이템 스택 시 Pip 바가 정상적으로 늘어나는지 육안 확인
- 실플레이로 F1 디버그 오버레이 및 빌보딩 육안 확인
- 실플레이로 JSON 값을 직접 수정해서 Unity/Claude 없이 밸런스 조정이
  실제로 되는지 최종 확인
- 실플레이로 패시브 아이템이 실제로 사선으로 누워 지면에 걸쳐 보이는지
  육안 확인
- 실플레이로 실제 발사체가 3유닛만 날아가고 사라지는 걸 육안으로 확인
  (계속 이월 중)
- Enhanced Shot 스킬 사거리도 같이 짧아진 게 의도한 바인지 확인 필요
- 아이콘 스와치를 실제 픽토그램으로 교체 (계속 이월 중)
- 아이템 방(#3), 방-방 카메라 전환 연출 실플레이 검증 (계속 이월 중)

---

## 2026-08-24

### 진행 내용
- Stage 2 남은 두 항목(#6 여러 종류의 일반 Enemy, #7 다중 방 콘텐츠 확장) 구현.
  설계는 이전 대화에서 먼저 합의: 적 풀은 기존 `GameObjectPool`(=
  `UnityEngine.Pool.ObjectPool<T>` 래퍼)을 종류별로 여러 개 두고 신규
  `EnemySpawner`가 오케스트레이션만 담당(풀링 로직 자체는 재구현하지 않음),
  스테이지 데이터는 `IStageDataProvider` 인터페이스 뒤에 `StageDataAsset`
  (ScriptableObject) 기반 구현체를 두어 추후 외부 파일/서버 공급자로 교체
  가능하게 함, 적 종류-풀 연결은 `EnemyVariantType` enum 방식.
  - 신규 스크립트: `EnemyVariantType`, `StageEnemyEntry`, `StageData`,
    `IStageDataProvider`, `StageDataAsset`, `ScriptableObjectStageDataProvider`,
    `StageManager`(`Assets/Scripts/Stage/`), `EnemySpawner`
    (`Assets/Scripts/Enemy/`), `ClearOverlayUI`(`Assets/Scripts/UI/`)
  - `RoomFlowManager` 리팩터: `_nextStageIsBoss` bool 및 고정 스폰 좌표 필드
    전부 제거, `StageManager`/`EnemySpawner`/`ClearOverlayUI` 기반 흐름으로 교체
  - `RoomManager.Start()`의 자동 `BeginRoom()` 호출 제거 — 이제
    `RoomFlowManager.Start()`가 스테이지 1을 스폰한 뒤 명시적으로 호출
  - 적 변종 2종: 근접(`EnemyBasicData`, moveSpeed 3→1.5로 변경) / 원거리
    (신규 `EnemyRangedData.asset`, 기존에 있었지만 아무도 안 쓰던
    `RangedAttack.asset` 패턴 재사용), 신규 `Enemy_Ranged.prefab`
    (`Enemy_Basic.prefab` 복제 + 파란색 톤 신규 머티리얼로 시각 구분)
  - `Assets/Data/Stages/Stage01~10.asset` 10개 생성 — Stage 1~9는 일반 웨이브
    (마리 수 2~10, 근접/원거리 교대 배치는 애셋에 직접 authoring, 스테이지
    인덱스로부터 계산하지 않음), Stage 10은 보스 1마리 + `isFinalStage=true`
  - 씬: 사전 배치돼 있던 `Enemy_Basic` 인스턴스 제거, `EnemyPool`을
    `EnemyPool_Melee`로 개명 + `EnemyPool_Ranged`/`EnemySpawner`/`StageManager`
    신규 배치, `Canvas` 하위에 `ClearOverlayPanel`(50% 알파 검정 오버레이 +
    흰색 "Clear" 텍스트, `PlayerInputActions` 잠금) 추가
- 테스트/UI 기반 작업: 적 머리 위 월드스페이스 체력 바, 화면 상단 중앙
  스테이지 텍스트, `UIManager` 파사드 도입.
  - 설계 논의: 처음엔 base `UIManager` + 씬 전용 파생 `StageUIManager`(is-a),
    로딩 시점 셋업까지 고려했으나 검토 끝에 보류 — 지금은 씬이 하나뿐이라
    base를 실제 중복 없이 추측으로 채워야 하고, 로딩 씬 자체가 아직 없어서
    셋업 훅을 걸 지점도 없음. "규모 커지면 그때 구조를 키운다" 원칙에 따라
    상속 없는 단일 `UIManager`로 축소, 정적 `Instance` 싱글톤도 도입하지 않음
    (지금은 필요한 곳이 없음)
  - 신규 스크립트: `BillboardUI`(월드스페이스 캔버스가 카메라 향하도록
    회전), `UIManager`(`Assets/Scripts/UI/`)
  - `HealthBarUI`는 기존 스크립트 그대로 재사용(수정 없음) — 이미
    `Health.Changed` 이벤트만으로 동작해 World Space든 Screen Space든
    상관없었음. 사망 시 숨김 로직도 별도로 안 만듦 — 적이 죽으면
    `PooledObject.ReturnToPool()`로 부모 GameObject 자체가 비활성화되고
    체력 바는 그 자식이라 자동으로 같이 꺼짐
  - `Enemy_Basic`/`Enemy_Ranged`/`Boss_Basic` 3개 프리팹에 `HealthBarCanvas`
    (World Space Canvas + Background/Fill Image + `BillboardUI` +
    `HealthBarUI`) 추가
  - `StageManager`에 `event Action<int> StageChanged` 추가 (`LoadCurrentStage()`
    에서 발생, `BeginFirstStage()`/`TryAdvance()` 양쪽 다 커버)
  - `UIManager`를 `UpgradeSelectionUI`/`EquipSelectionUI`/`CharacterSelectUI`/
    `ClearOverlayUI`/`StageManager.StageChanged`(스테이지 텍스트)를 감싸는
    파사드로 도입 — `RoomFlowManager`(`upgradeSelectionUI`/`equipSelectionUI`/
    `clearOverlayUI` 3개 필드 제거)와 `CharacterSelectController`(`selectUI`
    필드 제거)가 이제 `UIManager` 참조 하나만 들고 `ShowXxx(...)` 메서드로
    호출. `HealthBarUI`(플레이어)/`SkillCooldownUI`는 원래도 외부에서 호출되지
    않는 수동적 컴포넌트라 `UIManager`가 참조를 들지 않음(포워딩할 대상이
    없음) — 이미 `Canvas/HUD` 아래 있어 씬 계층 이동도 불필요
  - `UIManager` 컴포넌트는 기존 `Canvas` 오브젝트에 추가(모든 UI가 이미 그
    하위라 씬 계층 이동 없이 참조만 배선)
- 방 클리어 후 업그레이드/장착 UI가 떠 있는 동안 플레이어 조작 잠금, 다음
  스테이지 시작 시 재활성화 추가. `CharacterSelectController`/`ClearOverlayUI`가
  이미 쓰던 `PlayerInputActions.enabled = false/true` 패턴을 그대로 재사용 —
  새 구조 없이 `RoomFlowManager`에 `playerInputActions` 필드 하나만 추가.
  `HandleRoomCleared()`(업그레이드 표시 직전)에서 잠그고 `OnEquipConfirmed()`
  (다음 스테이지 스폰 직전)에서 해제
- 버그 수정: 10스테이지 보스 처치 후 Clear 오버레이가 안 뜨고 조작만 잠기는
  문제. 원인은 `ClearOverlayPanel`을 처음 생성할 때 씬에서 **비활성 상태로
  시작**하도록 만들어둔 것 — Unity는 오브젝트가 처음 활성화되는 시점에
  `Awake()`를 실행하므로, `ClearOverlayUI.Show()`의 첫 호출이 곧 그 "첫
  활성화"가 되어 `panel.SetActive(true)` 도중에 밀려있던 `Awake()`(내부에
  `panel.SetActive(false)` 포함)가 동기적으로 실행되며 방금 켠 패널을 다시
  꺼버림. 그 다음 줄인 입력 잠금은 이 흐름과 무관해서 정상 실행됨 — 그래서
  "조작은 잠기는데 화면은 안 뜨는" 증상으로 나타남. `UpgradePanel`/
  `EquipPanel`은 원래 씬에 활성 상태로 배치돼 있어 게임 시작 시 `Awake()`가
  정상적으로 한 번 실행되고 끝나기 때문에 이 문제가 없었음. 수정은
  `ClearOverlayPanel`을 나머지 둘과 동일하게 **씬에서 활성 상태로 시작**하도록
  변경(코드 변경 없음, 씬 파일의 초기 활성 상태만 수정)

### 테스트 결과
- [x] 전체 스크립트 컴파일 에러/경고 없음 (Unity MCP 콘솔 확인, 두 세션 다)
- [x] Play Mode 진입 직후 콘솔에 `Room started with 2 enemies.` 로그 확인,
      씬에 `Enemy_Basic(Clone)`(근접)·`Enemy_Ranged(Clone)`(원거리) 각 1마리가
      각자의 풀 아래 정상 스폰됨을 하이어라키로 확인 — 에러/경고 없음
- [x] `UIManager` 경유로 `CharacterSelectPanel`이 정상 표시됨(Play Mode 진입 시
      `CharacterSelectPanel`만 활성, `Upgrade`/`Equip`/`ClearOverlay`는 비활성)
      확인
- [x] `StageText`가 "Stage 1"로 정상 표시됨을 Play Mode에서 컴포넌트 값으로 확인
- [x] 적 체력 바(`HealthBarCanvas`)가 스폰된 `Enemy_Basic(Clone)`/
      `Enemy_Ranged(Clone)`에 자식으로 정상 생성되고, `BillboardUI`가 카메라
      각도에 맞춰 회전(`QuarterViewCameraFollow`의 약 59° 틸트와 일치)하는 것을
      Fill 오브젝트의 월드 회전값으로 확인
- [x] 스테이지 1→2→3 진행 확인 — RunCommand로 캐릭터 선택 완료 후 스테이지별
      적 전원에 `HealthComponent.TakeDamage(9999f, null)`로 즉사 처리 +
      업그레이드/장착 버튼의 `onClick.Invoke()`로 실제 클릭 흐름 재현.
      스테이지 2 적 3마리(Stage02.asset 저작값과 일치) 정상 스폰 확인,
      클리어 직후 `input enabled = False` + `UpgradePanel active = True`,
      장착 확정 직후 `input enabled = True` + `StageText = 'Stage 3'`까지
      로그로 확인 — 콘솔 에러/경고 없음
- [x] 보스 스테이지 전환, 10스테이지 클리어 후 Clear 오버레이 표시+입력 잠금
      — RunCommand로 캐릭터 선택→1~9스테이지 클리어(업그레이드/장착 버튼 실제
      클릭)→보스 즉사까지 전체 재현. 수정 전: `ClearOverlayPanel active=False`
      (버그), 수정 후: `IsCurrentStageFinal=True`, `ClearOverlayPanel
      active=True`, `input enabled=False`, `ClearText.text='Clear'`, 패널
      색상 `RGBA(0,0,0,0.5)`까지 전부 정상 확인. 콘솔 에러 없음
      (자동화 세션 특성상 강제 즉사 시뮬레이션으로 확인, 실제 전투 흐름 자체는
      아직 미확인)
- [ ] 원거리 변종의 실제 사격 동작, 근접 변종의 절반 이동속도 체감 — 미확인/예정
- [ ] 적이 실제로 데미지를 입었을 때 체력 바가 줄어드는지, 사망 시 자연스럽게
      사라지는지 — 미확인/예정 (구조상 되어야 하지만 실제 전투로는 아직 안 봄)

### 이슈/메모
- RunCommand 스크립트에서 `Image`/`Text`를 bare로 썼다가 기존에 알려진
  네임스페이스 충돌(`UnityEngine.UI.Image`/`Text` 완전 정규화 필요)을 다시
  겪음 — 기존 메모와 동일한 원인.
- `AssetDatabase.CreateAsset`은 상위 폴더가 없으면 예외 없이 실패만 로그로
  남기고 조용히 실패함 — `Assets/Materials` 폴더가 없어 첫 시도가 부분
  실행된 채 끝남, 폴더 생성 후 애셋을 정리하고 재실행해서 해결.
- 자기 자신을 `Awake()`에서 비활성화하는 UI 패널 패턴(`XxxUI.Awake(){
  panel.SetActive(false); }`, `panel`이 자기 자신 참조)을 쓸 때는 **그 패널이
  씬에서 처음부터 활성 상태로 시작해야** `Awake()`가 게임 시작 시 정상적으로
  한 번 실행되고 끝난다. 씬에서 비활성 상태로 시작하면 `Show()`의 첫
  `SetActive(true)` 호출이 그 자체로 "첫 활성화"가 되어 밀려있던 `Awake()`가
  `SetActive(true)` 도중에 실행되며 방금 켠 것을 다시 꺼버림. 새 UI 패널을
  이 패턴으로 만들 때마다 초기 활성 상태를 반드시 켜두고 시작할 것 —
  `HealthBarUI`처럼 `Awake()`에서 자기 자신을 끄지 않는 패턴이면 무관.

### 다음 작업
- 실제 전투로 적 체력 바 감소/사망 시 숨김 확인
- `Docs/ClaudeSessionSummary.md` 전체 재작성 완료(2026-08-24 세션 종료 시점) —
  폴더/씬 구조, 클래스 책임 표, Stage 2 상태, 설계 방향, 미결정 항목/다음
  작업까지 전부 반영함
- Stage 3 잔여 항목(`EnemyAIManager`, `GetComponent` 캐싱, `BenchmarkSpawner`,
  Profiler 기반 Before/After)은 여전히 미착수
- `UIManager`가 두 번째 구체 사례(예: 별도 메인 메뉴 씬)가 실제로 생기기 전까지는
  상속 구조로 확장하지 않기로 함 — 그 시점이 오면 재검토

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
