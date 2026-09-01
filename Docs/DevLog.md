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
