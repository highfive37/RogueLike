# Claude Session Summary — RogueLike (3D Arena Roguelite)

이 문서는 Claude Code + Unity MCP로 진행한 개발의 **현재 구조 스냅샷**이다.
날짜별 작업 사실은 `Docs/DevLog.md`, 개별 설계 결정의 대안/근거는
`Docs/AI_Decision_Log.md`를 참고. 원본 기획 문서는 `Docs/ProjectPlan.md`.
각 문서의 역할 구분은 `Docs/DocumentationGuide.md` 참고.

마지막 갱신: 2026-09-14 (RelentlessMelee 실플레이 검증 중 A* 경로추종/이동 클램프
버그 6건 발견·수정 — §6 #3d 사실상 완료); 2026-08-31 (Stage 2.5 — 아이작 스타일 방/문 시스템 전환 +
액티브/패시브 아이템 시스템 도입 시점 기준); 2026-09-01 실플레이 검증 결과 반영;
2026-09-02 로드맵 개정 — 다음 층 제작(목표 총 3층) + 3층 완성 후 UI Scene 전체
플레이 루프 구성으로 §3·§5·§6 갱신; 2026-09-02 (2) `FloorCompleteUI` 자기-비활성
패널 함정 재발 → CanvasGroup 방식으로 수정, §4 노트 갱신; 2026-09-02 (3) 층2("ㅂ"자
10개 방) authoring 완료 + 층1→층2 자동 진행(씬 리로드 없이 제자리 재빌드) 구현,
그 과정에서 발견한 잠복 버그 3건(Floor Exit 콜라이더/플레이어 시작 위치/미니맵
오프스크린) 수정, 미니맵을 로컬 스크롤 뷰포트 방식으로 개편 — §2·§3·§4·§6 갱신;
2026-09-04 스테이지3(아이작식 13방) authoring 완료 → 로드맵 §6 #1 소진, 대시를
이동속도 연동(× 배수, 수치 JSON화) + `linearVelocity` 구동으로 리워크, 걷기/대시
공용 벽 클램프(`WallClamp`) 도입, F2 디버그 즉사 토글(`DebugDamage`) 추가,
`autoSyncTransforms` off 기인 버그 3건 등 스테이지3 플레이테스트 버그 다수 수정 —
§2·§3·§4·§5·§6 갱신. 상세는 `DevLog.md`/`AI_Decision_Log.md` 2026-09-04; 2026-09-04 (2)
대시 쿨다운 HUD 슬롯 추가(로드맵 §6 #4 완료) + 1~3층 방 데이터를 `RoomDataAsset`(SO 29개)에서
`Assets/StreamingAssets/Balance/Stage/Floors/Floor{1,2,3}.json`으로 전면 외부화(로드맵 §6 #2
완료, 신규 `GameFloors`/`JsonFloorLayoutProvider`/`FloorLayout`, `RoomDataAsset`·
`ScriptableObjectFloorLayoutProvider` 삭제), 손상 층 안내용 범용 `SystemNotificationUI` 팝업 신설,
HUD 층 표시를 `x-y`(스테이지-층) 포맷으로 변경 — §2·§3·§4·§5·§6 갱신. 상세는
`DevLog.md`/`AI_Decision_Log.md` 2026-09-04 (2); 2026-09-04 (3) 로드맵 §6 #6 착수 —
타이틀 씬(`Title.unity`) 신설 + 캐릭터 선택을 인게임에서 타이틀로 이동 + 씬 라우팅 정적
클래스(`GameFlow`) + 클리어 화면에 "타이틀로" 버튼 + Build Settings 2씬(Title/Prototype).
사망 처리(Game Over)는 이번 범위 제외(임시로 Player `destroyOnDeath`만 off). 같은 (3) 세션에
HUD "당장 넣어볼 UI"도 함께 처리 — #3 크로스헤어(`CrosshairUI`), #2 런 타이머(`RunTimerUI`),
#4 체력 바 리스킨(코드 무수정, 스프라이트만), #1 일시정지 메뉴 버튼(`PauseMenuButtons` —
Restart Run/Return to Title → `GameFlow`), 캐릭터 선택을 텍스트 버튼 → 일러스트 버튼 +
hover 흰색 워시(`HoverHighlight`, 곱연산 틴트로 불가능한 것을 흰색 오버레이 자식으로 합성)로 교체.
상세는 `DevLog.md`/`AI_Decision_Log.md` 2026-09-04 (3)·(5); 2026-09-04 (4) 보스 HP 바 —
`EnemySpawner`에 보스 분기 없는 **제네릭** `EnemySpawned(HealthComponent, EnemyVariantType)`
이벤트를 추가하고 신규 `BossHealthBarUI`가 `variant == Boss`만 필터해 표시(보스 로직은 스폰
경로에 유입 안 함), 상세는 `AI_Decision_Log.md` 2026-09-04 (4); 2026-09-04 (5) 캐릭터 선택
일러스트 버튼 + `HoverHighlight`; 2026-09-04 (6) 타이틀 Start 버튼 홀드 시 캐릭터 선택이 즉시
클릭되던 상호작용 버그를 "포인터/키가 떼어질 때까지 melee/ranged `interactable` off" 방식으로
수정(`CharacterSelectUI` — arm-after-release 코루틴), 상세는 `AI_Decision_Log.md` 2026-09-04 (6).
일시정지 UI `ConfirmButton`을 `btn_close_*` 이미지 버튼으로 교체 + 3버튼(Restart/Title/Close)을
오른쪽 영역 하단 정렬로 재배치(RectTransform 튜닝, 코드 무수정).
2026-09-08 적 AI 보강 2차 + 사망 흐름 + 벤치마크 — (a) 히트 스태거/넉백(`EnemyStunnedState`,
`EnemyData.StunDuration` 재활용 + 단일 `KnockbackResistance`, `IAttackPattern.Interrupt()`),
(b) 원거리 3패턴을 공유 `RangedKitingBehavior`로 통일, (c) 이동 wobble 공유 헬퍼
`EnemyMovementFlavor`(+`EnemyData` wobble 2필드) + 신규 변형 `ErraticMelee`/`BurstRanged`
(`BurstRangedAttackPattern` 신규), (d) 보스 Rigidbody 오설정(useGravity/constraints) + 발사체
자해 수정, (e) 플레이어 사망 → `GameOverUI`(`FloorCompleteUI` 대응 구조) + `RoomFlowManager.
HandlePlayerDied`(스크립트 캡슐 틱오버 연출), (f) 대시 중 `Physics.IgnoreLayerCollision`으로
적 관통 + 종료 시 오버랩 보정 꼬리(`PlayerDodgeRoll` 재구성, `WallClamp.GetCapsuleGeometry`
추출), (g) 격리 스트레스 씬 `BenchmarkScene` + `BenchmarkSpawner`로 §6.5 Before 표(4종 ×
50/100/200) 완성, (h) 근접 적 공전 수정 — 사거리 안에서는 이동을 공격 패턴 런지에만 위임,
그 사이엔 정지·회전만(`EnemyChaseState`/`EnemyAttackState`, idle sway는 원거리 쿨다운에만) +
런지 오버슈트 클램프(`MeleeAttackPattern`, 플레이어 관통 방지 — 피격 넉백 시스템은 아직 없음,
차후),
(i) `GameOverUI` 페이드를 dim/content/buttons 단일 타임라인으로 병합 — §2·§3·§5 갱신, 상세는
`DevLog.md`/`AI_Decision_Log.md` 2026-09-08;
2026-09-09 씬 전환 페이드(로드맵 §6 #6 잔여) — 신규 `SceneTransitionController`(프로젝트 유일의
`DontDestroyOnLoad` 오브젝트, 전체화면 검은 `CanvasGroup` 오버레이를 프리팹 없이 코드로 생성).
`GameFlow.LoadScene`이 동기 `SceneManager.LoadScene` → `EnsureExists().FadeAndLoad(name,
PauseState.ForceResume)`로 위임(페이드 아웃 unscaled → `LoadSceneAsync(Single)` → 1프레임 →
페이드 인, `ForceResume`는 로드 직전 콜백으로). 씬 배선 무변경. 구조/1회 전환은 자동 검증,
연속 전환 페이드 완주는 유저 실플레이 대기 — §2·§3·§6 갱신, 상세는 `DevLog.md`/
`AI_Decision_Log.md` 2026-09-09;
2026-09-09 (2) 적 방 이탈 → 진행 불가 블로킹 버그 수정 — 적 이동 6곳을 단일 `EnemyStateMachine.
MoveTo`로 수렴 + 신규 `RoomController.ClampToInterior`로 방 내부 AABB 클램프(A) + `FixedUpdate`
`EnforceRoomContainment` 안전망(B). `EnemySpawner.SpawnRoom`도 스폰 좌표 클램프 + `RoomController`
주입. 원인은 넉백(2026-09-08)의 `MovePosition` 절대점 러프가 Discrete 콜라이더 벽(두께 1)을
관통 + `CanSeePlayer`가 순수 거리라 탈출한 적이 복귀 안 함. 클램프 수치·컴파일은 자동 검증,
런타임은 유저 실플레이 대기(자동화 Play Mode `frameCount` 고정) — §2·§4 갱신, 상세는 `DevLog.md`/
`AI_Decision_Log.md` 2026-09-09;
2026-09-09 (3) 신규 적 변형 2종(`ErraticMelee`/`BurstRanged`)을 `Floor2.json`·`Floor3.json`의
전 전투방에 고루 배치(2026-09-08엔 Floor1에만 있었음) — `variant` 문자열만 교체, 스폰 좌표·적
수·구조 불변. 실제 `GameFloors` 로더로 파싱·분포 검증 통과;
2026-09-09 (4) 포트폴리오 트랙 #1(자동화 테스트+CI) 완료 — asmdef 도입(`RogueLike.asmdef` 단일),
`Assets/Tests/{EditMode,PlayMode}`, `.github/workflows/ci.yml`(GameCI). CI에서 초록;
2026-09-09 (5) 포트폴리오 트랙 #2(절차적 층 생성) — **기본 던전이 이제 시드 기반 절차 생성**.
결정적 자체 PRNG(`DeterministicRng`/`RngStreams`) + `RunConfig`(시드+소스 모드, `GameFlow` 전달)
+ `IFloorSource` 심(`JsonFloorSource`/`ProceduralFloorSource`) + 아이작식 `ProceduralFloorGenerator`
(큐 성장 → 방/타입/문/적) + 층별 난이도 `ProceduralRules.json`. `GameFloors.ValidateGraph` →
공용 `FloorGraph.Validate` 추출. 자동화 테스트 EditMode 82 + PlayMode 10 전부 초록, `Prototype.unity`
씬 실측 통과. F1 오버레이에 `SEED` 표시. **유저 실플레이 검증 대기** — §2·§3, `DevLog.md`/
`AI_Decision_Log.md` 2026-09-09 (3). 남은 것: Phase F(시드 화면 표시 + 타이틀 시드 입력).
2026-09-09 (6) 트랙 #2 Phase E 마무리 + 발사체 처리 — (a) 방 바닥을 코드 생성 흑백 체커
머티리얼로(방 크기별 공유, 칸 2유닛 고정), 그 과정에서 드러난 Z-파이팅은 `Prototype.unity`의
Stage 1 시절 정적 `Ground` Plane(y=0, 룸 바닥과 동일 평면) **삭제**로 해소(유저 "A안"),
(b) 무작위 시드를 양수 전용(`Random.Next(1, int.MaxValue)`)으로, (c) F1 켠 상태에서 R = 런
재시작(새 시드) 디버그 단축키, (d) **적 발사체 ↔ 아군 발사체가 서로 상쇄되지 않고 통과** —
`Projectile.OnTriggerEnter`에서 상대가 `Projectile`이면 즉시 return(데미지·소멸 없음), 신규
PlayMode 테스트 `ProjectileTests` 1개로 회귀 잠금(유저 실플레이 확인 완료). 자동화 테스트
**EditMode 82 + PlayMode 11 전부 초록**. §2·§3·§6, 상세는 `DevLog.md`/`AI_Decision_Log.md`
2026-09-09 (4). 트랙 #2 남은 것은 그대로 Phase F + 절차 생성 유저 1주파 검증.
2026-09-11 **§6 #3 "방/장애물 배치"의 방 구성 절반 완료(장애물+A*는 다음 세션으로 분리)** —
(1) Phase 1: `RoomController.BuildGeometry`의 Plane/Cube 프리미티브를 `modular_space`(Kenney,
CC0) 코너/벽/반벽/바닥 타일 런타임 절차적 배치로 전면 교체, 신규 `RoomTileSet` SO(직렬화 필드
참조). (2) Phase 2: `RoomFootprint`(바운딩박스+선택적 코너 노치) 모델 도입, 전투방 한정
가로직사각형·ㄱ자(노치 코너) 형태 추가. (3) Phase 3: 겉모습만 다르던 단일 셀 방식이 아니라
**격자 셀을 실제로 2~3개 병합하는 진짜 멀티셀 방** 도입(`RoomData.GridPosition`
`Vector2Int`→`Vector2`, `RoomDoorConnection.SubCellIndex` 신설) — 기존 격자 성장/데드엔드/
보스판정 알고리즘은 건드리지 않고 "생성 후 병합 패스"만 추가. 한 변에 문 2개가 있어도
정확한 도착 문을 찾도록 `RoomTransitionController`를 방향 기반→`TargetRoom` 매칭 방식으로
교체(`DoorController.OwnerRoom` 신규). (4) 문 배치를 변 중앙이 아닌 절반 구역 중앙으로
오프셋, 미니맵이 실제 방 모양을 표시하도록 개선. (5) 유저 실플레이 피드백 기반 크기/확률
튜닝(`CombatRoomSize` 40→28, `MergedWideLong` 92→36, `LMergeBounding` 92→44, ㄱ자 병합
후보 선탐색 2단계 재구성으로 출현빈도 개선). (6) 병합 방 서브셀 분할 시 문 옆 벽 타일
배치가 타일 격자에 반올림 안 돼 **잠긴 문 옆으로 빠져나갈 수 있던 실제 버그** 발견·수정.
EditMode 89개 전부 초록, 유저 실플레이로 ㄱ자 출현·문 오프셋·틈 버그 수정 확인 완료. 무료
에셋(`Assets/Art/Props/modular_space`, `cyberpunk_game_kit`) 커밋 완료 — `cyberpunk_game_kit`은
다음 세션(장애물 배치)에서 사용 예정. §2·§3·§6 갱신, 상세는 `DevLog.md`/`AI_Decision_Log.md`
2026-09-11.
2026-09-12 **§6 #3 "방/장애물 배치"의 장애물 절반 완료(A*는 다음 세션 예정)** — 전투방 한정,
절차 생성 단계(`ProceduralFloorGenerator.BuildObstacles`)에서 문/입구 주변 2칸을 제외한 내부
타일 셀 중 시드로 섞어 고르되, 채택 직전마다 BFS로 "남은 빈 칸이 여전히 하나로 연결되는가"를
검사해 방을 완전히 막는 배치는 자동으로 걸러냄(전용 명명 RNG 스트림이라 기존 시드의 방 그래프/
적 배치는 무변경). 신규 `ObstaclePlacement`/`RoomObstacleSet`, `RoomData.Obstacles`(옵션 파라미터,
기본 빈 리스트라 기존 생성 지점 전부 무변경). `RoomController.BuildObstacles`가 같은 4유닛
타일 그리드 좌표로 프리팹을 배치하고, `ClampToInterior`(적 컨테인먼트 유일 지점)에
`RoomObstacleBounds` 클램프를 추가해 적도 장애물 박스 밖으로 밀려나게 함(단, 지능적 우회는
아직 없음 — A*의 몫). `cyberpunk_game_kit` 소품(AC/Fence/Computer/Support_Short) 4종을 실제
메시 크기 그대로(첫 시도 때 FBX 루트에 이미 박힌 임포트 스케일을 놓쳐 오히려 축소되는 버그를
겪고 수정) 콜라이더까지 부착해 프리팹화 — 콜라이더가 트리거 아님+Rigidbody 없음이라
`WallClamp`(플레이어)와 `Projectile.OnTriggerEnter`(발사체)가 코드 변경 없이 벽과 동일하게
차단. 가로직사각형 방은 짧은 변이 좁아 이번 규칙상 후보 칸이 없어 장애물 미배치(의도된 제한).
EditMode 99개(신규 10개 포함) 전부 초록, `RunCommand` 기반 기능 검증(인스턴스화·콜라이더·클램프
동작·물리 등록)까지 확인, 실제 Play Mode 육안 확인은 유저 몫으로 남김. §2·§3·§6 갱신, 상세는
`DevLog.md`/`AI_Decision_Log.md` 2026-09-12.
2026-09-12 (2) 같은 세션, 유저 요청으로 "1x1칸이 이어지는" 장애물(울타리 군집) 추가 — 신규
`Assets/Art/Props/cube_world_kit` 도입(`Rock1/2` 바위, `Fence_Center/Corner/T/End` 모듈형
울타리). `ObstaclePlacement`에 `ObstacleKind` 필드만 추가해 기존 데이터 모델·조립 루프·연결성
검사를 그대로 재사용, 신규는 `FenceObstacleKit`(4개 명명 필드 SO) 하나. 모양을 일자로만 안
만들려고 **랜덤워크로 군집 성장**(`BuildFenceCluster`/`GrowCluster` - 4연결/루프만 금지, 나머지는
자유롭게 뻗어나가 직선·코너·T자가 자연스럽게 섞여 나옴) 방식 채택. 조각-회전 매핑은 SceneView로
직접 측정한 각 조각의 기본 연결 방향(Center={서,동}, Corner={동,북}, T={서,동,북}, End={동})을
90° 회전 순환(남→서→북→동→남)으로 역산해 구현. **실제 버그 발견·수정**: 루프 방지 조건이 항상
참이 되는 논리 오류로 클러스터가 1칸을 절대 못 넘던 것을 EditMode 회귀 테스트가 잡아냄(수정 후
강제 확률 100% 디버그에서 30/30 성공, End/Corner/Straight/T 모두 등장 확인). 테스트 도중 Unity가
멈춘 것처럼 보인 해프닝은 실제로는 씬 저장 확인 팝업이 원인이었음(무한루프 아님). EditMode
102개 전부 초록. §2·§3·§6 갱신, 상세는 `DevLog.md`/`AI_Decision_Log.md` 2026-09-12 (2)(3).
2026-09-12 (3) 같은 세션, 유저 1층 실플레이 피드백("정사각형 방만 장애물 있고 썰렁하다, 아이작은
안 그랬다")으로 **장애물 배치를 라이브 절차 생성에서 "패턴 라이브러리 선택" 방식으로 전면 전환**
(아이작 실제 방식 재현 - 던전 그래프는 절차적, 방 내용물은 미리 만든 배치 중 시드로 선택).
같은 세션 (2)에서 만든 랜덤워크 절차 생성(`BuildFenceCluster`/`GrowCluster` 등)을 전부 삭제하고
신규 `ObstaclePatternLibrary`(JSON, ASCII 그리드로 정사각형 8종/ㄱ자 7종 패턴 작성)로 교체 —
단 조각-회전 산출(`PieceFor`/`RotateDir`)은 그대로 재사용. 유저 확정으로 ㄱ자 방도 패턴화 대상에
포함(가로직사각형은 여전히 장애물 없음) — ㄱ자는 NE 노치 기준 패턴 하나만 작성하고 런타임에
실제 노치 방향(4가지)에 맞춰 좌표를 회전(`RotateCell`/`NotchRotationSteps`)해 재사용, 정사각형은
노치가 없어 대신 매번 랜덤 회전을 적용해 패턴 8개로 체감 다양성 4배 확보. `FloorRule`의 장애물
관련 필드 전부 제거. 패턴 검증 테스트가 실제 결함(`Cross` 패턴이 중앙 칸을 고립시키는 버그)을
잡아내 수정. 60시드 실측 결과 빈 방 비율 정사각 12.6%·ㄱ자 13.3%("적지만 있다" 목표 부합).
SceneView로 실제 생성된 방(ㄱ자 SE 노치 - 7칸 꺾인 벽, 정사각형 - T자 울타리) 육안 확인. EditMode
108개 전부 초록. §2·§3·§6 갱신, 상세는 `DevLog.md`/`AI_Decision_Log.md` 2026-09-12 (3)(4).
2026-09-12 (4) 같은 세션, 유저가 "이어진 장애물이 1층에서 여전히 안 보인다" 지적 → 원인은 버그가
아니라 확률+층 구조(1층 전투방의 82%가 정사각형, ㄱ자는 병합 조건 희소성 때문에 1% 수준만 등장;
정사각형도 절반만 울타리 패턴이라 60시드 중 16개(27%)는 1층 전체에 울타리가 하나도 없었음).
울타리 포함 패턴 가중치를 2→3으로 올려 27%→17%로 개선(0%로는 안 만듦 - 약간의 변동성은 의도적
유지). 추가로 유저 요청으로 **`cyberpunk_game_kit` 장애물 소품(AC/Fence/Computer/Support) 완전
제거** — 실제 프리팹 삭제, `RoomObstacleSet`을 바위 2종만 남기고 `RoomObstacleSet_Solo`로 개명,
`ObstacleVariantCount` 6→2. 원본 `cyberpunk_game_kit` 애셋 폴더 자체는 유지(다른 용도 재사용
가능성). EditMode 108개 그대로 유지. 상세는 `DevLog.md` 2026-09-12 (4).
2026-09-12 (5)~(7) 같은 세션 후속: 소품 다양성 복구 위해 울타리 조각 4종을 단일 소품 풀에도
추가했다가, 유저가 씬의 실제 오브젝트를 지목하며 지적 → 유저의 원래 의도가 "방향별 자동 조각
선택"이 아니라 **"같은 정육면체 블록을 여러 칸에 반복 배치"**였음이 드러남. `ObstacleKind`
enum·`FenceObstacleKit`·`PieceFor`/`RotateDir` 등 방향 기반 조각 선택 시스템을 통째로 삭제하고,
패턴에 표시된 칸엔 전부 동일한 프리팹(`cube_world_kit`의 `Block_Stone` 1종)을 반복 배치하는
방식으로 단순화 — 코드 약 150줄 감소, 4연결 제한 같은 "메시가 없어서 생기던 제약"도 사라짐.
기존 패턴 JSON(칸 배치)은 그대로 재사용. EditMode 108→104개(더는 의미 없는 검증 4개 제거).
같은 방을 다시 렌더링해 블록 반복으로 정확히 나오는 것 확인. 상세는 `DevLog.md`/
`AI_Decision_Log.md` 2026-09-12 (5)~(7).
2026-09-13 유저가 실제 이어진 벽(ㅏ자)의 폭이 "너무 두껍다"고 피드백 → **칸마다 완전한 블록
대신, 인접한 두 칸 중심을 정확히 잇는 얇은 세그먼트(`ObstacleShape.Connector`, 세계 좌표
1×4×4)로 전환**. 코너/T자/십자 교차는 한 칸에서 여러 세그먼트가 겹치는 것만으로 저절로
표현되어 전용 코너 메시가 필요 없어짐(신규 `Block_Stone_Connector` 1종만 추가, `Block_Stone`
X축만 1/4 축소). 같은 세션에 유저가 요청한 "더 복잡한 패턴"(대각선 두 줄 등)도 반영해
`FenceCrossHub`/`PlusHub`/`DiagonalDoubleScatter` 3종 추가(대각선 패턴은 좁은 정사각형 방 대신
ㄱ자 방에 배치). EditMode 104개 전부 통과, SceneView로 사방 교차 Connector 육안 확인. 상세는
`DevLog.md`/`AI_Decision_Log.md` 2026-09-13.
2026-09-13 (계속)~2026-09-14 신규 적 `RelentlessMelee`(한 번 본 플레이어를 방 안에서
끝까지 쫓음, `TracksLivePositionWhileBlind`)를 실플레이로 검증하는 과정에서 **A*
경로추종·이동 시스템에 서로 다른 원인의 버그 6건**을 순차 발견·수정(전부 신규 회귀
테스트로 잠금): (1) 시작/목표가 같은 칸으로 반올림돼 트리비얼 경로가 나오던 문제 →
`FindPath`가 같은 칸일 때 실제 위치 기준 인접 칸으로 보정(`ResolveEffectiveCell`),
(2) 시야가 확보되면 공격 사거리 안에서도 멈추지 않던 문제 → `clearLine` 게이팅 추가,
(3) Connector(얇은 벽) 클램프 박스의 두 축 크기가 뒤바뀌어 실제보다 훨씬 두껍게
작동 → 축 스왑 수정, (4) 웨이포인트는 Y=0인데 적 몸체는 Y=1이라 도착 판정이 영원히
성립 안 하던 문제 → 수평 거리만 비교하도록 수정, (5) `Repath()`가 재탐색마다
`_pathIndex`를 0("지금 서 있는 칸 자신")으로 되돌려 진행을 스스로 취소하던 "떨림" 버그
→ 경로가 2칸 이상이면 1번 인덱스로 바로 건너뛰도록 수정, (6) Solo 장애물의 이동
클램프가 실제 프리팹 크기와 무관하게 항상 타일 전체(4×4)로 등록되어, 시야 판정(실제
콜라이더 기준)과 서로 다른 장애물 모델을 쓰는 바람에 대쉬처럼 순간적인 위치 변화
상황에서 적이 벽을 관통하던 문제 → 클램프 박스를 실제 `Collider.bounds`에서 읽도록
변경. §6 #3d(A*)는 실질적으로 이 6건까지 포함해 완료 — EditMode 115/115, PlayMode
36/36. 상세는 `DevLog.md` 2026-09-13(다섯 건)·2026-09-14(여섯 번째),
`AI_Decision_Log.md` 2026-09-14(여섯 번째, 대안 비교 있었던 결정).

---

## 1. 프로젝트 목표

- **장르**: 3D 쿼터뷰 액션 로그라이트 — **3D 바인딩 오브 아이작 / 엔터 더 건전** 계열
  (유저가 2026-09-09에 명시적으로 선언: 문으로 연결된 공간형 방, **절차적 생성 층**,
  타이트한 트윈스틱식 이동/회피, 런 기반). 앞으로 작업 제안·범위 판단의 기준점.
  `ProjectPlan.md` §2 Genre에도 반영.
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
- 밸런스 수치(플레이어 기본 스탯 + 대시 배수/지속시간, 패시브 아이템 효과)는 코드/씬이
  아니라 `Assets/StreamingAssets/Balance/*.json` 평문 JSON 파일에서 로드(§4 참고) —
  Unity/Claude 없이 텍스트 에디터로 직접 수정 가능한 워크플로가 실사용 중
- **층/방 레이아웃 데이터도 동일 워크플로로 외부화(2026-09-04 (2))**: 1~3층의 방 배치·
  크기·문 연결·몬스터 구성 전부가 `Assets/StreamingAssets/Balance/Stage/Floors/Floor{1,2,3}.json`
  평문 JSON. 파일 하나 = 한 층. `Floor1`부터 없어질 때까지 열거해 층 수 결정 → 4층 추가는
  `Floor4.json` 파일 하나만 추가하고 `Floor3.json`의 `isLastFloor`를 false로 바꾸면 끝
  (코드/인스펙터 무관). 파일 파싱 실패 시 그 층만 코드 내장 비상용 3방 층으로 대체 +
  진입 시 시스템 팝업. 구 `RoomDataAsset`(SO 29개) 방식은 완전 폐기.

### 폴더 구조 (대표 클래스만, 전체 목록은 코드 참고)
```
Assets/Scripts/
├─ Core/      (GameFlow — 씬 라우팅 정적 클래스 2026-09-04 (3): 씬 이름 상수 +
│              StartRun/RetryRun/ReturnToTitle + PendingArchetype(씬 넘겨 전달되는
│              선택 아키타입); SceneTransitionController — 2026-09-09 신규, 프로젝트
│              유일의 DontDestroyOnLoad. 검은 전체화면 CanvasGroup 오버레이(프리팹
│              없이 EnsureExists()가 코드로 생성). FadeAndLoad(name, onBeforeActivate):
│              페이드 아웃(unscaled) → LoadSceneAsync(Single) → 1프레임 → 페이드 인.
│              GameFlow.LoadScene이 여기로 위임, 인런 층 전환 FloorTransitionUI와는 별개)
├─ Player/    (PlayerMovement, PlayerAim, PlayerDodgeRoll, PlayerCombat,
│              PlayerInputActions, PlayerPassiveStats, PlayerActiveItemInventory,
│              ActiveItemController, CharacterSelectController — 2026-09-04 (3)부터
│              선택 UI가 아니라 GameFlow.PendingArchetype 소비만, PauseState(+ForceResume),
│              PrototypeActiveItemSwitcher, EquipMenuController,
│              WallClamp — 걷기/대시 공용 벽 방향 클램프 정적 헬퍼 2026-09-04 등)
├─ Enemy/     (EnemyStateMachine + FSM 상태 6종, IAttackPattern 구현체 3종,
│              EnemyData / AttackPatternData ScriptableObject, EnemySpawner)
├─ Room/      (RoomController, DoorController, RoomTransitionController,
│              RoomData, RoomDoorConnection, DoorDirection, RoomType,
│              IFloorLayoutProvider/JsonFloorLayoutProvider — 방 그래프 소스가
│              RoomDataAsset(SO)에서 FloorN.json으로 바뀜 2026-09-04 (2),
│              RoomFlowManager — 업그레이드/장착 UI 시퀀싱만 담당)
├─ Stage/     (StageManager — 층/방 그래프 빌드+현재 방 추적 + FloorDataCorrupted
│              이벤트, GameFloors — FloorN.json 로더(정적, GameBalance 패턴) +
│              DTO, FloorLayout — 런타임 1개 층 데이터, EnemyVariantType 등)
├─ Combat/    (IDamageable, Health, HealthComponent, IWeapon 구현체 2종,
│              IActiveItem 구현체 3종(`GrenadeActiveItem`/`EnhancedShotActiveItem`/
│              `FlamethrowerActiveItem`), Projectile류, ActiveItemData류,
│              PassiveItemData, PassiveItemType, ItemPickup, ItemDropManager,
│              DebugDamage — F2 적 즉사 토글 정적 상태 2026-09-04)
├─ Balance/   (GameBalance — JSON 밸런스 파일 로더, 실패 시 내장 기본값 폴백)
├─ Camera/    (QuarterViewCameraFollow)
├─ UI/        (UIManager, ActiveItemEquipUI, PlayerStatsPanelUI,
│              CharacterSelectUI(2026-09-04 (3)부터 Title 씬에 배치),
│              TitleScreenController(2026-09-04 (3) 신규 — 타이틀 화면 Start→캐릭터 선택 흐름),
│              FloorCompleteUI(구 ClearOverlayUI, 마지막 층엔 "다시하기"+"타이틀로"),
│              FloorTransitionUI(2026-09-02 (3) 신규, 층 전환 검은 페이드),
│              SystemNotificationUI(2026-09-04 (2) 신규 — 범용 시스템 알림 팝업,
│              딤+메시지+확인 버튼, CanvasGroup 방식, 메시지 큐),
│              HealthBarUI, ActiveItemCooldownUI, DashCooldownUI(2026-09-04 (2)
│              신규 — 대시 쿨다운 HUD 슬롯, ActiveItemCooldownUI 미러),
│              CrosshairUI/RunTimerUI/BossHealthBarUI/PauseMenuButtons(2026-09-04 (3)·(4)
│              신규 — HUD 크로스헤어·런 타이머·보스 HP 바·일시정지 메뉴 버튼),
│              HoverHighlight(2026-09-04 (5) 신규 — hover 시 흰색 오버레이 페이드, opt-in 비주얼),
│              BillboardUI, MinimapUI, PickupNotificationUI,
│              DebugOverlayController 등)
└─ Pooling/   (GameObjectPool, PooledObject)
```
asmdef: 2026-09-09에 `Assets/Scripts/RogueLike.asmdef` **하나**를 도입(전체를 단일 어셈블리로
래핑, A1). 이유는 **오직 테스트 프레임워크 요구**(UTF 테스트 asmdef는 `Assembly-CSharp`를 참조
못 함) — 계층 분리(Core/Runtime 등)는 여전히 안 함(실제 의존성 방향 문제가 드러나면 그때).
references: `Unity.InputSystem`, `UnityEngine.UI`. `Assembly-CSharp`엔 `Assets/TutorialInfo/`
템플릿 잔재만 남음(정리 대상). 상세 `AI_Decision_Log.md` 2026-09-09 (2).

테스트(2026-09-09 착수, 포트폴리오 트랙 #1): `Assets/Tests/EditMode/` + `Assets/Tests/PlayMode/`,
asmdef 각 1개(`RogueLike.Tests.EditMode`/`.PlayMode`). 현재 **49개**(EditMode 39 + PlayMode 10)
전부 통과 — `Health`/`KnockbackMath`/`RoomBounds` 순수 로직, `GameFloors` 파싱·그래프 검증·폴백,
`GameBalance` 플레이어 스탯 파싱, 오브젝트 풀 이중반환 가드, **방 이탈 컨테인먼트 회귀 잠금**,
적 FSM 전이. 테스트를 위해 순수 로직 추출(`KnockbackMath`/`RoomBounds`), `GameFloors`/`GameBalance`에
`internal` 파싱 시크릿 + `AssemblyInfo.cs`의 `InternalsVisibleTo`. `GameFloors`에 그래프 연결성·
dangling connection 검증 신규(단절/댕글링 = 소프트락 → 이제 `IsCorrupted` 폴백). Phase 4(CI)는
유저의 Unity 라이선스·GitHub 시크릿 등록 필요. 진행 상황 `DevLog.md` 2026-09-09.

`StageData`/`StageDataAsset`/`IStageDataProvider`/
`ScriptableObjectStageDataProvider`(4개, 웨이브 반복 방식)와
`Assets/Data/Stages/Stage01~10.asset`(10개)는 룸 시스템 전환으로 완전히
대체되어 삭제됨. `UpgradeSelectionUI`/`UpgradeSlotView`도 바닥 드롭 아이템
방식으로 대체되어 삭제됨. `RoomDataAsset`/`ScriptableObjectFloorLayoutProvider`와
`Assets/Data/Rooms/*.asset`(29개), `StageManager.FloorDefinition`도 JSON 외부화로
대체되어 삭제됨(2026-09-04 (2)). `RoomData`/`StageEnemyEntry`/`RoomDoorConnection`/
enum 4종은 JSON이 역직렬화되는 런타임 타입이라 유지.

"스킬" → "액티브 아이템" 전면 리네임 완료(2026-08-31): `SkillItemData`→
`ActiveItemData`, `ISkillItem`→`IActiveItem`, `SkillController`→
`ActiveItemController`, `PlayerSkillInventory`→`PlayerActiveItemInventory`,
`EquipSelectionUI`→`ActiveItemEquipUI` 등. 단, 직렬화된 `[SerializeField]`
필드 이름 자체는 기존 씬/에셋 데이터 보존을 위해 그대로 유지(공개 API만 새
이름으로 감쌈). 드롭/픽업 인프라(`ItemPickup`/`ItemDropManager`)는 반대로
"Active" 접두를 떼고 아이템 종류에 중립적인 이름으로 유지 — 패시브 아이템도
같은 드롭 방식을 쓸 예정이기 때문.

### 씬 구성

씬 2개 (2026-09-04 (3)): **`Title.unity`**(Build Settings index 0, 시작 씬) → **`Prototype.unity`**(index 1, 인게임).
씬 rename(`Prototype`→`Gameplay`)은 게임 구색 갖춰진 뒤로 보류.

**`Title.unity`**: Camera + EventSystem(`InputSystemUIInputModule`) + Canvas(1920×1080). 배경
Image(`Assets/Art/UI/Title/title_bg`) 위에 `TitleGroup`(로고 `title_logo` + Start 버튼
`btn_start_*` 3상태 SpriteSwap) / `CharacterSelectPanel`(기존 `CharacterSelectUI` 재사용, 씬에
active 저장). `TitleScreenController`(Canvas에 부착): Start → TitleGroup 숨김 → CharacterSelectUI
표시 → 근접/원거리 선택 시 `GameFlow.StartRun(archetype)` → `Prototype` 씬 로드.

**`Prototype.unity`** (이하):
- `Player`(프리팹 인스턴스), `Main Camera`. (Stage 1 시절의 정적 `Ground` Plane은 2026-09-09
  삭제 — 룸 시스템이 방마다 `Ground` 레이어+MeshCollider 바닥 Plane을 만들어 완전 대체, 남겨
  두면 y=0 동일 평면 Z-파이팅. `Ground` **레이어**는 유지.)
- **방/문 그리드 시스템**(2026-08-31 신규): 씬에 방이 사전 배치되어 있지 않고,
  `StageManager.BuildFloor()`가 현재 층의 `FloorLayout.Rooms`를 기준으로
  `Room_Base.prefab`을 그리드에 전부 인스턴스화(백트래킹 가능). 각 방 인스턴스는 자체 `RoomManager`+
  `RoomController`를 가짐(스테이지 1개당 하나였던 기존 `RoomManager`와
  달리, 방마다 별도 인스턴스).
  - **층 데이터 소스는 `IFloorSource` 뒤에 추상화**(2026-09-09). 실제 런은 기본이
    `ProceduralFloorSource`(시드로부터 아이작식 절차 생성, 층 3개 고정) — `RunConfig.Seed`가
    `GameFlow`에서 씬 넘겨 전달됨. `JsonFloorSource`(손authoring `FloorN.json` 3층)는
    `RunConfig.FloorSource = Json`으로 여전히 선택 가능한 디버그/레퍼런스 모드. 둘 다 손상 시
    비상층 폴백. 생성기·규칙은 §3·`AI_Decision_Log.md` 2026-09-09 (3).
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
  `HUD`(`HealthBarUI`/`ActiveItemCooldownUI`+`SkillCooldownSlot`/`DashCooldownSlot`(우하단,
  2026-09-04 (2))/`StageText`(이제 `x-y` 스테이지-층 표기, 예 `1-3`)),
  `SystemNotificationPanel`(2026-09-04 (2), 층 데이터 손상 안내 팝업, `SystemNotificationUI`),
  `EquipPanel`(ESC로 토글, 자식으로 액티브 아이템 슬롯 목록 +
  `StatsPanel`(패시브 스탯 Pip 바 4종)), `CharacterSelectPanel`,
  `ClearOverlayPanel`(컴포넌트명은 `FloorCompleteUI`), `Minimap`(우상단, 현재 방
  중심 로컬 스크롤 뷰포트 + 안개 효과 — §4 참고), `FloorTransition`(화면 전체
  검은 페이드, Canvas 마지막 자식이라 다른 패널 위에 그려짐)이 자식으로 배치,
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
  맞히던 버그를 고치기 위해 도입, 관련 레이캐스트를 이 레이어로 제한. 현재 이 레이어를
  쓰는 것은 룸 시스템이 방마다 만드는 바닥 Plane(`RoomController.BuildGeometry`) —
  옛 정적 `Ground` 오브젝트는 2026-09-09 삭제됨
- 레이어: `Enemy` (2026-09-08 추가) — 대시 중 `Physics.IgnoreLayerCollision(Player, Enemy)`로
  적 관통을 켜고 끄기 위해 도입. `PlayerDodgeRoll`의 오버랩 체크(`Physics.CheckCapsule`)도
  이 레이어 마스크를 씀

### 클래스 책임 요약 (영역별 대표 클래스 — 전체 목록 아님)
| 영역 | 대표 클래스 | 책임 |
|---|---|---|
| 이동/조준 | `PlayerMovement`(`CurrentMoveSpeed` = base+패시브, 이동속도 단일 소스), `PlayerAim`, `PlayerDodgeRoll`(구르기 = `CurrentMoveSpeed`×`dashSpeedMultiplier` 속도로 `dashDuration` 버스트, `linearVelocity` 구동, `CancelDodgeRoll()`로 전환/Floor Exit 시 취소, 대시 중 `Physics.IgnoreLayerCollision`으로 `Enemy` 관통 + 종료 시 적과 겹치면 저속 미끄러짐 보정 꼬리 — 2026-09-08), `WallClamp`(공용, `GetCapsuleGeometry()` 추출) | Rigidbody 기반 이동/회전/구르기, New Input System 액션 소비. 이동 명령 전 `WallClamp`로 벽 방향 성분 제거(관통 방지). 대시 수치는 `PlayerBaseStats.json` |
| 전투(플레이어) | `PlayerCombat`+`IWeapon`(`MeleeWeapon`/`RangedWeapon`), `ActiveItemController`+`IActiveItem`(`EnhancedShotActiveItem`/`GrenadeActiveItem`/`FlamethrowerActiveItem`) | 기본 공격과 장착 액티브 아이템을 각각 다형성 구조로 분리 |
| 캐릭터 선택 | `TitleScreenController`+`CharacterSelectUI`(Title 씬), `GameFlow.PendingArchetype`, `CharacterSelectController`(Prototype 씬, 소비만) | 2026-09-04 (3): 선택은 Title 씬에서 → SO 참조를 정적으로 씬 넘겨 전달 → Prototype 씬 Player가 `Start()`에서 무기 장착 + 입력 활성화(`fallbackArchetype`는 에디터 직접 실행용) |
| 액티브 아이템 보유/장착 | `PlayerActiveItemInventory`, `OwnedActiveItem`, `ActiveItemEquipUI` | ESC 메뉴에서 소지 아이템 목록 확인 + 장착 전환 |
| 패시브 스탯 | `PlayerPassiveStats`, `PlayerStatsPanelUI` | 공격력/이동속도/사거리/공격속도 4종 전부 "base + 고정값 보너스" 덧셈 모델로 통일(2026-08-31, §4 참고), ESC 메뉴에 Pip 바(10칸)로 표시 |
| 밸런스 수치 | `GameBalance`, `Assets/StreamingAssets/Balance/*.json` | 플레이어 기본 스탯/패시브 아이템 효과를 JSON에서 로드, 파일 없거나 파싱 실패 시 내장 기본값으로 폴백 |
| 적 AI | `EnemyStateMachine`(FSM: Idle→Patrol→Chase→Attack→Stunned/Dead; 피격 시 `HealthComponent.AnyDamageTaken` 구독으로 `StunDuration`만큼 Stunned + `KnockbackResistance` 기반 넉백 — 2026-09-08. **모든 적 이동은 단일 초크포인트 `MoveTo(worldPos)` 경유** → 스폰 시 주입된 `RoomController`의 `ClampToInterior`로 방 내부 AABB에 클램프 + `FixedUpdate`의 `EnforceRoomContainment`가 slack 밖으로 나간 적을 스폰 지점으로 스냅 — 2026-09-09 적 방 이탈/진행 불가 버그 수정, §4 참고), `IAttackPattern`(`MeleeAttackPattern`/`RangedAttackPattern`/`BurstRangedAttackPattern`/`RetreatAndShootPattern`, `Interrupt()` 있음), 공유 헬퍼 `RangedKitingBehavior`(원거리 3패턴 공통 후퇴/윈드업), `EnemyMovementFlavor`(직선 이동에 수직 sine wobble) | 일반 적과 보스가 같은 FSM을 공유, 공격 패턴 데이터로 차별화. 원거리는 예외 없이 "가까우면 후퇴 → 벌어지면 윈드업 → 발사". 사거리(`MaxAttackRange`) 안에서는 Chase가 전진을 멈추고 이동은 공격 패턴 런지만 담당 — 공격 사이엔 정지·회전만(2026-09-08, 근접 적 공전 수정). 벽 관통 방지는 물리(적 Rigidbody는 Discrete)가 아니라 `MoveTo` 클램프가 담당. Chase는 시야가 막히면(`HasClearLineToPlayer`) `RoomController.FindPath`(그리드 A*) 경로를 따르고 확보되면 직선 추격으로 복귀(2026-09-13) — 실플레이로 드러난 경로추종/클램프 버그 6건(트리비얼 same-cell 경로, Connector 클램프 축 스왑, 웨이포인트 도착판정 Y오차, `Repath` 진행취소 진동, 장애물 클램프-시야판정 불일치로 인한 벽관통 등) 전부 수정 완료(2026-09-13/14, 상세 `DevLog.md`) |
| 적 스폰 | `EnemySpawner` | `EnemyVariantType`(Melee/Ranged/Boss/ErraticMelee/BurstRanged — 뒤 2종 2026-09-08 추가) → `GameObjectPool` 매핑 보유, 방 진입 시 `RoomData` 구성대로 스폰(방 로컬 오프셋 → 월드 좌표 변환), 사망 시 드롭 굴림(`ItemDropManager`) 트리거. 격리 측정용 `BenchmarkSpawner`는 이 경로를 우회(§3 Stage 3) |
| 방/문 진행 | `StageManager`(`FloorDefinition[] floors` 기반 층/방 그래프 빌드+현재 방 추적, `AdvanceToNextFloor()`로 씬 리로드 없이 제자리에서 다음 층 재빌드— 2026-09-02 (3)), `RoomController`(방 하나의 생명주기: 지연 스폰, 클리어 추적, `modular_space` 타일 절차적 지오메트리 생성 — 2026-09-11, `RoomObstacleSet` 기반 장애물 배치(Solo 블록 + Connector 얇은 세그먼트 — 2026-09-13), 문 배선), `RoomFootprint`(바운딩박스+선택적 노치로 정사각형/가로직사각형/ㄱ자 표현 — 2026-09-11), `ObstaclePlacement`(`ObstacleShape{Solo,Connector}` — 2026-09-13)/`RoomObstacleSet`(`prefabs`+`connectorPrefab`)/`RoomObstacleBounds`(전투방 장애물 데이터·프리팹 세트·`ClampToInterior` 확장 클램프 — 2026-09-12, Connector는 세그먼트 실폭 기준 바운드로 정교화 — 2026-09-13), `DoorController`(문 잠금/해제 + 물리 차단 콜라이더 + 시각 마커, `OwnerRoom` 보유 — 2026-09-11), `RoomTransitionController`(문 통과 시 `TargetRoom` 매칭으로 도착 문 특정 + 카메라 연출 전환 + 층 시작 시 `PlacePlayerAt()`으로 플레이어를 시작방에 배치) | 스테이지 기반 진행을 아이작 스타일 공간형 방/문 시스템으로 전면 대체(2026-08-31), 다층 진행 지원 추가(2026-09-02 (3)), 전투방 한정 격자 셀 2~3개를 실제로 병합하는 진짜 멀티셀 방(한 변에 문 여러 개 가능) 도입(2026-09-11), 전투방 한정 시드 기반 장애물 배치 도입(2026-09-12), 이어진 장애물을 칸 중심을 잇는 얇은 세그먼트로 교체 + 패턴 복잡도 확장(2026-09-13), 적 우회는 그리드 A*(`RoomPathfindingGrid`)로 도입 완료(2026-09-13) — 이후 실플레이로 드러난 경로추종/클램프 버그 6건 수정(2026-09-13/14, 상세 `DevLog.md`) |
| 아이템 드롭/픽업 | `ItemDropManager`, `ItemPickup` | 적 사망 시 확률 굴림(중복 방지 — 드롭된 아이템은 풀에서 제거). 드롭 좌표는 `Instantiate` 전에 `NudgeClearOfDoors()`로 근처 문 트리거 밖으로 밀어냄(문 앞 드랍템을 문 안 밟고 줍게, 슬라이드 안 보이게). 바닥 픽업 오브젝트가 트리거로 습득 처리 + `PickupNotificationUI` 표시(`HideImmediate()`로 층 전환 시 즉시 정리) |
| 데미지 | `IDamageable`, `Health`(순수 C#), `HealthComponent` | 데미지 판정 + 수치·가해자 로그, 체력 상태, 사망 이벤트 |
| 방 클리어 후 흐름 / 런 종료 | `RoomFlowManager` | "업그레이드 선택" 흐름은 폐기됨(§3). `FloorCompleted`(Floor Exit 진입) 시 결과 화면 표시 + 진행 중 구르기 취소(`CancelDodgeRoll`) + 픽업 알림 즉시 숨김(`HideImmediate`) — 둘 다 `timeScale=0` 일시정지 전에. 비마지막 층은 `AutoAdvanceRequested` → 페이드 전환 지휘. 플레이어 사망(`Health.Died`)도 여기서 처리(2026-09-08): `HandlePlayerDied` → 입력 off + `PauseState.RequestPause` + 캡슐 틱오버 코루틴 + `UIManager.ShowGameOver`, 스탯은 승/패 공용 `BuildCurrentStats()` |
| 풀링 | `GameObjectPool`, `PooledObject` | Projectile/Enemy(근접·원거리)/Boss/Grenade 공용 재사용 풀. 같은 프레임 중복 반환(`_hasReturned`/`_isReleased` 가드, 2026-08-31)으로 `InvalidOperationException` 방지 |
| UI | `UIManager`(파사드), `HealthBarUI`, `ActiveItemCooldownUI`(쿨다운 아이콘 + 장착 아이템 이름 텍스트 — 2026-09-02 (3)), `ActiveItemEquipUI`, `PlayerStatsPanelUI`, `CharacterSelectUI`, `FloorCompleteUI`(구 ClearOverlayUI — 마지막 층이면 Retry 버튼, 아니면 3→0 카운트다운 후 자동 층 전환), `GameOverUI`(플레이어 사망 시 — `FloorCompleteUI`의 패배판, dim/content/buttons 3개 `CanvasGroup`을 같은 타임라인에서 동시 페이드, 버튼은 페이드 완료 후 클릭 가능 — 2026-09-08), `FloorTransitionUI`(층 전환 검은 페이드), `MinimapUI`(현재 방 중심 로컬 스크롤 뷰포트, `RectMask2D` — 2026-09-02 (3)에 전체-뷰 방식에서 개편), `PickupNotificationUI`(`HideImmediate()` 추가), `BillboardUI`, `DebugOverlayController`(F1 FPS·데미지 오버레이 + F2 적 즉사 토글 → `DebugDamage.EnemyOneShot`, FPS 텍스트 왼쪽 "1-SHOT" 인디케이터) | `UIManager`가 선택/오버레이 화면을 감싸 `ShowXxx(...)` 메서드로 노출. 상속 없는 단일 클래스. `BillboardUI`가 실제 카메라 추적 회전(`LateUpdate`)을 전담해 데미지 팝업/HP바/미니 알림이 항상 카메라를 향함(2026-08-31 — 이전엔 무회전 근사에 의존) |

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

### Stage 2.5 — 아이작 스타일 방/문 시스템 전환 (✅ 대부분 완료, 2026-08-31 / 실플레이 검증 2026-09-01)

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
- **층2 authoring 완료 + 층1→층2 자동 진행 구현(2026-09-02 (3))**: 층2를
  "ㅂ"자 모양(10개 방, 좌상단 시작/우상단·정중앙 등 authoring — 상세는
  `DevLog.md` 2026-09-02 (3))으로 구성. 층1 클리어 시 기존 Retry 대신
  3초 카운트다운 → 자동으로 씬 리로드 없이 층2로 전환(플레이어 진행 상황
  유지), 마지막 층(현재 층2) 클리어 시에만 기존 Retry(전체 재시작) 유지.
  이 과정에서 잠복 버그 3건(Floor Exit 콜라이더 스케일, 플레이어 시작
  위치 미설정, 미니맵 좌표계) 발견·수정, 미니맵을 전체-뷰에서 현재 방
  중심 로컬 스크롤 뷰포트로 개편.

⏳ 남은 작업:
- 아이템 방(#3) — 바닥 드롭 아이템 로직은 만들어졌으니, "전투 없이 보장된
  픽업 배치"만 추가하면 될 것으로 예상
- ~~방 지오메트리 지형 애셋 교체 + 형태 다양화/멀티셀 병합~~ — 완료(2026-09-11, Phase 1~3).
  `modular_space` 타일 기반 방 껍데기, 전투방 한정 가로직사각형/ㄱ자 형태, 진짜 멀티셀 병합
  (칸 2~3개, 한 변 다중 문). 상세는 위 헤더 changelog + `DevLog.md`/`AI_Decision_Log.md` 2026-09-11.
- ~~방 안 장애물(#2, `cyberpunk_game_kit` 소품 + `cube_world_kit` 바위/울타리 군집)~~ —
  완료(2026-09-12). 전투방 한정 시드 기반 배치(문/입구 2칸 클리어런스 + BFS 연결성 검사),
  단일 소품(바위/가구)뿐 아니라 랜덤워크로 자라는 연결형 울타리 군집(직선/코너/T자 자연 혼합)도
  지원. 플레이어/발사체는 기존 벽과 동일 메커니즘으로 자동 차단, 적은 `ClampToInterior` 확장으로
  통과만 막음(우회는 아직 없음). 상세는 위 헤더 changelog + `DevLog.md`/`AI_Decision_Log.md`
  2026-09-12.
- ~~적 경로탐색(그리드 A*)~~ — 완료(2026-09-13, `RoomPathfindingGrid`+`EnemyChaseState`).
  이후 실플레이로 드러난 경로추종/클램프 버그 6건도 수정 완료(2026-09-13/14). §6 #3d 참고.
- ~~**3층 제작**~~ — 완료(2026-09-04). 아이작식 13방(id 301~313, 중앙 시작 / 최원거리
  단문 보스 / CombatF 뒤 아이템 데드엔드 / 본체 루프 2개 / 선택 전투 데드엔드), 전투 적 54 +
  보스 1. 구조 검증(중복 0/상호대응·인접/BFS 13-13) 통과. 실플레이 최종 확인은 이월.
- ~~**1~3층 방 데이터 JSON 외부화**~~ — 완료(2026-09-04 (2)). `RoomDataAsset`(SO 29개) +
  씬 `StageManager.floors` 배선 → `Assets/StreamingAssets/Balance/Stage/Floors/Floor{1,2,3}.json`.
  신규 `GameFloors`(로더) / `JsonFloorLayoutProvider` / `FloorLayout`. 파일당 1개 층, `Floor1`부터
  열거해 층 수 결정, 마지막 층은 파일 위치로 판정. 손상 시 그 층만 비상용 3방 층 + 팝업.
  구조 검증(방 6/10/13, 연결·적 수 이관 전과 일치, 손상 폴백 동작) 통과. 실플레이 주파는 이월.
  상세는 `AI_Decision_Log.md` 2026-09-04 (2).
- 3층 완성 후 **UI Scene 신설 → 전체 플레이 루프**(타이틀 → 캐릭터 선택 →
  런(3층 주파) → 클리어/게임오버 → 재시작·타이틀 복귀) 구성. Game Over
  흐름도 이 작업에 통합(2026-09-02) — 단, 층간 전환 자체는 2026-09-02 (3)에
  UI Scene 신설 없이 앞당겨 구현됨(위 참고)
- (2026-09-01 실플레이 검증 완료) 방-방 카메라 전환 연출 완주, 시작방/40×40
  방 크기와 카메라 구도, 미니맵 안개·연결선, 빌보딩, ESC 메뉴 + Pip 바
  레이아웃, 패시브 픽업 배치, 기본 공격 사거리 3유닛/공격속도 체감 — 전부
  문제 없음 확인. 방 경계 재프레이밍/Cinemachine은 당장 불필요로 판단
  (`DevLog.md` 2026-09-01 참고)

### Stage 3 — 최적화 (부분 착수, 재개 시점 미정)
- ✅ Object Pooling — `Projectile`/`Enemy`/`Boss`/`Grenade` 4종 전부 적용
- ✅ 벤치마크 인프라 + §6.5 Before 표 (2026-09-08): 격리 씬 `BenchmarkScene` +
  `BenchmarkSpawner`(타입 선택 + additive Add 50/100 스폰, 고정 32×32 그리드에 순차 배치).
  측정 방법론은 "유저 수동 Play/버튼/Stop → Stop 후 `ProfilerDriver` 프레임 버퍼에서
  마지막 200프레임 리드백"(MCP 브리지가 프레임에 개입하면 신호가 묻힘 — `AI_Decision_Log.md`
  2026-09-08 (6)). 4종(Melee/Ranged/ErraticMelee/BurstRanged) × 50/100/200 median CPU Time
  기록, 최악이 BurstRanged 200마리 7.35ms(예산의 44%)로 정상 상태 병목 없음. 500/1000
  구간은 에디터 크래시로 스킵, 최적화+After 패스는 Phase 6(아트 통합 후)로 의도적 이연
  (`AI_Decision_Log.md` 2026-09-08 (5)).
- ⏳ 나머지: AI tick 분산(`EnemyAIManager`), `GetComponent` 캐싱 정리 — 실측 병목 없어 비긴급,
  Phase 6에서 실제 병목 위주로 접근
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
  - **재발 3회차 + 근본 대응(2026-09-02)**: 같은 함정이
    `ClearOverlayPanel`(8/24 최초 발견·수정)에서 Stage 2.5 / FloorExit 작업 중
    다시 `m_IsActive: 0`으로 되돌아가 재발("보스 클리어 → Floor Exit 진입 시
    조작 불가 + 결과 화면 안 뜸"). 이번엔 씬 활성 플래그만 되돌리는 대신
    `FloorCompleteUI`를 **`CanvasGroup` 방식**으로 전환: GameObject active를
    아예 토글하지 않고 `alpha`/`interactable`/`blocksRaycasts`로만 표시·숨김,
    컴포넌트의 GameObject는 씬 내내 활성 → `Awake()`가 로드 시 1회만 정상
    실행됨. 이 패턴이 필요한 UI는 앞으로 `SetActive` 토글 대신 CanvasGroup을
    우선 검토(`ActiveItemEquipUI`/`PickupNotificationUI`도 이관 후보).
    자세한 내용은 `DevLog.md` 2026-09-02 (2) 참고.
- **다층 진행은 씬 리로드가 아니라 제자리 재빌드(2026-09-02 (3))**: 층1→층2
  전환 시 씬을 다시 로드하지 않고 `StageManager.AdvanceToNextFloor()`가 현재
  층의 방 GameObject만 파괴하고 다음 층을 재빌드. 씬 리로드안(정적 상태로
  층 인덱스 전달)도 검토했으나, 리로드 시 Player가 재생성돼 패시브/액티브
  아이템 진행 상황이 초기화되고 캐릭터 선택도 재등장 억제용 로직이 추가로
  필요해짐 — 반면 제자리 재빌드는 Player를 그대로 두므로 둘 다 별도 처리
  없이 자연히 해결됨(오브젝트 풀은 적을 방이 아닌 풀 자신의 자식으로 관리해
  방 파괴에 안전함을 확인 후 채택). 마지막 층 클리어 시의 기존 Retry(완전
  재시작 의도)만 예외적으로 씬 리로드 유지.
- **미니맵을 전체-뷰에서 로컬 스크롤 뷰포트로 개편(2026-09-02 (3))**: 층 전체를
  고정 패널에 다 그리던 방식에서, 현재 방이 항상 패널 정중앙에 오도록 매 방
  입장마다 콘텐츠 컨테이너만 재배치하고 `RectMask2D`로 뷰포트 밖은 잘라내는
  방식으로 변경. 이 개편 과정에서 기존 좌표 계산이 3열 이상(여러 행)일 때
  화면 밖으로 밀려나는 잠복 버그를 발견해 같이 고침(층1은 1행뿐이라 한 번도
  발현 안 됐던 버그).
- **`Physics.autoSyncTransforms`가 off라 씬 재빌드/순간이동 지점마다 `SyncTransforms()`를
  명시 호출(2026-09-04)**: `AdvanceToNextFloor()`가 `timeScale=0` 암전 중 실행돼 자동 물리
  싱크가 안 걸리는 게 스테이지3 벽 관통·방 전환 드리프트의 공통 원인. 전역 ON 대신
  `StageManager.BuildFloor()` 끝과 `RoomTransitionController`의 `player.position` 순간이동
  직후에 `Physics.SyncTransforms()` 삽입. 같은 계열 버그 재발 시 전역 ON 재고 (대안 비교는
  `AI_Decision_Log.md` 2026-09-04).
- **대시를 이동속도 연동 + `linearVelocity` 구동으로(2026-09-04)**: 대시 = "현재 이동속도 ×
  `dashSpeedMultiplier`(기본 3) 속도로 `dashDuration`(0.2초) 버스트" — 기본 스탯에서 기존
  15 u/s·3유닛 보존, 이동속도 패시브가 대시에도 반영됨. 수치는 `PlayerBaseStats.json`으로
  외부화. 구동을 `MovePosition`(위치 명령, 솔버 추월 시 벽 관통) → `linearVelocity`(솔버가
  해소)로 전환.
- **걷기/대시 공용 벽 방향 클램프 `WallClamp`(2026-09-04)**: `MovePosition`/`linearVelocity`를
  물리 스텝당 ~0.2유닛 초과로 벽에 명령하면 솔버를 앞질러 관통(이동속도 패시브 스택 시 걷기도
  해당). 이동 전 캡슐을 반지름만큼 뒤로 물린 지점에서 `CapsuleCast`, 히트 시 `ProjectOnPlane`로
  벽 방향 성분 제거(정면=정지·스침=슬라이드). 정면 충돌은 모든 속도에서 막힘, 얕은 스침 +
  극단 이동속도 잔여는 §5 이월.
- **적 방 이탈 방지 = 물리가 아니라 방 내부 AABB 클램프(2026-09-09)**: 적 이동 6곳(chase/patrol/
  retreat/knockback/lunge/sway)이 전부 생짜 `Rigidbody.MovePosition`이라 방 유지가 벽 콜라이더
  충돌뿐이었는데, 적 Rigidbody는 Discrete이고 2026-09-08 넉백(`MovePosition` 절대 목표점 러프,
  첫 스텝 ≈0.46유닛)이 두께 1 벽을 관통 → 적이 방 밖으로 나가고 `CanSeePlayer`가 순수 거리라
  복귀도 안 해 방 클리어 불가(진행 정지). 플레이어 `WallClamp`(스윕 캐스트)와 달리 적은 **방이
  축 정렬 박스**라는 걸 이용해 `RoomController.ClampToInterior`로 위치를 내부로 투영하는 방식 채택
  — 물리 튜닝·충돌 모드·"모든 미래 이동 코드가 벽 체크를 기억"에 의존하지 않는 결정적 차단.
  단일 초크포인트 `EnemyStateMachine.MoveTo`로 수렴, `FixedUpdate` 안전망이 그래도 벗어난 적을
  스폰 지점으로 스냅. 대안 비교는 `AI_Decision_Log.md` 2026-09-09. 적이 방 사이를 이동하는 설계가
  생기면 문 통로 예외를 넣는 형태로 확장 필요.
- **발사체는 서로 통과가 기본(2026-09-09)**: `Projectile.OnTriggerEnter`는 상대가 다른
  `Projectile`이면 즉시 무시 — 적 탄과 아군 탄이 겹쳐도 상쇄·소멸 없음(목표 장르에서 탄 상쇄는
  기본이 아님). 나머지 충돌(`IDamageable`, 벽)은 불변. 의도적 상쇄/블랭크 메커닉이 필요해지면
  이 가드 한 곳 + `ProjectileTests` 회귀 테스트만 고치면 됨. 대안 비교는 `AI_Decision_Log.md`
  2026-09-09 (4).

---

## 5. 아직 결정하지 않은 항목

- **Game Over 흐름 / 런 종료 이후 흐름**: 사망 감지 + 연출 + 결과 화면 표시는 구현·유저
  Play Mode 확인 완료 (2026-09-08 — `RoomFlowManager.HandlePlayerDied` + `GameOverUI` + 캡슐
  틱오버, `AI_Decision_Log.md` 2026-09-08 (3)). 아직 미정: `GameOverUI` 버튼(재시작 / 타이틀
  복귀) 실제 배선, Clear 오버레이 이후 흐름 — 3층 완성 후 신설할 **UI Scene + 전체 플레이
  루프**에서 함께 설계 (2026-09-02, §3·§6 참고).
- **카메라 시스템에 Cinemachine 도입 여부**: 2026-09-01 실플레이에서 현재
  `QuarterViewCameraFollow`(단순 오프셋 추적)로 40×40 방도 구도가 적절함을
  확인 — 방 경계 재프레이밍/Cinemachine은 당장 불필요, 필요해지면 재논의.
- **아이템 방(#3) 구체 설계**: 바닥 드롭 인프라는 있으니 "전투 없이 보장된
  픽업 배치"만 추가하면 될 것으로 예상되나 미착수.
- ~~**방 안 장애물(#2)**~~: 완료(2026-09-12) — §6 #3b 참고.
- **아이콘 스와치 → 실제 픽토그램 교체**: 우선순위 낮음으로 이월 중.
- ~~**Enhanced Shot 스킬 사거리**~~: 해결됨(2026-09-01) — 기본 공격과 분리,
  `EnhancedShotActiveItemData.range`(기본 45유닛)로 스킬이 독립 소유.
  `Projectile.Initialize`에 `rangeOverride` 인자 추가.
- ~~**`FlamethrowerActiveItem.CooldownRemaining` 항상 0 갭**~~: 해결됨
  (2026-09-01) — `FlamethrowerActiveItemData.cooldown`(기본 3초) 추가,
  분사 종료 시점부터 쿨다운 시작 + 재분사 차단.
- **씬에 남은 비활성 `Boss_Basic` 플레이스홀더 인스턴스**: 정리 여부 미결정.
- **`UIManager`를 상속 구조로 확장할지**: 두 번째 구체적인 UI 매니저 사례가
  실제로 생기기 전까지는 보류(2026-08-24 결정 유지).
- ~~**1~3층 방 데이터 JSON 외부화 방식(2026-09-02 (3) 신규)**~~: 결정·구현 완료
  (2026-09-04 (2)) — 방 authoring 자체를 JSON 1차 소스로(층당 파일 1개), `RoomDataAsset` 폐기.
  마지막 층 판정은 파일 위치, 손상 시 비상용 층+팝업. 상세 `AI_Decision_Log.md` 2026-09-04 (2).
- **대시(구르기) 무적 판정 여부(2026-09-02 (3) 신규)**: 스페이스바 대시 이동
  중 실제로 무적 프레임이 있는지 아직 코드/실플레이 확인 전. (2026-09-04 대시 리워크로
  `EndDodgeRoll`에 무적 해제가 명시됐고 취소 경로도 정리됐으나, 인게임 무적 창 체감 확인은
  여전히 이월. 2026-09-08 리워크로 대시 중 `Enemy` 레이어 물리 관통 + 종료 시 오버랩 보정
  꼬리가 추가됐고 코드상 무적 해제는 `CompleteDodgeRoll`로 이동 — 적 관통/보정 동작은 유저
  Play Mode 확인 완료, 데미지 무적 창 체감만 아직 이월. `AI_Decision_Log.md` 2026-09-08 (4).)
- **플레이어 피격 반응 / 넉백(2026-09-08 신규)**: 현재 플레이어가 맞으면 HP만 깎이고
  아무 반응(넉백/경직/무적 프레임)이 없음 — 적 `KnockbackResistance` 기반 넉백은 적 전용.
  근접 런지가 플레이어를 물리로 밀던 문제는 런지 클램프(`AI_Decision_Log.md` 2026-09-08 (8))로
  가림. "제대로 된" 튜닝 가능한 피격 넉백/경직은 플레이어 넉백력 스탯과 함께 나중에.
- **플레이스홀더 지오메트리 — 아트 통합(Phase 4) 시 일괄 처리할 충돌 이슈(2026-09-04)**:
  지금은 큐브/캡슐 프리미티브 기준이라 아트로 지형·적·캐릭터를 넣으면 콜라이더·히트박스·
  스폰 오프셋을 어차피 재조정해야 함 — 그때 아래를 묶어서:
  1. `WallClamp` 잔여: 벽을 얕은 각도로 스치며 파고드는 이동을 매우 높은 이동속도(일반
     런의 픽업 수를 한참 넘는)에서 하면 여전히 조금 관통. 근본 해결 = 플레이어 이동을
     `MovePosition`에서 스윕 캐릭터 이동(`CharacterController`/full collide-and-slide)으로 교체.
  2. 문 통과 트리거가 보이는 문보다 깊음(`WallThickness×3`) — 드랍 nudge(`ItemDropManager.
     NudgeClearOfDoors`)로 우회 중, 근본은 지오메트리.
  3. 벽에 밀착한 두 오브젝트 사이 근접 발사 시 발사체가 벽 판정으로 소멸(데미지 안 들어감).

---

## 6. 앞으로 해야 할 작업 (우선순위 순, 2026-09-02 (3) 사용자 지정으로 갱신)

1. ~~**3층 맵 제작**~~ — 완료(2026-09-04, §3 참고). 실플레이 최종 검증만 이월.
2. ~~**1~3층 방 데이터 JSON 외부화**~~ — 완료(2026-09-04 (2), §3 참고). 실플레이 주파 검증만 이월.
3. **대시 무적 판정 확인**: 스페이스바 대시(구르기) 이동 중 실제로 무적
   프레임이 적용되는지 코드/실플레이로 확인.
4. ~~**대시 아이콘 UI + 쿨다운 표시**~~ — 완료(2026-09-04 (2)). 우하단 `DashCooldownSlot`,
   `SkillCooldownSlot`은 그 왼쪽으로 이동, `ActiveItemCooldownUI` 미러(`DashCooldownUI`).
5. **Stage 2.5 잔여 항목 재개**: 아이템 방(#3) 세부 밸런스("전투 없이 보장된
   픽업 배치"는 이미 있음), 방 안 장애물(#2, 지형 애셋 교체 시점까지 보류),
   ~~씬의 비활성 `Boss_Basic` 인스턴스 삭제~~(완료 2026-09-04), 아이콘 스와치 → 실제 픽토그램.
6. **전체 플레이 루프** — **골격 완료(2026-09-04 (3))**: `Title.unity` 신설, 타이틀→캐릭터
   선택(타이틀 씬으로 이동)→런→클리어 화면("다시하기"/"타이틀로") 루프 3방향 전환 검증.
   HUD "당장 넣어볼 UI"(크로스헤어/런 타이머/체력 바 리스킨/일시정지 메뉴 버튼/보스 HP 바)와
   캐릭터 선택 일러스트 버튼 + Start 홀드 즉시선택 버그 수정도 완료(2026-09-04 (3)~(6)).
   **Game Over 흐름**: 사망 감지/연출/`GameOverUI` 표시 + 버튼(재시작/타이틀) 동작까지
   구현·유저 확인 완료(2026-09-08, `GameOverUI` 페이드는 dim/content/buttons 단일 타임라인 동시).
   **씬 전환 페이드**: 구현 완료(2026-09-09, `SceneTransitionController` — §2 Core/ 참고).
   구조·1회 전환은 자동 검증 통과, **연속 전환 페이드 완주 + 전투 1주파 실플레이 검증만 이월**
   (자동화 Play Mode 코루틴 한계). 타이틀·일시정지 UI 레이아웃 미세조정은 **포트폴리오 마감
   작업 이후로 이연**(유저 지정 2026-09-08). 층간 전환(카운트다운+페이드+제자리 재빌드)은
   2026-09-02 (3)에 이미 구현됨. 타이틀·일시정지 UI 레이아웃 미세조정은 **포트폴리오 마감
   작업 이후로 이연**(유저 지정 2026-09-08). **#6 종료(2026-09-09 유저 1주파 완료 처리)** —
   신규 적 타입(ErraticMelee/BurstRanged)이 2·3층에 실제로 섞여 나오는지 육안 확인은 이후 다른
   작업 시 함께 보는 것으로 이월.
7. **Stage 3 잔여 항목** — 최적화(`EnemyAIManager` AI Tick 분산, `GetComponent` 캐싱) + §6.5
   After 측정은 **Phase 6(아트 통합 이후)로 의도적 이연**(2026-09-08). Before 표 + 벤치 인프라는
   완료(§3). 현 시점 실측 병목 없음이 근거(`AI_Decision_Log.md` 2026-09-08 (5)).
8. **Phase 4/5/7** (아트 교체, Addressables, 포트폴리오 문서화)는 위 단계들이
   안정화된 이후 별도로 진행.

---

### 포트폴리오 기술 트랙 — 확정 우선순위 (2026-09-09, 유저 검토 완료)

`DevLog.md` 2026-09-08 "다음 작업 후보" 리스트를 장르 선언(3D 아이작/건전)과 현재
진행도를 반영해 순서 확정. 상세 근거는 `DevLog.md` 2026-09-09.

0. ~~**§6 #6 마감**~~ — 완료(2026-09-09, 유저 1주파). 신규 적 타입 2·3층 배치 육안 확인만 이월.
1. ~~**자동화 테스트 스위트(EditMode+PlayMode) + CI**~~ — 완료(2026-09-09). `RogueLike.asmdef`(단일,
   A1) + `Assets/Tests/{EditMode,PlayMode}` + `.github/workflows/ci.yml`(GameCI, Unity 6000.3.22f1).
   **CI에서 49개(EditMode 39 + PlayMode 10) 전부 초록.** 방법 B 활성화(UNITY_EMAIL/PASSWORD
   시크릿만, Unity 6 엔타이틀먼트 라이선싱이라 `.ulf` 없음). `KnockbackMath`/`RoomBounds` 순수
   추출 + `GameFloors.ValidateGraph`(BFS 연결성 + dangling 검증) 신규. README + 배지.
2. ~~**결정론적 시드 절차적 층 생성**~~ — **완료.** 기본 던전이 시드 기반 절차 생성
   (`DeterministicRng`/`RngStreams`/`RunConfig`/`IFloorSource`/`ProceduralFloorGenerator`/
   `ProceduralRules.json`). 층 수 3 고정, 레이아웃·방 타입·문·적 구성이 절차적. Phase F(시드
   화면 표시 + 타이틀 시드 입력칸) 완료. 유저 1주파 실플레이 검증 완료.
3. ~~**적 경로탐색(그리드 A* 권장) + 방 안 장애물**~~ — **완료(2026-09-14).** 절차적 방이 생긴 뒤 가치 상승(장애물이
   생성기의 일부). `EnemyChaseState` 이동만 교체. 프로파일러 계측(원래 #7)을 여기 흡수. 규모 4~5 세션.
   - 3a. ~~**방 구성(형태 다양화 + 진짜 멀티셀 병합)**~~ — **완료(2026-09-11).** `modular_space`
     타일 기반 지오메트리, 전투방 한정 가로직사각형/ㄱ자 형태, 격자 셀 2~3개를 실제로 병합하는
     멀티셀 방(한 변 다중 문 지원). 상세는 헤더 changelog 2026-09-11 + `DevLog.md`/
     `AI_Decision_Log.md` 2026-09-11.
   - 3b. ~~**방 안 장애물(`cyberpunk_game_kit` 소품)**~~ — **완료(2026-09-12).** 전투방 한정
     시드 기반 배치(타일 그리드 4유닛 좌표, 문/입구 클리어런스 + BFS 연결성 검사),
     플레이어/발사체는 기존 벽 메커니즘 그대로 자동 차단, 적은 `ClampToInterior` 확장으로
     통과만 막음. 상세는 헤더 changelog 2026-09-12 + `DevLog.md`/`AI_Decision_Log.md` 2026-09-12.
   - 3c. **적 시야(Line of Sight) + 발사체 벽 차단** — **완료(2026-09-13).**
     `EnemyStateMachine.CanSeePlayer`에 거리 체크 통과 후 `Physics.Linecast` 차단 판정 추가
     (레이어 `~0` + `QueryTriggerInteraction.Ignore`, 히트 콜라이더에 `Rigidbody`가 없으면
     차단 - `WallClamp`가 이미 쓰는 관례 재사용, 신규 레이어 불필요). 발사체 벽 차단은
     `Projectile.OnTriggerEnter`가 owner 타입을 구분하지 않아 코드 변경 없이 이미 동작 확인.
     PlayMode 신규 6개(`EnemyLineOfSightTests` 5 + `ProjectileTests` 1) 포함 17개 전부 통과,
     EditMode 104개 회귀 없음. 상세는 `DevLog.md` 2026-09-13.
   - 3d. **적 경로탐색(A*)** — **완료(2026-09-13).** 신규 `RoomPathfindingGrid`(순수 클래스,
     `RoomBounds`/`RoomObstacleBounds`와 동일한 씬-비의존 패턴) - `ObstaclePlacement.LocalCell`
     좌표계 그대로 4방향 A*, Solo는 칸 전체 차단·Connector는 두 칸 사이 간선만 차단.
     `RoomController.FindPath`로 노출, `EnemyChaseState`는 시야가 막혔을 때만(`HasClearLineToPlayer`)
     경로를 따라가고 확보되면 기존 직선 추격 유지. 부수적으로 `CanSeePlayer`(사거리+차단
     통합)를 리쉬 체크에 그대로 쓰면 추격이 모퉁이에서 즉시 풀려버리는 상충을 발견해
     `IsPlayerWithinRange`(순수 거리)로 리쉬 체크를 분리. 상세는 `DevLog.md` 2026-09-13.
   - 3e. **실플레이 검증 중 경로추종/클램프 버그 6건 수정** — **완료(2026-09-14).**
     신규 적 `RelentlessMelee` 실플레이로 A*가 실제로는 안정적으로 동작하지 않는 지점을
     연속 발견: 트리비얼 same-cell 경로, 시야 확보 시에도 안 멈추던 문제, Connector 클램프
     축 스왑, 웨이포인트 도착판정 Y오차, `Repath`가 매번 진행을 스스로 취소하던 "떨림",
     Solo 장애물 클램프가 항상 4×4로 뭉뚱그려져 시야판정(실제 콜라이더)과 불일치해 대쉬 중
     벽을 관통하던 문제 — 전부 회귀 테스트로 잠금. EditMode 115/115, PlayMode 36/36.
     대안 비교가 있었던 마지막 건(벽 관통)은 `AI_Decision_Log.md` 2026-09-14, 전체 상세는
     `DevLog.md` 2026-09-13(1~5)·2026-09-14(6).
4. **입력 버퍼링 + 프레임레이트 독립성** — 건전식 타이트 조작. 규모 1 세션. 게임필 우선 시 3과 순서 교체 가능.
5. **커스텀 에디터 툴링 — SO 검증 패스** (+ 여유 시 층 그래프 비주얼라이저) — 노력 대비 신호 큼, 1 세션.
6. **세이브/로드 — 버전드 직렬화 + 런 이어하기** — 아무것도 언블록 안 함, 가장 나중 또는 마감 시 스킵.

**아트 리소스 적용(무료 에셋/AI 생성) 시점**: 2번(절차적 생성) 착수 직전 ~ 진행 중 병행 권장 —
가장 이른 항목은 아니지만 가장 늦지도 않음. 상세는 `DevLog.md` 2026-09-09.

**병렬/후속**: Addressables(Phase 5)는 아트 후 축소 범위로. Stage 3 최적화 After(Phase 6)는 아트에 게이팅.
