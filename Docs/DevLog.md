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

## 2026-09-17 (50)

### 진행 내용
- **§6.5 After 프로파일링 — 킬/재보충 루틴 구현부터 4종×50/100/200 실측까지 완료**
- `BenchmarkSpawner` 확장 (2026-09-15 (49)에서 논의만 하고 미룬 항목들 구현):
  - **킬/재보충 루틴**: 우하단 패널에 간격 입력(최소 3초 강제, 미만 입력 시 텍스트 자동
    보정)·퍼센트 입력·Play/Stop 토글 버튼 신설. Play 누르면 그 순간 선택된 variant의
    생존 수 중 지정 퍼센트를 실제 `HealthComponent.TakeDamage(99999)`로 즉시 죽이고
    (대기 없이 첫 사이클 즉시 실행 - 유저 요청으로 변경), 고정 3초 뒤 그만큼 재보충하는
    걸 반복. Stop 누르면 그 시점 대기 중인 재보충분을 즉시 채우고 종료.
  - **버그 발견/수정**: 최초 구현은 "킬 주기 타이머"와 "재보충 타이머"를 독립된
    카운트다운으로 관리했는데, 재보충 타이머만 매 킬마다 정확히 3.0으로 플랫 리셋되고
    킬 타이머는 오버슈트를 보존하는 방식이라 두 타이머가 미세하게 어긋나 재보충이
    다음 킬에 의해 계속 취소되는 현상을 실측으로 발견(개체 수가 20에서 서서히 3까지
    빠짐). 두 스케줄을 같은 절대시간 그리드에 고정하는 방식(`nextKillTime`/
    `respawnDueTime`을 스케줄된 시각 기준으로 계산)으로 재작성해 해결, 재측정으로
    10↔20 안정적 오실레이션 확인.
  - **Add Boss 버튼**: 보스 1마리를 전용으로 스폰하고, 그 인스턴스의
    `ChargeStrikePattern`에만(공유 `ChargeStrikePatternData` 애셋은 안 건드림)
    `SetForceAlwaysReady(true)` 플래그를 걸어 원래 15% 확률 판정을 우회 - 쿨다운
    (2.5s)이 끝날 때마다 무조건 발동하도록 해서, 원래는 "희귀한 서프라이즈"라 자연
    발생을 기다리면 언제 나올지 알 수 없던 보스 돌진을 프로파일러로 반복 관찰 가능하게 함.
    Play Mode에서 보스를 플레이어 근처로 옮겨 직접 추적, 쿨다운이 끝나는 순간
    `IsReady=True`와 동시에 `IsBusy=True`(대시 시작)로 전환되는 걸 로그로 캡처해 검증.
  - **적 스폰 위치를 그리드 → 중앙 집중 나선형으로 전환**: 기존 32×32 그리드 순차 채우기
    방식은 개체 수가 적으면(50~100) 그리드 앞쪽 칸(맵 한쪽 구석)에만 몰리는 문제가
    있었음(유저 제보: "적이 맵 하단에 쏠려 보인다"). 골든 앵글(~137.5°) 기반
    sunflower/Vogel spiral로 교체 - n번째 스폰 개체는 항상 "현재 생존 수"를 인덱스로
    반지름을 계산(`SpiralSpacing × sqrt(index+1)`)하므로, 죽은 개체가 있으면 그
    바깥쪽 자리를 재생성이 그대로 재사용해 몇 사이클을 반복해도 전체 반경이 점점
    바깥으로 밀려나지 않음. Play Mode에서 스폰 직후 좌표를 직접 찍어 검증(50마리
    최대 반경 11.55, 24% 킬 후 남은 38마리 최대 반경 9.87, 재보충 12마리도 스폰
    직후 프레임 기준 10.09~11.63으로 원래 범위 안에 들어옴 확인).
- **`CombatVfxLibrary`가 BenchmarkScene에 없었던 문제 발견/수정**: 유저가 "피격/발사
  이펙트가 안 보인다" 제보 → `CombatVfxLibrary`(정적 싱글톤, `HealthComponent.
  AnyDamageTaken` 전역 구독 방식)가 애초에 이 씬에 존재하지 않아 `Instance`가 계속
  null이었고, 모든 Spawn 호출이 `?.`(null 조건부)라 에러 없이 조용히 전부 무시되고
  있었음. `Prototype.unity`와 동일한 구성(`CombatVfxLibrary` + `VfxPool_HitImpact`/
  `MuzzleFlash`/`EnemyDeath`/`GrenadeExplosion`/`BossChargeDust` 5개 풀, 같은 프리팹·
  용량)을 그대로 복제해 추가. Play Mode에서 `Instance != null` 확인 + 적 1마리를
  실제로 죽여 히트/사망 VFX 경로가 예외 없이 실행되는 것까지 확인.
- **플레이어 무적 해제(옵션 B 확정)**: `BenchmarkSpawner.Start()`가 플레이어
  `HealthComponent`를 항상 무적으로 고정해뒀던 코드가 `AnyDamageTaken`(피격 VFX
  트리거 조건에 `!IsInvulnerable` 포함)을 원천 차단해 플레이어가 아무리 맞아도 피격
  이펙트가 안 뜨는 원인이었음(유저 제보로 발견). 무적 처리 코드 완전 제거 + Player의
  `HealthComponent.maxHealth`를 999999로 설정(BenchmarkScene 씬 인스턴스 한정,
  프리팹/Prototype 씬은 안 건드림) - 자세한 A/B 대안은 `AI_Decision_Log.md` 참고.
  Play Mode에서 근접 적 8마리로 플레이어를 둘러싸고 실측: HP 999999→999969로 실제
  감소, 죽지 않음, 에러 없음 확인.

### 테스트 결과
- [x] 킬/재보충 루틴 타이밍 버그 수정 후 장시간 오실레이션 안정성 (Melee 20마리 기준)
- [x] Add Boss 강제 트리거 (쿨다운마다 확정 발동, 로그로 캡처)
- [x] 나선형 스폰 배치 - 초기/킬 후/재보충 후 반경 전부 검증
- [x] CombatVfxLibrary 이식 - Instance 존재 확인 + 실제 킬로 VFX 경로 예외 없음 확인
- [x] 플레이어 무적 해제 - 실제 피격으로 HP 감소 확인
- [x] §6.5 After 프로파일링 4종×50/100/200 — 유저가 Play/버튼/Stop 수동 조작(그 구간엔
  명령 안 보냄), Stop 직후 Pause 상태에서 `ProfilerDriver` 프레임 버퍼 리드백
  (2026-09-08 (6)에서 확립한 방법론 그대로 재사용)
- [x] §6.5 보스 파트 — Add Boss 1마리, 쿨다운(2.5s)마다 강제 돌진, 같은 방법론으로 측정

**After #1 결과 (전부 "무적 해제 + VFX 전부 가동 + 킬루틴 25%/3초 5회" 조건)**:

| Enemy 수 | 항목 | Melee | Ranged | ErraticMelee | BurstRanged |
|---|---|---|---|---|---|
| 50 | Median CPU (Before) | 3.97ms (4.77ms) | 4.53ms (3.79ms) | 4.31ms (3.48ms) | 4.90ms (4.39ms) |
| 50 | 최대 CPU / GC | 21.56ms / 51.7KB | 39.77ms / 51.9KB | 21.32ms / 52.7KB | **122.52ms** ⚠️ / 54.3KB |
| 100 | Median CPU (Before) | 5.96ms (5.11ms) | 5.55ms (3.89ms) | 5.21ms (3.89ms) | 5.84ms (5.33ms) |
| 100 | 최대 CPU / GC | 24.59ms / 112.2KB | 27.74ms / 116.1KB | 22.80ms / 115.2KB | **107.69ms** ⚠️ / 117.4KB |
| 200 | Median CPU (Before) | 8.20ms (4.43ms) | 7.15ms (6.44ms) | 6.63ms (5.78ms) | 7.87ms (7.35ms) |
| 200 | 최대 CPU / GC | 37.89ms / 241.0KB | 41.35ms / 243.5KB | 36.58ms / 245.0KB | 42.68ms / 246.7KB |

**보스 파트 결과 (Add Boss 1마리, 강제 돌진) — 3회 반복 측정**:

| 회차 | Median CPU | 최대 CPU | 최대 GC | 최대 CPU 프레임의 정체 |
|---|---|---|---|---|
| 1차 | 3.71 ms | 38.24 ms | 54,278 B | `GarbageCollector.CollectIncremental` 68.0% + `GC.Collect` 22.9% |
| 2차 | 3.68 ms | 24.06 ms | 34,317 B | 버퍼 마지막 프레임(Pause 순간), `EditorLoop` 92.8% - 게임플레이 무관 |
| 3차 | 3.70 ms | 26.55 ms | 34,621 B | 버퍼 마지막 프레임(Pause 순간), `EditorLoop` 92.2% - 게임플레이 무관 |

### 이슈/메모
- **BurstRanged 50/100에서만 100ms대 CPU 스파이크(122.52ms, 107.69ms)**, 정작 200에서는
  다른 3종과 같은 정상 범위(42.68ms)로 돌아옴 - "개체 수가 늘수록 심해지는 구조적
  문제"라면 200에서 더 심해져야 하는데 반대라, **공용 투사체/이펙트 풀이 이 세션에서
  처음으로 특정 크기까지 자라나는 일회성 성장 비용**일 가능성이 높다고 추정(Melee→
  Ranged→ErraticMelee 200을 먼저 거치며 공용 풀이 커지다가, BurstRanged 특유의
  "한 번에 여러 발" 동시 수요가 50/100에서 풀을 추가로 늘려야 했고 200에선 이미
  다 자라 있어 안 늘어난 것으로 추정). 확정하려면 해당 스파이크 프레임의 top time
  sample을 추가로 봐야 함 - 미확인, 다음 세션 후보.
- 모든 구간에서 **median CPU는 예산(16.67ms)의 50% 이하로 여유롭고 median GC Alloc은
  0 bytes** - 상시 부담이 아니라 킬/재보충 순간에만 튀는 스파이크성 비용. 200마리
  구간에서 최대 GC가 4종 전부 240KB대로 수렴 - 킬 비율(25%)이 곧 50마리분 동시
  死+재생성이라 개체 수와 거의 선형 비례.
- Before(2026-09-08)와 직접 비교 시 median이 대체로 소폭 상승했는데, 이건 순수
  회귀라기보다 Before가 "그냥 서 있는 정적 population"이었던 것과 달리 After는
  "3초마다 25%가 죽고-VFX 터지고-재생성되는" 이벤트가 상시 섞여 있는 시나리오라
  시나리오 자체가 다름 - §6.5가 원래 노리던 "VFX 도입 후에도 예산 안에 있는가"
  확인이 목적이었고, 그 기준으로는 전부 통과.
- **보스 돌진 스파이크(1차 38.24ms) 원인 확정 — 돌진 로직이 아니라 GC 수집 사이클**.
  `GetFrameSelfTimeSample`로 해당 프레임을 까보니 `GarbageCollector.CollectIncremental`
  (68.0%)+`GC.Collect`(22.9%)가 프레임 시간의 90% 이상을 차지 - 대시 이동/히트박스/
  넉백/먼지 VFX 같은 실제 게임 로직 비용이 아니라 **Unity Incremental GC가 그 프레임에
  수집 사이클을 돌린 것과 우연히 겹친 것**. 2차·3차 반복 측정에서는 median이 3.68~
  3.70ms로 1차(3.71ms)와 거의 동일했고, 최대 CPU 프레임은 둘 다 게임플레이가 아니라
  **Pause를 누르는 순간의 에디터 오버헤드**(`EditorLoop` 92%대)였음 - 즉 진짜 "매 돌진마다
  드는 비용"은 3회 내내 일관되게 가벼웠고(median 3.7ms 안팎, 최대 GC 알록도 34~35KB로
  거의 고정값), 1차의 38ms는 재현 안 되는 일회성 사건이었음이 반복 측정으로 확인됨.
  같은 논리로 **BurstRanged 50/100의 100ms대 스파이크(아래)도 GC 수집 사이클 우연
  중첩일 가능성이 유력**하다고 재해석 - 다만 그쪽 스파이크 프레임은 버퍼가 밀려나
  재확인은 못 함.

- **Addressables 실제 작업 여부 판단 완료 — 진행 안 하기로 결정**(2026-09-15 (49)에서
  보류됐던 항목). 유저 질문("이미 있는 기술 요소들이 충분하고 Addressables 공부에
  많은 시간 안 들 정도로 가치가 없으면 안 해도 되지 않나")에 Portfolio value 25/100
  으로 평가해 동의 - 이유는 (1) 룸 타일 프리팹 때도 이미 같은 이유(한 씬에 모든 게
  동시 존재, 비동기 로딩 이득 없음)로 채택 안 한 전례가 있어 지금 넣어도 구조적으로
  어색함, (2) 억지로 넣으면 "왜 여기 썼냐"는 질문에 궁색한 답만 남음 - 반대로 "검토
  했지만 구조상 안 맞아서 안 썼다"는 판단 자체가 이미 두 번(룸 타일, 이번) 기록된
  토킹 포인트, (3) 이미 확보한 기술 스택(절차적 생성/A*/커스텀 툴링/풀링/FSM/테스트
  +CI/AI 제안 비판적 검토 로그)이 이미 두꺼움. 대신 `README.md`(커밋 1개뿐, 테스트+CI
  트랙 완료 시점 스냅샷 그대로라 이후 절차적 생성/A*/툴링/아트·애니/보스 AI/VFX/
  이번 프로파일링이 전혀 반영 안 됨)를 먼저 현행화하는 쪽이 레버리지가 크다고 판단.

### 다음 작업
- BurstRanged 50/100 스파이크가 보스와 같은 "GC 수집 사이클 우연 중첩"인지, 다음에
  재현 시 `GetFrameSelfTimeSample`로 즉시 확인(이번엔 확정 못 하고 추정으로 남김).
- **README.md 현행화** (다음 세션 최우선) — 절차적 생성/A*/커스텀 툴링/아트·애니메이션/
  보스 AI/VFX/이번 §6.5 After 프로파일링 결과 반영. 이번 라운드는 텍스트만, 스크린샷/
  GIF는 별도 라운드로 미룸.
- README 이후: `ProjectPlan.md` §11(Phase 7)이 요구하는 나머지 — Claude Code+Unity MCP
  워크플로우, AI 제안 거부/수정 사례, 배운 점을 담을 별도의 좀 더 서술적인 포트폴리오
  문서(README보다 넓은 범위) 작성 + 스크린샷/GIF 캡처.

---

## 2026-09-15 (49)

### 진행 내용
- 코드 작업 없이 진행 방향 논의만 진행한 세션 - 오늘(38~48) VFX 버그 수정 마무리 후
  유저가 "남은 작업이 뭐가 있을까" 질문 → `ProjectPlan.md`/`ClaudeSessionSummary.md`
  §5·§6 재확인해 우선순위 트랙 기준 남은 건 **6번(세이브/로드, 최하위 취급)** 뿐이고,
  Phase 4(아트 통합)가 오늘부로 사실상 마감돼 그동안 게이팅돼 있던 **Phase 6(최적화
  After 측정)**·**Phase 5(Addressables, 축소 범위)**·**Phase 7(포트폴리오 문서화)**이
  풀렸다고 정리해 제시.
- 이어서 "지금 시점에 문서화한다면 서류전형에서 포트폴리오로 괜찮은가" 질문에 냉정한
  평가 제시: 내용물(자동화 테스트+CI, 결정론적 절차적 생성, A* 경로탐색, 커스텀 에디터
  툴링, `AI_Decision_Log.md`의 AI 제안 비판적 검토 기록)은 이미 준수한 수준이나, 현재
  `README.md`가 절차적 생성/A*/커스텀 툴링/아트·애니메이션/보스 AI/VFX를 전혀 반영 못한
  초기 스냅샷 상태로 방치돼 있고 스크린샷/GIF가 전무해 서류전형 단계에서 그 내용물이
  실제로는 안 읽힐 가능성이 높다고 지적.
- "기술 요소를 더 넣어 점수를 올리는 건 어떤가" 질문에는 비추천 의견 제시 - `ProjectPlan.md`
  자체가 "완결된 작은 프로젝트 > 크고 미완성인 프로젝트" 원칙을 명시하고 있고, 오늘 VFX
  하나로 10회 넘게 버그 수정 라운드가 돈 것 자체가 새 시스템 추가의 실제 비용을 보여주는
  사례라 새 기능보다는 이미 계획에 있는 빈 칸(Addressables) 채우기 + 문서화가 더
  레버리지가 크다고 판단.
- 유저가 다음 작업을 **"After 프로파일링" 로 확정**, Addressables는 "어떤 작업인지
  확인 후 진행 여부 판단"으로 보류. 곧이어 "오늘 한 VFX 작업까지가 아직 Before
  단계였냐"는 질문에 답하며 **`BenchmarkSpawner`(§6.5 벤치마크 하네스)를 코드로
  재확인**한 결과 실제 결함(구멍) 발견:
  - `BenchmarkSpawner.Start()`가 `playerHealth.Health.SetInvulnerable(true)`를 걸어둠 →
    `Health.TakeDamage`/`HealthComponent.TakeDamage`(`AnyDamageTaken` 발화 조건에
    `!Health.IsInvulnerable` 포함, `HealthComponent.cs:58`)가 전부 무효화돼 **피격
    이펙트가 이 벤치마크에서 원천적으로 한 번도 안 나옴**을 코드 레벨로 확인.
  - 이 하네스는 몹을 그냥 흩뿌려 배치만 하고 플레이어가 실제로 뭔가를 죽이는 경로가
    없어 **적 사망 이펙트도 발동 안 함**.
  - `BenchmarkSpawner`가 애초에 보스를 grunt count 스케일링 대상에서 제외하고 있어
    (`풀 크기가 다름 2/5 vs 20/220` 주석 확인) **보스 돌진 먼지는 이 벤치 방식 자체와
    안 맞음**.
  - 총구 플래시는 `Ranged`/`BurstRanged` variant가 실제로 주기적으로 발사하므로 이미
    어느 정도 커버됨 - 유일하게 손 안 대도 되는 항목.
- 위 구멍을 메우기 위한 **After 프로파일링 방침(초안, 아직 미확정·미구현)**을 제안 -
  다음 대화에서 유저와 더 논의 예정:
  1. **피격 이펙트**: 두 대안 제시 - (A) 무적 유지한 채 벤치마크 스크립트가
     `CombatVfxLibrary.SpawnHitImpact()`를 정해진 주기로 직접 호출(VFX만 순수
     격리해서 재는 방식, 실제 전투 파이프라인과는 분리) vs (B) 무적 대신 체력을
     아주 크게 잡고 실제 적 공격이 진짜로 맞게 둠(`HealthComponent`→`AnyDamageTaken`
     →`CombatVfxLibrary`까지 실제 프로덕션 경로를 그대로 태움, 다만 적 수가 늘수록
     동시 피격 빈도도 자연스럽게 늘어나 오히려 더 현실적인 스케일링이 됨). 어느 쪽을
     쓸지는 다음 논의에서 결정.
  2. **적 사망 이펙트**: `BenchmarkSpawner`에 "일정 주기로 추적 중인 적 K마리를 실제
     `HealthComponent.TakeDamage`(치사량)로 죽이고 즉시 K마리를 재보충"하는 옵션
     모드를 추가하는 안 - 실제 `EnemyDeadState`→`SpawnEnemyDeath` 경로를 그대로 타되,
     측정 구간 내내 "N마리 유지" 조건을 깨지 않음(죽는 만큼 즉시 리스폰).
  3. **보스 돌진 먼지**: 20/50/100/200 표에 억지로 끼워 넣지 않고, 보스 1마리만 스폰해
     `ChargeStrikePattern`을 반복 트리거하며 프로파일러로 스파이크 유무만 별도로 확인하는
     **독립된 소규모 체크**로 분리(실제 게임에도 보스는 항상 1마리뿐이라 "200개" 테스트가
     의미가 없음).
  4. **수류탄 폭발**: 플레이어 전용 스킬이고 쿨다운 때문에 사실상 항상 최대 1개뿐이라
     적 수 스케일링과 무관 - 이번 적 수 스케일링 표에서는 **의도적으로 제외**하기로
     제안(누락이 아니라 결정으로 문서화).

### 테스트 결과
- 해당 없음(코드 변경 없음, 논의/코드 재확인만).

### 이슈/메모
- `BenchmarkSpawner.Start()`의 무적 처리와 보스 제외는 원래 "적 수 대 FPS/GC"만 순수
  격리해서 재려는 의도로 만들어진 기존 설계라, 이번에 드러난 "VFX 커버리지 구멍"은
  버그라기보다 애초에 VFX가 존재하기 전에 설계된 하네스가 최근 신설된 VFX 시스템을
  못 따라간 것에 가까움.

### 다음 작업
- 위 4개 항목(특히 피격 이펙트 A/B 선택)을 유저와 마저 논의해 방침 확정.
- 방침 확정 후 `BenchmarkSpawner` 확장 구현 → `BenchmarkScene`에서 20/50/100/200
  시나리오로 After 수치(FPS/CPU/GC Alloc) 실측, 기존 Before 표와 나란히 정리.
- After 수치까지 나오면 Addressables가 실제로 어떤 작업인지 조사해 진행 여부 판단.

---

## 2026-09-15 (48)

### 진행 내용
- 유저 버그 보고: `Enemy_Ranged`는 머리 위에 총 모델이 얹혀 있는 형태인데, 그 총
  모델 안에 적 체력바(HealthBarCanvas)가 파묻혀 안 보임. 조사 후 원인/해결 방향
  2가지(①위치만 올리기 ②깊이 테스트 무시하고 항상 위에 그리기)를 제시, 유저가 ②
  근본 해결 선택("적 캐릭터가 어떻게 생겼든 HP가 제대로 보여야 정상").
- 원인 재확인: `HealthBarCanvas`(WorldSpace `Canvas` + `BillboardUI`로 카메라만
  바라보게 회전)는 일반 3D 지오메트리처럼 깊이 버퍼 테스트를 받는 채로 그려짐 -
  `Enemy_Ranged`의 총 모델(`Cylinder`, 월드 Y 1.7~2.7 범위) 바운딩 박스 안에 체력바
  (월드 Y 2.4)가 들어가 있어 카메라 시야각에서 총에 가려짐. `BillboardUI`의 회전
  로직이나 Sorting Layer 둘 다 이 문제와 무관 - Sorting Layer는 반투명 오브젝트끼리의
  순서만 정하지 불투명 지오메트리에 가려지는 것 자체는 못 막음.
- 신규 셰이더 `Assets/Shaders/UI-AlwaysOnTop.shader`(`UI/AlwaysOnTop`) 작성 - Unity
  기본 `UI/Default` 셰이더(uGUI Image/Text가 기본으로 쓰는 것)를 그대로 가져와
  `ZTest Always` 한 줄만 추가(원본은 `ZTest`를 지정 안 해 기본값 `LEqual`로 일반 깊이
  테스트를 받음). `ZWrite Off`는 유지해서 체력바 자신이 뒤에 있는 다른 걸 가리지는
  않음. CGPROGRAM 기반이라 URP 프로젝트에서도 문제없이 컴파일/지원됨(uGUI 자체가
  이미 이 방식의 셰이더로 동작 중).
- `Assets/Materials/UI_AlwaysOnTop.mat` 머티리얼 생성(위 셰이더 참조) - `HealthBarCanvas`
  를 가진 6개 프리팹(`Boss_Basic`/`Enemy_Basic`/`Enemy_BurstRanged`/`Enemy_ErraticMelee`/
  `Enemy_Ranged`/`Enemy_RelentlessMelee`) 전부의 `Background`/`Fill` `Image` 컴포넌트
  머티리얼을 이걸로 교체(`PrefabUtility.LoadPrefabContents` 스크립팅, 프리팹마다 공유
  구조라 전부 동일하게 처리됨).

### 테스트 결과
- [x] 셰이더 임포트 후 `Shader.Find("UI/AlwaysOnTop")`로 컴파일/지원 여부 확인 -
      정상.
- [x] Play Mode에서 `Enemy_Ranged`를 플레이어 앞에 스폰하고 피격시켜 체력바를 활성화
      시킨 뒤 `Unity_Camera_Capture`로 확인 - 빨간 체력바가 총 모델 앞을 그대로
      가로질러 선명하게 보임(수정 전엔 총 안에 파묻혀 안 보이던 것과 대조).
- [x] 콘솔 에러 0건.

### 다음 작업
- 유저 실플레이 재확인: 모든 적 타입에서 체력바가 항상 잘 보이는지.

---

## 2026-09-15 (47)

### 진행 내용
- 유저 버그 보고: 보스방에서 보스 잡고 다음 층 오브젝트(Floor Exit)와 충돌 시 플레이어
  걷는 애니메이션이 멈추지 않고 계속 유지되는 현상.
- 원인: `RoomFlowManager.HandleFloorCompleted()`(Floor Exit 트리거 진입 시
  `StageManager.FloorCompleted` 이벤트로 호출됨)가 `uiManager.ShowFloorComplete(...)`
  를 호출하는데, 그 안의 `FloorCompleteUI.Show()`가 `PauseState.RequestPause`로
  `Time.timeScale = 0`을 걺. `PlayerMovement.FixedUpdate()`는 `isMoving`이 바뀔 때만
  애니메이션을 전환하는 엣지 트리거 방식인데, 타임스케일이 0이 되면 FixedUpdate 자체가
  더 이상 호출되지 않아 이동 중이던 상태(Run)에서 멈춰버림. 반면
  `PlayerAnimationController`는 `Awake()`에서 `animator.updateMode =
  AnimatorUpdateMode.UnscaledTime`을 걸어둬서 Animator 자체는 일시정지와 무관하게
  계속 평가됨 - 그 결과 Run 클립이 제자리에서 계속 루프 재생되어 "걷는 게 유지되는"
  것처럼 보였음. `HandlePlayerDied()`는 이미 같은 상황에서 `_playerAnimation?.
  PlayDeath()`로 명시적으로 애니메이션을 덮어써서 문제가 없었는데, `HandleFloorCompleted()`
  에는 그런 처리가 빠져 있었음.
- 수정: `HandleFloorCompleted()`에 `uiManager.ShowFloorComplete(...)` 호출 직전
  `_playerAnimation?.PlayIdle();` 한 줄 추가 - `HandlePlayerDied()`의 `PlayDeath()`
  호출과 동일한 패턴.

### 테스트 결과
- [x] Play Mode에서 플레이어의 `PlayerAnimationController.PlayRun()`으로 Run 상태를
      만든 뒤 `PlayIdle()`을 호출해 `Animator.GetCurrentAnimatorStateInfo(0)`으로
      확인 - Run → Idle로 정상 전환됨을 확인. `HandleFloorCompleted()`는 `private`이고
      `StageManager.FloorCompleted`/`FloorExitController.Entered`는 외부에서 직접
      `Invoke`할 수 없는 C# 이벤트라(리플렉션 사용 불가 환경) 실제 Floor Exit 트리거를
      물리적으로 통과시키는 end-to-end 재현은 이번엔 생략 - 같은 클래스의 이미 검증된
      `PlayDeath()` 패턴을 그대로 재사용한 한 줄 변경이라 코드 리뷰 수준 확신으로 충분
      하다고 판단.
- [x] 콘솔 에러 0건. (참고: "The referenced script (Unknown) on this Behaviour is
      missing!" 경고가 다수 찍혔으나, 현재 씬에 로드된 모든 GameObject를 직접 스캔한
      결과 실제로 컴포넌트가 null인 오브젝트는 0개 - 이번 수정과 무관한 기존 잔여
      노이즈로 판단(미사용 상태로 남아있는 예전 Particle Pack 프리팹 쪽일 가능성),
      이번 작업 범위 밖이라 더 파고들지 않음.)

### 다음 작업
- 유저 실플레이 재확인: 보스방 클리어 후 Floor Exit 진입 시 애니메이션이 제대로
  Idle로 멈추는지.
- (낮은 우선순위, 별건) 콘솔에 계속 보이는 "missing script" 경고 원인 - 실제 씬
  오브젝트엔 영향 없는 것으로 확인했으나 원한다면 별도로 추적 가능.

---

## 2026-09-15 (46)

### 진행 내용
- 유저 관찰: `Prototype.unity`를 (타이틀 경유 없이) 바로 실행하면 플레이어 시작
  위치에서 피격 이펙트가 보였다 사라짐 - (41)에서 만든 VFX 풀 워밍업이 실제로 그
  지점에서 보이는 게 맞는지 확인 후, 맞다면 워밍업 위치를 (999, 0, 999) 같은 곳으로
  옮겨달라는 요청.
- 원인 확인: `GameObjectPool.WarmUpRoutine()`은 `_pool.Get()`만 호출하고 위치를 따로
  지정하지 않아, 워밍업 인스턴스는 그냥 프리팹의 로컬 위치 그대로 **풀 GameObject
  자신의 월드 위치**에서 잠깐 재생됨. 5개 `VfxPool_*` GameObject 전부 씬에서 정확히
  `(0, 0, 0)`에 있었는데, 이게 플레이어 스폰 위치 `(0, 1, 0)`과 거의 같은 자리 - (41)의
  "페이드 구간 안에서 끝나니 안 보인다"는 가정은 **타이틀→게임플레이 페이드를 거칠
  때만** 성립하고, 에디터에서 `Prototype.unity`를 바로 Play하는 흔한 개발자 테스트
  경로에는 그 페이드 오버레이 자체가 아예 생성되지 않아 워밍업이 고스란히 노출되고
  있었음.
- 수정: 5개 `VfxPool_*` GameObject의 씬 위치를 전부 `(999, 0, 999)`로 이동. 코드
  변경은 없음 - 실제 스폰은 항상 `GameObjectPool.Get(position, rotation)`이 즉시
  `SetPositionAndRotation`으로 요청 위치에 재배치하므로 풀 자신의 위치는 실제 게임플레이
  스폰 결과에 영향 없음(워밍업만 풀 위치를 그대로 물려받음).

### 테스트 결과
- [x] Play Mode에서 `VfxPool_HitImpact`의 워밍업 인스턴스가 `(999, 0, 999)`에서
      비활성 상태로 대기 중인 것 확인, 이어서 `CombatVfxLibrary.SpawnHitImpact`로 실제
      스폰을 요청하니 같은 인스턴스가 요청한 좌표(`(0, 2, 0)`)로 정확히 재배치되는 것
      확인 - 풀 위치 이동이 실제 스폰 동작에 전혀 영향 없음을 수치로 검증.
- [x] 콘솔 에러/경고 0건.

### 이슈/메모
- 참고로 남겨둘 트레이드오프: 워밍업 인스턴스를 카메라가 절대 보지 않는 위치로
  옮기면 "그 자리에서 번쩍이는 게 보인다"는 이번 문제는 확실히 해결되지만, Unity는
  카메라 프러스텀 밖의 오브젝트는 실제로 그리지 않고 컬링하므로 - 원래 워밍업이
  노리던 "첫 사용 시 셰이더/머티리얼 비용을 미리 치른다"는 효과 자체는 약해질 수 있음
  (화면에 실제로 그려져야 GPU가 그 셰이더 variant를 컴파일하기 때문). 다만 (41)에서
  보고된 청록색 깜빡임 버그가 이 수정 이후 실제로 재발하는지는 아직 보고된 바 없음 -
  재발하면 `ShaderVariantCollection.WarmUp()`처럼 화면에 그리지 않고도 GPU 컴파일만
  강제하는 방식으로 다시 손볼 필요가 있음.

### 다음 작업
- 유저 재확인: `Prototype.unity` 직접 실행 시 스폰 지점에서 이펙트가 더 이상 안
  보이는지, 그리고 (원래 문제였던) 세션 최초 이펙트 사용 시 청록색 깜빡임이 재발하지
  않는지.

---

## 2026-09-15 (45)

### 진행 내용
- 유저 확인: 적 사망 이펙트는 이제 잘 보임(완료). 보스 돌진 이펙트 유지시간을
  **현재 상태에서 다시 1/2로** 축소 요청.
- `DustStorm.prefab`의 자식 3개(`DustStorm`/`RockDebris`/`SandParticles`) `duration`/
  `startLifetime`(min/max)에 0.5를 곱해 재축소(예: `RockDebris` duration 1.67→0.83,
  lifetime max 1.33→0.67) - (44)에서 이미 1/3로 줄여둔 값 기준으로 추가 반영, 최초
  원본 대비로는 약 1/6 수준.

### 테스트 결과
- [x] `BossPool`에서 실제 보스를 꺼내 `ChargeStrikePattern.Execute()` 호출 후 생성된
      더스트 인스턴스를 `ParticleSystem.Simulate(0.1s)` 반복 측정 - 전체 소멸까지 약
      1.0초(직전 (44) 측정값 1.7초 대비 약 0.6배 - "1/2" 요청과 근사).
- [x] 콘솔 에러/경고 0건.

### 다음 작업
- 유저 재확인: 보스 돌진 유지시간 체감.

---

## 2026-09-15 (44)

### 진행 내용
- 유저 재확인 결과 2건: ① 적 사망 이펙트가 (43) 수정 후에도 여전히 잘 안 보임 ②
  보스 돌진 이펙트 유지시간을 지금의 1/3로 줄여달라.
- **적 사망 이펙트 재조사**: 이전엔 빈 바닥에서만 스폰해 테스트했던 게 맹점 - 이번엔
  `EnemyPool_Melee`에서 실제 적을 꺼내 `HealthComponent.TakeDamage`로 진짜 죽여서
  `EnemyDeadState` 경로 그대로 재현 후 캡처해보니 원인이 보임: **적 사망 시 시체
  모델 자체가 밝은 금색/노란색으로 빛나는 로봇 형태**였는데, 스파크 색이 주황~빨강
  계열(RGBA 1, 0.313, 0.099)이라 시체 자체의 금색 발광과 색상이 겹쳐서 파티클이
  시체 안에 묻혀버리고 있었음(크기/렌더링 문제가 아니라 순수 색 대비 문제).
  - `vfx_Sparks_01.prefab`의 `startColor`를 차가운 흰색-하늘색 계열(0.75, 0.9, 1.0)로
    교체 - 따뜻한 금색 로봇 몸체와 확실히 대비되고, "로봇 사망 시 전기 스파크" 컨셉에도
    주황 잉걸불보다 더 잘 맞음. 여유분으로 `startSize`도 0.01~0.15 → 0.03~0.22로 약간
    키움.
- **보스 돌진 이펙트 유지시간 1/3 축소**: `DustStorm.prefab`(기존 Particle Pack,
  이번엔 교체 안 하고 그대로 값만 조정)의 자식 3개(`DustStorm`/`RockDebris`/
  `SandParticles`) 전부의 `duration`과 `startLifetime`(min/max)에 1/3을 곱해 일괄
  축소 - 세 서브시스템 간 상대적 타이밍 비율은 유지한 채 전체를 3배 빠르게 마무리되게
  함(예: `RockDebris` duration 5→1.67, lifetime max 4→1.33).

### 테스트 결과
- [x] 적 사망 이펙트: Play Mode에서 실제 적을 꺼내 `TakeDamage`로 죽이고
      `Unity_Camera_Capture`로 확인 - 금색 시체 주변에 하얀빛 스파크가 확실히 구분되어
      튀는 것을 확인(이전엔 거의 안 보이던 것과 대조). 플레이어 근처에도 옅은 스파크가
      같이 보였는데, 이는 `CombatVfxLibrary.Start()`의 풀 워밍업 코루틴이 이 세션의
      "창 포커스 없으면 프레임 정지" 한계로 아직 1프레임을 못 넘겨 반환 전 상태로 남아
      있던 것으로 판단(워밍업 인스턴스가 플레이어 스폰 위치 근처인 풀 부모 트랜스폼
      위치에서 재생 중이었던 것 - 실제 플레이에서는 정상적으로 다음 프레임에 반환됨).
      콘솔 에러 없음.
- [x] 보스 돌진 유지시간: `BossPool`에서 실제 보스를 꺼내 `ChargeStrikePattern.
      Execute()` 호출 후 생성된 더스트 인스턴스를 `ParticleSystem.Simulate(0.1s)`
      반복으로 측정 - 전체 파티클이 다 사라지는 데 약 1.7초(이전엔 같은 방식으로
      측정하면 대략 3배인 5초 안팎으로 추정) - 목표한 1/3 축소와 부합.

### 다음 작업
- 유저 재확인: 적 사망 이펙트가 이제 시체와 구분되어 잘 보이는지, 보스 돌진 유지시간
  체감이 괜찮은지.

---

## 2026-09-15 (43)

### 진행 내용
- 유저 실플레이 결과 보고 2건: ① 수류탄 폭발은 확인됨(정상) ② 적 사망 이펙트가 잘
  안 보여서 제대로 동작하는지 판단이 안 됨 ③ 보스 돌진 이펙트를 착지 시점이 아니라
  돌진 시작 시점에 나오도록 바꿔달라.
- **적 사망 이펙트(`SparksEffect`, Particle Pack "Misc Effects") 원인 조사**: 4개
  자식 `ParticleSystem`(`SparksEffect`/`SideSparksEffect`/`SparksEffect2`/
  `SparkDeathEffect`) 전부를 직접 조회해보니 **`ParticleSystemRenderer.renderMode =
  None`, `material = null`** - 파티클 자체는 렌더링되는 메시/머티리얼이 아예 없고
  전적으로 `Trails`(PerParticle) 모듈의 `Sparks` 트레일 머티리얼에만 의존하는 구조였음.
  게다가 4개 중 2개(`SparksEffect`/`SparksEffect2`)는 `startSpeed`가 아예 0 - 파티클이
  움직이질 않으니 트레일 자체가 길이 0으로 찍혀서 사실상 안 보임. `startSize`도 전
  시스템 공통 0.01(1cm급)로 극단적으로 작음. "Legacy Particles" 폴더의 동명 프리팹도
  대조해봤지만 완전히 동일한 구조(루트 스케일만 1.0 vs 1.5 차이) - 이 이펙트는 원래
  가까운 FPS 카메라용 "금속 그라인딩 불티" 데모였지, 이 프로젝트의 탑다운 카메라
  거리에서 단독으로 쓰기엔 애초에 안 맞는 리그로 판단.
  - **교체**: (40)에서 이미 검증된 Free Quick Effects Vol.1의 `vfx_Sparks_01`을
    가져다 재조정해서 사용 - 원래는 `rateOverTime=50`으로 `duration=5초` 동안 계속
    뿜어져 나오고 파티클 하나하나가 3~8초씩 사는 "횃불용 지속 불꽃 분수" 설계라 그대로
    쓰면 사망할 때마다 불티가 몇 초씩 바닥에 흩어져 있는 꼴이 됨 - `emission.
    rateOverTime`을 0으로 끄고 t=0 버스트 15~22개(기존 `SparkDeathEffect`의 버스트
    10~15를 참고 기준으로 잡음)로 교체, `main.duration`을 0.3초로, `startLifetime`을
    0.3~0.6초로 대폭 축소해서 순간적으로 터졌다 빠르게 사그라드는 형태로 재조정.
    `startSpeed`(5~12)/`gravityModifier`(6)/색상(주황)은 원본 그대로 유지 - 위로 튀었다
    중력으로 떨어지는 모양이 그 자체로 이미 죽음 이펙트로 적합. `PooledObject`/
    `PooledVfxEffect` 추가, `VfxPool_EnemyDeath`의 `prefab` 참조 교체.
- **보스 돌진 이펙트 타이밍 변경**: `ChargeStrikePattern.Execute()`(돌진 시작 - 방향
  계산 직후) 쪽으로 `SpawnBossChargeDust` 호출을 옮기고 `TickDash()`의 도착/타임아웃
  분기에서는 제거. 회전값은 `machine.transform.rotation`을 다시 읽지 않고 `Execute()`
  안에서 이미 계산해 둔 `facing`(`Quaternion.LookRotation(_dashDirection)`) 지역변수를
  그대로 재사용 - `Rigidbody.MoveRotation`은 다음 물리 스텝에야 반영되므로 호출 직후
  `transform.rotation`을 읽으면 돌진 시작 전의 낡은 회전값이 나올 수 있어서였음.

### 테스트 결과
- [x] 적 사망 이펙트: Play Mode에서 `CombatVfxLibrary.Instance.SpawnEnemyDeath` 직접
      호출 + `Unity_Camera_Capture`로 확인 - 주황/노랑 불티가 위로 튀는 형태로 명확하게
      보임(이전엔 사실상 안 보이던 것과 대조).
- [x] 보스 돌진 이펙트 타이밍: `BossPool`에서 실제 보스 인스턴스를 꺼내 `ChargeStrikePattern`
      을 직접 `Execute()`→`PhysicsTick()` 200회(총 4초 시뮬레이션, 돌진 도착/타임아웃까지
      확실히 지나가도록)로 구동, `VfxPool_BossChargeDust`의 활성 인스턴스 수를 각 단계마다
      측정 - `Execute()` 직후 0→1(스폰됨), 돌진이 끝난 뒤에도 그대로 1(추가 스폰 없음)로
      확인 - 시작 시점에만 1회 나가는 것을 수치로 검증.
- [x] 콘솔 에러/경고 0건(양쪽 다).

### 다음 작업
- 유저 실플레이 재확인: 적 사망 이펙트가 이제 잘 보이는지, 보스 돌진 이펙트가 원하는
  타이밍(돌진 시작)에 나오는지.

---

## 2026-09-15 (42)

### 진행 내용
- 유저 요청: Enhanced Shot 스킬 사용 시에도 총구 이펙트가 나오게 해달라.
- 원인: `EnhancedShotActiveItem.Fire()`가 `RangedWeapon.Fire()`/적 원거리 패턴들과 달리
  공용 `ProjectileSpawner.Spawn()`을 거치지 않고 `_projectilePool.Get()`을 직접 호출해
  발사체를 꺼내고 있었음 - 총구 이펙트 호출(`CombatVfxLibrary.Instance?.
  SpawnMuzzleFlash(...)`)이 사는 곳이 바로 `ProjectileSpawner.Spawn()` 안이라, 이걸
  건너뛴 Enhanced Shot만 총구 이펙트 없이 발사되고 있었음.
- 수정: `EnhancedShotActiveItem.Fire()`를 `ProjectileSpawner.Spawn(_projectilePool,
  _owner.position, _owner.forward, 1f, _damage, _owner, 0f, _data.Range)` 호출로 교체
  (기존 스폰 위치/사거리 계산과 동일하게 매개변수 맞춤 - 스폰 거리 1f, damage/range는
  기존 값 그대로 전달). 발사체 자체 로직은 `ProjectileSpawner` 안에서 그대로 처리됨.

### 테스트 결과
- [x] Play Mode에서 `EnhancedShotActiveItem`을 플레이어에 직접 붙여 `OnActivatePressed()`
      호출 - `VfxPool_MuzzleFlash`에 활성 인스턴스가 새로 생기고 `ParticleSystem.
      isPlaying == true`인 것 확인, `Unity_Camera_Capture`로 총구 플래시 + 발사체가
      동시에 보이는 것도 스크린샷으로 확인.
- [x] 콘솔 에러 0건.

### 다음 작업
- 없음(요청 사항 완료, 유저 실플레이 확인 대기).

---

## 2026-09-15 (41)

### 진행 내용
- 유저 신규 요청 2건: ① 모든 VFX 이펙트가 **세션 중 처음 생성될 때만** 청록색으로
  보이는 현상(유저 추측대로 프리로드 부재가 원인으로 판단) ② 화염방사기 이펙트 길이가
  실제 히트박스(디버그 모드 F1 표시)보다 훨씬 길어서 축소 요청.
- **VFX 첫 생성 시 청록색 깜빡임**: `GameObjectPool`이 `UnityEngine.Pool.ObjectPool<T>`
  위에 얇게 얹혀 있는데, 이 풀은 완전히 지연 생성(lazy) - `defaultCapacity`는 내부
  스택의 초기 용량 힌트일 뿐 미리 `Instantiate`하지 않음. 즉 세션에서 각 VFX 타입별
  "진짜 첫 `Get()`"이 실전 전투 중 처음 일어나는 순간이 곧 그 프리팹의 첫
  `Instantiate` + 머티리얼/셰이더 첫 렌더 시점과 겹침 - 이 세션 환경에서는 반복 테스트로
  셰이더 캐시가 이미 데워져 있어 직접 재현/캡처하진 못했지만, 유저의 "프리로드 안 해서"
  가설과 정확히 들어맞는 증상(세션당 최초 1회, 이후엔 정상)이라 판단.
  - `GameObjectPool`에 `WarmUp(int count)` 추가: `count`개를 `_pool.Get()`으로 미리
    만들어 **실제로 1프레임 활성 상태로 유지**(카메라가 최소 한 번은 그려야 첫 사용
    비용이 여기서 소모됨 - `Get()`+`Release()`를 같은 프레임에 바로 이어 붙이면
    비활성화가 먼저 일어나 카메라가 그릴 기회조차 없음) 후 `Release()`.
  - `CombatVfxLibrary`에 `Start()` 추가, 5개 풀 전부 `WarmUp(1)` 호출. `Awake()`가 아닌
    `Start()`인 이유: 각 풀 자신의 `Awake()`(내부 `ObjectPool` 생성)가 먼저 끝나 있어야
    하는데 Unity는 서로 다른 GameObject 간 `Awake()` 순서를 보장하지 않음([[Unity
    Awake() order race]] 패턴 재확인 - 프로젝트에 이미 기록된 교훈과 동일).
  - 타이밍 근거: `SceneTransitionController.FadeAndLoadRoutine`이 씬 로드 후 완전한
    검은 화면 상태로 2프레임을 대기한 다음에야 페이드인을 시작함 - `CombatVfxLibrary.
    Start()`의 워밍업 1프레임은 이 구간 안에서 끝나므로 실제로는 화면이 까만 상태에서
    소모되어 유저 눈에 보이지 않음(실제 플레이는 항상 타이틀→게임플레이 페이드를 거침).
  - 라이브 확인: Play Mode 진입 후 5개 `VfxPool_*`의 자식 수/활성 상태를 조회 -
    전부 1개씩 생성돼 있고 전부 비활성(워밍업 코루틴이 실제로 실행되어 완료까지 간
    것 확인), 콘솔 에러 없음.
- **화염방사기 길이 축소**: `Player.prefab`의 `FlamethrowerFX`(`ParticleSystem`) 모듈을
  직접 조회해 원인 특정 - `startSpeed.max=8`, `startLifetime.max=1.25`,
  `velocityOverLifetime`/`speedModifier` 전부 사실상 무효(커브 전부 0 또는 배율 1)라
  월드 스페이스에서 **직선 등속**으로 날아가는 구조 → 실제 도달 거리 =
  `startSpeed.max × startLifetime.max` = 8 × 1.25 = **10유닛**. 반면 실제 데미지 판정
  박스 길이(`GameBalance.FlamethrowerSkill.zoneLength`, `FlamethrowerActiveItem.
  ApplyDamageTick`/`UpdateDebugVisual`이 공유하는 값) = **4.5유닛** - 시각적 사거리가
  판정 범위의 두 배 이상이었음.
  - `startSpeed`(속도감/밀도에 영향)가 아니라 `startLifetime.max`만 축소해 사거리를
    줄이는 쪽 선택(움직이는 속도 자체는 그대로 유지, 그냥 더 일찍 사라지게 함) -
    `startLifetime.max = ZoneLength / startSpeed.max = 4.5/8 = 0.5625`로 변경(`min`은
    기존 0 그대로 유지 - 노즐 근처의 짧게 흩어지는 불티 느낌 보존).
  - 라이브 검증: Play Mode에서 실제 `FlamethrowerActiveItem`을 플레이어 트랜스폼에
    직접 붙여 발동 + `ZoneLength`(4.5) 위치에 참조용 시안색 라인을 그려 `Unity_
    Camera_Capture`로 비교 - 화염 다발의 끝단이 참조선 끝과 거의 정확히 일치하는 것을
    스크린샷으로 확인.

### 테스트 결과
- [x] VFX 워밍업: Play Mode에서 5개 `VfxPool_*` 전부 자식 1개 생성 + 비활성 상태로
      정상 완료 확인, 콘솔 에러 0건.
- [x] 화염방사기 사거리: 스크린샷으로 화염 다발 끝단이 `ZoneLength`(4.5) 참조선과
      거의 일치함을 확인(완전히 똑같진 않음 - 파티클마다 속도가 min~max로 퍼져 있어
      자연스러운 그라데이션 형태로 줄어듦, 유저 요청대로 "완전히 같지 않아도" 되는
      수준으로 충분히 좁혀짐).
- [ ] 실제 세션 최초 생성 시 청록색이 정말 사라졌는지는 이 세션(반복 테스트로 이미
      셰이더가 데워진 상태)에서는 재현/재확인이 원천적으로 불가능 - 유저가 에디터를
      완전히 새로 켜거나 빌드를 새로 실행한 직후의 "진짜 첫 실행"에서 재확인 필요.

### 이슈/메모
- `WarmUp()`은 현재 VFX 5개 풀에만 호출했음 - 같은 지연 인스턴스화 구조를 쓰는
  `Projectile`/`Grenade`/적 풀들도 이론상 동일한 첫 사용 하치가 있을 수 있지만, 이번
  유저 신고는 VFX 한정이라 그쪽만 손댐(범위 확대는 실제 증상이 보고되면 판단).

### 다음 작업
- 유저 재확인: 에디터/빌드를 새로 시작한 직후의 진짜 "첫 이펙트"에서 청록색이
  사라졌는지, 화염방사기 길이가 체감상 괜찮은지.

---

## 2026-09-15 (40)

### 진행 내용
- (39)에서 "고쳤다"고 판단한 두 버그가 유저 실플레이에서 여전히 재현: 총구 번쩍 여전히
  2번, 피격 이펙트는 구는 사라졌지만 **너무 오래 남음**. 유저가 아예 Asset Store에서
  **Gabriel Aguiar Productions - Free Quick Effects Vol.1**을 새로 받아와 총구/피격
  이펙트를 이걸로 교체해달라고 요청.
- **임포트**: 다운로드는 돼 있었지만(`%AppData%/Unity/Asset Store-5.x/Gabriel Aguiar
  Prod/.../Free Quick Effects Vol 1.unitypackage`) 프로젝트에 실제 반영 전이라
  `AssetDatabase.ImportPackage`로 스크립트 임포트(이전 Particle Pack 때와 달리 이번엔
  에디터 팝업 없이 한 번에 됨). 이 최상위 패키지는 실은 BIRP/HDRP/URP 3개 렌더파이프라인
  버전을 담은 컨테이너였음(`Assets/GabrielAguiarProductions/`에 `.unitypackage` 3개 +
  문서 PDF로 떨어짐) - 프로젝트가 URP라 `FreeQuickEffectsVol1_2022_URP_v1.0.unitypackage`
  를 한 번 더 `ImportPackage`로 임포트해야 실제 프리팹/머티리얼이 들어옴.
- **진짜 원인 재조사** (기존 Particle Pack 버그의 근본 원인을 여기서야 제대로 파악):
  - 총구 번쩍 2번: `MuzzleFlash.prefab` 루트 자체 `ParticleSystem`이 `loop = true`였음.
    `PooledVfxEffect`가 계산한 수명(`duration+startLifetime` ≈ 0.65s)이 시스템 자체의
    루프 주기(`duration` 0.5s)보다 길어서, 우리가 반환하기 전에 시스템이 한 번 더
    루프를 돌며 버스트를 재발사 - (39)에서 지운 `FlashHeadon` 자식은 원인이 아니었고
    (제거는 무해했지만) 진짜 원인은 루트 시스템 자신의 `loop` 설정이었음.
  - 피격 이펙트가 오래 남음: `FleshImpacts.prefab`의 파티클 자식들 `duration`이 꽤 길게
    잡혀 있어서(수 초 단위), 기존 `PooledVfxEffect`가 `duration+startLifetime`으로 계산한
    수명이 실제 버스트가 화면에서 다 사라지는 시점보다 한참 김 - 최초 시도로 타이머
    계산을 `ParticleSystem.IsAlive()` 기반으로 바꿔봤으나 **`IsAlive()`도 `loop=false`인
    시스템에서 `duration`(재생 윈도우) 안에서는 파티클이 하나도 안 남아 있어도 계속
    `true`를 반환**한다는 걸 라이브 테스트로 확인(버스트만 있는 `duration=5` 시스템이
    파티클 다 사라진 지 한참 지난 시뮬레이션 2.0초 시점에도 `IsAlive()==true`) - `duration`
    필드는 "번쩍이는 데 걸리는 시간"이 아니라 그냥 재생/루프 윈도우 길이라는 걸 실측으로
    확인, `IsAlive()`도 이 필드에 종속돼 있어 못 씀.
- **`PooledVfxEffect.cs` 재설계**: 완료 판정을 `duration+startLifetime` 타이머 →
  `IsAlive()` → 최종적으로 **각 자식 `ParticleSystem.particleCount`가 전부 0인지**로
  교체(`duration`/`loop` 어느 쪽에도 영향받지 않고 실제로 화면에 남은 파티클 수만 봄).
  `OnEnable()` 직후 1프레임은 완료 체크를 건너뛰는 `_justEnabled` 가드 추가(막 `Play()`
  호출된 프레임엔 아직 버스트가 시뮬레이션되기 전이라 `particleCount`가 0으로 보여서
  즉시 되돌아가버리는 것 방지). `Awake()`에서 모든 자식의 `main.loop`를 강제로 `false`로
  끄는 안전장치는 유지(원본 프리팹이 `loop=true`로 만들어놨어도 무조건 1회성으로 동작).
- **교체 프리팹 선정**: 새 팩 안의 `vfx_MuzzleFlash_01`/`_02`(총구), `vfx_Impact_01`/
  `vfx_Sparks_01`(피격) 후보를 계층·버스트 구성으로 비교 - `_02`가 `_01`보다 자식들
  `duration`이 짧고(5s→1s) 더 컴팩트, `vfx_Impact_01`은 4개 서브시스템(`Particles`/
  `BrightHold`/`Darkback`)으로 구성된 "번쩍임+스파크+어두운 잔광" 복합 구조라 순수
  스파크 한 종류뿐인 `vfx_Sparks_01`(수명 최대 8초로 피격엔 과함)보다 타격감에 적합
  판단 → `vfx_MuzzleFlash_02`/`vfx_Impact_01` 선정. 둘 다 `PrefabUtility.
  LoadPrefabContents`로 `PooledObject`+`PooledVfxEffect` 추가.
- 씬 반영: `VfxPool_MuzzleFlash`/`VfxPool_HitImpact`(둘 다 `GameObjectPool`)의 `prefab`
  참조를 `SerializedObject`로 새 프리팹으로 교체, 씬 저장. 기존 `MuzzleFlash.prefab`/
  `FleshImpacts.prefab`은 프로젝트에 남겨두되 더 이상 참조되지 않음(삭제는 안 함).

### 테스트 결과
- [x] Play Mode에서 `CombatVfxLibrary.Instance.SpawnHitImpact`/`SpawnMuzzleFlash` 직접
      호출 + `Unity_Camera_Capture`로 스크린샷 확인: 피격은 노란 스파크 방사 버스트,
      총구는 짧은 주황 플래시로 정상 렌더링(둘 다 이전처럼 구/중복 없음).
- [x] **완료 타이밍 실측**: 스폰 직후 `ParticleSystem.Simulate(0.1s)`를 반복 호출하며
      모든 자식의 `particleCount`가 0이 되는 시점을 측정 - 피격 이펙트 약 0.5초, 총구
      플래시 약 0.1초에서 전부 소진 확인(구 버전이었다면 `duration=5s` 기준으로 최소
      5초 넘게 붙잡고 있었을 것 - "너무 오래 남는다" 버그가 실제로 해소됐음을 수치로
      확인).
- [x] 콘솔 에러/경고 0건.
- [ ] 총구 번쩍이 실제 게임에서 "한 번만" 보이는지는 이 세션 환경(창 포커스 없으면
      프레임 정지) 한계로 여전히 눈으로 직접 못 봄 - 다만 이번엔 원인(루트 시스템
      `loop=true` + 짧은 계산 수명)을 정확히 특정하고 새 프리팹은 애초에 `loop=false`
      구조라 같은 클래스의 버그가 재발할 이론적 경로가 없음.

### 이슈/메모
- (39)에서 "해결"이라고 기록했던 두 항목은 **둘 다 증상만 없어 보였을 뿐 근본 원인이
  아니었음** - 이번처럼 프리팹을 통째로 교체하는 김에 `PooledVfxEffect`의 완료 판정
  로직 자체를 구조적으로 다시 설계해서, 이후 다른 에셋팩의 프리팹을 또 붙이더라도
  (`duration`/`loop`를 얼마로 authoring했든) 흔들리지 않게 만든 것이 이번 세션의 실질
  성과.
- `Assets/GabrielAguiarProductions/`에 BIRP/HDRP/URP 3종 `.unitypackage`가 그대로 남아
  있음(용량 크지 않으면 무해, 필요하면 나중에 URP만 남기고 정리 가능).

### 다음 작업
- 유저 실플레이 재확인: 총구 번쩍 단일 재생 여부, 피격 이펙트 타이밍/느낌.
- 나머지 3개 VFX(수류탄 폭발/적 사망/보스 돌진 착지)에 쓰는 기존 Particle Pack
  프리팹들도 같은 `duration`/`loop` authoring 문제가 있는지 - 지금 당장 증상 보고는
  없지만 이번에 밝혀진 `PooledVfxEffect` 재설계 덕분에 이미 구조적으로는 안전함(굳이
  프리팹을 다시 열어볼 필요는 없음, 교체는 증상이 실제로 나올 때 판단).

---

## 2026-09-15 (39)

### 진행 내용
- (38) 전투 VFX 풀링 시스템을 유저가 실플레이로 확인, 버그 2건 보고: ① 총구 번쩍
  이펙트가 2번 나타남, ② 피격 이펙트가 타격 느낌이 아니라 "공 안에서 액체가 새는
  형태"로 보임.
- **총구 번쩍 중복 재생**: `MuzzleFlash.prefab`을 열어보니 루트 자체 `ParticleSystem`
  외에 자식 `FlashHeadon`에도 별도 `ParticleSystem`이 있었음. `PooledVfxEffect.
  OnEnable()`은 `GetComponentsInChildren<ParticleSystem>(true)`로 찾은 전부를
  무조건 `Play()`하므로 둘이 동시에 재생 - 원본 데모에서 "카메라 정면 각도용" 보조
  이펙트로 넣어둔 것으로 추정. `PrefabUtility.LoadPrefabContents`로 `FlashHeadon`
  자식 제거.
- **피격 이펙트 구형 렌더링**: `FleshImpacts.prefab` 루트를 조사하니 파티클 자식들
  (`HitEffect/Streaks`, `HitEffect/Mist`, `HitEffect/Decal`)과 별개로 **루트
  GameObject 자체에 `MeshFilter`(유니티 내장 Sphere 프리미티브, fileID 10207) +
  `MeshRenderer`(머티리얼 `FleshSurface` - 광택 있는 살구색) + `SphereCollider`**가
  붙어 있었음. 처음엔 `HitEffect/Decal` 자식이 원인인 줄 알고 제거했으나(경로가
  2단 중첩이라 `Find("Decal")`이 처음엔 실패 - 전체 계층 덤프로 정확한 경로
  `HitEffect/Decal` 확인 후 제거) 재검증 결과 구는 그대로 남아있어 원인이 아니었음.
  루트의 `MeshRenderer`/`MeshFilter`를 제거하자(둘 다 `PrefabUtility.
  LoadPrefabContents` 스크립팅으로) 구가 사라짐 - 원본 데모에서 "타격 표면"을
  나타내려던 평면 데칼용 구였던 것으로 보이나, 기본 스케일 그대로 스폰되니 허공에
  뜬 광택 구로 보였던 것. `SphereCollider`는 렌더링과 무관해 그대로 둠.

### 테스트 결과
- [x] Play Mode에서 `CombatVfxLibrary.Instance.SpawnHitImpact`/`SpawnMuzzleFlash`를
      플레이어 앞에 직접 호출 + `Unity_Camera_Capture`로 스크린샷 비교.
      수정 전: 광택 있는 살구색 구 렌더링(유저 신고와 일치). 수정 후: 구 사라지고
      순수 파티클 스파크만 남음(초록/노랑 퍼프 형태로 확인, 스크린샷 대조 완료).
- [x] 콘솔 에러 0건(Camera_Capture 관련 무관한 RenderTexture 경고 1건만 있음).
- [ ] 총구 번쩍 중복 제거는 프리팹 자식 삭제 자체는 확인했으나, 실제 두 번 깜빡이던
      것이 한 번으로 줄었는지는 이 환경(창 포커스 없으면 프레임 정지)의 한계로 순간
      재생 타이밍까지 직접 눈으로 못 봄 - 구조적으로는 중복 발생 원인(자식
      `ParticleSystem` 2개 동시 재생)이 제거됐으므로 해결로 판단하나 유저 실플레이
      재확인 필요.

### 이슈/메모
- Particle Pack 프리팹들은 원래 Built-in RP 데모 씬 전용으로 제작된 것이라, 이번
  구형 렌더러처럼 "데모 맥락에서만 의미 있는" 부속 컴포넌트가 섞여 있을 수 있음 -
  다른 3개 프리팹(`TinyExplosion`/`DustStorm`/`SparksEffect`)도 루트에 불필요한
  Mesh/Collider 컴포넌트가 없는지 한 번 훑어볼 가치 있음(이번엔 유저가 신고한 2건만
  대응).

### 다음 작업
- 유저 재확인: 총구 번쩍 단일 재생 여부, 피격 이펙트 시각적 적합성.
- 나머지 3개 VFX(수류탄 폭발/적 사망/보스 돌진 착지)도 같은 방식(루트 컴포넌트 점검)
  으로 한 번 훑어보는 것 고려.

---

## 2026-09-15 (38)

### 진행 내용
- (37) 화염방사기 이후 나머지 전투 VFX 4종(타격/원거리 발사/수류탄 폭발/보스 돌진
  착지)을 마저 진행. 유저 요청으로 **오브젝트 풀링 기반**으로 설계 변경(Instantiate/
  Destroy 대신 - 계획 공유 후 승인).
- 프리팹 조사 중 발견: `Dissolve`(적 사망 후보였음)는 파티클이 아니라 자체 메시+셰이더로
  "용해"되는 구조(대상 메시를 갈아 끼워야 함)라 이 패턴에 안 맞음 → **`SparksEffect`로
  교체**(순수 파티클, 로봇형 적 테마와도 더 맞음). 수류탄 폭발도 실제 폭발 반경(~1유닛)
  기준 `SmallExplosion`보다 `TinyExplosion`이 스케일 적합 판단, 교체.
- Particle Pack 프리팹 5개(`FleshImpacts`/`MuzzleFlash`/`TinyExplosion`/`DustStorm`/
  `SparksEffect`) 전부 **루트 자체엔 ParticleSystem이 없고 자식 여러 개에 나눠 붙은
  합성 구조**(FlameThrower처럼 루트 하나짜리가 예외적으로 단순했던 것) - 자식들의
  Play On Awake 설정에 기대지 않고 명시적으로 제어하는 쪽으로 설계.
- 신규 `Assets/Scripts/Combat/PooledVfxEffect.cs`: `Projectile.cs`와 동일한 "자기
  수명 관리" 패턴(`RequireComponent(PooledObject)`) - `Awake()`에서 자식 파티클시스템
  전부의 `duration+startLifetime` 중 최댓값 1회 계산해 캐싱, `OnEnable()`(풀에서
  꺼내질 때마다)마다 자식 전부 명시적 `Play()`, `Update()`에서 경과 시간이 캐싱된
  최대값 넘으면 `PooledObject.ReturnToPool()`(Destroy 아님), `OnDisable()`에서
  `Stop(StopEmittingAndClear)`로 다음 재사용을 위한 클린 상태 확보.
- 신규 `Assets/Scripts/Combat/CombatVfxLibrary.cs`: `PauseState`/`DebugOverlayController.
  IsDebugVisible`과 같은 정적 서비스 싱글턴 - 프리팹 대신 `GameObjectPool` 5개를
  Inspector로 들고 `SpawnHitImpact`/`SpawnMuzzleFlash`/`SpawnGrenadeExplosion`/
  `SpawnEnemyDeath`/`SpawnBossChargeDust(pos, rot)` 제공(내부는 전부 `pool.Get()`
  한 줄). 타격 이펙트는 `HealthComponent.AnyDamageTaken`을 직접 구독(기존
  `EnemyStateMachine`/`PlayerHitReaction`과 같은 패턴, self 필터링 없이 - 적/플레이어
  피격 전부 대상) - 별도 리스너 클래스 없이 이 파일 안에서 처리.
- 호출부 4곳에 한 줄씩 추가: `ProjectileSpawner.Spawn()`(원거리 발사 - 플레이어/적
  전부 자동 커버), `GrenadeProjectile.Explode()`, `EnemyDeadState.Enter()`,
  `ChargeStrikePattern.TickDash()`의 돌진 종료 시점.
- 씬(`Prototype.unity`) 작업: `PrefabUtility.LoadPrefabContents`로 5개 프리팹 각각에
  `PooledVfxEffect` 컴포넌트 추가. 씬에 `GameObjectPool` 5개 신설(`VfxPool_HitImpact`/
  `VfxPool_MuzzleFlash`(각 capacity 6/max 20 - 빈도 높음) / `VfxPool_GrenadeExplosion`/
  `VfxPool_BossChargeDust`(각 capacity 3/max 10 - 빈도 낮음) / `VfxPool_EnemyDeath`
  (capacity 6/max 20)) + `CombatVfxLibrary` 1개 신설, 5개 풀 전부 연결.

### 테스트 결과
- [x] `Unity_ValidateScript`: 신규/수정 파일 6개 전부 클린
- [x] 콘솔 에러 0건
- [x] **Play Mode 라이브 검증**: `CombatVfxLibrary.Instance` 정상 확인, `Spawn*` 5개
      메서드 전부 예외 없이 실행되고 각 풀에서 실제로 파티클시스템이 재생 중인 인스턴스가
      나온 것까지 확인(`GetComponentsInChildren<ParticleSystem>().isPlaying` 검사).
      **`HealthComponent.AnyDamageTaken` 자동 연결도 별도로 검증**: 플레이어에
      `TakeDamage` 직접 호출 시 `CombatVfxLibrary`가 자동으로 반응해 `VfxPool_HitImpact`
      에서 새 인스턴스를 꺼내 재생하는 것 확인(직접 `Spawn` 호출 없이 이벤트만으로 동작).
      풀 재사용 확인: 이전 인스턴스가 아직 안 끝난 상태에서 추가 요청이 오면 풀이 새
      인스턴스를 내주는 것도 확인(정상 동작 - `defaultCapacity` 여유분 안에서).
- [ ] 자식 파티클시스템의 실제 재생 시간이 지나 `PooledVfxEffect`가 스스로
      `ReturnToPool()`하는 것까지는 라이브로 확인 못 함(이 세션의 "창 포커스 없으면
      프레임 정지" 한계로 여러 초 경과를 못 지켜봄) - 로직은 `Projectile.cs`와 동일한
      이미 검증된 패턴이라 정상 동작 예상.
- [ ] 실제 전투에서 5개 이펙트가 각자 위치/타이밍에 맞게 자연스럽게 보이는지는 유저
      실플레이 확인 필요.

### 다음 작업
- 유저 실플레이로 5개 이펙트(타격/발사/수류탄/사망/보스 돌진) 전부 확인 - 위치·스케일·
  타이밍 조정 필요 여부.

---

## 2026-09-15 (37)

### 진행 내용
- 유저가 Unity 공식 무료 **Particle Pack**(Asset Store)을 프로젝트에 임포트(`Assets/
  UnityTechnologies/ParticlePack/`) - 화염방사기 스킬에 실제 화염 VFX를 붙여달라는
  요청. 임포트가 한 번에 안 잡혀서(Package Manager "My Assets"에서 다운로드만 되고
  Import 체크리스트 팝업에서 확정을 안 누른 상태) 두 차례 `AssetDatabase.FindAssets`로
  재확인 후에야 실제로 `Assets/` 아래 들어온 걸 확인. 팩 안에서 이름부터 정확히
  일치하는 `FlameThrower.prefab`(Fire & Explosion Effects, `ParticleSystem` 1개짜리
  단순 구조) 확인.
- 계획 공유 후 승인받은 대로 구현: `Player.prefab`에 자식 `FlamethrowerFX`(local
  (0, 1.0, 0.5), `FlameThrower.prefab`의 nested prefab 인스턴스 - 원본 프리팹이
  나중에 업데이트되면 자동 반영됨) 추가, `Play On Awake`는 꺼서 플레이어 스폰 시
  자동 발동 안 되게 함(`PrefabUtility.LoadPrefabContents`로 프리팹 자체를 스크립트
  편집). `FlamethrowerActiveItem`(순수 C# 클래스)이 생성자에서 `owner.Find(
  "FlamethrowerFX")`로 찾아 `ParticleSystem` 캐싱(디버그 시각화와 마찬가지로 이
  클래스가 직접 소유, 새 필드를 `ActiveItemContext`/데이터 애셋에 안 뚫음) -
  `StartActivation()`에서 `Play()`, `EndActivation()`에서 `Stop(true,
  ParticleSystemStopBehavior.StopEmitting)`(유저 지정 - 즉시 안 끊고 이미 나간
  불씨는 자연스럽게 사그라들게). 기존 F1 디버그 판정 박스 시각화는 그대로 유지,
  이번에 추가한 건 항상 보이는 실제 이펙트.

### 테스트 결과
- [x] `Unity_ValidateScript`: `FlamethrowerActiveItem.cs` 클린
- [x] 콘솔 에러 0건
- [x] Play Mode 라이브 검증: 씬의 실제 Player 인스턴스에서 `transform.Find(
      "FlamethrowerFX")`로 정확히 찾아짐, `playOnAwake=False`(스폰 시 자동 재생
      안 됨) 확인, `Play()` 호출 시 `isPlaying`/`isEmitting` 모두 true로 전환,
      `Stop(StopEmitting)` 호출도 에러 없이 정상 동작 확인. 다만 이 세션은 에디터
      창 포커스가 없으면 프레임이 안 흐르는 한계가 있어 Play→Stop을 같은 프레임에
      연달아 호출한 탓에 실제로 불씨가 몇 초에 걸쳐 사그라드는 모습 자체(시각적
      페이드)는 라이브로 확인 못 함 - 로직상 `StopEmitting`(vs `StopEmittingAndClear`)
      을 쓴 건 맞으니 정상 작동할 것으로 예상하나, 유저 실플레이 확인 필요.

### 이슈/메모
- Particle Pack 임포트로 `Packages/manifest.json`에 의존 패키지가 다수 추가됨
  (shadergraph, render-pipelines.core, postprocessing, progrids, 2d sprite/tilemap,
  burst, collections, mathematics 등) - 팩 자체가 요구하는 것들이라 정상이지만,
  프로젝트 패키지 목록이 눈에 띄게 늘어난 점 기록해둠.

### 다음 작업
- 유저 실플레이로 화염 이펙트가 자연스럽게 켜지고 꺼지는지, 위치/스케일이 판정
  범위(`ZoneLength`/`ZoneWidth`)와 시각적으로 잘 맞는지 확인 - 필요시 로컬 위치나
  스케일 조정.

---

## 2026-09-15 (36)

### 진행 내용
- (35)의 미이월 과제 실행: `BenchmarkScene`으로 4종(Melee/Ranged/ErraticMelee/BurstRanged)
  × 50/100/200 전부 Before(2026-09-08) 대비 After 실측. 방법론은 2026-09-08에 확립한 것
  그대로 재사용 - **유저가 직접 Play→타입 선택→Add 버튼→Pause(또는 Stop)까지 전부 조작,
  그 구간 동안 Claude는 MCP 명령을 전혀 안 보냄**(브리지가 프레임에 개입하면 수치가
  뭉개졌던 2026-09-08 전례), 유저가 멈춘 뒤 `UnityEditorInternal.ProfilerDriver.
  firstFrameIndex/lastFrameIndex`로 프레임 히스토리 버퍼를 찾아 마지막 200프레임을
  `Unity_Profiler_GetFrameRangeTopTimeSummary`/`GetFrameRangeGcAllocationsSummary`로 읽음.
  매 구간 읽기 전 `FindObjectsByType<EnemyStateMachine>().Length`로 실제 스폰 마릿수를
  먼저 확인하는 절차를 이번에 추가(아래 이슈 참고 - 한 번 잘못 읽을 뻔함).
- **Melee 100/200에서 측정 중 실제 버그 발견**: `RoomAttackSlots.GetSlotPosition`이 매
  물리 틱마다 `List.IndexOf(enemy)`로 링 내 순번을 찾고 있었음(O(N)) - 벤치마크가
  한 타입만 몰아서 스폰하는 구조라 200마리가 전부 같은 링 하나에 들어가면서 매 틱
  200×O(200) ≈ 4만 회 연산이 발생, 50마리는 개선(4.77→3.05ms)됐는데 100(5.11→6.10ms)
  ·200(4.43→8.03ms)은 오히려 Before보다 느려지는 걸로 드러남. **즉시 수정**: 각 적의
  링 내 순번을 `Dictionary<EnemyStateMachine, int>`에 캐싱해 조회를 O(1)로 변경,
  비용이 드는 `Release()`(뒤 순번 재배치)도 프레임마다가 아니라 적 이탈 시 1회로 한정.
  수정 후 재측정: 100 4.65ms(Before보다 개선), 200 7.52ms(버그 버전보다는 개선됐으나
  Before보다는 여전히 느림 - 근접 링 반경 1.1(둘레 ~6.9유닛)엔 원래 7마리 정도만
  안 겹치게 들어가는데 같은 타입 200마리를 몰아넣는 벤치마크 자체가 포메이션 시스템엔
  구조적으로 불리한 인위적 극단치라는 결론, 유저도 이대로 받아들이고 진행하기로 함).
- **인게임 확인 중 별도 이슈 발견 → 조사 → 수정**: 유저가 플레이어를 적 무리 한가운데로
  옮겨 관찰하던 중 Ranged 타입 적이 근접 거리까지 파고드는 현상 제보. 보스 전용
  `ChargeStrikePattern`이 섞여 들어간 게 아닌지 질문받아 `EnemyRangedData.asset`의
  `attackPatterns`를 직접 확인(순수 `RangedAttackPatternData` 하나뿐, ChargeStrike
  없음)해 아니라고 답변. 가볍게 원인만 보기로 합의 후 확인한 결과: Enemy 레이어와
  플레이어가 있는 Default 레이어 간 물리 충돌이 켜져 있어서, `RoomAttackSlots`가
  목표로 잡은 반경 5.5 지점은 맞는데 주변에 몰린 다른 적들에게 물리적으로 떠밀려
  실제 위치가 그보다 가까워진 것으로 진단(슬롯은 AI 목표 좌표만 정할 뿐 물리적
  겹침 자체를 막진 못한다는, 설계 당시 이미 인지했던 한계가 실제로 드러난 사례) -
  깊은 조사는 이월.
- **오발(friendly fire) 버그 발견 → 즉시 수정**: 위 조사와 별개로, 유저가 "다른 적이
  쏜 발사체에 적이 맞지 않게" 요청 - `Projectile.OnTriggerEnter`가 발사 주체(`_owner`)
  자신만 제외하고 그 외 모든 `IDamageable`에 데미지를 넣고 있어서 적끼리도 서로 맞고
  있었음(발사체끼리는 이미 서로 통과하도록 돼 있었는데 적-적은 빠져 있었던 것).
  `_ownerIsEnemy`(Enemy 레이어 여부, `Initialize()`에서 1회 캐싱) + 피격 대상도
  Enemy 레이어면 발사체끼리 통과와 동일하게 그냥 통과하도록 추가. 플레이어가 쏘는
  발사체는 무관(`_ownerIsEnemy`가 애초에 false). 근접 공격/보스 돌진 히트박스는
  원래부터 `PlayerDamageable`/`"Player"` 태그만 특정해서 때리는 구조라 이 문제
  자체가 없었음(발사체만 해당).

### 테스트 결과
- [x] `Unity_ValidateScript`: `RoomAttackSlots.cs`(O(1) 수정) + `Projectile.cs`(오발 수정)
      전부 클린
- [x] 콘솔 에러 0건
- [x] **After 실측표(BenchmarkScene, 단일 종별 개별 측정, Unity Editor Play Mode)** -
      아래 참고. Median GC Alloc은 Before와 동일하게 전 구간 0 bytes.

**After (2026-09-15, O(1) 수정 + 오발 수정 반영 후)**

| Enemy 수 | Melee | Ranged | ErraticMelee | BurstRanged |
|---|---|---|---|---|
| 50 | 3.05 ms (1.0%) | 4.60 ms (1.5%) | 4.20 ms (0.5%) | 4.18 ms (0.5%) |
| 100 | 4.65 ms (1.0%) | 5.84 ms (0.5%) | 5.10 ms (1.0%) | 5.17 ms (0.5%) |
| 200 | 7.52 ms (1.0%) | 7.46 ms (1.0%) | 6.74 ms (1.0%) | 6.99 ms (0.5%) |

**Before → After 요약**: 50마리(비교적 현실적인 단일-타입 밀집도)는 4종 전부 Before
대비 같거나 나음. 100/200은 BurstRanged(가장 무거웠던 타입)는 전 구간 개선, 나머지
3종은 대체로 소폭 악화 - 원인은 위에서 특정한 대로 "한 타입만 몰아서 100~200마리"가
슬롯 링의 물리적 수용 한계(근접 ~7마리, 원거리 ~34마리)를 훨씬 초과하는 인위적 조건이기
때문. 반대로 **예산(16.67ms) 초과 프레임 비율은 사실상 전 구간·전 타입에서 개선**
(Before 최대 7.5%였던 게 After는 전부 1.5% 이하) - median만 보면 손해로 보일 수 있는
구간도 "프레임이 튀는 빈도" 자체는 확실히 줄어든 것으로 보임.

### 이슈/메모
- 벤치마크 중 유저가 "Pause했다"고 했는데 실제로는 직전에 Clear까지 누른 뒤였던 순간이
  있었음 - 프레임 버퍼는 여전히 유효해서 에러 없이 읽혔지만 실제로는 0마리 상태를
  측정한 것이었고, 이후 `FindObjectsByType<EnemyStateMachine>().Length`로 실제
  마릿수를 먼저 확인하고서 버퍼를 읽는 절차를 추가해 재발 방지.
- Pause(Stop 아님) 상태에서도 프레임 히스토리 버퍼 읽기가 정상 동작함을 확인(2026-09-08엔
  Stop 기준으로만 검증됐었음) - 다음에도 Pause만으로 충분.

### 다음 작업
- 100/200 단일-타입 극단치에서의 악화를 실제로 손볼지(예: 슬롯 링이 넘치면 2번째
  동심원으로 분산하는 등) 여부는 유저 판단 대기 - 현재는 "실제 게임 규모(룸당
  10~30마리, 혼합 타입)에선 문제 안 됨"으로 정리하고 보류.
- 물리적으로 밀려서 근접 거리까지 파고드는 현상의 깊은 조사(필요 시 슬롯 반경 이탈 시
  보정하는 로직 추가 등)는 유저가 나중에 요청 시 진행.
- Test Runner 전체 스위트 수동 확인 - 계속 이월 중.

---

## 2026-09-15 (35)

### 진행 내용
- 유저 리포트: 여러 적이 플레이어를 타겟으로 몰릴 때 물리 충돌로 서로 비비적대며
  "우글우글"거림. 다른 게임처럼 근접은 가까이 원형으로, 원거리는 멀리 원형으로
  겹치지 않게 서는 포메이션 AI가 있는지 질문받고 설명 후 승인받아 착수.
- **바로잡은 전제**: 유저가 언급한 "500마리 버벅임"은 사실 그 세션에 **에디터
  크래시**(OOM+GPU TDR)였고, 팀이 이미 500/1000 구간은 게임 실제 범위(룸당 10~30마리)
  밖이라고 측정·최적화 모두 스킵하기로 결정한 상태였음(`DevLog.md`/`AI_Decision_Log.md`
  2026-09-08). 이번 작업의 목표를 "500마리 버티기"가 아니라 "실제 게임 규모에서의
  포메이션 개선 + 그 규모에서의 충돌 비용 절감"으로 재설정하고 진행.
- 조사 확인: `EnemyAttackState.HoldOrSway`에 슬롯/각도 개념 전혀 없음(근접은 제자리
  정지, 원거리는 사인파 흔들림뿐 - 둘 다 플레이어 정확 좌표 기준). 물리 충돌 매트릭스상
  Enemy-Enemy 충돌 켜져 있음(`DynamicsManager.asset`). 중앙에서 여러 적을 아는
  관리자 없음(각 `EnemyStateMachine` 완전 독립). `RoomController`가 이미
  `EnemySpawner`/`RoomManager`/경로탐색(`FindPath`)을 들고 있어 슬롯 예약도 같은
  자리가 자연스러움.
- 신규 `Assets/Scripts/Room/RoomAttackSlots.cs`: `RoomPathfindingGrid`와 동일한
  "순수/씬 비의존, `RoomController`가 소유" 패턴. `EnemyStateMachine.MaxAttackRange`
  기준으로 근접/원거리 두 링으로 분리(임계값 2.5), 각 링은 **고정 슬롯 개수가 아니라
  현재 멤버 수로 매번 균등 각도 분배**(오버플로우 예외 처리 불필요) - `GetSlotPosition`을
  매 프레임 새로 계산해서 멤버 변화 시 자연스럽게 리플로우(스냅 없음). 근접 링 반경 1.1
  ·원거리 링 반경 5.5(각 역할의 공격 사거리 안쪽에 오도록 - `EnemyChaseState`의 기존
  "접근 중단" 판정이 플레이어와의 실거리 기준이라, 링 반경이 사거리 밖이면 영원히
  공격 전환이 안 되는 문제를 미리 피함).
- `RoomController`에 `_attackSlots` 필드 + `ReserveAttackSlot`/`ReleaseAttackSlot`/
  `GetAttackSlotPosition` 얇은 래퍼 추가(`FindPath` 래퍼와 동일한 스타일).
- **적용 범위를 의도적으로 좁힘** - `EnemyChaseState`의 목표 좌표(플레이어 정확 좌표 →
  예약된 슬롯 좌표)만 교체. 근접 돌진, 원거리 카이팅 재배치, 보스 돌진 같은 개별 공격
  패턴 도중의 순간 이동은 전혀 안 건드림 - 이미 검증된 전투 모션 리스크 회피.
- 슬롯 생명주기: `EnemyChaseState.Enter()`에서 예약(재진입 시 멱등), `EnemyIdleState`/
  `EnemyPatrolState`/`EnemyDeadState`의 `Enter()`에서 해제(이탈/사망 시점). 기절은
  `EnemyStunnedState`가 항상 Idle로 복귀하는 기존 구조 덕에 별도 처리 없이 자연스럽게
  해제→재감지 시 재예약됨. `EnemyStateMachine.OnDisable()`에도 안전망으로 해제 추가
  (죽음 외의 경로로 비활성화되는 경우 대비, 풀 재사용 시 이전 방에 예약이 남는 걸 방지).
- 신규 `Assets/Tests/PlayMode/RoomAttackSlotsTests.cs` 5개: 2인 근접 180도 분산, 근접/
  원거리 다른 반경, 해제 후 리플로우, 중복 예약 방지(멱등성), 미예약 시 타겟 좌표 폴백.

### 테스트 결과
- [x] `Unity_ValidateScript`: 신규/수정 파일 8개 전부 클린
- [x] 콘솔 에러 0건
- [x] **실제 씬의 `RoomController`(살아있는 방)에 합성 적 4마리(근접 3+원거리 1)를
      `SetRoom`으로 연결해 `ReserveAttackSlot`/`GetAttackSlotPosition` 직접 호출** -
      4마리가 target 기준 반경 1.10에서 정확히 90도씩(0°/90°/180°/270°) 떨어진 4개
      서로 다른 위치로 계산됨(균등 분산 로직 자체는 라이브로 확인). 다만 이 검증에서
      `MaxAttackRange`가 4마리 전부 0으로 나와 역할 분리(근접/원거리 다른 반경)는 이
      경로로는 확인 못 함 - RunCommand로 만든 오브젝트가 Play Mode 중이어도 에디터
      창 포커스가 없으면 프레임이 안 흘러 `Start()`가 실행되지 않는 이 세션 고질적
      한계(이전에도 발견된 문제) 때문으로 판단. 역할 분리 자체는 신규 PlayMode 테스트
      (`yield return null`로 최소 1프레임 확보하는 정석 경로)가 유닛 레벨에서
      다루고 있고, Test Runner로 실행하면 통과할 것으로 예상하나 이번 세션에서 직접
      실행 확인은 못 함.
- [ ] Test Runner 전체 스위트 실행(신규 5개 포함) - 여전히 MCP `RegisterCallbacks`
      제약으로 이 세션에서 자동 실행 불가, 유저가 직접 Test Runner 창에서 확인 필요
- [ ] 실제 전투에서(여러 근접+원거리 적이 동시에 플레이어를 노릴 때) 체감상 우글거림이
      해소됐는지, 근접/원거리 링이 자연스럽게 보이는지는 실플레이 확인 필요
- [ ] Before/After 충돌 비용 측정(`BenchmarkScene`으로 200마리 기준) - 이번 세션에서는
      미실시, 다음 작업으로 이월

### 이슈/메모
- Play Mode 중이라도 Unity 창에 포커스가 없으면 `Start()`를 포함한 프레임 진행 자체가
  멈춘다는 걸 재확인(이전에도 겪은 문제) - RunCommand로 만든 오브젝트의 `Start()` 의존
  값을 검증하려면 실제 유저 조작이나 다른 방법이 필요, MCP 세션만으로는 한계가 있음을
  인지하고 다음에도 이 함정을 피할 것.

### 다음 작업
- 유저 실플레이로 포메이션이 실제로 잘 보이는지, 근접/원거리 링 반경(1.1/5.5)이
  체감상 적절한지 확인 → 필요시 튜닝.
- `BenchmarkScene`으로 Before/After 충돌 비용 실측(적용 전 커밋 기준과 비교).
- Test Runner 전체 스위트 수동 확인.

---

## 2026-09-15 (34)

### 진행 내용
- 유저 요청: (33)의 미니맵/전체 지도 둘 다 "발견됨(Discovered)" 상태의 방은 모양(실루엣)이
  아예 안 보이게, 연결선은 그대로 유지.
- `MinimapUI.RefreshIcon`/`FullFloorMapUI.RefreshIcon` 둘 다 동일하게 수정(각 파일
  독립 수정 - (33)에서 정한 비공유 원칙 유지): 기존에 `Discovered`만 따로 갈색조
  `discoveredColor`로 칠해서 보여주던 분기를 제거하고, `Hidden`과 같은 취급으로 합쳐서
  아이콘 자체를 비활성화. 연결선 표시 로직(`RefreshAll`의 "양쪽 다 Hidden이 아니면
  보임")은 전혀 안 건드림 - 요청대로 연결선만 살아있고 방 모양은 안 보임.
  `discoveredColor` 필드 자체는 다른 폴백 분기(방문했지만 현재 방도 아니고 클리어도
  아닌 경우)에서 계속 쓰여서 그대로 유지.

### 테스트 결과
- [x] `Unity_ValidateScript`: 두 파일 모두 클린
- [x] Play Mode 실측(`RunCommand`로 `FullFloorMapUI.Open()` 직접 호출, `MinimapUI`는
      씬에 항상 떠 있어 별도 호출 불필요): 두 UI 모두 동일하게 활성 아이콘 1개(현재
      방)·비활성 아이콘 5개(발견됨 2 + 미발견 3)·활성 연결선 3개·비활성 연결선 2개로
      확인 - 발견된 인접방 2개의 아이콘은 꺼지고 그 연결선만 남는 요청 그대로 재현됨.
- 콘솔 에러 0건.

### 다음 작업
- ~~유저 실플레이로 시각적으로 원하는 느낌인지 확인~~ — **확인 완료(유저, 2026-09-15)**.
  미니맵/전체 지도 둘 다 실플레이로 확정.

---

## 2026-09-15 (33)

### 진행 내용
- 유저 요청(§6과 무관한 인게임 기능): 우상단 미니맵 좌클릭(또는 Tab) 시 일시정지 UI와
  같은 방식으로 화면이 반투명해지고 층 전체 방 구조가 보이는 맵 UI가 뜨도록.
  작업 전 계획 공유 → 논의를 거쳐 확정: ① 안개(발견 상태) 유지, 스포일러 없음
  ② `MinimapUI`와 코드/상태 완전 비공유(노치 분해 로직 포함 전부 독립 구현 - "서로
  의존하면 양쪽 다 고쳐야 하고 꼬일 위험" 사유로 유저가 헬퍼 공유안도 기각) ③ 클릭
  드래그로 지도를 스무스하게 패닝(줌은 이번 스코프 밖) ④ Tab 키로도 열고 닫기. 중간에
  "아무 데나 클릭하면 닫힘" 요구사항은 드래그와 충돌해 폐기 - 닫기는 Tab 전용으로 확정.
- 신규 `Assets/Scripts/UI/FullFloorMapUI.cs`: `GameOverUI`의 dim/content 2단
  `CanvasGroup` + `Time.unscaledDeltaTime` 페이드 패턴, `PauseState.RequestPause/
  ReleasePause`(다른 일시정지 UI와 충돌 방지 가드 포함), `MinimapUI`와 동일한 Hidden/
  Discovered/Visited 안개 로직(단, 완전 별도 구현 - 아이콘/커넥터 생성, ㄱ자 노치
  2조각 분해 포함) 재사용. 코너 미니맵보다 큰 `cellSpacing`(90)·아이콘 크기(48)로 전체
  화면에 맞게 조정. 열 때마다 현재 위치한 방을 중심으로 재정렬 후 자유 드래그(`IDragHandler`,
  콘텐츠 바운딩 박스 기준 여유(150) 클램프 - 무한정 밀려나지 않게).
- 신규 `Assets/Scripts/UI/MinimapMapOpenTrigger.cs`: 미니맵과 같은 오브젝트에 별도
  컴포넌트로 붙여 클릭 시 `FullFloorMapUI.Open()` 호출 - `MinimapUI.cs` 자체는
  전혀 안 건드림(독립성 유지).
- 신규 `Assets/InputActions/FullFloorMapActions.inputactions`: ESC 메뉴가 쓰는
  `EquipMenuActions.inputactions`와 같은 구조(always-enabled "Menu" 맵)이되 별도
  에셋으로 분리(`ToggleMap` → `<Keyboard>/tab`) - 같은 에셋을 두 컴포넌트가 각자
  Enable()/Disable() 하면 서로의 라이프사이클에 영향을 줄 위험이 있어, 이번에도
  "서로 안 건드리기" 원칙에 맞춰 완전히 분리.
- `StageManager`에 `fullFloorMapUI` 필드 추가, `minimapUI.Initialize(...)` 옆에
  `fullFloorMapUI.Initialize(...)` 호출 추가(같은 방 목록을 두 UI에 각자 전달).
- 씬(`Prototype.unity`) 작업: `Canvas` 하위(HUD/EquipPanel/GameOverPanel과 같은
  레벨)에 `FullFloorMapPanel`(전체화면 `RectTransform` + `RectMask2D` +
  `FullFloorMapUI`) 신설, 자식 `DimBackground`(검은 `Image`+`CanvasGroup`)·`Content`
  (중앙 앵커 `RectTransform`+`CanvasGroup`, 아이콘/커넥터가 런타임에 이 아래 생성됨).
  기존 `MinimapIcon.prefab`/`MinimapConnector.prefab` 그대로 재사용(신규 프리팹 없음).
  기존 `Minimap` 오브젝트에 `MinimapMapOpenTrigger` 추가.

### 테스트 결과
- [x] `Unity_ValidateScript`: 신규/수정 파일(`FullFloorMapUI.cs`, `MinimapMapOpenTrigger.cs`,
      `StageManager.cs`) 전부 클린
- [x] 콘솔 에러 0건(컴파일, 씬 저장, Play Mode 진입 전 구간 전부)
- [x] **Play Mode 실측 검증**(RunCommand로 `FullFloorMapUI`의 public API를 직접 호출-
      리플렉션 아님): `Open()` 호출 시 `Time.timeScale` 1→0, `PauseState.IsPaused` true,
      0.25초 후 `dimGroup.alpha`=0.75(설정값)·`contentGroup.alpha`=1 도달, 현재 방
      아이콘이 노란색(현재)·인접 미발견방이 회색(발견됨)·나머지 4개는 비활성(안개)으로
      정확히 반영됨(9개 자식 중 활성 5/비활성 4). `OnDrag` 직접 호출로 소량 드래그(50,-30)가
      1:1로 정확히 반영, 거대한 드래그(100000,0)는 클램프돼 1005.5에서 멈춤(무한정 밀림
      없음) 확인. `Close()` 호출 즉시 `Time.timeScale` 1로 복귀·`IsPaused` false 확인.
- [x] **Tab 키 실제 입력 / 미니맵 클릭 / 드래그 체감 / 페이드·딤 톤** — **유저 실플레이
      확인 완료(2026-09-15)**. "미니맵, 맵UI 작업된 것 잘 확인했다."

### 이슈/메모
- Unity MCP `set_component_property`로 Component 타입 필드(예: `CanvasGroup`,
  `RectTransform` 참조)를 채울 때 문서화된 `{"find":..., "method":..., "component":...}`
  형식이 매번 "Unexpected token type 'EndObject'" 에러로 실패함 - `{"instanceID": N}`
  형식(그 컴포넌트 자신의 instanceID, GameObject의 instanceID 아님)으로 넘겨야 성공함.
  기존 메모리에 있던 "component-ref wiring quirk"가 이번에도 동일하게 재현됨 - 다음에도
  이 형식부터 바로 쓸 것.
- 씬에서 GameObject를 새로 만들면 Unity가 내부적으로 GameObject 자체의 instanceID와
  그 위 각 컴포넌트(Transform/RectTransform 포함)의 instanceID를 별도로 매기는 걸
  재확인(예: `FullFloorMapPanel`의 GameObject instanceID -20454 vs 그 RectTransform
  instanceID는 다른 값) - "부모의 instanceID"로 보이는 값도 실제로는 부모의 Transform
  instanceID인 경우가 있어 헷갈리기 쉬움. `get_components`로 컴포넌트별 정확한
  instanceID를 먼저 확인하고 참조를 연결하는 순서가 안전함.

### 다음 작업
- 유저가 직접 미니맵 클릭 / Tab 키 / 드래그를 실플레이로 확인.
- 확정 우선순위 트랙상 다음 순번은 6번(세이브/로드)이지만, 이 작업은 그 트랙과 무관한
  요청이었으므로 별개로 진행 여부 결정 필요.

---

## 2026-09-15 (32)

### 진행 내용
- §6 항목 5b "층 그래프 비주얼라이저" 착수 전 계획 공유 → 유저 승인(3개 설계 결정
  전부 제안 A안: 그리드 좌표 그대로 자동 맞춤 배치 / IMGUI 렌더링(`DataValidationWindow`
  관례 재사용) / 팬·줌 없이 자동 스케일만).
- 신규 `Assets/Editor/FloorGraphWindow.cs`(`RogueLike/Floor Graph Visualizer` 메뉴) 구현.
  `new ProceduralFloorSource(seed).Floors[floor-1]`를 Edit Mode에서 직접 호출(Play Mode/씬
  불필요, 순수 C#) → `FloorLayout`을 그래프로 렌더링. 노드는 `RoomData.GridPosition`
  좌표를 min/max로 캔버스에 자동 맞춤(패딩만, 재배치 알고리즘 없음) - North가
  `GridPosition.y` 증가 방향(`ProceduralFloorGenerator.Dirs` 확인)이라 화면 위쪽에
  오도록 Y축 반전. 노드는 `RoomType`별 색(Start 녹/Combat 파랑/Boss 빨강/Item 금색),
  `Footprint`의 가로세로 비율을 대략 반영한 크기, ㄱ자(`HasNotch`)면 노치 방향에 대각선
  마커. `FloorGraph.Validate`와 동일한 BFS를 창 안에서 직접 재계산해 시작방에서 도달
  불가능한 방을 빨간 테두리로 개별 표시(예외 메시지 하나만 띄우는 대신 어느 방이
  문제인지 바로 보이게). 노드 클릭 시 우측 패널에 RoomId/Type/GridPosition/Footprint
  (크기+노치)/연결 목록(방향+대상+SubCellIndex)/적 구성(변종별 개수)/장애물 개수 표시 -
  `DataValidationWindow`의 좌목록·우상세 레이아웃과 같은 결.
- 시드 입력(0~99999, `RunConfig.MaxSeedValue`와 동일 범위 - `RunConfig.
  TryParseSeedOverride` 그대로 재사용해 파싱 규칙 이중 정의 안 함) + Random 버튼 + 층
  드롭다운(1~3) + Generate 버튼.

### 테스트 결과
- [x] `Unity_ValidateScript`: 클린(진단 0건)
- [x] 콘솔 에러 0건(컴파일+메뉴 실행 시점 둘 다)
- [x] 메뉴 `RogueLike/Floor Graph Visualizer` 등록 확인, `Unity_ManageMenuItem`으로 실행 →
      창이 예외 없이 열림
- [x] 창이 소비하는 데이터 파이프라인(`ProceduralFloorSource`) 엣지케이스 시드(0/1/12345/
      99999) × 3개 층 전부 `RunCommand`로 직접 호출해 방 개수(5~10개)·그리드 좌표 범위·
      시작방 존재 전부 정상 확인
- [ ] 실제 시드 입력 → Generate 클릭 → 그래프가 눈으로 보기에 올바른지(노드 위치/색/
      ㄱ자 마커/빨간 테두리), 노드 클릭 → 우측 패널 갱신 확인은 GUI 이벤트라 자동화로
      재현 불가 - `DataValidationWindow` 때와 동일한 사유로 유저 직접 확인 필요

### 이슈/메모
- Write 도구로 새 에디터 스크립트를 만들면 `.meta`가 자동 생성 안 되고 메뉴에도 즉시
  안 잡힘 - `AssetDatabase.Refresh()`를 명시적으로 한 번 호출해야 Unity가 임포트/
  컴파일하고 메뉴가 등록됨(앞선 테스트 파일들은 우연히 다른 타이밍에 자동 인식됐던
  것으로 보임). 다음에 신규 스크립트 추가 후 메뉴/컴파일이 바로 안 잡히면 이것부터
  의심할 것.

### 다음 작업
- 유저가 실제로 창을 열어 시드 몇 개(특히 극단값 0/99999)로 그래프가 기대대로
  보이는지 확인.
- 확정 우선순위 트랙 5번 완료(5a SO 검증 패스 + 5b 그래프 비주얼라이저). 다음 순번은
  6번(세이브/로드) - 유저 선택 필요.

---

## 2026-09-15 (31)

### 진행 내용
- 유저 확인: (30)의 돌진 넉백 2.5배가 실플레이로 만족스러움 확인 → 8n 마감. 아울러
  "대시 무적 판정 확인"도 이전 작업 중 이미 실플레이로 검증됐다고 확인 → §5 항목 마감
  (`PlayerDodgeRoll.StartDash()`가 `HealthComponent.SetInvulnerable(true)`를 걸고
  `CompleteDodgeRoll()`/`CancelDodgeRoll()`에서 해제하는 구조를 코드로도 재확인).
- 확정 우선순위 트랙 4번의 마지막 잔여 항목 "4b. 프레임레이트 독립성" 착수(유저 승인,
  간단 설명 요청 후 진행). Update()/FixedUpdate()/Tick(deltaTime)/PhysicsTick
  (fixedDeltaTime)를 쓰는 프로젝트 전체 스크립트(~25개 파일 - Player 이동/대시/조준/
  전투/피격반응, Enemy FSM 전체 상태(Idle/Patrol/Chase/Attack/Stunned/Dead), 공격
  패턴 5종(Melee/ChargeStrike/Ranged/BurstRanged/RetreatAndShoot) + 공용
  `RangedKitingBehavior`, `Projectile`/`GrenadeProjectile`, 액티브 아이템 3종
  (Flamethrower/Grenade/EnhancedShot), `InputBuffer`, 씬 전환/페이드 UI 4종)를 전수
  감사.
- **감사 결과: 위반 0건.** 모든 이동 코드가 `위치 += 방향 * 속도 * deltaTime` 형태,
  모든 쿨다운/타이머가 `remaining -= deltaTime`(나눗셈 없이 누적/차감) 형태였고,
  `Lerp`류도 전부 `elapsed / duration` 비율을 인자로 써서 "매 프레임 같은 계수로
  Lerp"(프레임 수에 따라 수렴 속도가 달라지는 흔한 함정) 사례가 없었음.
  `FlamethrowerActiveItem.ApplyDamage`는 `while (_tickTimer >= _tickInterval)`
  루프로 긴 프레임(저fps)에서도 틱을 누락하지 않는 구조까지 이미 갖춰져 있었음.
  `EquipMenuController.cs`의 기존 주석이 이 관례를 "프로젝트 전역에서 지켜지는 불변식"
  으로 이미 명시하고 있었음(`Time.timeScale = 0` 하나로 전체 시스템이 함께 멈추는
  pause 설계 자체가 이 불변식 위에 세워져 있다는 설명) - 이번 감사가 그 문서화된
  전제를 실제로 재검증한 셈.
- 위반이 없어 프로덕션 코드 수정은 없음. 대신 이 불변식을 회귀 테스트로 잠금:
  `InputBufferTests`에 고fps(144, 잘게 쪼갠 스텝)/저fps(15, 굵게 쪼갠 스텝) 두
  스케줄이 같은 누적 시간(0.14초/0.24초)에서 항상 같은 상태(대기 중/만료)에
  도달하는지 검증하는 케이스 1개 추가. 신규 `FrameRateIndependenceTests.cs`
  (EditMode) - `ChargeStrikePattern`/`MeleeAttackPattern`의 쿨다운이 같은 원리로
  스텝 크기 무관하게 동일 누적 시간에 준비 완료되는지 검증. 신규
  `FrameRateIndependenceMovementTests.cs`(PlayMode) - `EnemyChaseState.PhysicsTick`
  이동 거리가 스텝 크기(144fps 스케줄 vs 15fps 스케줄, 같은 총 1.2초)에 무관하게
  같은지, 실제 `Rigidbody.MovePosition`을 통해 검증(기존 `EnemyChaseAttackRangeMargin
  Tests`와 동일한 "FSM 자동 틱 정지 후 PhysicsTick 직접 구동" 패턴 재사용).
- 검증: `Unity_ValidateScript`로 신규/수정 파일 3개 전부 클린 확인. `InputBuffer`와
  `ChargeStrikePattern`/`MeleeAttackPattern` 쿨다운 로직은 Unity MCP RunCommand로
  테스트와 동일한 로직을 독립 실행해 PASS 직접 확인(고fps/저fps 스케줄 모두 0.14초
  지점 대기 중·0.24초 지점 만료 일치, 쿨다운 0.9초 지점 미완료·1.1초 지점 완료 일치).
- **한계**: 전체 EditMode/PlayMode 스위트를 이 세션에서 자동 실행해 통과 개수를 받아오지
  못함 - Unity MCP의 `TestRunnerApi.RegisterCallbacks` 호출이 "user interaction" 사유로
  차단되어 있어(콜백 없이 `Execute()`만 호출하는 건 가능하지만 결과를 받을 방법이
  없음), 새로 작성한 PlayMode 이동 테스트(`FrameRateIndependenceMovementTests`)도
  같은 이유로 이 세션에서 직접 실행 확인은 못 함 - 대안으로 Play Mode에 진입해 임시
  스크립트로 같은 로직을 직접 실행해보려 했으나, 마침 씬에 실제 게임(Room/Stage
  매니저가 이미 층을 빌드해 진행 중)이 로드돼 있어 합성 테스트용 "FRT_Player"
  오브젝트가 `FindGameObjectWithTag("Player")`에서 실제 플레이어와 충돌할 위험이
  있어 중단, Play Mode 종료. 프로덕션 코드는 무변경(테스트 파일만 추가/수정)이라
  회귀 위험 자체는 낮다고 판단하지만, **다음 세션에서 Unity Editor의 Test Runner
  창을 직접 열어 EditMode+PlayMode 전체 스위트를 재확인 필요**.

### 테스트 결과
- [x] `Unity_ValidateScript`: `InputBufferTests.cs`/`FrameRateIndependenceTests.cs`/
      `FrameRateIndependenceMovementTests.cs` 전부 클린
- [x] `InputBuffer`/`ChargeStrikePattern`/`MeleeAttackPattern` 쿨다운의 스텝-그래뉼래리티
      무관성: Unity MCP RunCommand 독립 실행으로 PASS 직접 확인(위 진행 내용 참고)
- [ ] EditMode/PlayMode 전체 스위트 자동 실행 - 이 세션에서 미확인(위 "한계" 참고),
      다음 세션에서 Test Runner 창으로 재확인 필요
- [ ] `FrameRateIndependenceMovementTests`(PlayMode, 신규) 자체의 실제 통과 여부 -
      코드 작성 및 컴파일 검증까지만 완료, 실행 확인은 이월

### 이슈/메모
- Unity MCP RunCommand가 `System.Reflection`(BindingFlags 등)과
  `TestRunnerApi.RegisterCallbacks` 호출을 차단하는 걸 이번에 처음 확인 - 앞으로
  이 MCP로 전체 테스트 스위트의 pass/fail 개수를 자동으로 받아오는 건 불가능하고,
  개별 로직을 RunCommand로 직접 실행해 결과를 `Debug.Log`로 찍어 확인하는 우회만
  가능함. 이후 세션에서 "테스트 실행해서 확인" 요청이 오면 이 제약을 먼저 알릴 것.

### 다음 작업
- Test Runner 창에서 EditMode/PlayMode 전체 스위트 수동 재확인(신규 테스트 3개 포함).
- 확정 우선순위 트랙 4번(입력 버퍼링+프레임레이트 독립성)이 4a/4b/4c 전부 완료로
  마감. 다음 순번 후보는 5번(SO 검증 패스는 이미 완료, 남은 건 5b 층 그래프
  비주얼라이저) 또는 6번(세이브/로드) - 유저 선택 필요.

---

## 2026-09-15 (30)

### 진행 내용
- 유저 리포트: 히트박스 기반 재설계(29) 이후 데미지는 정상적으로 들어오는데 여전히
  "넉백 맛이 안 산다". 요청: `ChargeStrike` 이 패턴의 공격만 넉백시키는 힘을 2.5배로.
- 조사: `PlayerHitReaction`은 모든 피격에 대해 고정된 넉백 거리/속도(`KnockbackDistance`
  = `KnockbackMath.BaseKnockbackDistance * 0.7`, 0.3초 동안)를 그대로 적용 - 공격
  출처(근접/원거리/보스 돌진)를 구분하지 않는 구조라, 데미지량과 달리 넉백 세기를
  공격별로 다르게 줄 방법이 아예 없었음.
- 설계 선택: `IDamageable.TakeDamage`의 세 번째 인자 `knockbackDirection`을 새 파라미터
  추가 없이 재활용 - 기존 모든 호출부(`MeleeWeapon`, `Projectile`, `MeleeAttackPattern`
  등)가 예외 없이 단위벡터(`transform.forward`, `direction.normalized`)만 넘기고 있었던
  것을 확인, 이 벡터의 **크기(magnitude)를 넉백 세기 배율**로 재해석하는 쪽이 인터페이스
  시그니처를 건드리지 않고 기존 호출부 전부 무변화로 남길 수 있어 가장 단순했음(새
  파라미터를 추가하는 대안은 모든 구현체/호출부를 건드려야 해서 기각).
- 구현: `PlayerHitReaction`의 `FlattenAndNormalize`를 `FlattenAndSplit`으로 교체 - 방향은
  그대로 정규화해 저장하되 원래 크기를 `_knockbackStrength`(기본 1)로 별도 보관,
  `FixedUpdate`에서 `KnockbackSpeed * _knockbackStrength`로 속도(=같은 0.3초 동안 이동
  거리)를 스케일. `EnemyChargeHitbox.Arm()`에 `knockbackMultiplier` 파라미터(기본 1) 추가,
  `TakeDamage` 호출 시 `_owner.forward * _knockbackMultiplier`로 전달.
  `ChargeStrikePattern`에 `KnockbackMultiplier = 2.5f` 상수 추가, `Arm()` 호출에 전달.
  `IDamageable.cs`의 `knockbackDirection` 파라미터 문서에 이 크기=배율 관례를 명시.

### 테스트 결과
- [x] `Unity_ValidateScript`: 수정 파일 4개(`PlayerHitReaction`, `EnemyChargeHitbox`,
      `ChargeStrikePattern`, `IDamageable`) 전부 클린(기존에도 있던 무관 경고만 재출력)
- [x] EditMode 139/139 (`TestRunnerApi` 재실행, 회귀 없음)
- [x] PlayMode 47/47(1회차 `EnemyChasePathfindingTests`의 동일 기존 플레이키 테스트
      1건 실패 → 단독 재실행 통과로 무관함 재확인)
- [x] 실제 Play Mode 확인 완료 - 유저가 돌진 넉백 2.5배 임팩트를 실플레이로 확인,
      만족스러움을 확인함

### 이슈/메모
- 넉백 세기를 `knockbackDirection` 벡터의 크기로 암묵적으로 인코딩하는 방식은 인터페이스를
  안 건드리는 대신 "단위벡터=1배"라는 안 보이는 규약에 의존하게 됨 - `IDamageable.cs`
  문서 주석에 명시해뒀지만, 앞으로 넉백 배율이 필요한 공격이 더 늘어나면(현재는
  `ChargeStrike` 하나뿐) 명시적 파라미터로 승격하는 편이 나을 수 있음.

### 다음 작업
- §8(Phase 4 아트+보스 AI)이 8a~8n까지 전부 완료되어 사실상 마감. 다음 순번 후보:
  확정 우선순위 트랙 4번의 잔여 항목 "4b. 프레임레이트 독립성", 더 가벼운 대안으로
  §5 미결 항목 "대시 무적 판정 확인" — 유저 선택 필요.

---

## 2026-09-15 (29)

### 진행 내용
- 유저 리포트: `ChargeStrike`가 이제 명중은 하는데(8l 수정 완료) "넉백 맛이 안 산다".
  유저 제안: 돌진 완료 후 0.2초까지 유지되는 판정용 히트박스를 보스 바라보는 방향에
  붙여서, 이 박스와 충돌하면 데미지+넉백이 들어가게 하자 - "이전의 0.4초 대기 끝
  거리 체크를 **대체**하는 것"으로 명확화(추가가 아님, 판정을 하나로 유지).
- 세부 사항 확정(유저 답변):
  1. 돌진 시작부터(30% 지점 아님) 바로 히트박스 활성화 - 어차피 코앞에서 멈추니
     부자연스럽지 않을 것
  2. 박스 크기: 높이/폭은 보스 모델 크기(캡슐 반지름 0.5·높이 2 → 폭 1/높이 2),
     두께는 적당히(1유닛으로 확정)
  3. 한 번만 맞게 - 연속 히트 방지
  4. 기존 0.4초 대기는 "공격 후딜레이"로 남기되 0.2초로 축소, 더 이상 히트박스와
     연계되지 않음(정지 후엔 판정 없음, 그냥 회복 시간)
- 구현: 신규 `EnemyChargeHitbox`(트리거 `BoxCollider` + 컴포넌트, 최초 1회만 명중
  처리 후 자동 `Disarm`) - `ChargeStrikePattern`은 순수 C# 클래스라 `OnTriggerEnter`를
  직접 못 받아서 보스 프리팹에 자식 오브젝트로 붙이고 패턴이 `Arm()`/`Disarm()`으로
  켜고 끔. `EnemyStateMachine`에 `ChargeHitbox` 프로퍼티 신설(`Animation`과 동일한
  null-conditional 관례). `Boss_Basic.prefab`에 `ChargeHitbox` 자식(박스 1×2×1,
  보스 앞 1유닛 오프셋, 기본 비활성) 추가. `ChargeStrikePattern`에서 `Execute()`
  (돌진 시작) 시 `Arm()`, 돌진 종료(도착/타임아웃) 시 `Disarm()`(정지 후 대기엔 판정
  없음), `Interrupt()`에도 `Disarm()` 추가. 기존 "0.4초 대기 끝 거리 체크" 로직은
  완전히 제거(대체 확인됨). `ChargeStrike.asset`의 `windUpDuration`을 0.4→0.2로 축소.
- **버그 발견 및 수정**: 신규/수정 테스트 작성 중 히트박스가 매번 명중 실패 -
  진단 결과 **테스트 버그가 아니라 실제 코드 결함**: `TickDash()`가 보스를 이동만
  시키고 **회전은 시키지 않아서**, 자식으로 붙은 히트박스가 돌진 방향과 무관하게
  이전 회전값 그대로인 채 엉뚱한 방향을 향하고 있었음. 실제 게임에선 보스가 직전
  상태(추격/대기)에서 이미 플레이어를 바라보고 있던 관성 덕에 우연히 안 드러났을
  뿐, 원점에 갓 스폰된(임의 회전) 상황이면 재현되는 진짜 결함이었음. `Execute()`에서
  돌진 방향을 계산한 직후 `Body.MoveRotation`으로 그 방향을 즉시 바라보게 하는
  한 줄 추가로 수정.

### 테스트 결과
- [x] `Unity_ValidateScript`: 클린
- [x] `ChargeStrikePatternPhysicsTests` 전면 재작성(히트박스 기반 설계에 맞춰):
      돌진 도중 히트박스가 명중시키는지+한 번만 맞는지(신규) / 충돌 무시가 돌진+
      회복 구간 유지되다 회복 끝나면 복원되는지(갱신) / 중단 시 즉시 복원(기존
      유지) - **최초 실행 시 2개 실패**(회전 결함), 수정 후 3개 전부 통과
- [x] EditMode 139/139
- [x] PlayMode 47/47(기존 플레이키 1건 재검증 통과)

### 이슈/메모
- **테스트가 실제 게임플레이 버그를 잡아낸 두 번째 사례(이 보스 작업에서만)**:
  "실제 플레이에서는 우연히 안 드러나지만 테스트의 초기 조건(원점 스폰, 임의 회전)
  에서는 바로 드러나는" 패턴 - 방금 스폰됐거나 다른 방향을 보고 있던 상태에서
  바로 돌진이 트리거되면 라이브에서도 언젠가 재현될 수 있는 잠재 버그였음. 자동화
  테스트가 "우연히 맞는 조건"에 의존하지 않는 깨끗한 초기 상태에서 시작하는 것의
  가치를 다시 확인.

### 다음 작업
- 유저 실플레이로 돌진 공격의 넉백 임팩트가 이제 만족스러운지 확인 필요.

---

## 2026-09-15 (28)

### 진행 내용
- 유저 리포트: `ChargeStrike`(보스 돌진 공격)이 "그냥 오브젝트에 밀리는 느낌"만 나고
  데미지 로그도 안 찍힘 - 명중이 안 되는 것으로 추정.
- 유저 제안(돌진 시작~정지까지 유지되는 추적 히트박스)에 대해 의견 요청받음 - 검토
  후 두 가지 이유로 기각 의견 제시: ① 돌진 도중 스치기만 해도 맞아버려 "0.4초 대기 후
  타격"이라는 페어플레이 텔레그래프 설계가 무너짐 ② 이 프로젝트의 다른 모든 공격
  패턴이 "특정 순간 거리 계산" 방식으로 통일돼 있는데 이것만 트리거 콜라이더 방식이
  되면 구조가 하나 늘어남. 대안으로 플레이어 대시가 이미 쓰는 "돌진 중 충돌 무시"
  패턴(`PlayerDodgeRoll`의 `Physics.IgnoreLayerCollision`) 재사용을 제안.
- 유저가 날카로운 질문: "그럼 플레이어 원거리 공격도 안 맞게 되는 거 아니냐?" - 확인
  결과 **정확한 지적**이었음: `Player.prefab`과 `Projectile_Basic.prefab`이 둘 다
  레이어 0(Default)에 있어서, `IgnoreLayerCollision(Default, Enemy)`처럼 레이어
  전체를 끄는 방식은 발사체까지 전부 막아버림. 플레이어 자신의 대시에서 이게 안
  드러났던 이유는 그때는 `HealthComponent.SetInvulnerable(true)`로 이미 별도
  무적 처리가 돼 있어서였을 뿐. 보스는 그런 장치가 없어 진짜 부작용으로 드러날 뻔함.
- 수정된 방향으로 확정: 레이어 전체가 아니라 **`Physics.IgnoreCollision(보스캡슐,
  플레이어캡슐)`로 이 두 콜라이더 쌍만 좁게** 무시. 발사체는 완전히 다른 콜라이더라
  전혀 영향 없음.
- 구현: `EnemyStateMachine`에 `PlayerCollider` 프로�터티 신설(`Start()`에서
  `TryGetComponent`로 채움, 기존 `PlayerDamageable`/`PlayerVelocity`와 동일 패턴).
  `ChargeStrikePattern`이 `Execute()`(돌진 시작) 시점에 `Physics.IgnoreCollision`을
  켜고, 대기가 끝나 타격 판정을 마친 직후(또는 `Interrupt()`로 중단됐을 때) 다시
  끄도록 수정.

### 테스트 결과
- [x] `Unity_ValidateScript`: 클린
- [x] 신규 PlayMode 테스트 2개(돌진~타격 전 구간엔 무시 상태 유지되고 타격 직후
      복원되는지 / `Interrupt()` 시 즉시 복원되는지) - **최초 실행에서 1개 실패**,
      원인 조사 결과 **수정 자체는 정상 작동**(거리가 접촉 여유값 1.1에 소수점까지
      완벽히 고정되는 것을 진단 로그로 확인 - 충돌 무시가 제대로 먹히고 있다는 뜻)
      했지만, **테스트용 가짜 플레이어에 Rigidbody가 빠져있어서** `EnemyStateMachine.
      HasClearLineToPlayer()`가 "Rigidbody 없는 콜라이더 = 벽"으로 오판, 플레이어
      자신의 캡슐을 벽으로 오인해 지근거리에서도 시야 차단 판정을 내려 타격이
      불발됐음(실제 `Player.prefab`은 Rigidbody가 있어 실제 게임에선 해당 없음) -
      테스트에 `Rigidbody`(kinematic) 추가로 수정 후 재실행 전부 통과
- [x] EditMode 139/139
- [x] PlayMode 47/47(기존 플레이키 테스트 포함 전부 한 번에 통과)

### 이슈/메모
- **재사용 가능한 교훈**: `EnemyStateMachine.IsSightBlocked`는 "Rigidbody가 없는
  콜라이더 = 벽/장애물"이라는 관례로 시야 차단을 판정하므로, 테스트에서 플레이어
  스탠드인을 만들 때 **Collider만 넣고 Rigidbody를 빠뜨리면 실제 플레이어 프리팹과
  다르게 동작**한다(자기 자신의 콜라이더가 "벽"으로 오인되어 근거리 시야/사거리
  판정이 전부 깨짐). 플레이어 콜라이더가 관여하는 테스트를 짤 때는 항상 Rigidbody도
  같이 넣을 것.
- `Physics.IgnoreLayerCollision`(레이어 전체) vs `Physics.IgnoreCollision`(콜라이더
  쌍 하나)의 차이를 실제로 헷갈릴 뻔한 사례 - 같은 레이어를 여러 종류의 오브젝트
  (플레이어 본체, 발사체)가 공유하고 있다면 레이어 단위 무시는 항상 의도치 않은
  범위까지 영향을 줄 수 있음을 유저가 먼저 캐치함.

### 다음 작업
- 유저 실플레이로 보스 돌진 공격이 실제로 명중하는지, 원거리 공격은 여전히 정상
  들어가는지 확인 필요.

---

## 2026-09-14 (27)

### 진행 내용
- (26) 수정 후에도 유저가 "여전히 제자리걸음"이라고 재보고, 재조사 요청.
- 라이브 재진단 과정에서 제 자체 진단 도구가 이 세션 내내 겪어온 "에디터 포커스
  없으면 프레임 안 흐름" 문제에 다시 걸려, 직전 턴의 "보스가 완전히 영구 정지"
  결론 일부가 오진임을 스스로 정정(강제로 Chase 상태로 되돌린 뒤 재확인하니
  실제로는 16유닛 넘게 정상 이동했음을 확인).
- 다만 **독립적인 두 시점, 서로 다른 월드 좌표에서 거리만 정확히 "8.0을 살짝
  넘는 값"(8.00035, 8.000388)에 반복적으로 멈춰있는 패턴은 재현 확인** - 우연이라기엔
  너무 정확한 일치. 코드 검토 결과: `EnemyChaseState.PhysicsTick()`의 "그만 다가가기"
  조건(`toPlayer.magnitude <= MaxAttackRange`)과 `EnemyAttackState`가 공격 시도
  가능 여부를 판단하는 조건(`CanSeePlayer(MaxAttackRange)`)이 **정확히 같은 경계값
  (8)을 공유**하고 있어, 거리가 그 경계에 딱 걸리면 두 상태 어느 쪽도 확실한 전진을
  보장 못 하는 불안정 지점이 생길 수 있음을 확인.
- 유저 승인 후 수정: `EnemyChaseState`가 `MaxAttackRange` 정확히가 아니라 그
  **90%(`AttackRangeApproachFactor`) 지점까지** 계속 다가가도록 변경 - 경계를 공유하지
  않게 되어 `EnemyAttackState`의 사거리 판정에 항상 여유를 확보.

### 테스트 결과
- [x] `Unity_ValidateScript`: 클린
- [x] 신규 PlayMode `EnemyChaseAttackRangeMarginTests` 2개(구 경계(8) 안이지만 신
      마진(7.2) 밖이면 계속 접근 / 이미 마진 안이면 더 이상 안 움직임) - 첫 실행부터
      통과
- [x] EditMode 139/139
- [x] PlayMode 45/45 - 1회차에 무관한 기존 플레이키 테스트(`ChaseState_
      SlowEnemyAcrossManyRepathCycles...`, 이 세션에서 여러 차례 이미 확인된 것)
      1건만 실패, 단독 재실행으로 통과 재확인

### 이슈/메모
- **자기 정정 기록**: 직전 턴에서 "보스가 강제로 Chase 상태를 줘도 전혀 안 움직인다"
  고 단정했던 부분은, 제 진단 스크립트 자신이 그 순간 프레임 정지 문제에 걸려 있어서
  생긴 오판이었음. 라이브 디버깅 중 `Time.time`이 실제로 흐르고 있는지를 매번 같이
  확인하지 않으면 이런 식으로 원인을 완전히 잘못 짚을 수 있음 - 이번처럼 "강제로
  상태를 리셋해도 안 움직인다"는 결론을 내리기 전에는 반드시 `Time.time`/
  `frameCount`가 그 관찰 구간 동안 실제로 진행됐는지부터 재확인할 것.
- 그럼에도 불구하고 "정확히 같은 거리값(8.0 부근)에서 반복 재현"이라는 독립적 관측
  자체는 유효했고, 실제 코드에 있던 경계 공유 문제로 이어짐 - 진단 도구의 노이즈와
  실제 신호를 구분해서 유효한 패턴만 추려낸 사례.

### 다음 작업
- 유저 실플레이 최종 확인 대기(사거리 경계에서 더 이상 제자리걸음 안 하는지).

---

## 2026-09-14 (26)

### 진행 내용
- 유저 추가 리포트(분석만 요청): 사거리 끄트머리에서 보스에게 발각된 직후, `ChargeStrike`
  가 15% 룰렛을 통과하기 전까지 "뛰는 애니를 하면서 제자리에 있는" 현상.
- 원인 분석: 보스의 `detectionRange`와 `MaxAttackRange`가 둘 다 8(`ChargeStrike`의
  사거리)이라, 발각 즉시 `EnemyChaseState`(진입 시 `PlayRun()` 1회 재생)에서
  `EnemyAttackState`로 바로 전환됨. 그런데 `EnemyAttackState.Enter()`/`Tick()`은
  **선택 가능한 패턴이 하나도 없으면 애니메이션을 전혀 건드리지 않고 그냥 리턴** -
  6~8유닛 구간에서는 `RangedAttack`/`BurstRangedAttack`(사거리 6)이 아직 사거리
  밖이고 `ChargeStrike`(사거리 8)만 사거리 안인데 85% 확률로 미발동이라, `SelectPattern`
  이 `null`을 반환하는 프레임이 이어짐 - 그 동안 Chase에서 재생된 "Run" 애니가 그대로
  남아있고, 실제 이동은 `HoldOrSway()`가 담당(웨이브 진폭 0.2, 위치 변화 미미)해서
  "뛰는 애니인데 제자리"로 보임.
- 유저와 논의 후 해결 방향 확정(대안: 우선순위만 바꾸기 - 근본 원인이 사거리 갭이라
  효과 없고 `ChargeStrike` 발동 빈도만 죽는다는 점을 설명해 기각):
  1. `RangedAttack`/`BurstRangedAttack`의 보스 전용 복제본(사거리 8)을 만들어 6~8
     사각지대 자체를 없앰 - 원본 에셋은 일반 적(`EnemyRangedData`/`EnemyBurstRangedData`)
     이 그대로 공유 중이라 직접 못 건드림, 반드시 별도 에셋으로 분리해야 함을 먼저 확인.
  2. `EnemyAttackState`가 선택된 패턴이 없어질 때(`Enter()` 진입 시, 그리고 패턴이
     막 끝나 `_activePattern`이 `null`로 돌아갈 때) `PlayIdle()`을 재생하도록 추가 -
     사거리를 아무리 맞춰도 남을 수 있는 "모든 패턴이 동시에 쿨다운" 같은 다른 틈까지
     방어.
- 구현: `RangedAttack_Boss.asset`/`BurstRangedAttack_Boss.asset` 신규 생성(원본을
  `AssetDatabase.CopyAsset`으로 복제 후 `range`만 8로 변경, 나머지 수치·`retreatTriggerRange
  =0`은 원본과 동일 유지). `BossData.asset.attackPatterns`를
  `[ChargeStrike, RangedAttack_Boss, BurstRangedAttack_Boss]`로 교체. `EnemyAttackState.
  Enter()`와 "패턴 방금 종료" 분기 2곳에 `_machine.Animation?.PlayIdle();` 추가(매
  프레임이 아니라 전환 시점에만 1회씩 - 스팸 방지).
- 작은 사고: `BurstRangedAttack_Boss.asset` 복제 직후 같은 커맨드 안에서 연달아
  `SerializedObject`로 값을 고쳤는데 `AssetDatabase.SaveAssets()`를 안 불러서 range
  변경이 디스크에 안 반영됨(첫 번째로 처리한 `RangedAttack_Boss`는 우연히 반영됨) -
  파일을 직접 읽어보고 발견, 별도 커맨드로 `SaveAssets()`+재로드 검증까지 포함해서
  재수정.

### 테스트 결과
- [x] `Unity_ValidateScript`: 수정 파일 클린
- [x] EditMode 139/139
- [x] PlayMode 43/43 - 1회차에 무관한 기존 플레이키 테스트 2건(`ChaseState_
      SlowEnemyAcrossManyRepathCycles...`, `Chase_SearchTimesOut_GivesUpToPatrol_
      WhenLastKnownSpotUnreachable` - 둘 다 이 세션/DevLog에 이미 기록된 상습
      플레이키) 실패, 해당 2건만 재실행해 통과 확인
- [ ] 실제 Play Mode 확인 필요 - 사거리 끄트머리에서 발각 직후 더 이상 "뛰는 애니
      제자리" 현상이 안 보이는지, 원거리 두 패턴이 8까지 정상적으로 반응하는지

### 이슈/메모
- **재사용 가능한 교훈**: `AssetDatabase.CopyAsset` 직후 같은 스크립트 실행 안에서
  `SerializedObject`로 여러 에셋을 연속 수정할 때는 **에셋마다 개별적으로
  `AssetDatabase.SaveAssets()`를 확실히 호출**하거나, 최소한 마지막에 한 번이라도
  호출을 빼먹지 말 것 - 이번처럼 첫 번째 에셋은 우연히 반영되고 두 번째만 안 반영되는
  식으로 반쪽만 실패해서 알아채기 쉽지 않았음.
- 공유 데이터 에셋(여러 적 타입이 같은 `AttackPatternData`를 참조)을 특정 적 전용으로
  튜닝해야 할 때는 항상 "이 에셋을 다른 누가 쓰고 있는지" 먼저 확인하고, 필요하면
  전용 복제본을 만들 것 - 이번에 `RangedAttack.asset`을 그냥 고쳤으면 일반 원거리
  적들의 사거리까지 같이 바뀔 뻔했음.

### 다음 작업
- 유저 실플레이 최종 확인 대기.

---

## 2026-09-14 (25)

### 진행 내용
- (24) 수정 후 라이브 재확인 결과, 이번엔 다른 증상으로 여전히 멈춤 확인(분석만 요청받음):
  보스 위치가 2분 넘게 완전히 고정, 거리도 정확히 6.0015(후퇴 목표 6을 이미 살짝
  넘어선 상태)에서 정지 - 지난번(벽에 막혀 목표 거리에 못 미침)과 달리 **이미 목표
  거리에 도달했는데도 조준/발사로 못 넘어가는** 다른 정지 케이스. `RangedKitingBehavior`
  /`RangedAttackPattern`/`BurstRangedAttackPattern`/`RetreatAndShootPattern` 조합의
  다단계 상태 전환 자체가 계속 엣지 케이스에 걸리는 것으로 판단, **보스의 해당 패턴을
  통째로 교체**하기로 결정(유저 판단, 코드 근거로 동의).
- 유저 요청: 보스에게 "낮은 확률로 빠르게 접근해 근접 공격하는" 패턴 신설 + 기존
  `RetreatAndShoot` 제거 + `RangedAttack`/`BurstRangedAttack`(둘 다 이미
  `retreatTriggerRange=0`, 후퇴 로직 자체를 안 탐)으로 교체.
- 수치 확정(유저): 발동 확률 15%, 트리거 사거리 8, 데미지 15, 쿨다운 2.5초, 돌진 후
  0.4초 대기 뒤 히트박스+공격 애니 실행. 돌진 속도는 보스 이동속도(2.5) 확인 후
  10으로 확정(트리거 최대 거리 8을 0.8초에 주파, 평소 굼뜬 보스가 갑자기 빠르게
  튀어나오는 인상을 주는 선).
- 신규 `ChargeStrikePattern`/`ChargeStrikePatternData` 구현: 돌진(호밍 아님, 트리거
  시점 방향 고정, `MeleeAttackPattern` 컨벤션과 동일하게 접촉 여유 1.1에서 정지) →
  정지 후 대기(기존 `windUpDuration` 필드를 "돌진 전 텔레그래프"가 아니라 "돌진 후
  대기"로 재활용) → 대기 종료 시 사거리 1.3 안이면 데미지+`Attack` 애니 큐.
- **확률 처리 설계의 핵심**: "매 프레임 15% 굴리기"는 사실상 즉시 발동과 다름없어서
  (1초 안에 99%+ 누적 확률) 무의미함 → **쿨다운이 0으로 떨어지는 바로 그 순간에 딱
  한 번만 굴리고, 실패하면 쿨다운을 통째로 다시 채워 다음 사이클까지 기다리게** 하는
  방식으로 패턴 자체 안에서 완결 처리(`IAttackPattern`/`EnemyAttackState`는 전혀
  안 건드림).
- `ChargeStrike.asset` 신규 생성(range 8/cooldown 2.5/damage 15/windUpDuration 0.4/
  dashSpeed 10/selectionChance 0.15). `BossData.asset.attackPatterns`를
  `[ChargeStrike, RangedAttack, BurstRangedAttack]`으로 교체(`RetreatAndShoot` 제거,
  에셋 파일 자체는 다른 곳에서 참조 안 해서 그대로 둠).
- **버그 발견 및 즉시 수정**: 신규 EditMode 테스트 작성 중 `ChargeStrikePattern`의
  `_cooldownRemaining`이 생성자에서 초기화 안 돼 있어 C# 기본값 0으로 시작 →
  `Tick()`의 "이미 0 이하면 굴릴 필요 없음" 가드에 걸려 **생성 직후 첫 기회에서만
  15% 확률 롤 자체를 건너뛰고 무조건 즉시 사용 가능**해지는 결함을 테스트가 바로
  잡아냄. 생성자에서 `_cooldownRemaining = data.Cooldown`으로 초기화하도록 수정 -
  첫 기회도 다른 사이클과 똑같이 쿨다운을 다 채운 뒤 롤을 거치게 됨.

### 테스트 결과
- [x] 신규 EditMode `ChargeStrikePatternTests` 3개(확률 100%면 쿨다운 후 계속
      준비 상태 유지 / 확률 0%면 영원히 준비 안 됨(매 사이클 쿨다운 리필 확인) /
      실제 프레임 주기(1/60초)로 잘게 틱해도 사이클당 한 번만 롤되는지) - **최초
      실행 시 3개 전부 실패**, 위 생성자 초기화 버그가 원인이었음을 바로 특정, 수정
      후 재실행 전부 통과
- [x] 신규 PlayMode `ChargeStrikePatternPhysicsTests` 1개(돌진 중엔 데미지 없음 →
      정지 후 대기 중에도 없음 → 대기(0.4초) 끝나야 데미지 15 적용 + 쿨다운 재시작) -
      통과
- [x] EditMode 139/139, PlayMode 43/43(이전 세션에서 반복 확인됐던 플레이키 경로탐색
      테스트도 이번엔 통과) - 전부 회귀 없음

### 이슈/메모
- 확률 기반 선택 로직을 넣을 때는 항상 "몇 번째 프레임/틱마다 굴리는지"부터 명확히
  정의할 것 - 프레임마다 굴리면 사실상 확률이 무의미해진다는 함정은 재사용 가능한
  교훈.
- **테스트가 실제로 버그를 잡아낸 사례**: `_cooldownRemaining`의 C# 기본값(0)이
  "쿨다운 없음 = 즉시 준비됨"으로 해석되는 기존 패턴들의 공통 관례(다른 패턴들도
  전부 이렇게 시작함, 지금까지는 의도된 동작)가, 이번처럼 "생성 시점에 아직 아무
  것도 안 굴린 상태"를 구분해야 하는 새 패턴에서는 함정이 될 수 있음을 자동 테스트가
  구현 직후 바로 확인시켜줌 - 라이브 플레이로만 검증했다면 "가끔 확률 무시하고 바로
  나온다"는 식으로 훨씬 늦게, 훨씬 애매하게 발견됐을 문제.

### 다음 작업
- 유저 실플레이로 보스전에서 (1) 원거리 두 패턴이 정상적으로 로테이션되는지
  (2) 가끔(체감상 드물게) 빠른 돌진 근접 공격이 나오는지 (3) 돌진 후 0.4초 대기가
  자연스럽게 읽히는지 확인 필요.

---

## 2026-09-14 (24)

### 진행 내용
- 유저 리포트: 피격 넉백은 잘 되는데, 보스에게 맞고 넉백된 후 **보스가 이동 애니를 계속
  재생하면서도 실제로는 움직이지 않는 상태가 계속 유지**됨. 원인만 확인 요청.
- 진단(코드 정독, 수정 없이): `BossData.asset`의 공격 패턴은 `RetreatAndShoot`(1순위,
  트리거 2.2/후퇴목표 6)과 `RangedAttack`(2순위, `retreatTriggerRange=0`이라 후퇴 없음)
  둘뿐 - 근접 패턴 없음. 두 패턴 모두 공유하는 `RangedKitingBehavior.Tick()`의 후퇴
  로직이 "플레이어와의 거리가 `retreatToRange` 이상 벌어질 때까지" 무조건 후퇴만
  반복하는 구조였고, **그 외의 탈출 조건(타임아웃, 정체 감지)이 전혀 없었음**. 벽/방
  구석에 막혀 거리를 더 벌릴 수 없는 위치에서 후퇴가 시작되면 `awayFromPlayer.magnitude`
  가 영영 `retreatToRange`에 도달하지 못해 `_isRepositioning`이 계속 `true`로 남고,
  `IsBusy`가 안 풀리니 `EnemyAttackState`가 패턴을 다시 선택하지도 못해 그 자리에서
  무한 반복됨(코드상 원래부터 있던 구멍). 애니 쪽은 `EnemyAttackState.Tick()`이 패턴
  최초 실행 시 1회만 `AnimationCue`를 재생하는 구조라, 실제로는 "후퇴하려고 애쓰는 중"
  인데 화면상 사격 관련 포즈가 계속 유지되는 것처럼 보임.
- 넉백 기능 자체와는 무관한 기존 버그지만, 플레이어가 피격 시 순간 크게 밀려나면서
  보스의 트리거 사거리(2.2) 안으로 갑자기 들어가는 상황이 이전보다 쉽게 만들어져
  이 기존 구멍을 밟을 확률이 올라간 것으로 추정.
- 유저 승인 후 수정: `RangedKitingBehavior`에 정체 감지 추가 - 0.5초 간격으로 거리가
  0.15유닛 이상 벌어졌는지 확인, 못 벌어졌으면 후퇴를 포기하고 그 자리에서 바로
  조준/발사로 전환(`BeginWindUp`). 3개 패턴(`RangedAttackPattern`/
  `BurstRangedAttackPattern`/`RetreatAndShootPattern`) 전부가 이 클래스를 공유하므로
  한 곳 수정으로 전부 적용됨.

### 테스트 결과
- [x] `Unity_ValidateScript`: 클린(무관한 스타일 오탐 1건, 무시)
- [x] 신규 PlayMode `RangedKitingBehaviorTests` 2개(벽에 막히면 포기하고 발사 / 열린
      공간에서는 정상적으로 후퇴 목표 거리까지 도달) - 첫 실행부터 통과
- [x] EditMode 136/136
- [x] PlayMode 41/42 - 실패 1건은 `EnemyChasePathfindingTests`의 기존에 이미 확인된
      플레이키 테스트(이번 세션 (19) 근처에서도 동일 테스트로 재현/재검증한 바로 그
      테스트)로, 이번 변경과 무관.

### 이슈/메모
- 이번 건도 "겉보기엔 신규 기능(넉백)의 부작용처럼 보이지만, 실제로는 그 기능이
  새로운 상황(플레이어 위치 급변)을 더 자주 만들어서 기존에 드물게만 발생하던
  구조적 결함을 노출시킨 것"인 패턴 - 새 기능을 넣을 때는 "이게 기존 시스템이 암묵적
  으로 가정하고 있던 조건(여기선 '후퇴할 공간이 항상 있다')을 깨뜨리지 않는지"도
  같이 점검할 가치가 있음.

### 다음 작업
- 유저 실플레이로 보스방에서 후퇴가 막히는 상황이 더 이상 무한 대기로 안 이어지는지
  확인.

---

## 2026-09-14 (23)

### 진행 내용
- 유저 요청: 플레이어 피격 반응 신규 구현 - 근접 적에게 맞으면 **적이 바라보는 방향**으로,
  원거리 적 투사체에 맞으면 **투사체 진행 방향**으로 넉백 + 피격 애니 재생. 피격 후
  0.3초간 밀려나고(방/벽 관통 금지), 0.4초간 조작 불가(애니는 이 0.4초에 맞춰 스케일),
  이후 자동 복구. 넉백 중 재피격 시 기존 넉백을 버리고 최신 히트로 덮어씀.
- 설계(A안, 유저 승인): `IDamageable.TakeDamage`에 `Vector3 knockbackDirection = default`
  옵션 인자 추가 - 공격자가 이미 알고 있는 방향(근접은 `transform.forward`, 발사체는
  자기 `_direction`)을 그대로 실어 보냄. 기존 `HealthComponent.AnyDamageTaken` 이벤트에
  같은 필드 추가, 기존 구독자 2곳(`EnemyStateMachine`, `DebugOverlayController`)은
  시그니처만 맞추고 동작 무변경(적 쪽 넉백은 기존 위치 기반 로직 그대로 유지).
- 신규 `PlayerHitReaction` 컴포넌트(Player 루트): `AnyDamageTaken` 구독, 넉백 거리 =
  `KnockbackMath.BaseKnockbackDistance`(적 넉백 기준값 1.5)의 70% = 1.05유닛을 0.3초
  등속 이동(`PlayerDodgeRoll.RunDash()`와 동일하게 `Rigidbody.linearVelocity` +
  `WallClamp.ClampDirection`으로 매 프레임 벽 클램프). 재피격 시 타이머/방향을 그 자리에서
  즉시 리셋(별도 목표 좌표 보간이 아니라 매 스텝 "현재 위치 → 그 순간의 방향"으로
  진행하는 구조라 덮어쓰기가 자연스럽게 처리됨). 0.4초간 `PlayerMovement.enabled=false`
  (대시가 쓰는 것과 동일한 기존 패턴), 종료 시 `PlayerMovement.OnEnable()`이 이미
  `_forceAnimationRefresh=true`를 세팅하므로 Idle/Run 복귀도 추가 코드 없이 자동 처리.
  `IsHitStunned` 공개 프로퍼티를 `PlayerAim`/`PlayerCombat`/`PlayerDodgeRoll`이 각각
  기존 `IsDodgeRolling` 체크 자리에 추가로 걸어 조준/공격/대시 입력을 동시에 차단.
- `PlayerAnimationController`: `PlayHit()` 추가(레이어0, 전신). 지난 (21)에서 업바디
  레이어에 적용했던 "이미 같은 state면 CrossFade가 씹히는 문제" 가드를 베이스 레이어
  `Play()` 헬퍼로 일반화(`PlayOnLayer(stateName, layer)`로 통합) - 재피격 시 Hit 애니가
  씹히지 않고 항상 처음부터 재생되도록. `AC_PlayerAdventurer.controller` 베이스
  레이어에 `Hit` state 신규 추가(클립 `HitRecieve` 0.5667초 → speed 1.4167로 스케일해
  정확히 0.4초에 맞춤, Roll과 동일한 방식).
- `Player.prefab`에 `PlayerHitReaction` 추가(RequireComponent 체인을 통해 자동 부착됨).

### 테스트 결과
- [x] 수정/신규 파일 11개 전부 `Unity_ValidateScript` 클린(기존과 동일한 무관 스타일
      경고만, 새 오류 없음)
- [x] EditMode 136/136 (회귀 없음)
- [x] PlayMode **40/40**(기존 36 + 신규 `PlayerHitReactionTests` 4개) - 최초 1회 실행에서
      `EnemyChasePathfindingTests`의 기존에 알려진 타이밍 플레이키 테스트 1건이 실패했으나
      해당 테스트만 재실행해 통과 확인 - 이번 변경과 무관한 기존 결함성 플레이키니스로 확정.
      신규 4개(넉백 방향 적용/이동 정지, 0.4초 후 조작 복구, 넉백 중 재피격 덮어쓰기,
      벽 앞에서 넉백 시 관통 안 함)는 첫 실행부터 전부 통과.
- [ ] 실제 Play Mode 확인 필요 - 근접/원거리 각각 맞았을 때 넉백 방향·피격 애니·조작
      잠금/복구 타이밍이 체감상 자연스러운지 확인 필요

### 이슈/메모
- `PlayerHitReactionTests`는 `PlayerMovementResidualVelocityTests`가 이미 확립해둔
  "`PlayerInputActions`에 실제 `PlayerActions.inputactions` 에셋을 리플렉션으로 주입"
  패턴을 그대로 재사용 - 플레이어 컴포넌트가 얽힌 PlayMode 테스트를 짤 때 표준 방법으로
  계속 사용할 것.
- 이번 기능은 코드 근거상 완결됐지만, "체감"(넉백 거리/조작 잠금 시간이 적절한지)은
  자동화로 검증 불가 - 실플레이 피드백에 따라 상수(0.7배율/0.3초/0.4초) 재튜닝 여지 있음.

### 다음 작업
- 유저 실플레이 피드백 대기.

---

## 2026-09-14 (22)

### 진행 내용
- 유저 리포트: (21) 수정으로 공격 애니 정상 재생 확인됐으나, 시작방 플레이어 스폰
  지점에 같은 모델의 캐릭터가 하나 더 서 있다고 보고.
- 원인 확인: (21) 진단 중 `Gun_Shoot`/`Sword_Slash` 클립의 실제 팔 회전 폭을
  비교하려고 `Object.Instantiate(prefab)`으로 `Adventurer.fbx`를 임시 복제해
  `AnimationClip.SampleAnimation`으로 본 회전값을 측정하는 작업을 2회 수행했는데,
  이걸 Play Mode가 아니라 **Edit Mode에서 실행**해 정리(`Destroy`) 없이 그대로 씬에
  남았음(Play Mode였다면 종료 시 자동 폐기됨). 씬 루트 스캔 결과
  `Adventurer(Clone)` 2개(둘 다 (0,0,0), Start룸/스폰 지점과 겹침) 확인.
- 조치: 둘 다 제거, `EditorSceneManager.SaveOpenScenes()`로 씬 저장, 재스캔으로
  잔여 없음 확인.

### 테스트 결과
- [x] 씬 루트 재스캔: `Adventurer` 관련 잔여 오브젝트 0개(Player 프리팹 인스턴스 제외)
- [x] 씬 저장 후 dirty flag 정상 해제

### 이슈/메모
- **교훈**: Editor 스크립팅으로 진단용 임시 오브젝트를 인스턴스화할 때는 Play Mode
  중인지 항상 먼저 확인할 것 - Edit Mode에서 `Object.Instantiate`로 만든 오브젝트는
  Play Mode와 달리 자동 폐기되지 않고 씬에 영구히 남는다. 앞으로 이런 임시
  인스턴스는 (a) 반드시 Play Mode 중에만 만들거나, (b) 만든 즉시 같은 명령 안에서
  `result.DestroyObject`로 정리할 것.

### 다음 작업
- 없음(§8f 공격 애니 타이밍 버그 계열 완전히 종결).

---

## 2026-09-14 (21)

### 진행 내용
- 유저 재검증: (20) 수정 후에도 "캐릭터를 카메라 쪽으로 향하게 하고 양팔을 유심히
  보면서 여러 번 공격해봤는데 공격 애니가 대부분 안 나온다"로 동일 계열 증상 재확인.
  "애니메이션 블렌딩 결과 아니냐"는 유저 본인의 추가 의심 제기.
- 1차로 "클립 움직임 폭이 작아서 안 보이는 것 아니냐"(Gun_Shoot vs Sword_Slash 회전량
  비교, 약 50도 vs 180도+) 가설을 제시했으나, 유저가 직접 캐릭터를 카메라로 향하게
  하고 정밀 관찰한 결과로 **명확히 반박** - "작아서 안 보이는 게 아니라 아예 안
  나온다"고 재확인.
- 라이브 Play Mode에서 `Animator.Update(deltaTime)`으로 애니메이터 평가를 직접
  강제해 실험한 결과, **마스크/블렌딩 자체는 완전히 정상 작동함을 실측으로 증명**
  (`UpperArm.L` 로컬 회전이 Idle 152° → Shoot t=0 143° → t=0.3s 92°로, 오프라인
  `AnimationClip.SampleAnimation` 예측치와 정확히 일치하며 실제로 변함). 즉 애니메이터
  내부 상태 전환과 마스크 블렌딩 파이프라인 자체는 문제가 없음이 확정됨.
- 근본 원인 확정: `Animator.Play()`/`CrossFadeInFixedTime()` 호출 결과는 **호출된 바로
  그 프레임에는 `GetCurrentAnimatorStateInfo()`에 반영되지 않고, 애니메이터의 다음
  내부 평가 시점에야 반영됨**(직접 실험으로 재확인: 호출 직후 같은 프레임에서 조회하면
  호출 전 값 그대로). `PlayerAnimationController.Update()`의 완료-감지 폴링이 입력
  트리거와 **같은 프레임**에 걸리면, 방금 막 새로 재생을 시작했는데 아직 반영 전인
  "직전 값"(=직전 샷이 끝나서 `normalizedTime>=1`이 된 상태)을 그대로 보고 "이미
  끝났다"고 오판 → weight를 즉시 0으로 되돌려 **애니가 화면에 단 한 프레임도 나오지
  못한 채 취소**됨. 발사체는 `Fire()`에서 애니와 무관하게 항상 나가므로 "발사는
  되는데 애니만 안 보인다"는 증상과 정확히 일치. 입력 처리(Input System 콜백)와
  `PlayerAnimationController.Update()`의 프레임 내 실행 순서가 매번 이 쪽으로 치우쳐
  있었던 것으로 추정되며, 이 순서 자체는 쿨타임 여유와 무관하게 매 트리거마다
  반복될 수 있는 구조적 경합이라 "대부분 안 나온다"는 빈도와 일치.
- 수정: `PlayerAnimationController`에 `_upperBodyTriggerFrame`(트리거된
  `Time.frameCount`) 기록 추가, `Update()`의 완료-감지 체크를 **트리거된 바로 그
  프레임에는 건너뛰도록** 가드. 최소 한 번은 애니메이터가 실제로 갱신된 뒤에야
  "끝났는지" 판단하게 되어 경합 자체가 구조적으로 사라짐.

### 테스트 결과
- [x] `Unity_ValidateScript`: 클린(기존과 같은 무관 오탐 경고 1건, 무시)
- [x] EditMode 136/136 (`TestRunnerApi` 재실행, 회귀 없음)
- [ ] 실제 Play Mode 확인 필요 - 유저가 다시 한번 카메라를 캐릭터 방향으로 하고
      여러 번 연속 공격해 애니가 매번 나오는지 최종 확인 필요

### 이슈/메모
- **핵심 교훈**: `Animator.Play`/`CrossFadeInFixedTime` 호출 직후 같은 프레임에서
  `GetCurrentAnimatorStateInfo`/`IsInTransition`을 조회하면 신뢰할 수 없다 - 최소
  한 프레임(또는 명시적 `Animator.Update(dt)` 강제 호출) 뒤에 조회할 것. 트리거와
  완료-감지를 같은 `Update()` 계열 폴링에 의존시킬 때는 항상 "트리거된 바로 그
  프레임"을 배제하는 가드를 기본으로 넣을 것.
- 이번 진단에서 유용했던 기법: `Animator.Update(0f)` / `Animator.Update(dt)`로
  애니메이터 평가를 수동으로 강제하면, 이 자동화 환경의 고질적인 "Play Mode 진입은
  되지만 포커스 없으면 프레임이 안 흐르는" 문제를 완전히 우회하고 라이브 애니메이터
  상태를 결정론적으로 검증할 수 있었음 - 앞으로 비슷한 라이브 애니메이터 진단에
  재사용할 것.
- `AnimationClip.SampleAnimation(gameObject, time)`을 임시 인스턴스에 직접 걸어
  본(bone) 회전값을 오프라인으로 측정하는 기법도 유효했음(Play Mode 불필요, 결정론적) -
  클립 자체의 움직임 폭을 정량적으로 비교할 때 재사용 가능.
- 이번 세션에서만 이 증상 하나에 3라운드(입력-발사 타이밍 동기화 → 같은 state
  재진입 시 CrossFade 씹힘 → 같은 프레임 stale 읽기 경합)를 거쳐 근본 원인에
  도달함 - 매 라운드 유저의 재현/반박이 정확했고, 결과적으로 서로 다른 세 가지
  실제 결함을 각각 고친 것으로 확인됨(모두 유효한 수정, 서로 배타적이지 않음).

### 다음 작업
- 유저 실플레이 최종 확인 대기.

---

## 2026-09-14 (20)

### 진행 내용
- 유저 재검증 결과: (19)의 `OnFired` 동기화 적용 후에도 실플레이에서 "발사만 되고
  상체 애니가 안 나올 때가 많고, 간혹 애니는 나오는데 그 중에 또 발사체가 안 나올
  때도 있다"는 동일 계열 증상이 여전히 재현됨.
- 이번 세션 자동화 환경에서 직접 Play Mode 재현을 시도했으나, `Time.frameCount`가
  실제 80초 넘게 지나도 계속 2에 멈춰 있는 것으로 확인 - 이전에도 기록된 "에디터가
  포커스/리페인트를 못 받으면 게임 루프가 멈추는" 환경 한계에 재차 걸려 라이브 관측은
  불가, 코드 정밀 재검토로 진단 전환.
- `AC_PlayerAdventurer.controller`를 직접 열어 수치 재확인: `Shoot`(`Gun_Shoot`) 클립
  길이 **0.6초**(Loop Time 꺼짐) vs 원거리 공격 쿨타임(`rangedFireCooldown`) **0.65초** -
  여유가 **0.05초(~3프레임)**뿐. `PlayerAnimationController.Update()`는 매 프레임
  폴링으로 클립이 끝난 걸 감지해야 레이어1 weight를 0으로 내리는 구조라, 다음 발사가
  이 짧은 여유 안에 들어오면 레이어가 아직 "Shoot" state인 채로 남아있는 상태에서
  다시 같은 state로 `CrossFadeInFixedTime`이 걸림.
- 원인 확정(코드 근거, 라이브 미검증): Unity의 `Animator.CrossFade`/`Play` 계열은
  **레이어가 이미 그 state를 현재 state로 보고 있으면 재시작을 무시하고 씹어버리는
  경우가 있음** - 같은 state로의 재진입이 "새로 시작"이 아니라 "이미 거기 있음"으로
  처리되는 것으로 추정. `OnFired`(실제 `Fire()` 호출)는 매번 정확히 실행되므로 발사체는
  항상 나가지만, 애니 트리거(`CrossFadeInFixedTime`) 호출 자체는 씹혀서 화면엔 안
  보이는 상황이 됨 - "발사는 되는데 애니가 안 나온다"와 정확히 일치.
- 수정: `PlayerAnimationController.PlayUpperBody()`에서 대상 state가 **이미 레이어1의
  현재 state**(`!IsInTransition && shortNameHash 일치`)인 경우엔 `CrossFadeInFixedTime`
  대신 `Animator.Play(hash, layer, 0f)`로 **강제 프레임 0 재시작**, 그 외(다른
  state에서 넘어오는 경우, 예: Attack→Shoot)엔 기존처럼 `CrossFadeInFixedTime` 유지.
  Idle/Run/Roll/Death(레이어 0, `Play()`)는 이번 증상과 무관하고 재발 사례도 없어
  범위 밖으로 유지.

### 테스트 결과
- [x] `Unity_ValidateScript`: 수정 파일 클린(기존에도 있던 무관한 "Update() 문자열
      연결" 경고 재출력 - 이 파일 `Update()`엔 문자열 연결이 없어 오탐으로 판단, 무시)
- [x] EditMode 136/136 (`TestRunnerApi` 재실행, 회귀 없음)
- [ ] 실제 Play Mode 확인 필요 - 이번 수정 이후에도 여전히 자동화 환경의 프레임 정지
      한계로 라이브 검증 불가, 유저 실플레이로만 확인 가능

### 이슈/메모
- 이번 세션에서 두 번째로 확인된 자동화 환경의 "Play Mode 진입은 되지만 게임 루프
  자체가 멈춰 있는" 현상 - `Time.frameCount`가 실제 경과시간과 무관하게 고정. 원인
  미확정(에디터 창 포커스/리페인트 문제로 추정, 기존 기록과 동일 패턴). Play Mode
  진입 성공 여부(`IsPlaying: true`)만으로는 게임 로직이 실제로 돌아가는지 보장 못하므로,
  라이브 검증이 필요한 진단에서는 항상 `Time.frameCount`가 실제로 진행되는지부터
  확인할 것 - 이번에 다시 발목 잡힘.
- `CrossFadeInFixedTime`/`Play`의 "이미 그 state면 재시작 무시" 동작은 Unity Mecanim의
  잘 알려진 함정 - 클립 길이와 재사용 주기(쿨타임 등)가 타이트하게 맞물리는 반복
  트리거형 애니메이션(공격/피격 등)에서는 항상 "같은 state 재진입 시 강제 Play(hash,
  layer, 0f)" 패턴을 기본으로 고려할 것.

### 다음 작업
- 유저 실플레이로 원거리 연속 공격 시 애니-발사체가 매번 같이 나오는지 최종 확인.

---

## 2026-09-14 (19)

### 진행 내용
- 유저 버그 리포트: "가만히 서서 원거리 공격을 한 발씩 끊어서 써도 상체 공격 애니가
  안 나온다(10번까지 확인) / 1~2번은 애니 없이 발사체만 2번 나오고 3번째에 애니가
  나오는데, 이때 발사체가 나올 때도 있고 안 나올 때도 있다 - 공격 쿨타임과 애니 실행
  타이밍이 꼬인 것 같다."
- 원인 확정: `PlayerCombat.OnAttackPerformed`가 **입력을 누른 시점에 무조건**
  `PlayShoot()`/`PlayAttack()`을 호출하고 있었는데, `RangedWeapon.Attack()`은 쿨타임
  중이면 실제 발사(`Fire()`) 없이 `InputBuffer`(창 0.15초, 슬롯 1개)에 기록만 하고
  나중에 `Tick()`에서 쿨타임이 풀릴 때 실제로 발사함 - 즉 "버튼을 누른 시점"과 "실제
  발사체가 나가는 시점"이 서로 다를 수 있는데 애니는 항상 전자에 묶여 있어, 근접
  쿨타임(0.65초)보다 살짝 빠르게 누르면 애니만 먼저 재생되고 발사는 나중(또는 버퍼
  창 0.15초를 넘기면 영영 안 나감)에 일어나는 식으로 어긋났음. 유저가 스스로 짚은
  "쿨타임과 애니 타이밍이 꼬였다"는 진단이 정확했음.
- 유저 승인한 해결안(A안): `IWeapon`에 `Action OnFired` 콜백 추가 - 실제
  `Fire()`/타격이 일어나는 순간에만 호출되도록 `RangedWeapon.Fire()`·
  `MeleeWeapon.Attack()` 맨 앞에서 `OnFired?.Invoke()` 호출. `PlayerCombat.
  EquipWeapon()`에서 무기 장착 시 이 콜백에 애니 재생을 1회 연결하고, `OnAttackPerformed`
  에서는 애니 호출을 제거하고 `_weapon.Attack()`만 남김. `CrossFadeInFixedTime`이
  같은 상태라도 호출될 때마다 처음부터 재생하고, `Fire()` 자체가 이미 무기 쪽
  쿨타임으로 막혀 있어 별도의 "끊고 재생" 로직 없이도 요구사항이 그대로 충족됨.
- 신규 EditMode 테스트 `WeaponOnFiredTimingTests`(4개) 추가: 쿨타임 밖에서 즉시
  발사/버퍼링 중엔 미발동/버퍼가 실제로 소모되는 순간에만 발동/버퍼 창을 넘기면
  영영 미발동 - 4개 시나리오를 순수 로직으로 고정.

### 테스트 결과
- [x] 콘솔 에러/경고 0 (수정 파일 4개 전부 `Unity_ValidateScript` 통과)
- [x] EditMode 136/136 (`TestRunnerApi` 실행, 신규 4개 포함 - 최초 1회는 테스트
      자체의 시나리오 설계 실수로 1개 실패했다가, 근접 발사 직후가 아니라 쿨타임
      막바지에 눌러 버퍼 창 안에 들어오도록 고친 뒤 재실행해 136/136 통과)
- [ ] 실제 Play Mode 확인 필요 (정지 상태 연속 공격 / 이동 중 공격 양쪽 다 애니가
      매번 나오는지, 원거리 연사 시 애니-발사체 타이밍 어긋남이 사라졌는지)

### 이슈/메모
- 지난 항목(§8f, 이동 중 좌클릭 공격 시 상체 애니 미실행)에서 처음 추정했던 "레이어
  가중치 폴링 경합" 가설은 이번 근본 원인과는 별개 - 실제 증상은 이동 여부가 아니라
  이 쿨타임/버퍼 타이밍 어긋남이 원인이었을 가능성이 높음. 이번 수정으로 그 현상도
  같이 해소되는지 다음 실플레이 때 같이 확인할 것.
- 이 프로젝트에서 `Action`(단일 델리게이트) 콜백은 구독자가 항상 1명(현재 장착된
  무기 하나)뿐이라 C# `event` 대신 평범한 settable 프로퍼티로 충분 - `EquipWeapon()`이
  호출될 때마다 덮어써지는 것도 의도된 동작(무기 교체 시 이전 콜백은 자연히 사라짐).

### 다음 작업
- 유저 실플레이로: (1) 정지 상태 원거리 연속 공격, (2) 이동 중 공격, 양쪽 다 상체
  애니가 매번 발사체와 함께 나오는지 확인.

---

## 2026-09-14 (18)

### 진행 내용
- 유저 버그 리포트: "플레이 중인데 갑자기 왜 이렇게 밝아지냐?"
- 진단(Play Mode 중 읽기 전용 조사): `RenderSettings`/`Light` 컴포넌트 전수 조회 결과
  Directional Light가 **3개** 동시 활성 — 원래 씬 소유의 `Directional Light`(intensity 2)
  외에 `Sun`(intensity 1)과 `Sun.001`(intensity **4.3**)이 추가로 켜져 있었음. 저장된
  씬 파일(`Prototype.unity`)엔 `Directional Light` 하나뿐이라 이 둘은 런타임에 스폰된
  것으로 특정.
- 원인 확정: `Enemy_Flying_Gun.fbx`(cyberpunk_game_kit, `Enemy_BurstRanged`의 비주얼로
  사용 중)를 직접 열어보니 **에셋 파일 자체에 Directional Light 2개(Sun, Sun.001)가
  안 지워진 채 남아있었음** - 원작자가 Blender에서 선택 항목만이 아니라 씬 전체(조명
  포함)를 export한 것으로 추정. 같은 계열 `Enemy_Flying.fbx`(Gun 없는 버전)는 이 문제
  없음 - `_Gun` 버전에만 있던 문제. `Enemy_BurstRanged`가 스폰될 때마다 이 조명 2개가
  비주얼 하위에 그대로 딸려 들어와, Directional Light는 위치 무관하게 씬 전체에 영향을
  주므로 BurstRanged 한 마리만 나와도 화면 전체가 과다 노출됨.
- **수정**: `Enemy_Flying_Gun.fbx`의 `ModelImporter` 설정에서 `importLights`를
  `false`로 변경 + 재임포트 - **원본 FBX 자체가 더 이상 조명을 안 갖고 오도록** 근본
  수정. `Enemy_BurstRanged.prefab`의 비주얼은 이 FBX를 참조하는 **중첩 프리팹
  인스턴스**라 별도 프리팹 수정 없이 자동으로 조명 0개로 반영됨(재확인 완료).
- 현재 진행 중이던 플레이 세션의 라이브 씬에선 마침 해당 조명이 이미 사라진 상태였음
  (BurstRanged 개체가 죽거나 방이 바뀌며 자연 해소된 것으로 추정) - 즉시 조치는 불필요,
  영구 수정만 적용.

### 테스트 결과
- [x] `Enemy_Flying_Gun.fbx` 재임포트 후 `GetComponentsInChildren<Light>` = 0개 확인
- [x] `Enemy_BurstRanged.prefab` 및 그 인스턴스에서도 Light 0개로 자동 반영 확인
- [x] EditMode 132/132

### 이슈/메모
- **교훈**: 무료 에셋 킷을 새로 들여올 때, 메시/애니메이션뿐 아니라 **Light/Camera가
  같이 딸려 들어오지 않는지도 확인할 것** - Blender→FBX export가 "선택 항목만"이 아니라
  씬 전체를 내보내는 실수는 흔하고, Directional Light는 위치와 무관하게 전역으로
  영향을 줘서 다른 오브젝트들보다 훨씬 눈에 띄는 방식으로 터짐. 앞으로 새 캐릭터/적
  모델을 프리팹에 연결하기 전에 `GetComponentsInChildren<Light>(true)`(+`Camera`도
  마찬가지 위험) 한 번 찍어보는 걸 체크리스트에 추가.

### 다음 작업
- 유저 실플레이로 `Enemy_BurstRanged` 재등장 시 더 이상 화면이 밝아지지 않는지 확인.

---

## 2026-09-14 (17)

### 진행 내용
- 유저 질문 검토: "원거리 적이 사거리 안에 있어도 사거리 끝까지 가서 쏘려는 로직이
  있는가?" → `RangedKitingBehavior.Start()`가 `DistanceToPlayer < RetreatTriggerRange`일
  때만 `Range`(=`RetreatToRange`)까지 후퇴한 뒤 발사하는 조건부 로직임을 확인.
  `RangedAttack.asset`/`BurstRangedAttack.asset`은 `retreatTriggerRange`를 에셋에 따로
  안 채워놔서 코드 기본값 2.5가 쓰이고 있었음(`Range`는 둘 다 6) - 전투 중 플레이어가
  자주 2.5 안쪽으로 들어가게 되니 "사거리 안인데도 끝까지 도망가서 쏜다"는 인상을 줬을
  것으로 추정. 보스 패턴(`RetreatAndShoot`)은 트리거 2.2/후퇴목표 6으로 의도적으로 더
  공격적인 카이팅 설계라 그대로 둬야 함을 확인.
- 유저 결정: **보스 제외 일반 원거리 적(`RangedAttack`/`BurstRangedAttack`)의
  `retreatTriggerRange`를 0으로.** `distance < 0`은 항상 거짓이므로 후퇴 자체가 아예
  발동하지 않게 됨 - 항상 제자리에서 바로 조준·발사.
- `RangedAttack.asset`/`BurstRangedAttack.asset`의 `retreatTriggerRange`를 2.5 → 0으로
  변경. `RetreatAndShoot.asset`(보스)은 손대지 않음.

### 테스트 결과
- [x] `RogueLike/Validate All Game Data`: 23개 에셋 0 issue (0은 음수가 아니므로
      `AttackPatternData.Validate()`의 "RetreatTriggerRange should not be negative"
      체크 통과)
- [x] EditMode 132/132

### 다음 작업
- 유저 실플레이로 일반 원거리 적이 더 이상 근거리에서 도망가지 않고 제자리에서 쏘는지 확인.

---

## 2026-09-14 (16)

### 진행 내용
- 유저 피드백: "다른 게임에 비해 대시 이동 거리가 살짝 짧아 보인다, 4.5유닛으로."
- `dashDuration`(0.5초)은 그대로 두고 `dashSpeedMultiplier`만 1.2 → **1.8**로 조정
  (5 × 1.8 × 0.5 = 4.5유닛). 지속시간이 안 바뀌었으므로 Roll 애니메이션 재생 속도
  (2.666667, 직전 항목에서 설정)는 그대로 유효 - 추가 조정 불필요.
- `PlayerBaseStats.json` + `GameBalance.cs` 폴백 기본값 동일하게 갱신.

### 테스트 결과
- [x] `GameBalance.Player` 로드 후 계산한 거리값이 정확히 4.5임을 확인

### 다음 작업
- 유저 실플레이로 대시 거리 체감 재확인.

---

## 2026-09-14 (15)

### 진행 내용
- 확인 결과 공유: 대시 중 재생되는 애니메이션은 `Roll`(Adventurer FBX `CharacterArmature|
  Roll` 클립, 전체 길이 1.333초)인데, 실제 대시 지속시간(`dashDuration`)이 0.2초라
  클립의 약 15%(최악의 겹침 보정 포함해도 34%)만 재생되고 잘리고 있었음.
- 유저 결정: `dashDuration` 0.2 → **0.5초**로 확대, **대시 거리는 기존과 동일하게 유지**
  (`dashSpeedMultiplier`를 3.0 → **1.2**로 같이 낮춤 - 5×1.2×0.5 = 5×3×0.2 = 3유닛으로
  거리 불변, 대신 무적시간은 0.2초 → 0.5초로 늘어남 - 이 트레이드오프는 사전에 설명하고
  유저가 확정한 것).
- `Assets/StreamingAssets/Balance/PlayerBaseStats.json`과 `GameBalance.cs`의 내장 폴백
  기본값(`DefaultPlayerBaseStats()`) 둘 다 갱신 - 이 프로젝트 컨벤션상 JSON이 깨졌을 때
  폴백이 "최근 합의된 값"과 어긋나면 안 되므로 항상 같이 맞춤.
- `AC_PlayerAdventurer.controller`의 `Roll` 상태 `Speed`를 **2.666667**(=클립길이
  1.333초 ÷ 새 dashDuration 0.5초)로 설정 - 대시가 지속되는 0.5초 동안 Roll 클립이
  정확히 처음부터 끝까지 다 재생되도록 배속.

### 테스트 결과
- [x] EditMode 132/132
- [x] `GameBalance.Player.dashDuration=0.5`, `dashSpeedMultiplier=1.2` 로드 확인,
      `AC_PlayerAdventurer.controller`의 Roll 상태 `speed=2.666667`(Idle/Run/Death는
      그대로 1) 저장·재조회로 확인
- [ ] 실제로 대시 0.5초 동안 구르기 동작이 자연스럽게 끝까지 보이는지, 거리 체감이
      기존과 비슷한지는 사람 플레이로 확인 필요 - 미확인/예정

### 다음 작업
- 유저 실플레이로 대시 거리 체감(기존과 비슷한지) + Roll 애니메이션이 0.5초 동안 자연스럽게
  완주하는지 확인.

---

## 2026-09-14 (14)

### 진행 내용
- 유저 요청: "대시하면 이동 방향을 바라보고, 대시 끝나면 다시 마우스 커서 방향을 보도록."
- 기존 `PlayerAim.FixedUpdate()`는 대시 여부와 무관하게 매 물리 프레임 마우스 방향으로
  강제 회전시키고 있었음(그래서 지금까지는 대시 중에도 계속 마우스를 보고 있었음).
- `PlayerAim`에 `[RequireComponent(typeof(PlayerDodgeRoll))]` 추가 - `FixedUpdate()` 맨
  앞에서 `_dodgeRoll.IsDodgeRolling`(대시 본편 + 겹침 보정 꼬리 전부 포함, 기존 프로퍼티
  재사용)이면 그 프레임은 그냥 리턴해서 마우스 조준 회전을 건너뜀.
- `PlayerDodgeRoll.StartDash()`에 `_rigidbody.MoveRotation(Quaternion.LookRotation(
  _dodgeRollDirection))` 한 줄 추가 - 대시 방향은 롤 도중 안 바뀌므로 시작 시점 1회만
  회전시키면 충분.
- **대시 종료 후 원래대로 돌아오는 데 별도 코드 불필요**: `IsDodgeRolling`이 다시
  `false`가 되는 순간 `PlayerAim.FixedUpdate()`가 다음 물리 프레임부터 자동으로 평소
  로직(마우스 방향 회전)을 재개함 - 오늘 애니메이션 레이어 작업 때와 같은 "게이팅만
  해두면 복귀는 저절로 된다" 패턴.

### 테스트 결과
- [x] EditMode 132/132
- [x] Play Mode 진입 시 콘솔 에러/경고 없음(`RequireComponent` 순환 참조 없음,
      `_dodgeRoll` 참조 정상 해석 확인)
- [ ] 실제로 대시 중 이동 방향을 보고, 대시 끝나면 마우스로 복귀하는 게 눈으로 봤을 때
      자연스러운지는 사람 플레이로 확인 필요 - 미확인/예정 (대시 트리거 자체가 Input
      System 이벤트를 거쳐야 해서 이번 세션에서는 재현 스크립트로 대체 검증 안 함, 코드
      리뷰로 로직 정확성만 확인)

### 다음 작업
- 유저 실플레이로 대시 중/후 회전 전환이 자연스러운지 확인.

---

## 2026-09-14 (13)

### 진행 내용
- **GPT 캐릭터 생성 루프 종료 결정**: `gpt_output`을 3차까지 검토(모델 스케일 100배 → "고쳤다"고
  했지만 실제로는 몸통 자체가 여전히 4배 큼 → 몸통 크기는 진짜 고쳐졌지만 이번엔 머리가
  몸통에서 완전히 분리되어 붕 뜨는 새 버그) - 매번 지적한 것만 정확히 고쳐지고 다른 곳이
  깨지는 패턴이 반복됨을 확인, 유저가 이미 검증된 `man_kit` Adventurer로 확정하기로 결정.
- 유저 버그 리포트: "공격 후 공격 애니가 끝나기 전에 계속 방향키로 움직이면 공격 끝 포즈가
  계속 유지된다." 원인: `PlayerMovement`가 `isMoving != _wasMoving`(상태 변화 시점)에만
  `PlayRun()`/`PlayIdle()`을 호출하는데, 공격 중에도 계속 같은 방향으로 이동 중이었다면
  이동 상태 자체가 안 바뀌어서 공격 애니(1회성, loopTime=false) 종료 후 아무도 다시
  `PlayRun()`을 호출하지 않아 마지막 프레임에 고정.
- **수정 전에 유저가 먼저 요청**: "상체는 공격 애니, 하체는 걷는 애니로 블렌딩 가능한가?" →
  Mecanim의 **Layer + Avatar Mask**(상체 마스킹, Override 블렌드)로 구현 - 표준 액션 게임
  기법이며, 이 방식 자체가 버그도 부수적으로 해결함(아래 참고).
- `Assets/Prefabs/PlayerAnimators/AM_UpperBody.mask` 신규 - Adventurer 리그(`CharacterArmature`
  하위, Mixamo류 명명: Hips/Abdomen/Torso/Chest/Neck/Head, Shoulder·UpperArm·LowerArm·Wrist+
  손가락 본, UpperLeg/LowerLeg/Foot/PT)를 코드로 순회하며 하체 본 15개(Hips, UpperLeg.L/R,
  LowerLeg.L/R(+_end), Foot.L/R(+_end), PT.L/R(+_end))만 비활성화, 나머지(복부 위+양팔)는
  활성 상태로 남김.
- `AC_PlayerAdventurer.controller` 레이어 2개로 재구성:
  - Layer 0(Base, 전신, 마스크 없음): Idle/Run/Roll/Death만 유지
  - Layer 1("UpperBody", `AM_UpperBody.mask` 적용, Blending=Override, defaultWeight=0):
    Attack(Sword_Slash)/Shoot(Gun_Shoot)만 여기로 이동
- `PlayerAnimationController` 재작성: `PlayAttack()`/`PlayShoot()`가 이제
  `SetLayerWeight(1, 1f)` + 레이어 1에 `CrossFadeInFixedTime`. 매 프레임 `Update()`에서
  레이어 1이 활성 상태일 때 `GetCurrentAnimatorStateInfo(1).normalizedTime >= 1f`(전환 중이
  아닐 때)를 감지하면 가중치를 다시 0으로 내림 - **호출부(`PlayerCombat` 등)는 전혀 안
  바뀜**, 메서드 이름만 그대로 재사용.
- **버그가 저절로 해결되는 이유**: 이제 공격은 레이어 0(하체+이동 상태)을 전혀 건드리지
  않음 - `PlayerMovement`는 계속 정상적으로 Run/Idle을 레이어 0에 유지하고 있고, 공격이
  끝나면 그냥 레이어 1 가중치가 0으로 내려가면서 "원래부터 맞게 재생되고 있던" 레이어 0이
  그대로 드러남. "공격 후 걷기 재개"를 위한 별도 로직이 필요 없어짐 - 레이어링 자체가
  구조적으로 이 버그 클래스를 없앰.

### 테스트 결과
- [x] EditMode 132/132 (2회 확인)
- [x] Play Mode 진입 후 `Animator.layerCount=2`, `layer[0]=Base Layer weight=1`,
      `layer[1]=UpperBody weight=0`(기본값) 확인. `PlayAttack()` 호출 직후 레이어 1
      가중치가 1로 전환되는 것도 확인.
- [ ] "공격 애니 종료 후 가중치가 다시 0으로 내려가는지"는 **이 세션에서 `Time.time`이
      아예 진행되지 않는 상태**(이전에도 몇 차례 겪은 자동화 환경의 한계, 이번엔 재현성
      있게 다시 발생)라 실시간 재현 확인은 못함 - `PlayerAnimationController.Update()`의
      조건식 자체는 코드 리뷰로 정확함을 확인(표준적인 "1회성 애니메이션 종료 감지" 패턴).
      Play Mode 진입 자체가 이번엔 약 9분 걸렸는데(콘솔 에러 없음, 이전 EditMode 테스트
      러너의 내부 정리 작업과 Play Mode 진입 요청이 겹치며 생긴 무관한 TestRunner 예외
      8건만 발생 - 게임 코드 에러 아님), 최초 진입 지연과 이번 Time 정지가 관련 있는지는
      불명확.

### 이슈/메모
- Mecanim에서 "1회성 애니메이션이 끝났는지" 감지하는 표준 패턴을 처음 도입:
  `!animator.IsInTransition(layer) && animator.GetCurrentAnimatorStateInfo(layer)
  .normalizedTime >= 1f`. Exit Time 기반 전이 대신 이 폴링 방식을 쓴 이유는, 레이어
  가중치 제어(0↔1)는 애니메이션 상태 그래프 밖의 관심사라 C# 쪽에서 다루는 게 더
  명확하다고 판단해서.
- 이 자동화 세션에서 `Time.time`이 진행되는 경우와 멈춰있는 경우가 실행마다 달라
  보이는 현상이 계속 관찰됨 - 정확한 트리거 조건은 아직 특정 못함(에디터 포커스,
  백그라운드 작업 유무 등으로 추정). 실시간 애니메이션 타이밍 검증이 필요한 작업은
  앞으로도 사람 플레이에 의존해야 할 가능성이 높음을 감안할 것.

### 다음 작업
- 유저 실플레이로 (1) 공격 중 계속 이동 시 하체는 계속 걷고 상체만 공격 모션인지,
  (2) 공격 종료 후 더 이상 마지막 프레임에 고정되지 않는지 확인.

---

## 2026-09-14 (12)

### 진행 내용
- 유저 버그 리포트: "대시하고 내 캐릭터가 기본 애니 상태로 굳었다, 에러 로그도 나왔다."
- 콘솔 확인 결과 원인 즉시 특정:
  ```
  UnassignedReferenceException: The variable playerRenderers of RoomTransitionController
  has not been assigned.
    at RoomTransitionController.SetPlayerVisible (...)
    at RoomTransitionController+<TransitionRoutine>d__14.MoveNext ()
  ```
- **근본 원인**: 2026-09-14 (11)에서 Player 프리팹 비주얼을 교체하며 루트의 캡슐
  `MeshRenderer`를 삭제했는데, `Assets/Scenes/Prototype.unity`의 `RoomTransitionController`가
  인스펙터에서 그 정확한 컴포넌트 인스턴스를 `playerRenderers` 배열(1칸)에 참조하고
  있었음 - 삭제되면서 그 슬롯이 끊어진 참조(`{fileID: 0}`)로 남음. 이건 Player.prefab
  에셋 자체가 아니라 **씬에 박혀 있던 참조**라 프리팹만 봐서는 안 보였던 문제.
- **증상 연쇄**: `TransitionRoutine` 코루틴이 `playerInputActions.enabled = false;` →
  `SetPlayerVisible(false);`(여기서 예외 발생) 순서로 실행되는데, 코루틴 안에서 처리
  안 된 예외가 나면 그 지점에서 코루틴이 완전히 멈춤 - 즉 `playerInputActions.enabled`가
  `false`로 설정된 뒤 다시는 `true`로 복구되지 못함. 그래서 대시 도중 문을 통과하며
  방 전환이 걸리면 그 순간부터 모든 입력(이동/공격/대시)이 죽고, 대시 애니메이션
  (`Roll`)이 마지막으로 재생되던 포즈에서 그대로 멈춰 "기본 애니 상태로 굳었다"로
  보였던 것 - 별개의 애니메이션 버그가 아니라 이 예외 하나의 결과.
- **수정**: `RoomTransitionController`가 이미 참조 중인 `player` Transform 아래에서
  `GetComponentsInChildren<Renderer>(true)`로 실제 렌더러를 다시 수집(Adventurer 모델의
  `Adventurer_Body`/`Adventurer_Feet`/`Adventurer_Head`/`Adventurer_Legs`/`Backpack` -
  총 5개 SkinnedMeshRenderer)해서 `playerRenderers` 배열을 `SerializedObject`로 재할당,
  씬 저장.

### 테스트 결과
- [x] EditMode 132/132
- [x] Play Mode 진입 직후 콘솔 에러/경고 없음 확인
- [x] 실제 문 트리거 재현 시도: 씬의 연결된 문(`Door_West_0`, `TargetRoom=Room_Base`)
      위치로 플레이어를 순간이동시켜 물리 트리거가 걸리도록 유도 - 이 시도 자체에서는
      콘솔 에러 없음을 확인했으나, 이 세션에서 `Time.time`이 호출 사이에 진행되지 않는
      경우가 있어(2026-08-18에 이미 기록된 자동화 환경 한계) 전환 코루틴이 실제로 끝까지
      실행되는 것(0.6초짜리 전환 완료 후 렌더러/입력 복구)까지는 자동으로 확인 못함 -
      다만 원인이 정확히 특정된 null 참조를 실제 존재하는 컴포넌트로 교체한 것이라 수정
      자체의 정확성에는 의심의 여지가 낮음
- [ ] 유저 실플레이로 문 통과 시 더 이상 에러 없이 전환되고 캐릭터가 계속 조작되는지
      최종 확인 필요

### 이슈/메모
- **교훈**: 프리팹의 컴포넌트를 삭제/재구성할 때, 그 프리팹 에셋 자체만 봐서는 안 되고
  **씬에 박힌 참조도 같이 확인해야 함** - `RoomTransitionController`처럼 매니저 컴포넌트가
  프리팹 인스턴스의 특정 하위 컴포넌트를 인스펙터에서 직접 드래그해 참조해두는 패턴이
  이 프로젝트에 존재. 다음에 유사한 비주얼 교체 작업을 할 때는 대상 컴포넌트를
  참조하는 다른 스크립트가 씬에 있는지 먼저 검색할 것.
- Player.prefab의 씬 인스턴스에 예전 캡슐 시절 컴포넌트를 가리키던 프리팹 오버라이드
  잔재(`m_Mesh`, `m_Materials`, 콜라이더 `m_Material` 등, 전부 이제 존재하지 않는
  fileID를 가리킴)가 남아있음 - Unity가 알아서 무시하므로 기능상 무해하지만, 씬 파일
  청소 차원에서 나중에 "Player 프리팹 오버라이드 Revert" 한 번 해주면 깔끔해짐(급하지 않음).

### 다음 작업
- 유저 실플레이로 문 통과 시나리오 최종 확인.

---

## 2026-09-14 (11)

### 진행 내용
- **플레이어 캐릭터 비주얼 교체.** 유저가 처음엔 "붕괴 스타레일 개척자와 비슷한 SD 캐릭터"를
  AI로 생성해달라고 요청 → (1) 특정 상업 IP 캐릭터 흉내는 유사도를 낮춰도(30% 등) 동일한
  문제라 거절, (2) 프로젝트의 Unity AI 생성 기능 자체가 계정에 구독이 없어 `GenerateMesh`
  호출 시 "A model must be selected" 에러로 완전히 막혀있음을 실제 호출로 확인 → 유저가
  `Assets/Art/Props/man_kit`(Quaternius "Ultimate Modular Males", **CC0 퍼블릭 도메인**)를
  직접 추가, 그중 하나를 골라 적용하는 것으로 방향 전환.
- `man_kit/Individual Characters/FBX/Adventurer.fbx` 채택 — 실측 결과 키 1.88유닛(발이
  원점)으로 플레이어 캡슐(높이 2)에 스케일 조정 없이 거의 그대로 맞음(로봇 적들이 2.8배
  키워야 했던 것과 대조적). 클립 24개 중 게임 상태와 매칭:
  Idle/Run/Roll(대시)/Sword_Slash(근접)/Gun_Shoot(원거리)/Death, 덤으로 HitRecieve(피격
  경직 - 아직 미사용, 향후 스턴 연출에 활용 가능).
- `PlayerAnimationController` 신규(`EnemyAnimationController`와 동일 패턴) - 차이점은
  `Awake()`에서 `Animator.updateMode = UnscaledTime`로 강제 설정: 사망 시
  `PauseState.RequestPause`가 `Time.timeScale`을 0으로 만드는데, 일반 Animator는 그 순간
  같이 얼어붙어서 Death 클립이 재생되다 말기 때문 - 기존에 있던 "언스케일드 타임으로 도는
  스크립트 캡슐 쓰러뜨리기"가 이미 이 문제를 인지하고 우회했던 것과 같은 이유.
- 연결 지점:
  - `PlayerMovement`: 이동 시작/정지 전환에서만(매 프레임 아님) `PlayRun()`/`PlayIdle()`
    호출. `OnEnable()`에서 강제 리프레시 플래그를 세워, 대시 종료 후 `PlayerDodgeRoll`이
    이 컴포넌트를 재활성화할 때 이동 상태가 안 바뀌어도 Roll의 마지막 프레임에 멈춰있던
    문제(로봇 적 Run 애니메이션 때 겪었던 것과 같은 종류)를 방지.
  - `PlayerDodgeRoll.StartDash()`: `PlayRoll()`.
  - `PlayerCombat.OnAttackPerformed()`: 장착 무기 타입(`RangedWeaponData`/
    `MeleeWeaponData`)에 따라 `PlayShoot()`/`PlayAttack()`.
  - `RoomFlowManager.HandlePlayerDied()`: 기존 "플레이스홀더 캡슐 쓰러뜨리기"
    코루틴(`PlayerFallOverRoutine`, 클래스 주석에 "Revisit once real character art/animation
    replaces it"라고 미리 적혀 있던 부분)을 완전히 제거하고 `PlayDeath()` + Rigidbody
    kinematic 전환 + 콜라이더 비활성화로 교체.
- `Assets/Art/Props/man_kit/Individual Characters/FBX/Adventurer.fbx`도 로봇 때와 동일한
  Loop Time 버그 확인 → Idle/Run만 loopTime=true로 재임포트(Roll/Attack/Shoot/Death는
  원래도 1회성이 맞아서 그대로 둠).
- `Assets/Prefabs/PlayerAnimators/AC_PlayerAdventurer.controller` 신규(Idle/Run/Roll/
  Attack/Shoot/Death 6개 상태, 전이 없이 코드가 `CrossFadeInFixedTime`으로 직접 상태
  지정 - 로봇 적들과 동일한 방식). `Player.prefab` 루트의 캡슐 `MeshFilter`/`MeshRenderer`
  제거 → `Visual` 자식으로 Adventurer 모델 + Animator + `PlayerAnimationController` 추가.

### 테스트 결과
- [x] EditMode 132/132 (3회 반복 확인)
- [x] PlayMode 36/36 - 중간에 두 종류의 기존 플레이키 테스트(`ChaseState_
      SlowEnemyAcrossManyRepathCycles_...`, `Chase_SearchTimesOut_GivesUpToPatrol_
      WhenLastKnownSpotUnreachable`)가 각각 한 번씩 따로 실패했으나 둘 다 이전 세션에
      이미 "스위트 순서 민감 플레이키" 기존 사례로 기록된 것들이고 플레이어 코드와 전혀
      무관 - 최종 재실행 36/36 클린
- [x] Play Mode 실제 검증: 플레이어 스폰 확인(`PlayerAnimationController`/`Animator`/
      컨트롤러 전부 정상 연결), 치사량 데미지 → `Rigidbody.isKinematic=true`,
      콜라이더 비활성화, `Health.IsDead=true`, `Time.timeScale=0`까지 전부 확인, 콘솔
      경고/에러 없음(처음엔 속도 초기화 순서 문제로 "kinematic body에 velocity 설정 불가"
      경고가 떴다가, `isKinematic=true` 설정을 velocity 초기화 뒤로 옮겨서 해결)
- [ ] 실제 이동/공격/대시 중 애니메이션 전환이 눈으로 봤을 때 자연스러운지는 사람이 직접
      플레이해서 확인 필요 - 미확인/예정

### 이슈/메모
- Adventurer 클립도 로봇 때와 똑같이 전부 Loop Time이 꺼진 채 임포트됨 - 이 게임에서 쓰는
  무료 캐릭터 킷들의 공통 특성으로 보임. **앞으로 이 종류의 킷을 새로 추가할 때마다
  Idle/Walk/Run 계열 클립의 Loop Time부터 확인하는 걸 체크리스트로 삼을 것.**
- 무기(야구 방망이/깃펜 또는 Sword.fbx/Pistol.fbx 프롭)를 캐릭터 손에 쥐여주는 작업은
  이번 범위에서 제외 - 본体+애니메이션 교체까지만 진행. 본에 프롭을 붙이려면 손 뼈대를
  찾아 부착하는 별도 작업이 필요.
- `HitRecieve`/`HitRecieve_2` 클립은 있지만 아직 아무 상태에도 연결 안 함 - 플레이어
  피격 경직 연출을 나중에 추가하고 싶다면 여기 활용 가능.

### 다음 작업
- 유저 실플레이로 이동/공격/대시/사망 애니메이션 체감 확인.
- 무기 프롭 부착, 피격 경직 연출은 원하면 후속 작업으로.

---

## 2026-09-14 (10)

### 진행 내용
- 유저 버그 리포트: "죽는 애니 실행 중에도 이전 상태에서 남은 회전이 계속된다."
- 원인: `EnemyDeadState.Enter()`가 `Body.linearVelocity`는 0으로 초기화했지만
  `Body.angularVelocity`는 그대로 뒀음. 적 Rigidbody의 constraints(84 =
  FreezePositionY + FreezeRotationX + FreezeRotationZ)를 보면 **Y축 회전(방향 전환)은
  얼려져 있지 않아서**, Chase/Attack 상태가 `Body.MoveRotation`으로 플레이어를 바라보게
  하던 회전이나 충돌로 생긴 각속도가 죽는 순간 그대로 남아 죽는 애니메이션 내내 계속
  회전하는 것처럼 보였음.
- 수정: `EnemyDeadState.Enter()`에 `_machine.Body.angularVelocity = Vector3.zero;` 추가.
  콜라이더를 같은 시점에 꺼두기 때문에(이전 세션에서 추가) 이후 어떤 충돌도 새로 각속도를
  줄 수 없어, 진입 시 한 번만 0으로 만들면 충분함(매 프레임 클리어할 필요 없음).

### 테스트 결과
- [x] EditMode 132/132
- [x] Play Mode 실제 재현 검증: 스폰한 적의 `Rigidbody.angularVelocity`를 (0,5,0)으로
      강제로 주입해 버그 상황을 재현한 뒤 치사량 데미지 → 사망 직후 `angularVelocity`가
      정확히 (0,0,0)으로 초기화됨을 확인

### 다음 작업
- 유저 실플레이로 죽는 애니메이션 중 더 이상 회전하지 않는지 최종 확인.

---

## 2026-09-14 (9)

### 진행 내용
- 유저 요청: "모든 적의 죽는 상태를 추가하자, 죽는 애니를 활용해야 하니까." — 지난 세션에서
  의도적으로 범위 밖으로 뺐던 Death 애니메이션을 이번에 구현.
- 근본 문제: `HealthComponent.HandleDied()`가 `Health.Died` 이벤트 콜백 안에서 그 자리에서
  바로 `pooledObject.ReturnToPool()`/`Destroy()`를 호출해, 죽음 연출을 보여줄 유예 시간이
  전혀 없었음(같은 이벤트를 구독하는 `EnemyStateMachine.HandleDied()`가 `DeadState`로
  전환은 하지만, 그 시점엔 이미 오브젝트가 풀로 반환/파괴된 뒤라 의미 없음).
- `HealthComponent`에 `deathDelay`(초, 기본값 0) 필드 추가 - 0보다 크면 `Invoke(FinishDeath,
  deathDelay)`로 실제 반환/파괴를 지연, 0이면 기존과 완전히 동일하게 즉시 처리. 플레이어
  프리팹은 값을 안 건드려 기본 0 유지 → 기존 게임오버 흐름 무변화(플레이어는 애초에
  `destroyOnDeath=0`이라 이 흐름과 무관).
- `EnemyDeadState`를 다른 상태들과 같은 패턴(`EnemyStateMachine` 참조를 생성자로 받음)으로
  변경, `Enter()`에서 `Animation?.PlayDeath()` + 캡슐 콜라이더 비활성화(시체가 플레이어/
  투사체를 계속 막지 않도록) + 속도 0으로 정지. `EnemyStateMachine.OnEnable()`에서 매
  활성화(최초 스폰 + 풀 재사용 전부)마다 콜라이더를 다시 켜서, 죽었던 인스턴스가 풀에서
  재사용될 때 콜라이더가 꺼진 채로 나오지 않도록 함.
- `EnemyAnimationController.PlayDeath()` 신규.
- 애니메이션 상태 이름 정리: 기존에 만든 5개 `AnimatorController`가 모델별로 클립 이름이
  달라(`Death` vs `Dead`) 상태 이름도 제각각이었음 → 빌드 스크립트를 "Death"/"Dead" 클립
  모두 동일하게 "Death"라는 하나의 상태 이름으로 정규화하도록 고쳐서 5개 컨트롤러 전부
  재생성(상태만 다시 채움, 다른 상태들은 그대로) - `EnemyAnimationController`가 모델
  종류와 무관하게 항상 `PlayDeath() → CrossFade("Death")` 하나만 알면 되도록.
- 각 프리팹의 `HealthComponent.deathDelay`를 실제 모델의 Death/Dead 클립 길이 + 0.1초
  여유로 설정: 2Legs 계열(`Enemy_Basic`/`Enemy_RelentlessMelee`/`Enemy_Ranged`) 0.89초,
  Flying 계열(`Enemy_ErraticMelee`/`Enemy_BurstRanged`) 0.47초, `Boss_Basic`(Large_Gun)
  0.73초.

### 테스트 결과
- [x] EditMode 132/132, PlayMode 36/36 (플레이어 프리팹은 deathDelay 기본값 0 유지라
      기존 회귀 테스트 전부 무영향 확인)
- [x] Play Mode 실사용 검증: `Enemy_Basic`을 씬에 직접 스폰(풀 미경유) → 치사량 데미지 →
      즉시 콜라이더 `false`, `Health.IsDead` `true` 확인. 이후 여러 툴 호출 사이에 실제로
      38초 정도 Play Mode 시간이 흐른 뒤 재조회하니 오브젝트가 씬에서 사라져 있음을 확인 -
      죽는 즉시 사라지지 않고 지연 후 정리되는 흐름이 실제로 동작함을 확인. (이전 세션에서
      "자동화 환경은 Time이 안 흐른다"고 기록했던 건 Play Mode 진입 직후 찰나의 관측
      이었을 뿐, 실제로는 툴 호출 간 왕복 시간만큼 Play Mode 시간도 정상적으로 흐른다는
      것을 이번에 재확인 - 향후 세션을 위해 정정)
- [ ] Death 애니메이션 자체가 시각적으로 자연스럽게 재생되는지(포즈, 타이밍)는 사람이
      직접 보고 확인 필요 - 미확인/예정

### 다음 작업
- 유저 실플레이로 Death 연출 확인, 필요하면 각 프리팹의 `deathDelay` 값 조정.
- Stunned(피격 경직) 애니메이션은 대응 클립 자체가 없어 여전히 별도 과제로 남음.

---

## 2026-09-14 (8)

### 진행 내용
- 유저 실플레이 피드백: "근접 적이 Chase 상태일 때 다리가 걷는 게 아니라 스케이트 타듯
  다리를 뻗은 자세로 고정돼 있다."
- 원인 확인: `Enemy_2Legs` 등 5개 FBX 전부 `AnimationClipSettings.loopTime = false`
  (Unity 기본 FBX 임포트 값)로 들어와 있었음 - `Idle`/`Walk`/`Run`처럼 계속 반복돼야 하는
  이동 클립까지 전부 "한 번 재생하고 마지막 프레임에서 정지"하는 설정이었던 것.
  `EnemyAnimationController`가 `CrossFadeInFixedTime`으로 상태에 진입시키는 시점엔 정상
  재생되지만, Run 클립(0.63초)이 다리를 뻗은 중간 프레임에서 끝나버리면서 그 자세로 굳어
  보였던 것 - "스케이트" 표현이 정확히 이 증상과 일치.
- 수정: `ModelImporter.defaultClipAnimations`를 읽어 이름이 `Idle`/`Walk`/`Run`으로 끝나는
  클립만 `loopTime = true`로 바꾼 뒤 `clipAnimations`에 재할당 + `SaveAndReimport()` -
  5개 FBX(`Enemy_2Legs`/`Enemy_2Legs_Gun`/`Enemy_Large_Gun`/`Enemy_Flying`/
  `Enemy_Flying_Gun`) 전부 적용. `Attack`/`Shoot`/`Death`/`Jump`는 코드가 매번 명시적으로
  재트리거하는 1회성 동작이라 의도적으로 그대로(loopTime=false) 둠. `AnimatorController`
  자산은 클립을 참조만 하고 있어 재생성 불필요 - 임포트 설정만 바꾸면 바로 반영됨.

### 테스트 결과
- [x] `AnimationUtility.GetAnimationClipSettings`로 5개 FBX 전부 Idle/Walk/Run=true,
      Attack/Shoot/Death/Jump=false 확정 확인
- [x] EditMode 132/132 (변경 없음 확인용 재실행)
- [ ] 실제로 Chase 상태에서 다리가 반복해서 걷는 것처럼 보이는지는 **이 자동화 환경의
      기존 한계로 확인 불가** - Play Mode에 진입해 적을 플레이어 근처에 스폰해도
      `Time.time`이 0에서 멈춰 진행되지 않음(2026-08-18에 이미 기록된 "에디터가 포커스를
      못 받는 자동화 환경에서 Time.frameCount가 안 움직이는" 문제와 동일 패턴) - 사람이
      직접 플레이해서 확인 필요

### 다음 작업
- 유저 실플레이로 Chase 애니메이션이 실제로 반복 재생되는지 확인.

---

## 2026-09-14 (7)

### 진행 내용
- **Phase 4(아트 리소스 적용) 착수 — 적 6종 비주얼을 캡슐 프리미티브에서 `cyberpunk_game_kit`
  로봇 모델 + 애니메이션으로 교체.**
- 사전 조사: `Art/Props/cyberpunk_game_kit/Enemies/`의 6개 FBX(`Enemy_2Legs`,
  `Enemy_2Legs_Gun`, `Enemy_Large`, `Enemy_Large_Gun`, `Enemy_Flying`, `Enemy_Flying_Gun`)를
  Unity RunCommand로 직접 검사 - 파일 목록만으로는 `.anim` 에셋이 안 보여 정적 목업인 줄
  알았으나, 실제로는 SkinnedMeshRenderer + Generic 리그 + FBX 내장 애니메이션 클립
  (Idle/Walk/Run/Attack/Shoot/Jump/Death 등 12~16개)이 있는 걸 확인. 유저에게 미리보기
  스크린샷을 보여준 결과 "2족 보행 인간형"이 아니라 둥근 몸통 + 다리형 소형 드론이라는
  실제 형태를 확인(이름과 실제 실루엣이 다름) - 여기서 색 틴트 계획을 재검토하게 됨.
- 유저 결정: (1) 비율은 임의 판단에 맡김, (2) 원래 안 쓰기로 했던 `Enemy_Flying`/
  `Enemy_Flying_Gun`도 "공중 적이 전혀 없었다"는 이유로 포함시켜 종류를 더 채우기로 함.
- **매핑 확정**(근접/원거리/보스 스타일과 테마 적합성 기준):
  - `Enemy_Basic`, `Enemy_RelentlessMelee` → `Enemy_2Legs` (지상)
  - `Enemy_ErraticMelee` → `Enemy_Flying` (호버링 — "erratic 흔들림"과 "떠다니는 드론"
    테마가 잘 맞음)
  - `Enemy_Ranged` → `Enemy_2Legs_Gun` (지상)
  - `Enemy_BurstRanged` → `Enemy_Flying_Gun` (호버링 — "burst 연사 드론" 테마)
  - `Boss_Basic` → `Enemy_Large_Gun` (지상, 덩치 큰 실루엣)
- 스케일/피벗 보정: `AnimationClip.SampleAnimation`으로 Idle 포즈를 직접 샘플링해 각 모델의
  실제 서 있는 높이/발 위치(로컬 y=0)를 측정 → 전 모델 공통 배율 2.8x, 지상형은 루트(캡슐
  중심) 기준 y=-1(캡슐 바닥에 발이 닿음), 호버형은 y=-0.5(바닥에서 0.5유닛 띄움)로 결정.
  `SceneView_CaptureMultiAngleSceneView`로 실제 렌더링 결과를 반복 확인하며 결정(순수 치수
  계산만으로는 실제 실루엣이 예상과 달라 신뢰 불가했음 - 폭이 키보다 넓게 나오는 등).
- 코드 변경:
  - `EnemyAnimationCue`(enum: Attack/Shoot) 신규 - `IAttackPattern`에
    `AnimationCue { get; }` 추가, `MeleeAttackPattern`(Attack) /
    `RangedAttackPattern`/`BurstRangedAttackPattern`/`RetreatAndShootPattern`(모두 Shoot)
    구현.
  - `EnemyAnimationController`(MonoBehaviour) 신규 - `Animator.CrossFadeInFixedTime`으로
    상태 이름("Idle"/"Walk"/"Run"/"Attack"/"Shoot")만 알고 실제 클립/컨트롤러는 모델별로
    다르게 주입되는 얇은 브리지. `EnemyStateMachine.Animation`(nullable, 비주얼 없는 테스트
    더미는 자동으로 no-op)으로 노출.
  - `EnemyIdleState`/`EnemyPatrolState`/`EnemyChaseState`의 `Enter()`에
    PlayIdle/PlayWalk/PlayRun 각각 추가, `EnemyAttackState.Tick()`의 `selected.Execute()`
    직후 `Play(selected.AnimationCue)` 추가.
  - **의도적으로 범위 밖으로 뺀 것**: Stunned(경직) 애니메이션(대응 클립 자체가 없음),
    Death 애니메이션(`HealthComponent.HandleDied()`가 `Health.Died` 이벤트 한 프레임 안에
    바로 `ReturnToPool()`/`Destroy()`를 호출해 죽음 연출용 유예시간이 아예 없음 - 이건
    플레이어와 공유하는 `HealthComponent`의 사망 흐름 자체를 건드리는 별도 설계 작업이라
    이번 세션 범위 밖으로 명시적으로 분리).
- 에셋 작업(전부 `Unity_RunCommand` 스크립트로 처리, 수작업 없음):
  - `Assets/Prefabs/EnemyAnimators/`에 `AnimatorController` 5개 신규 생성
    (`AC_Enemy2Legs`/`AC_Enemy2LegsGun`/`AC_EnemyLargeGun`/`AC_EnemyFlying`/
    `AC_EnemyFlyingGun`) - 각 FBX에 내장된 클립을 이름 매칭으로 상태에 연결, 전이(transition)
    없이 이름 상태만 등록(코드가 `CrossFadeInFixedTime`으로 직접 상태를 지정하는 방식이라
    파라미터/전이 그래프 자체가 불필요).
  - 적 프리팹 6개(`Enemy_Basic`/`Enemy_ErraticMelee`/`Enemy_RelentlessMelee`/`Enemy_Ranged`/
    `Enemy_BurstRanged`/`Boss_Basic`) 전부 `PrefabUtility.LoadPrefabContents`로 열어
    루트의 `MeshFilter`/`MeshRenderer`(캡슐 프리미티브) 제거 → 해당 로봇 FBX를 중첩 프리팹으로
    자식 `Visual`에 추가(`Animator`+위 컨트롤러+`EnemyAnimationController` 포함) →
    `SaveAsPrefabAsset`. `CapsuleCollider`/`Rigidbody`/`EnemyStateMachine`/`HealthComponent`
    등 루트의 게임플레이 컴포넌트는 전혀 안 건드림.

### 테스트 결과
- [x] 컴파일 에러 없음 (매 단계 `Unity_ReadConsole`로 확인)
- [x] EditMode 132/132
- [x] PlayMode 36/36 - 1차 실행은 최초 진입 시 신규 머티리얼/셰이더 컴파일로 추정되는
      지연(약 10분, 이 시간 동안 RunCommand/ReadConsole 호출이 같이 멈춰 있었으나 콘솔에
      에러는 없었음)이 있었고 그 안에서 무관한 기존 플레이키 테스트
      (`ChaseState_SlowEnemyAcrossManyRepathCycles_...`) 1건만 실패, 캐시가 워밍업된 2차
      실행은 정상 속도로 36/36 클린
- [x] 실제 Play Mode 진입 → `Enemy_Basic` 프리팹을 룸 안에 스폰 → 콘솔 에러 없이 바닥
      타일 위에 정상 렌더링 + HP 바 부착 + Idle 애니메이션 재생 확인 (`SceneView_
      CaptureMultiAngleSceneView` 스크린샷으로 확인)
- [ ] 6종 각각의 Walk/Run/Attack/Shoot 전환이 실제 추격/공격 상황에서 자연스럽게 보이는지,
      호버형 2종의 "떠 있는" 느낌이 실제로 잘 읽히는지는 실플레이로 확인 필요 - 미확인/예정
- [ ] 스케일(2.8x)이 실제 플레이 화면에서 너무 작거나 크지 않은지 - 유저가 "일단 자연스러운
      걸로" 위임했으므로 실플레이 후 조정 여지 있음

### 이슈/메모
- 로봇 이름("2Legs")과 실제 형태(다리 여러 개인 둥근 드론)가 다름 - 스크린샷 없이 이름과
  치수만으로 판단했다면 완전히 다른(그리고 부적절한) 스케일/배치를 선택했을 것. Idle 클립의
  frame 0을 그대로 측정하면 무게중심이 한쪽으로 쏠린 과도기 포즈라 오해를 살 수 있어,
  `SampleAnimation(clip.length * 0.5f)`처럼 클립 중간 지점을 측정하는 편이 더 대표성 있는
  실루엣을 준다는 것도 확인.
- Play Mode 최초 진입 시 RunCommand/ReadConsole이 모두 수 분간 "실행 중"으로 걸린 현상 -
  콘솔에 에러가 없었고 재실행 시 정상 속도였던 것으로 보아 신규 텍스처/머티리얼의 최초
  셰이더 컴파일 비용으로 추정(진짜 무한루프/행이었다면 재실행도 똑같이 느렸을 것). 앞으로
  아트 에셋을 대량으로 처음 사용할 때 첫 Play Mode 진입은 오래 걸릴 수 있음을 감안할 것.

### 다음 작업
- 실플레이로 6종 애니메이션 전환 체감 + 스케일 확인, 필요하면 `Scale`/`GroundY`/`HoverY`
  상수(현재 적용 스크립트에만 존재 - 필요시 프리팹 재실행) 조정.
- Stunned/Death 애니메이션은 각각 별도 과제(클립 신규 제작 여부, `HealthComponent`
  사망 유예시간 설계)로 남겨둠.
- 플레이어 캐릭터 비주얼(`Character.fbx`, 클립 44개 보유)도 같은 방식으로 다음에 진행 가능.

---

## 2026-09-14 (6)

### 진행 내용
- 유저 피드백: "문제 있는 에셋을 콘솔/리스트에서 찾아서 Project 창으로 점프 → 거기서 다시
  더블클릭해서 인스펙터 열기"는 여전히 번거롭다, 애초에 원했던 건 **에셋 데이터 수정
  기능 자체를 커스텀 에디터 하나에 몰아넣는 것**이었다는 정정 → `DataValidationWindow`를
  "이슈 리스트 + Select 버튼" 구조에서 **좌측 에셋 목록 + 우측 인라인 편집기** 구조로
  전면 재작성. Project 창 왕복이 아예 사라짐.
- `DataValidationScanner.cs`에 `FindAllValidatableAssets()` 신규 - 이슈 유무와 무관하게
  프로젝트의 `IValidatable` 에셋 전부를 열거(기존 `Scan()`은 이제 이 메서드 위에서 이슈만
  걸러내는 얇은 래퍼로 리팩터, 스캔 루프가 두 곳에 따로 존재하지 않도록 유지).
- `DataValidationWindow.cs` 재작성:
  - 좌측: 타입별로 그룹핑된 에셋 이름 버튼 목록(검색어 필터 지원), 이슈 있는 항목은
    "⚠ 이름"으로 표시
  - 우측: 좌측에서 클릭한 에셋을 `UnityEditor.Editor.CreateEditor(asset)` +
    `editor.OnInspectorGUI()`로 그 자리에서 바로 렌더링 - Unity 기본 인스펙터가 쓰는
    바로 그 필드 그리기/Undo/직렬화 로직을 그대로 재사용하는 것이라 커스텀 필드 UI를
    타입별로 새로 짤 필요가 없음. 값을 바꾸면 그 자리에서 즉시 저장되고 `OnValidate()`도
    정상 발동.
  - 선택된 에셋의 실시간 검증 메시지(`DataValidation.CollectIssues`)를 인라인 편집기
    위에 경고 박스로 표시 - 필드를 고치면 다음 리페인트에 바로 사라짐(전체 리스캔 불필요).
  - 창을 닫거나 다른 에셋을 선택할 때 이전 `Editor` 인스턴스를 `DestroyImmediate`로 정리.
- 콘솔 커맨드(`RogueLike/Validate All Game Data`, `DataValidationMenu.cs`)는 손대지
  않고 그대로 유지 - "빠르게 한 번 전체 스윕"이 필요할 때는 여전히 유효.

### 테스트 결과
- [x] EditMode 132/132 (이번 변경도 Editor 전용 코드만 건드려서 런타임/테스트 대상 로직
      변경 없음)
- [x] 메뉴로 창 열기 → `Rescan()` 자동 실행 → 콘솔 에러/경고 없음 (RunCommand로 재현),
      창 닫기 정상 동작
- [ ] 좌측 목록에서 실제로 에셋을 클릭했을 때 우측에 인스펙터가 렌더링되고 필드 수정이
      실제로 저장되는지 - 미확인/예정. RunCommand 동적 어셈블리는 `internal` 타입
      (`DataValidationScanner`)을 볼 수 없고 `System.Reflection`도 차단돼 있어, 버튼
      클릭을 자동으로 재현할 방법이 없었음(GUI 이벤트 시뮬레이션 자체가 이 환경의
      한계) - 사람이 직접 클릭해서 확인 필요.

### 다음 작업
- 유저가 직접 `RogueLike > Data Validation Window`에서 아무 에셋이나 클릭해 우측에
  필드가 뜨는지, 값을 바꾸면 저장되는지, 검증 경고가 실시간으로 뜨고 사라지는지 확인.

### 추가 - 좌측 목록 그룹 기준을 타입 알파벳순 → 도메인별로 변경
유저가 실제로 써보고 "타입 이름순 말고 플레이어 관련/적 관련으로 묶어달라"고 요청.
- `Assets/Editor/DataValidationCategories.cs` 신규 - SO 타입을 Player/Enemy/Items/Room/
  Other 5개 버킷으로 분류하는 순수 매핑(`Categorize(ScriptableObject)`). 런타임 타입에
  카테고리 개념을 넣지 않고 에디터 전용 코드에만 둠 - 게임플레이 데이터 클래스가 "내가
  인스펙터에서 어떻게 묶여 보일지"를 알 필요는 없다고 판단.
  - Player: `WeaponData`/`CharacterArchetypeData`
  - Enemy: `EnemyData`/`AttackPatternData`(+4개 서브클래스)
  - Items: `ItemData`(+Passive/Grenade/EnhancedShot/Flamethrower)
  - Room: `RoomTileSet`/`RoomObstacleSet`
- `DataValidationWindow.DrawAssetList()`를 "카테고리 → 타입 → 에셋" 2단 그룹으로 재작성
  (기존 "타입 → 에셋" 1단에서 카테고리 레벨 추가, `EditorGUI.indentLevel`로 들여쓰기).
  표시 순서는 알파벳순이 아니라 `DataValidationCategories.Order`(Player→Enemy→Items→
  Room→Other) 고정 - 기획자가 시스템을 떠올리는 순서에 맞춤.

### 테스트 결과 (추가분)
- [x] EditMode 132/132 (역시 Editor 전용 코드만 변경)
- [x] 메뉴로 창 재실행 → 콘솔 에러/경고 없음
- [ ] 실제 좌측 목록이 Player/Enemy/Items/Room 순으로 접혀 보이는지는 사람이 직접 확인 필요

---

## 2026-09-14 (4)

### 진행 내용
- §5(커스텀 에디터 툴링 — SO 검증 패스) 착수. 하이브리드 방식(인스펙터 즉시 경고 + 프로젝트
  전체 스캔 메뉴, 둘 다 같은 순수 로직 재사용)으로 유저 승인 받고 진행.
- `Assets/Scripts/Core/IValidatable.cs` 신규 — `void Validate(List<string> errors)` 하나뿐인
  인터페이스. UnityEditor 의존성이 전혀 없는 순수 로직이라 EditMode 유닛테스트로 직접
  검증 가능.
- `Assets/Scripts/Core/DataValidation.cs` 신규 — `LogIssues(asset, validatable)`(각 에셋의
  `OnValidate()`에서 호출, 인스펙터에 즉시 경고) / `CollectIssues(validatable)`(에디터 메뉴
  스캔이 사용) 두 헬퍼. 둘 다 같은 `Validate()`를 재사용하므로 검사 로직은 한 곳에만 존재.
- `IValidatable` 적용 대상 7개 SO 타입: `WeaponData`(+`RangedWeaponData`), `ItemData`,
  `AttackPatternData`(+`Ranged`/`Melee`/`BurstRanged`/`RetreatAndShoot` 4개 서브클래스),
  `EnemyData`, `RoomTileSet`, `RoomObstacleSet`, `CharacterArchetypeData`. 각각
  `public virtual/override void Validate(List<string> errors)` + `private void OnValidate()
  => DataValidation.LogIssues(this, this)` 추가. 참조 누락(프리팹/무기/공격 패턴 null 또는
  빈 배열)과 수치 불변식(속도/범위/쿨다운이 0 이하, `EnemyData.LeashRange < DetectionRange`
  같은 앞뒤가 안 맞는 조합)을 검사. `MeleeWeaponData`/`PassiveItemData`/액티브 아이템
  데이터 3종(Grenade/EnhancedShot/Flamethrower)은 검사할 만한 자기 필드가 없어(전부
  GameBalance JSON 소유) 베이스 클래스 검사만 상속받고 오버라이드 없음.
- `Assets/Editor/DataValidationMenu.cs` 신규 — 프로젝트에 처음 생기는 Editor 전용 코드
  (매직 폴더, 별도 asmdef 불필요). 메뉴 `RogueLike/Validate All Game Data`가
  `AssetDatabase.FindAssets("t:ScriptableObject")`로 전체를 훑어 `IValidatable`인 것만
  같은 `Validate()`로 검사하고 콘솔에 한 번에 리포트 - 인터페이스 기반이라 나중에 SO
  타입이 늘어도 스캐너 코드는 안 건드려도 됨. 실행 결과: 기존 23개 에셋 전부 0 issue(현재
  프로젝트 데이터가 이미 깨끗하다는 뜻 - 새 버그를 못 잡은 게 아니라 예방 도구가 정상
  동작함을 확인한 것).
- `Assets/Tests/EditMode/DataValidationTests.cs` 신규 11개 - `EnemyData`(정상값/빈 배열/
  null 요소/LeashRange<DetectionRange), `RoomTileSet`(누락 4종/전부 할당), `RoomObstacleSet`
  (누락/배열 내 null), `CharacterArchetypeData`(이름·무기 누락), `RangedWeaponData`(스폰
  거리 0 이하), `AttackPatternData` 베이스+서브클래스 동시 검사(쿨다운 음수 + lungeSpeed
  음수 둘 다 잡히는지). 리플렉션 기반 `SetField` 헬퍼는 `Assets/Tests/PlayMode/
  TestHelpers.cs`의 `SetPrivateField`와 동일한 패턴(베이스 클래스 private 필드까지
  타입 계층을 걸어 올라가며 탐색)을 EditMode 쪽에 맞게 새로 작성(테스트 asmdef가 달라
  기존 걸 그대로 재사용은 못 함).
- 로드맵에 있던 "여유 시" 항목인 층 그래프 비주얼라이저(EditorWindow)는 이번 세션 범위에서
  제외 - 유저와 사전 합의된 대로 별도 세션으로 이월.

### 테스트 결과
- [x] EditMode 132/132 (기존 121 + 신규 `DataValidationTests` 11)
- [x] PlayMode 36/36 클린 (재실행 없이 1회 통과)
- [x] `RogueLike/Validate All Game Data` 메뉴 실행 확인: "checked 23 asset(s), found 0
      issue(s)" - 스캐너 자체가 정상적으로 전체 에셋을 순회하고 있음을 확인

### 이슈/메모
- `OnValidate()`는 에디터 전용 콜백이라 그냥 정의만 해두면 되고 `#if UNITY_EDITOR` 가드가
  필요 없음(빌드에선 아예 호출되지 않음) - 실제로 UnityEditor 네임스페이스를 쓰는 코드는
  `Validate()`/`OnValidate()`가 아니라 `Assets/Editor/`의 스캐너 쪽뿐이라 메인 어셈블리는
  깨끗하게 유지됨.
- `knockbackResistance`([Range(0,1)])처럼 이미 Unity 어트리뷰트가 인스펙터 단에서 막아주는
  필드는 중복 검사를 추가하지 않음 - 검사 대상은 "인스펙터 슬라이더/필드로는 못 막는" 참조
  누락과 크로스필드 불변식으로 한정.

### 다음 작업
- 여유 있을 때 §5의 나머지 절반(층 그래프 비주얼라이저, EditorWindow) 검토.
- 로드맵 §6(세이브/로드)로 넘어갈지, Phase 4(아트 리소스)를 먼저 병행할지 결정 필요.

---

## 2026-09-14 (3)

### 진행 내용
- 대시 중 일반 공격(근접/원거리)·액티브 아이템 스킬을 실행할 수 없고, 대시(+겹침 보정 꼬리)가
  완전히 끝나야 다시 가능하도록 제한 추가. 유저가 먼저 "공격 중 대시 불가/대시 중 공격 불가"
  상호 차단이 있는지 물어와 검토한 결과 둘 다 없음을 확인(근접/원거리 공격은 지속시간 있는
  모션 자체가 없어 "공격 중"이라는 상태가 존재하지 않아 반대 방향은 구조적으로 성립 불가) →
  유저가 "대시 → 공격/스킬 차단" 단방향만 요청.
- `PlayerDodgeRoll`에 `IsDodgeRolling`(`_isDodgeRolling || _isCorrectingOverlap`) 공개
  프로퍼티 신규. `PlayerCombat`/`ActiveItemController`가 `[RequireComponent(typeof(
  PlayerDodgeRoll))]`로 같은 GameObject의 이 컴포넌트를 직접 참조(기존에 `PlayerDodgeRoll`이
  `PlayerMovement.enabled`를 직접 토글하는 것과 동일한 형제-컴포넌트 참조 패턴)해서
  `OnAttackPerformed`/`OnActivatePressed` 진입 시점에 차단.
- 차단된 입력은 버퍼링하지 않고 그냥 버림(직전에 만든 `InputBuffer`는 재사용하지 않음) -
  쿨다운 타이밍 실수를 구제하는 성격과 달리 "대시 중엔 원천 차단"이라는 의도적 규칙이라,
  자동 발동시키면 오히려 안 누른 입력이 뒤늦게 나가는 위화감이 생길 수 있다고 판단해 유저
  승인 하에 이 방향으로 결정. `OnActivateReleased`는 차단하지 않음(대시 시작 전에 이미
  활성화된 화염방사기가 대시 중에도 정상적으로 해제될 수 있어야 함).
- `IWeapon`/`IActiveItem` 구현체(`RangedWeapon`/`MeleeWeapon`/`GrenadeActiveItem`/
  `EnhancedShotActiveItem`/`FlamethrowerActiveItem`)는 전혀 수정하지 않음 - 차단 지점을
  플레이어 레벨 컨트롤러(`PlayerCombat`/`ActiveItemController`)에 둬서 무기/스킬 종류와
  무관하게 한 곳씩만 고치면 되도록 함.

### 테스트 결과
- [x] EditMode 121/121
- [x] PlayMode 36/36 - 중간에 `ChaseState_SlowEnemyAcrossManyRepathCycles_MakesSteadyProgress
      InsteadOfOscillating`이 스위트 전체 실행 중 2회 연속 실패했으나, 이 테스트만 단독
      필터링해 실행하면 통과(1/1) - `MakePlayer` 테스트 헬퍼는 컴포넌트가 하나도 없는 빈
      GameObject라 이번 변경(`RequireComponent`)과 인과관계가 있을 수 없음을 코드로 확인,
      2026-09-14 (1)에서 이미 기록한 "스위트 순서 민감 플레이키니스" 네 번째 사례로 확정.
      최종 재실행 36/36 클린.
- [ ] 실제 Play Mode에서 대시 중 공격/스킬 버튼이 먹통이 되는지, 대시가 끝나자마자 바로
      다시 가능해지는지 체감 확인 - 미확인/예정

### 다음 작업
- 실플레이로 대시 중 차단 체감 확인. "그냥 버림" 대신 버퍼링이 더 나아 보이면 전환 검토.
- §4b(프레임레이트 독립성)는 아직 미착수.

---

## 2026-09-14 (2)

### 진행 내용
- §4 입력 버퍼링(로드맵 확정 순번 4번) 착수. 대시/원거리 공격/액티브 아이템(강화탄·수류탄·
  화염방사기) 5곳에, 쿨다운으로 막힌 입력을 짧은 시간(0.15초) 동안 기억해뒀다가 조건이
  풀리면 자동 실행하는 방식을 적용.
- `Assets/Scripts/Core/InputBuffer.cs` 신규 — countdown 기반 순수 struct(Record/Tick/
  TryConsume). 기존 `_cooldownRemaining -= deltaTime` 패턴과 같은 스타일이라 각 호출부의
  기존 Tick()에 그대로 얹음.
- 적용 지점: `PlayerDodgeRoll`(대시, `OnDodgeRollPerformed`→`StartDash` 분리),
  `RangedWeapon`(`Attack`→`Fire` 분리), `EnhancedShotActiveItem`, `GrenadeActiveItem`
  (장전 시도만 - 이미 든 수류탄을 던지는 쪽은 게이트가 없어 버퍼링 대상 아님),
  `FlamethrowerActiveItem`(`OnActivatePressed`→`StartActivation` 분리).
- `MeleeWeapon`은 쿨다운 게이트 자체가 없어(항상 즉시 성립) 버퍼링 대상에서 제외.
- 버퍼 윈도우 값은 0.15초(150ms) — 격투 게임 커맨드 윈도우(60fps 기준 약 3~10프레임,
  ~50~166ms), 플랫포머 점프 버퍼(보통 4~6프레임, ~66~100ms) 등 액션 게임에서 흔히 쓰이는
  100~200ms 구간의 중간값으로 유저 승인 하에 임의 결정. `InputBuffer.DefaultWindowSeconds`
  한 곳만 고치면 5곳 전부에 반영되므로, 실플레이 체감으로 조정 예정.
- `Assets/Tests/EditMode/InputBufferTests.cs` 신규 6개(미대기/Record/즉시 소비/윈도우 내
  유지/윈도우 만료/기본값 범위 검증).

### 테스트 결과
- [x] EditMode 121/121 (`TestRunnerApi` 실행 - 기존 115 + 신규 6)
- [x] PlayMode 36/36 (`TestRunnerApi` 실행 2회 - 1회차에서
      `ChaseState_SlowEnemyAcrossManyRepathCycles_MakesSteadyProgressInsteadOfOscillating`
      1건 실패했으나 이번 변경은 Enemy/경로탐색 쪽 파일을 전혀 건드리지 않았고, 바로 위
      2026-09-14 (1) 세션에서 이미 "스위트 전체를 순서대로 돌릴 때만 발생하는 타이밍 민감
      플레이키" 3번째 사례로 기록된 것과 동일 테스트 → 재실행해 2회차 36/36 클린 확인)
- [ ] 실제 Play Mode에서 대시/원거리 공격/액티브 아이템 각각 쿨다운이 막 끝나기 직전에
      입력했을 때 자동으로 나가는지 체감 확인 - 미확인/예정(유저 실플레이 필요, 0.15초
      값 튜닝 대상)

### 이슈/메모
- 버퍼링 대상은 "쿨다운/재사용 대기"로 한정하고, 대시 중(`_isDodgeRolling`)·수류탄 보유
  중(`IsBusy`)·화염방사 중(`_isActive`) 같은 "동작 진행 중" 상태는 별도로 구분하지 않고
  같은 가드에 얹음 - 버퍼 윈도우(0.15초)가 이런 진행 상태의 지속시간보다 훨씬 짧아
  실질적으로는 쿨다운 케이스만 구제하므로 단순화해도 무방하다고 판단.

### 다음 작업
- 유저 실플레이로 0.15초 체감 확인 후 필요하면 `InputBuffer.DefaultWindowSeconds` 조정.
- §4 잔여 절반(프레임레이트 독립성)을 별도로 볼지, 아니면 §5 커스텀 에디터 툴링으로
  넘어갈지 결정.

---

## 2026-09-14

### 진행 내용

**RelentlessMelee 여섯 번째 원인: 장애물 이동 클램프가 항상 4×4 타일 박스로 뭉뚱그려져, 실제 콜라이더를 쓰는 시야 판정과 서로 다른 장애물 모델을 씀 ("대쉬하면 벽을 뚫고 온다")**
- 유저가 다섯 번째 수정 이후에도 라이브 플레이 중 재현: "적이 계속 벽을 비비고 있고, 내가
  위쪽으로 대쉬하면 얘가 벽을 뚫고 온다." 대쉬와 상관관계가 있다는 새 단서.
- `Time.frameCount`를 여러 번 확인해 시뮬레이션이 실제로 멈춰있던(에디터 창 포커스 상실)
  구간과 실제로 진행 중인 구간을 먼저 구분한 뒤(이번 세션에서 반복 확인된 환경적 아티팩트),
  `RunCommand`로 실제 위치/`FindPath`/`HasClearLineToPlayer`를 읽어 코드 경로를 추적.
- 근본 원인: `EnemyStateMachine.HasClearLineToPlayer()`는 `Physics.Linecast`로 인스턴스의
  **실제 콜라이더**를 검사하는 반면, 이동을 막는 `RoomObstacleBounds`/`ClampToInterior`의
  장애물 박스(`RoomController._obstacleBounds`)는 Solo형 장애물의 경우 실제 프리팹 크기와
  무관하게 항상 **타일 전체(4×4)** 크기로 등록되어 있었음(`BuildObstacles`, 이전 세션에
  "장애물이 항상 4x4로 취급된다"고 지적했던 바로 그 지점). 즉 시야 판정과 이동 클램프가
  서로 다른 두 장애물 모델을 씀. 대쉬처럼 플레이어 위치가 짧은 시간에 크게 바뀌는 상황에서
  `EnemyChaseState.PhysicsTick`의 `clearLine` 분기(177~181줄, 실제 콜라이더 기준 시야가
  열리면 A* 경로를 버리고 플레이어를 향해 직선 이동)가 켜지는 순간, 그 직선 이동이 화면상
  벽으로 보이는 지점(실제보다 부풀려진 4×4 클램프 박스와 실제 콜라이더 사이의 불일치 구간)을
  가로지르며 클램프에 걸리지 않고 통과하는 경우가 생김.
- 수정: `RoomController.BuildObstacles`가 Solo 장애물 인스턴스를 배치한 직후
  `Physics.SyncTransforms()`로 트랜스폼을 동기화하고, 그 인스턴스의 실제 `Collider.bounds`를
  읽어 이동 클램프 박스를 구성하도록 변경(`SoloClampBounds` 신규 메서드) - 콜라이더가 없는
  변형은 안전하게 기존 전체 타일 박스로 폴백. Connector 장애물은 2026-09-13에 이미 실제
  두께에 맞게 고쳐져 있어 이번 수정 대상에서 제외(그대로 유지).
- A* 경로탐색 그리드(`RoomPathfindingGrid`, 셀 단위 walkable/blocked)는 이번 수정과 무관 -
  여전히 셀 전체를 막힌 것으로 취급하며, 이번 수정은 오직 이동 클램프의 "실제로 어디까지가
  물리적으로 막힌 영역인가"만 시야 판정과 일치시킨 것.
- 회귀 테스트 추가: `ClampToInterior_SoloObstacleSmallerThanTile_ClampBoxMatchesRealColliderNotFullTile`
  - 1×1×1 콜라이더를 가진 테스트용 Solo 프리팹을 타일 중심에 배치하고, 실제 콜라이더
  바깥이지만 옛 전체-타일 가정 안쪽인 지점(로컬 z=1.5)이 더 이상 밀려나지 않는지 검증.
- [x] 재검증: EditMode 115/115. PlayMode는 신규 테스트 포함 36개 기준 4회 반복 실행 -
  35/36, 34/36, 35/36, 36/36(첫 클린 런은 재실행 없이 확정하지 않고 추가 확인). 실패는 항상
  기존에 알려진 Patrol/Idle 타이밍 플레이키 테스트(`Chase_VisitsEveryCheckpoint_With
  NobodyThere_GivesUpToPatrol`) 또는 다섯 번째 수정의 회귀 테스트(`ChaseState_
  SlowEnemyAcrossManyRepathCycles_MakesSteadyProgressInsteadOfOscillating`, 대칭 우회
  경로의 A* 동률 특성상 원래도 결과 변동폭이 있다고 문서화된 테스트)에서만 발생 - 이 두
  테스트가 정확히 관여하는 `RoomController` 코드 경로(전자는 obstacleSet 없이 room을
  빌드해 `BuildObstacles`가 조기 반환, 후자도 obstacleSet 미전달이라 이번 수정 대상인
  `SoloClampBounds`가 전혀 호출되지 않음)를 확인해 이번 수정과 무관함을 확정. 신규 테스트
  자체는 4회 모두, 그리고 별도 단독 실행 2회에서도 전부 통과.

### 테스트 결과
- [x] EditMode 115/115
- [x] PlayMode 36/36 (신규 테스트 포함) - 반복 실행 중 실패는 전부 기존에 알려진 무관한
  플레이키 테스트에서만 발생, 최종 1회는 완전 클린(36/36)

### 이슈/메모
- `ChaseState_SlowEnemyAcrossManyRepathCycles_MakesSteadyProgressInsteadOfOscillating`이
  전체 36개 스위트 안에서 돌 때는 이번 세션 확인 중 2/3회 실패(둘 다 "거리가 오히려 늘어남"
  형태)했지만, 이 테스트만 단독으로 필터링해 돌리면 2/2회 모두 통과 - 이 테스트가 관여하는
  코드 경로가 이번 수정과 무관함을 이미 확인했으므로, 스위트 전체를 순서대로 돌릴 때만
  발생하는 타이밍/부하 민감성(다른 테스트들이 남긴 상태나 프레임 타이밍 영향)으로 보임.
  기존에 알려진 Patrol/Idle 플레이키 2종에 이어 세 번째로 "정상적인 결함이 아닌 스위트
  실행 순서 민감성" 사례로 추가 기록.

### 다음 작업
- 로드맵 §3(방/장애물 배치 + A* 경로탐색) 관련 라이브 버그 6건 모두 수정 완료. 이제 §4
  입력 버퍼링 + 프레임레이트 독립성, 또는 §5 커스텀 에디터 툴링으로 넘어갈 시점.
- 유저 최종 판단(2026-09-14): 적이 매끄럽게 코너를 돌지는 못하지만(직선 MoveTo + 클램프
  방식의 태생적 한계, 코너 비빔 잔존) 결국 찾아오고 공격은 성립하므로, 움직임 스무딩은
  더 파지 않고 여기서 §3 마무리 — 이동 다듬기는 우선순위 낮은 백로그로 남김.

---

## 2026-09-13

### 진행 내용

**이어진 장애물을 "칸마다 완전한 블록" → "칸 중심을 잇는 얇은 세그먼트"로 교체 + 패턴 복잡도 확장**
- 유저가 전날(2026-09-12) 완성한 블록 반복 이어붙이기 방식을 1층에서 실제로 확인한 뒤
  "이어진 벽(ㅏ자 모양)의 가로세로 폭이 너무 두껍다"고 피드백, 본인이 직접 "블록 크기를
  1/4로 줄이면 어떨까"라는 대안까지 제시. 다만 단순 축소는 칸 사이에 다시 틈이 생기는
  문제(플레이어가 뚫고 지나갈 수 있는 실제 버그)를 재발시키므로, 대신 "칸이 아니라 인접한
  두 칸 중심 사이를 정확히 잇는 얇고 긴 기둥 하나"로 바꾸는 안을 제안해 승인받음 (상세 비교는
  `AI_Decision_Log.md` 2026-09-13).
- `ObstaclePlacement`에 `ObstacleShape{Solo, Connector}` 재도입 - 이번엔 칸마다 다른 메시를
  고르던 예전 조각 카탈로그가 아니라 "완전한 블록이냐 얇은 연결 세그먼트냐"의 2지 분류이고,
  Connector의 방향도 2가지(동/북)뿐. `LocalCell`/`RotationY` 필드 재해석만으로 표현(필드 추가
  없음): Connector는 `LocalCell`이 "두 칸 중 작은 쪽", `RotationY`가 90=동쪽 이웃 연결,
  0=북쪽 이웃 연결.
- `ProceduralFloorGenerator.PlacementsFromPattern`: `#`(fence) 칸끼리 정준 좌표계에서 동/북
  인접 여부를 먼저 검사(회전은 인접 관계를 보존하므로 회전 전에 검사해도 무방) → 인접한 쌍마다
  Connector 배치 하나 생성. 코너/T자/십자 교차 칸은 자연히 같은 `LocalCell`을 공유하는 여러
  Connector가 겹치며 표현됨 - 전용 코너/T자 메시가 전혀 필요 없어짐.
- 신규 프리팹 `Block_Stone_Connector`: `Block_Stone`을 그대로 복제 후 로컬 스케일의 X축만
  0.25배(정확히 유저가 제안한 "1/4"). `Block_Stone`의 메시가 등방(정육면체)이라 회전으로 인한
  로컬-월드 축 대응을 수동 계산할 필요 없이 X만 줄이면 세계 좌표 기준 1×4×4 얇은 기둥이 그대로
  나옴을 RunCommand로 확인(콜라이더 world bounds 실측 (1,4,4)).
- `RoomObstacleSet`에 `connectorPrefab` 필드 추가(`prefabs` 배열은 Solo 전용으로 유지),
  `RoomObstacleSet_Solo.asset`에 와이어링. `RoomController.BuildObstacles`: Connector는 두 칸
  중심의 정확한 중점에 배치, 적 컨테인먼트 바운드도 칸 전체(4×4)가 아니라 세그먼트 실제
  폭(4×1 또는 1×4)만큼만 추가 - Solo는 기존 로직(4×4 바운드) 그대로 유지.
- 패턴 라이브러리에 신규 4종 추가(유저가 "더 복잡한 패턴" 예시로 든 "대각선 두 줄" 포함):
  정사각형 `FenceCrossHub`(사방 교차 Connector), ㄱ자 `PlusHub`(동일한 사방 교차), ㄱ자
  `DiagonalDoubleScatter`(대각선으로 흩어진 단독 블록 두 줄 - "이어진 선"이 아니라 "점처럼
  흩어진 대각선"이라는 유저의 정정을 반영, 대각선 인접은 Connector로 이을 수 없음). 정사각형
  방은 클리어런스 제외 시 3×3칸뿐이라 폭이 필요한 대각선 패턴은 ㄱ자 방(실질 7×7 가용 영역)에만
  배치.

**적 시야(Line of Sight) 추가 + 발사체 벽 차단 검증 (계획대로 A* 이전에 선행)**
- 설계는 사전 합의된 A안 그대로: `EnemyStateMachine.CanSeePlayer(range)` 한 곳에만 손을 댐 -
  기존 거리 체크를 통과한 뒤 `Physics.Linecast`로 적↔플레이어 사이를 검사, 막혔으면 `false`.
  이 메서드가 Chase/Attack/Idle/Patrol 상태와 공격 패턴 4종까지 총 8곳에서 이미 유일한
  "보인다" 판정 지점이라 호출부는 전혀 안 건드림.
- 차단 판정은 새 레이어를 만들지 않고 `WallClamp.ClampDirection`이 이미 쓰는 관례를 그대로
  재사용: 레이어 `~0` + `QueryTriggerInteraction.Ignore`로 캐스트하고, 히트한 콜라이더에
  `Rigidbody`가 없으면(=벽/장애물/잠긴 문 같은 정적 지형) 시야 차단으로 취급. `Rigidbody`가
  있으면(적/플레이어 캡슐) 차단하지 않음 - 적끼리 서로의 시야를 가리는 일이 없음. 신규 레이어
  세팅이나 프리팹 재배정이 전혀 필요 없었음.
- 플레이어/적 캡슐 프리팹의 `CapsuleCollider.center`가 둘 다 `(0,0,0)`(피벗 = 몸통 중심)이라
  레이캐스트 시작/끝점에 별도 눈높이 오프셋을 줄 필요가 없음을 프리팹 값 확인 후 결정.
- 발사체 벽 차단은 코드 변경 없이 기존 구조로 이미 동작함을 확인만 함: `Projectile.
  OnTriggerEnter`는 owner 타입(플레이어/적)을 전혀 구분하지 않고 "owner 자신도, 다른
  Projectile도 아닌 콜라이더에 닿으면 소멸"이라 벽/장애물(non-trigger, Rigidbody 없음)에
  플레이어 발사체든 적 발사체든 동일하게 막힘.

### 테스트 결과
**플레이어-적 스폰 겹침 + 유휴 상태 잔여 속도 드리프트 수정 (유저 실플레이 중 발견)**
- 유저가 1층 근접 전투방에서 겪은 두 증상을 보고: (a) 입장 시 스폰된 적과 플레이어가 겹쳐
  "비벼지는" 현상, (b) 적을 몇 마리 잡은 뒤 아무 조작 없이도 플레이어가 화면 위쪽으로 천천히
  계속 밀리는 현상. 원인 분석 결과 두 개의 별개 구멍이 연쇄된 것으로 확인 (상세 비교는
  `AI_Decision_Log.md` 2026-09-13 (2)):
  1. `EnemySpawner.SpawnRoom`이 스폰 위치를 방 벽/장애물 기준으로만 클램프하고 플레이어의
     실제 현재 위치와의 거리는 전혀 검사하지 않아, 근접 전투방에서 적이 플레이어 위에 그대로
     스폰될 수 있었음.
  2. 플레이어 Rigidbody는 `m_LinearDamping: 0`(감쇠 없음)인데 `PlayerMovement.FixedUpdate`는
     입력이 0이어도 `MovePosition(현재위치)`만 호출할 뿐 `Rigidbody.linearVelocity`를 절대
     리셋하지 않음 - 위 스폰 겹침처럼 물리 솔버가 접촉 시 부여한 속도가 있으면, 대시
     로직(`PlayerDodgeRoll`)과 달리 아무것도 그 속도를 지우지 않아 등속으로 무한히 남음.
- 수정 1(`EnemySpawner`): 벽 클램프 직후 `ClearOfPlayer` 단계 추가 - 스폰 위치가 플레이어와
  (두 캡슐 반지름 합 + 여유 마진) 미만으로 가까우면 플레이어→스폰위치 방향으로 필요한 만큼만
  밀어내고 다시 방 경계로 클램프. 정확히 같은 좌표(거리 0)인 엣지 케이스는 임의 방향
  (`Vector3.forward`)으로 폴백.
- 수정 2(`PlayerMovement`): 이동 방식을 `MovePosition` → `Rigidbody.linearVelocity` 직접
  구동으로 전환(입력 0이면 매 FixedUpdate마다 속도를 정확히 0으로 덮어씀) - `PlayerDodgeRoll`
  대시가 이미 쓰는 것과 동일한 관례로 통일. `WallClamp.ClampDirection`으로 벽 방향 성분을
  걸러내는 로직은 그대로 유지(밀어붙이면 멈춤, 스치면 벽을 따라 슬라이드).
- 실플레이 중 부가로 발견했던 넉백 관통도 이번에 같이 수정: 원인은
  `EnemyStunnedState.PhysicsTick`의 넉백이 PhysX 충돌이 아니라 `Lerp` + `RoomController.
  ClampToInterior`(순수 위치 클램프, AABB 기준)로 구현되어 있어 - 한 프레임의 lerp 이동량이
  벽 두께(1유닛)보다 크면 시작점과 끝점 둘 다 벽 바깥이라 클램프가 "통과 자체"를 못 봄
  (경로를 따라가며 검사하는 스윕 방식이 아니라 최종 위치만 사후 검사하는 방식의 한계).
  수정: `EnemyStunnedState.SetKnockback`에서 넉백 목표 지점을 계산하는 시점에 플레이어
  걷기가 이미 쓰는 `WallClamp.ClampDirection`(캡슐 스윕)을 그대로 재사용해 전체 넉백 거리를
  한 번에 스윕 검사 - 매 프레임 재검사가 아니라 넉백 시작 시 한 번만 계산하므로 `PhysicsTick`
  자체는 무수정. `EnemyStateMachine`에 `Capsule` 프로퍼티를 새로 노출해 넉백 계산이 실제 캡슐
  형상을 참조할 수 있게 함.

**C1(velocity 구동 전환) 수정 후 유저가 "초기 이동 속도가 눈에 띄게 느려진 것 같다"고 피드백
- 새 버그가 아니라 원래 있던 문제가 처음 드러난 것**
- 실측 결과 플레이어 캡슐이 바닥 콜라이더에 실제로 0.1유닛 파묻혀 있었음
  (`Physics.ComputePenetration` 확인). 바닥은 `Ground` 레이어의 진짜(트리거 아닌) 콜라이더고
  레이어 충돌 행렬은 전부 켜져 있어 물리적으로 충돌 중 - 그런데 플레이어 Rigidbody가
  `FreezePositionY` 제약이라 이 겹침을 절대 못 풂. 이전 `MovePosition` 방식은 매 프레임 위치를
  강제로 덮어써서 이 마찰이 드러날 자리가 없었는데, `linearVelocity` 직접 구동으로 바뀌면서
  진짜 PhysX 접촉 마찰이 매 스텝 이동량을 갉아먹기 시작한 것 (`Physics.Simulate`로 10스텝
  격리 측정: 마찰 없다면 1.2유닛 이동해야 하는데 실제로는 0.72유닛, 약 40% 손실).
- 수정: 유저와 상의 후 스폰/정지 높이를 조정하는 대신(눈에 보이는 팝/드롭 위험 우려)
  **Physics Layer Collision Matrix에서 `Default × Ground` 충돌 자체를 끔**
  (`Physics.IgnoreLayerCollision(Default, Ground, true)`를 에디터 모드에서 호출해
  `ProjectSettings/DynamicsManager.asset`에 영구 반영, 코드 무변경). 적/벽/장애물도 전부
  Default 레이어라 이 설정으로 다 같이 혜택 보는데, 벽/장애물은 정적이라 원래 영향 없고 적은
  아직 `MoveTo`(포지션 강제 이동) 방식이라 이 마찰이 원래도 안 드러나던 상태 그대로라
  회귀 없음. 위치값은 전혀 안 건드리므로 시각적 변화 없음.

**적 경로탐색(그리드 A*) 도입 — 로드맵 §3 마지막 항목**
- 사전 확정한 설계대로 진행: (1) Connector 얇은 세그먼트는 셀 전체가 아니라 두 칸 사이의
  간선(edge)만 차단, (2) 시야가 확보되면 직선 추격(기존 그대로, 저렴함) / 막혔을 때만 경로
  추종 - 오늘 만든 `CanSeePlayer`(시야) 작업이 정확히 이 게이팅에 재사용됨.
- 신규 `RoomPathfindingGrid`(순수 클래스, `RoomBounds`/`RoomObstacleBounds`와 동일하게 씬
  의존성 없음) - `ObstaclePlacement.LocalCell`과 같은 좌표계로 4방향 A* 수행. Solo는 칸 전체
  차단, Connector는 `LocalCell`↔이웃 칸 사이의 간선만 별도 `HashSet`으로 차단(칸 자체는
  걸어다닐 수 있음). 노치(L자 방) 칸은 `RoomController.BuildFloor`의 동일한 판정식을 그대로
  복제해 항상 통행 불가 처리. 오픈셋은 배열 선형 스캔(방 크기가 수십 칸 수준이라 힙 오버헤드가
  손해).
- `RoomController.Initialize`에서 지오메트리/장애물 빌드 직후 그리드 1회 생성해 캐싱,
  `FindPath(월드좌표, 월드좌표)`로 얇은 래퍼 노출(`ClampToInterior`와 동일한 위상의 룸 소유
  공간 쿼리) - 셀↔월드 변환은 `RoomController.BuildFloor`가 쓰는 것과 동일한 공식.
  `RoomData.Obstacles`가 원래부터 옵션 파라미터(기본 빈 리스트)라 장애물 없는 방도 그대로
  동작(전 칸이 열린 그리드).
- `EnemyChaseState`가 매 프레임 직선 추격 대신: `HasClearLineToPlayer()`(신규, 사거리 무관
  순수 시야 차단 여부)가 참이면 기존 직선 로직 그대로, 거짓이면 캐싱된 경로의 다음 웨이포인트로
  이동 - 0.3초 주기 또는 경로 소진 시 재탐색. 룸이 없거나 경로를 못 찾으면 플레이어 위치로
  직행하는 예전 동작으로 안전하게 폴백.
- **부수 발견/수정**: 오늘 오전 시야 작업에서 `CanSeePlayer`(사거리+차단 통합 판정)를
  `EnemyChaseState`의 리쉬(leash) 체크에도 그대로 썼는데, 그러면 추격 중 모퉁이에 잠깐
  가려지기만 해도 즉시 Patrol로 풀려버려 - A*가 존재할 이유 자체가 없어지는 상충이 발견됨.
  `EnemyStateMachine`에 순수 거리 전용 `IsPlayerWithinRange(range)`를 분리해 리쉬 체크는
  이걸로 교체(공격 전이/최초 발견 판정은 기존처럼 차단 포함 `CanSeePlayer` 유지) - 추격은
  거리로만 유지, 시야 차단 여부는 순수하게 "직선이냐 경로냐"만 결정하도록 역할을 나눔.

**같은 세션 (2) — 실플레이 재검증 중 발견: 근접 적이 십자형 장애물 뒤에 숨은 플레이어를
놓친 뒤 "경로탐색을 안 하는 것처럼" 보임 → 원인은 `leashRange` 값 자체가 방 크기 대비
너무 작았던 것 (A* 코드 문제 아님), + 후속으로 "마지막 목격 위치 수색" 행동 추가**
- 원인: `EnemyBasicData.leashRange = 9`인데 전투방은 28×28(정사각형)~36×16(병합)~40×40(보스)
  - 리쉬가 방 한 변보다도 작아서, 장애물 뒤에 숨어 시야가 막히기도 전에 이미 순수 직선
    거리만으로 추격이 풀려버림(A* 코드 자체는 실제 절차생성 방들에서 이미 `FindPath` 정상
    반환 확인됨 - 버그 아니라 밸런스 수치 문제였음).
- 유저와 논의 후 `leashRange: 9 → 12`(`EnemyBasicData.asset`)로 조정 + 근본적으로
  `EnemyChaseState`에 "마지막 목격 위치 수색" 행동 추가(새 상태 클래스 대신 기존 State
  확장 - 경로 추종 로직이 이미 있어서 재사용). `EnemyData`에 `searchDuration`(기본 4초)
  신규 필드 추가.
- `EnemyChaseState.Tick`: 시야 확보 중엔 기존 그대로(실시간 거리로 리쉬 감시 + Attack 전이).
  시야를 잃으면 실시간 거리 감시를 멈추고 셋 중 하나로만 포기 판단 - (a) 마지막 목격
  위치(`_lastKnownPlayerPosition`, 시야 확보 중 매 틱 갱신)에 도착했는데 없음, (b)
  `searchDuration` 초과, (c) 그새 실제 위치가 `leashRange × 2`(하드 컷오프, 코드 상수)보다
  멀어짐 - 이건 수색 중에도 남겨두는 안전장치. `EnemyChaseState.PhysicsTick`/`Repath`도
  경로 목표를 실시간 플레이어 위치 대신 이 스냅샷으로 교체(수색이지 전지적 추적이 아니게).
- 다른 적 데이터(`EnemyErraticMeleeData` 등)는 이번엔 안 건드림 - 신규 `searchDuration`
  필드는 직렬화 안 된 에셋도 C# 기본값(4)으로 정상 로드됨을 라이브로 확인.

**같은 세션 (3) — 실플레이 재검증 중 재발견: 근접 적이 바짝 붙어 쫓다가 코너에서 놓치면
수색을 한 걸음도 안 해보고 바로 Patrol로 빠짐 → "도착 판정이 시야를 잃은 즉시 이미
충족돼 있는" 근접전 특유의 엣지 케이스로 확인, 최소 수색 시간 추가로 수정**
- 원인: `_lastKnownPlayerPosition`은 시야가 확보된 마지막 틱의 플레이어 위치인데, 근접
  적은 공격 사거리(~1.2)까지 파고들며 쫓기 때문에 시야를 잃는 그 순간 이미 적이 그 위치
  바로 옆(`SearchArriveDistance` 1.5 이내)에 서 있는 경우가 대부분 - 그래서 수색 이동을
  시작하기도 전에 "도착했는데 없음" 조건이 즉시 참이 됨. 원거리에서 놓친 경우(방이 크거나
  거리를 두고 시야가 끊기는 경우)엔 원래 의도대로 몇 걸음 이동한 뒤 판정되어 문제 없었음 -
  근접 교전 특유의 좁은 사거리 때문에 생기는 엣지 케이스였음.
- 수정: `EnemyChaseState`에 `MinimumSearchTime`(0.5초, 코드 상수) 추가 - "도착했는데 없음"
  조건이 이 시간 이상 지난 뒤에만 성립하도록 게이트. 이미 도착해 있어도 최소 그 시간만큼은
  그 자리에서 버틴 뒤 포기하게 되어, 시야를 잃자마자 즉시 Patrol로 빠지는 일이 없어짐.
  나머지 두 포기 조건(시간 초과/하드 컷오프)은 무수정.
- 논의 중 나온 "계속 추적하는 적 타입을 따로 만드는 게 나은가"는 기각 - A*가 눈에 보이려면
  "절대 포기 안 함"이 필요조건이 아니라, 포기 조건(리쉬/도착 판정)이 실제 교전 거리와 안
  맞아서 A*가 발동할 틈도 없이 상태가 넘어가는 게 문제였을 뿐. 이번 수정으로 일반 적들도
  충분히 "경로를 따라 찾아오는" 모습을 보여줄 수 있을 것으로 판단, "포기 안 하는 적"은
  버그 수정이 아니라 순수 컨텐츠 다양성 차원의 별개 아이디어로 보류.

**같은 세션 (4) — 최소 수색 시간만으로는 "적이 이미 목표 지점에 있어서 갈 곳이 없는" 경우
(얇은 벽 옆에서 빠르게 빠지는 경우) 여전히 제자리에 서 있다가 돌아가는 것처럼 보임 →
"플레이어 이동 방향 예측"(A안) + "도착 후 주변 재확인"(B안, 실제 이동 버전)을 조합해 추가**
- A안: 시야가 확보된 동안 `_lastKnownPlayerVelocity`(플레이어 Rigidbody의 `linearVelocity`,
  `EnemyStateMachine.PlayerVelocity`로 신규 노출)도 같이 스냅샷 - 시야를 잃으면 목표를
  "마지막 위치" 그대로가 아니라 `마지막 위치 + 마지막 속도 × PredictionLookAheadTime(0.6초)`로
  약간 밀어서 계산. 플레이어가 실제로 이동 중이었다면 근접 상황에서도 실제로 몇 걸음은
  이동하게 됨.
- B안(제대로 된 버전): 예측 지점(체크포인트 0)에 도착했는데도 없으면 곧장 포기하지 않고,
  마지막 목격 위치 주변에 무작위로 2곳(`LookAroundPointCount`, 반경 `LookAroundRadius` 3유닛)을
  더 체크포인트로 잡아 순서대로 이동/확인. 모든 체크포인트를 다 확인했는데도 없으면 그때
  Patrol로 전환. 재사용성을 위해 `EnemyChaseState`가 이제 단일 목표가 아니라 체크포인트
  리스트(`_searchCheckpoints`/`_checkpointIndex`)를 순회하는 구조로 재구성 - 기존
  `Repath`/`NextWaypoint`/`MinimumSearchTime`/하드 컷오프 로직은 "현재 체크포인트가 무엇이냐"만
  바뀌었을 뿐 그대로 재사용됨.
- 구현 중 발견한 버그: `Enter()`가 `_lastKnownPlayerVelocity`를 실제 플레이어 속도가 아니라
  `Vector3.zero`로 하드코딩하고 있었음(위치만 제대로 스냅샷하고 속도는 빠뜨림) - 예측이
  전혀 안 먹히는 상태였던 것을 테스트 작성 중 발견, `_machine.PlayerVelocity`를 읽도록 수정.
- 지점 유효성 사전 검증은 안 함(경로를 못 찾는 지점이 나와도 `Repath`의 기존 null 처리가
  그대로 커버) - `PatrolState.Enter()`가 스폰 위치 주변 무작위 지점을 검증 없이 쓰는 것과
  동일한 철학.

**같은 세션 (5) — A* 길찾기를 직관적으로 보여줄 "한 번 발각되면 방 안에서는 끝까지 쫓는"
근접 적(구 모양) 신규 추가 — 1층 1~2마리(방당 최대 1), 2층부터 층당 2~4마리**
- 신규 `EnemyVariantType.RelentlessMelee`. "절대 포기 안 함"은 새 코드 분기 없이 순수
  데이터로 구현 - 전용 `EnemyRelentlessMeleeData.asset`의 `leashRange=200`/`searchDuration=300`이
  어떤 방의 대각선(보스방 40×40 기준 약 56.6)보다도, 수색 중 하드 컷오프(`leashRange×2`)보다도
  훨씬 커서 거리/시간 기반 포기 조건이 절대 안 걸림 - 방 자체를 못 벗어나는 건 기존
  `RoomController.ClampToInterior`가 그대로 담당. 나머지 스펙(이동속도, 근접 공격 패턴 등)은
  `EnemyBasicData`와 동일.
- 배치는 `VariantWeights`(슬롯마다 확률roll) 방식이 아니라 **정확한 개수 보장 후처리 패스**로
  구현 - `FloorRule.MinRelentlessEnemies/Max`(신규, `ProceduralRules.json`의 `relentlessCount`)를
  시드로 뽑아 그만큼의 **서로 다른** 전투방에 정확히 1마리씩 추가(요청한 방식과 동일하게 방당
  최대 1마리를 전 층 공통 규칙으로 채택 - 1층만 특별 취급하지 않고 "겹치지 않게 분산"을
  기본값으로 둠). 오늘 확립된 "핵심 알고리즘은 안 건드리고 후처리 패스만 추가" 컨벤션(장애물
  배치와 동일) 그대로 재사용 - `ProceduralFloorGenerator.ApplyRelentlessEnemies`가
  `FloorGraph.Validate` 통과 직후, `FloorLayout` 생성 직전에 실행되고, `RoomData`가 불변이라
  선택된 방은 기존 적 리스트에 하나를 더한 새 `RoomData`로 교체됨(장애물/커넥션/모양 등 다른
  필드는 그대로 이전). 전용 명명 RNG 스트림(`relentless.f{층}`)이라 기존 시드의 방 그래프/적
  구성은 전혀 안 바뀜.
- 시각적으로는 구(Unity 내장 Sphere 메시)로 구분, 물리 콜라이더는 다른 적과 동일한
  CapsuleCollider 유지(경로탐색/시야 코드가 `CapsuleCollider` 타입을 직접 참조하므로) - `Enemy_
  Basic.prefab`을 그대로 복제한 뒤 메시와 `EnemyStateMachine.data` 참조만 교체. Unity MCP로
  프리팹 복제(`Unity_ManageAsset` Duplicate) 후 `PrefabUtility.LoadPrefabContents`/
  `SerializedObject`로 수정, 씬의 `EnemySpawner.variantPools` 배열도 같은 방식(`SerializedObject`
  로 배열 엔트리 추가)으로 신규 풀(`EnemyPool_RelentlessMelee`)을 연결.
- 테스트 작성 중 발견한 순수 타이밍 결함(오늘 (4)의 체크포인트 테스트 2개) 수정: 무작위
  둘러보기 지점이 우연히 가까워서 여러 체크포인트가 한 번에 연쇄로 넘어가거나, Patrol 진입
  직후 같은 폴링 간격 안에서 Patrol→Idle까지 넘어가버리는 경우가 드물게 있었음 - 프레임 단위
  폴링 + "어느 시점에든 체크포인트가 전진하는 걸 관측했는가"로 판정 방식을 바꿔 타이밍에
  안전하게 만듦.

**끝까지 쫓아오는 적이 실제로는 안 따라오는 것처럼 보이던 문제 진단 + 진단용 로그 제거**
- 유저 피드백: "적어도 새로 만든 근접 적은 A* 알고리즘으로 길찾기 해서 내가 어디에 있든
  길찾기해서 따라와야되는거 아닌가? 현재는 잘 따라오고 있지 못하고 있는 걸로 보여." 자동화
  테스트(`Chase_TracksLivePositionWhileBlind_RepathsTowardNewPosition_NotFrozenLastKnown`)는
  이미 통과하고 있어서, 실제 에디터 Play Mode에서 재현하며 원인을 좁혀나감.
- 1차 진단용으로 `EnemyChaseState.PhysicsTick`에 임시 `Debug.Log("[DIAG] ...")`를 여러 지점에
  추가해 재현했더니 `_machine.Player == null`에서 즉시 리턴하는 것으로 보였음 - 그런데 이건
  재현 스크립트가 `pool.Get()` 직후 같은 동기 호출 안에서 프레임 하나도 안 지난 채 바로
  `ChangeState(ChaseState)`를 강제로 불러서 `EnemyStateMachine.Start()`(플레이어 참조를 채우는
  지점)가 아직 실행되지 않았던 것 - 실제 게임 흐름에서는 `Start()`가 먼저 여러 프레임 지난
  뒤에야 Chase로 전이되므로 절대 발생하지 않는, 순전히 테스트 스크립트 자체의 아티팩트였음.
  진단용 로그는 모두 제거.
- 이 결론을 실제 게임 흐름 그대로 재현해 재확인: `EnemySpawner.SpawnRoom`과 동일한 순서
  (`pool.Get` → 벽 클램프 → `SetRoom`/`SetSpawnPosition` → `Physics.SyncTransforms()`)로
  `EnemyPool_RelentlessMelee`에서 인스턴스를 하나 스폰해 Play Mode에서 자연 상태 전이를
  관찰 - 플레이어를 자동으로 감지해 Chase→Attack까지 정상 진행했고("Player took 5 damage
  from VERIFY_Relentless3." 로그로 실제 타격까지 확인), 이후 플레이어를 직선거리 13유닛
  밖으로 순간이동시켰더니 거리가 13.7 → 4.27로 계속 줄어드는 것을 확인해 시야를 잃어도
  마지막 목격 위치가 아니라 플레이어의 실시간 위치로 계속 재경로 탐색함을 라이브로 검증함.
  (중간에 한 번 `Time.timeScale=0`으로 멈춰 있어 안 움직이는 것처럼 보인 적이 있었는데, 이는
  방금 돌린 PlayMode 테스트 스위트가 종료 후 timeScale을 복구하지 않고 남긴 것으로, 추적
  로직과는 무관한 순수 테스트 부작용이었음 - `Time.timeScale = 1`로 되돌리자마자 정상 진행.)
- 별도로 재현 스크립트에서 `GameObject.FindFirstObjectByType<RoomController>()`로 아무 방이나
  집으면(StageManager가 층 전체 방을 미리 각자의 그리드 오프셋에 인스턴스화해두므로 씬에 방이
  여러 개 동시에 존재함) 플레이어가 있는 방이 아니라 완전히 다른 좌표의 방을 집어 그 방의
  `ClampToInterior` 결과로 순간이동하듯 보이는 현상도 발견 - 실제 코드(`EnemySpawner`)는 항상
  올바른 `RoomController` 참조를 인자로 받으므로 이 역시 테스트 스크립트만의 함정이었음.

**진짜 버그 발견: 발각 후 벽 뒤로 숨으면 안 따라오는 문제 (RelentlessMelee 한정이 아니라 근접 적 전체)**
- 유저가 실제 플레이 중 재현: "적어도 새로 만든 근접 적은... 한번 발각되고 벽 뒤로 숨었는데
  안따라온다. 이 적 로직만 발각 후 시야 처리 같은 거를 건너 뛰어야하는거 아닐까." 위의
  라이브 검증(플레이어를 순간이동시켜 확인)은 우연히 장애물이 없는 트인 공간에서만 진행돼
  이 경로를 건드리지 못했던 것으로 드러남.
- 근본 원인: `EnemyChaseState.PhysicsTick`의 "공격 사거리 안이면 멈춘다" 체크가 시야 확보
  여부와 무관하게 순수 직선 거리(`toPlayer.magnitude <= MaxAttackRange`)만 보고 있었음 -
  플레이어가 얇은 벽/장애물 바로 뒤에 숨어 직선 거리는 가깝지만 실제로는 막혀 있는 상황이면,
  벽 반대편으로 돌아가는 `HasClearLineToPlayer()`/`NextWaypoint` 경로탐색 분기까지 가지도
  못하고 그 자리에서 그냥 멈춰버림. 기존 테스트는 전부 플레이어를 공격 사거리보다 훨씬 멀리
  (8유닛 이상) 배치해서 이 분기를 검증한 적이 없었고, 테스트용 `EnemyData`도 기본적으로
  공격 패턴이 비어 있어(`MaxAttackRange=0`) 이 버그 자체가 애초에 발동할 수 없는 조건이었음.
- 수정: "공격 사거리 안이면 멈춘다" 체크에 `HasClearLineToPlayer()` 결과를 함께 게이트 -
  막혀 있으면 직선 거리와 무관하게 항상 경로탐색 분기로 진입하도록 변경(`EnemyChaseState.cs`).
  RelentlessMelee뿐 아니라 모든 근접 적에 적용되는 일반 수정.
- 회귀 테스트 `EnemyChasePathfindingTests.ChaseState_PlayerHidesBehindThinWall_WithinStraight
  LineAttackRange_StillRoutesAround` 신규 추가: Connector(칸 사이 얇은 간선 차단)로 나뉜 두
  칸에 적과 플레이어를 1유닛 간격(기본 공격 사거리 1.2 이내)으로 마주 세워두고, 적이 제자리에
  얼어붙지 않고 실제로 돌아가서 플레이어에게 도달하는지 검증. 작성 중 두 가지 부수적 결함도
  같이 드러나 고침: (1) `ObstaclePlacement` 생성자 인자 순서(`variantIndex`가 3번째 `int`,
  `rotationY`가 4번째 `float`)를 착각해 CS1503 컴파일 에러 - 순서 수정. (2) `TestHelpers.
  ResolveField`가 `target.GetType().GetField(...)`만 봐서 부모 클래스(`AttackPatternData`)에
  선언된 `private` 필드(`range`)를 못 찾음 - 타입 계층을 타고 올라가며 찾도록 수정.

### 테스트 결과
- [x] EditMode 115개 전부 통과 (`TestRunnerApi` in-process 실행, `TestResults.xml` 확인) -
      신규 `RoomPathfindingGridTests` 5개(빈 방 최단경로, Solo 장애물 우회, Connector는 칸이
      아니라 간선만 차단됨, 노치 칸은 항상 통행 불가, 완전히 둘러싸인 목표는 경로 없음) +
      `ProceduralFloorGeneratorTests` 신규 6개(RelentlessMelee: 규칙 없으면 절대 안 나옴 / 층
      전체 총 개수가 요청 범위 안 / 방당 최대 1마리 / 평소 적 구성에 추가로 얹힘 / 요청 개수가
      전투방 수보다 많아도 절대 안 넘침 / 스폰 위치가 방 안에 머무름) 포함
- [x] PlayMode 28개 전부 통과(연속 3회 재실행 모두 그린 - 무작위 지점 선택이 얽혀 있어 1회성
      통과가 아님을 확인, 그 과정에서 타이밍에 민감했던 검증 방식 2건도 같이 안정화) -
      `EnemyLineOfSightTests` 5개(사거리 내 시야 확보, 정적 벽에 막힘,
      아주 가까운 거리에서도 벽에 막힘, 사거리 밖이면 시야가 뚫려 있어도 실패, 다른 적 캐릭터는
      시야를 막지 않음) + `ProjectileTests` 신규 1개(정적 벽 콜라이더에 닿으면 관통하지 않고
      디스폰) + `EnemySpawnerPlayerClearanceTests` 신규 2개(플레이어와 겹치는 스폰 지점은
      밀려남 / 멀리 있는 스폰 지점은 그대로 유지) + `PlayerMovementResidualVelocityTests`
      신규 1개(입력 없을 때 기존에 걸려 있던 속도가 다음 FixedUpdate에 정확히 0이 됨) +
      `EnemyStunnedStateKnockbackTests` 신규 2개(얇은 벽을 향한 넉백은 관통하지 않고 멈춤 /
      빈 공간에서는 여전히 전체 거리 이동) + `EnemyChasePathfindingTests` 신규 1개(장애물이
      직선을 막고 있어도 우회해서 실제로 거리를 좁힘) + `EnemyChaseSearchTests` 5개(모든
      체크포인트를 확인했는데 없으면 Patrol / 이미 첫 체크포인트에 있어도 최소 수색 시간
      전엔 안 풀리고 그 이후엔 Patrol이 아니라 다음 체크포인트로 넘어감 / 플레이어가 이동
      중이었으면 첫 체크포인트가 마지막 위치 그대로가 아니라 속도 방향으로 예측되어 있음 /
      수색 시간 초과로 Patrol / 수색 도중 시야 재확보하면 Chase 유지) 포함
      - 검증 중 상태 폴링을 `WaitForFixedUpdate` 기준으로 했다가, Update(Tick 발생 지점)가
        FixedUpdate보다 더 자주 도는 구간에서 Patrol 진입 프레임을 건너뛰고 그다음 Patrol→Idle
        전이까지 관측해버리는 문제 발견 → `yield return null`(프레임당 1회, Tick과 같은
        빈도)로 폴링하도록 수정해 해결.
- [x] 실제 Play Mode(`Prototype.unity`)에서 라이브 검증 5건: (1) 플레이어 Rigidbody에
      `linearVelocity = (3,0,0)` 직접 주입 후 다음 프레임에 정확히 `(0,0,0)`으로 복귀 확인,
      (2) `Physics.GetIgnoreLayerCollision(Default, Ground)`가 런타임에 `true`로 반영됨을 확인,
      (3) `Physics.Simulate` 10스텝 격리 측정 결과 마찰 손실이 완전히 사라짐(수정 전 0.72유닛 →
      수정 후 1.20유닛, 마찰 없는 기대값과 정확히 일치), (4) 실제로 절차 생성된 전투방(1002~1004)
      전부에서 `RoomController.FindPath`가 예외 없이 유효한 경로를 반환함을 확인, (5) 실제
      전투방에 `EnterRoom()` 호출 후 스폰된 `Enemy_Basic`이 `leashRange=12, searchDuration=4`로
      정상 로드됨을 확인(신규 필드가 없는 다른 에셋도 C# 기본값 4로 정상 로드됨도 함께 확인),
      (6) `EnemyPool_RelentlessMelee`에서 실제로 스폰한 인스턴스가 구 메시 + `leashRange=200`으로
      정상 로드됨을 확인, 플레이어를 32유닛 밖으로 이동시키고 강제로 Chase 진입시켜도
      `IsPlayerWithinRange(leash)`가 `true`를 유지하고 실제로 그 방향으로 계속 이동함을 확인
      (일반 근접 적이라면 리쉬 12에서 이미 풀렸을 거리)
- [x] `DiagonalDoubleScatter`를 처음 대각선 간격 2칸으로 저작했다가
      `LShapePatterns_NeverDisconnectTheRoom_ForEveryNotchCorner`에서 실패 → 두 대각선
      사이에 낀 칸이 4방향 전부 막혀 고립되는 "대각선 체커보드 함정" 확인, 간격 3칸으로 수정
      후 재검증 통과
- [x] SceneView 멀티앵글 캡처로 사방 교차(+) Connector + 회전된/축정렬 Solo 블록을 한 방에
      직접 배치해 육안 확인 - 이음매 없이 매끈하게 이어진 얇은 십자형 벽, 정확한 90°/45° 블록
      회전 모두 정상
- [x] 진단용 로그 제거 후 재검증: EditMode 115/115, PlayMode 29/29 전부 통과(1차 PlayMode
      실행에서 `Chase_SearchTimesOut_GivesUpToPatrol_WhenLastKnownSpotUnreachable` 1건이
      Patrol 대신 Idle을 관측해 실패했으나, 이번 로그 제거와 무관한 재현 스크립트 수정
      없이 그대로 재실행했더니 29/29 전부 통과 - 기존에도 있던 Patrol→Idle 타이밍
      경쟁상태로 보이는 기존 결함성 플레이키니스, 이번 작업으로 인한 회귀 아님)
- [x] `EnemyPool_RelentlessMelee`로 실제 스폰한 인스턴스가 `EnemySpawner`와 동일한 순서로
      Play Mode에서 자연스럽게 Idle→Chase→Attack까지 전이하고 실제로 플레이어를 타격,
      시야 차단 후에도 마지막 목격 위치가 아니라 플레이어의 실시간 위치로 계속 접근함을
      라이브로 확인(상세 내용은 위 진행 내용 참고)
- [x] "벽 뒤 숨음" 수정 후 재검증: EditMode 115/115, PlayMode 30/30(신규
      `ChaseState_PlayerHidesBehindThinWall_WithinStraightLineAttackRange_StillRoutesAround`
      포함) 전부 통과. 중간에 신규 테스트 자체의 결함(생성자 인자 순서, 컷오프 기준 2유닛이
      실제 정상 동작인 1.83유닛 이동보다 과하게 엄격했던 것) 두 건과, 무관한 `Chase_
      VisitsEveryCheckpoint_WithNobodyThere_GivesUpToPatrol` 1건의 기존 Patrol/Idle
      타이밍 플레이키니스(재실행 시 통과)를 거쳐 최종 30/30 확정.

**"+"자 십자 장애물(FenceCrossHub)에서 반대편 플레이어를 못 쫓아오는 문제 - 진짜 원인은 A*가 아니라 "같은 칸" 판정**
- 유저가 실제 플레이 중(Play Mode) 재현: "+"자 모양 얇은 장애물이 있는 방에서 플레이어가
  좌상단 모서리, RelentlessMelee가 반대편 우하단 모서리에 있는데, 적이 우회하지 않고 자기
  쪽 벽에 계속 비비기만 함. 라이브 상태를 직접 조회해서 확인(`Room.FindPath(적,플레이어)`가
  딱 1개짜리 웨이포인트, 그것도 두 장애물 조각(`Connector_3_2`)이 막고 있는 방 정중앙 좌표를
  반환) - `ObstaclePatterns.json`의 `FenceCrossHub` 패턴(중앙 허브 칸 하나 + 사방 연결
  Connector 4개)을 그대로 씀.
- 근본 원인: Connector는 "칸과 칸 사이 간선"만 막지 칸 자체를 막지 않으므로, 허브 칸은
  이웃 4방향 전부 막혀 고립돼 있어도 그 칸 자체는 여전히 "통행 가능"으로 남아있음. 플레이어와
  적이 둘 다 그 허브 칸 한가운데 반경 2유닛 이내(칸 하나의 실제 물리 폭)에 서 있으면, 좌표를
  칸 인덱스로 반올림하는 `WorldToCell`이 둘을 정확히 같은 칸으로 계산해버림. `RoomPathfinding
  Grid.TryFindPath`는 "출발 칸==도착 칸"이면 두 칸 사이 간선이 막혔는지 확인도 안 하고 바로
  "이미 도착"으로 처리해 그 칸 중심 좌표 하나짜리 경로를 반환 - 그 중심이 하필 십자가 교차하는
  지점이라 적은 거기로 계속 걸어가려다 벽에 막혀 비비기만 함. A* 알고리즘이나 실시간 추적
  설계 자체의 결함이 아니라, 격자 한 칸보다 촘촘한 장애물(칸 하나를 4등분하는 +자)을 지금
  격자 모델이 표현 못 하는 구조적 허점이었음(검토 방식은 2건 사전 제시 후 유저가 1안 선택).
- 수정(1안): `RoomController.FindPath`에서 출발/도착 칸이 우연히 같아도, 각자의 연속 좌표가
  그 칸 중심 기준 어느 이웃 방향에 더 가까운지 보고 그 방향의 간선이 실제로 막혀 있으면(그리고
  이웃 칸 자체는 통행 가능하면) 그 이웃 칸으로 스냅 보정한 뒤 A*를 돌리도록 변경
  (`ResolveEffectiveCell` 신규). 막힌 간선이 없는 평범한 칸에서는 아무 영향 없음(안전한
  국소 보정). 이를 위해 `RoomPathfindingGrid.IsEdgeBlocked`를 private에서 public으로 공개.
- 회귀 테스트 2건 추가(`EnemyChasePathfindingTests.cs`): `FindPath_BothSidesOfCrossHubRound
  ToSameCell_ReturnsRouteInsteadOfTrivialSameCellPath`(허브 사방을 막는 Connector 4개를
  구성해 `Room.FindPath`가 더 이상 1개짜리 트리비얼 경로를 안 주고 실제 우회 경로를 반환하는지
  직접 검증) + `ChaseState_BothSidesOfCrossHubRoundToSameCell_StillRoutesAroundInsteadOf
  RubbingOnFence`(같은 배치에서 실제 `EnemyChaseState`가 우회해서 거리를 좁히는지 행동
  단위로 검증). 작성 중 테스트 자체의 실수 발견: 십자 벽 대역으로 세운 콜라이더 박스가
  너무 커서 양쪽 테스트 좌표를 둘 다 감싸버림 → `Physics.Linecast`는 두 끝점이 이미 같은
  콜라이더 안에 있으면 표면을 가로지르는 지점이 없어 막힘 판정이 안 나옴 - 박스를 두 좌표
  사이에 걸치되 양쪽 다 바깥에 남도록 축소해서 해결.

**세 번째 원인: Connector 클램프 박스의 가로/세로 축이 서로 뒤바뀌어 있었음**
- 유저가 같은 세션에서 곧바로 또 다른 재현 발견: 이번엔 "+"자 허브가 아니라 평범한 얇은
  Connector 벽 하나 앞에서, 길찾기 자체는 정상 경로를 계산하는데도 적이 실제로는 한 발짝도
  못 나아가고 벽을 계속 비비는 상태. 라이브 조회로 확인: `Room.FindPath(적→플레이어)`가
  올바른 2칸짜리 우회 경로((4,3)→(3,3)→(3,2))를 반환하고 있었는데도, 그 첫 걸음(칸 (4,3)
  중심 방향으로 0.03유닛 이동 시도)이 `room.ClampToInterior`에 의해 매번 제자리로 되돌아옴.
  (※ 중간에 한 번은 유니티 창이 OS 포커스를 못 받아 `Time.frameCount` 자체가 멈춰있던 상태를
  실제 버그로 오인할 뻔했음 - 포커스를 주고 재확인해 그건 오탐임을 먼저 걸러냄.)
- 근본 원인: `RoomController.BuildObstacles`가 Connector 하나당 클램프 전용(연속 좌표
  밀어내기용) 박스 크기를 계산하는 삼항연산자가 **가로/세로가 서로 뒤바뀌어** 있었음 - 원래는
  "그 벽을 가로질러 통과하는 축으로는 얇게, 벽이 걸쳐 있는 경계선 축으로는 칸 폭(4유닛)
  그대로"여야 하는데, 코드는 정확히 반대로 "경계선 축으로 얇게, 통과하는 축으로 칸 폭 그대로"
  되어 있었음. 그 결과 이 클램프 박스 하나가 실제로는 얇은 벽 하나의 자리가 아니라 **적이 서
  있는 칸의 중심부터 그 너머 칸의 중심까지, 못해도 한 칸 전체 깊이(4유닛+bodyRadius)를
  통째로 "장애물"로 취급**하고 있었음. 경로탐색(`RoomPathfindingGrid`)은 이 박스 크기를 전혀
  참조하지 않고 "어느 칸-칸 간선이 막혔는지"만 보므로 정상적으로 우회 경로를 계산하지만,
  실제 이동을 맡는 `MoveTo`의 연속 좌표 클램프(`RoomObstacleBounds.ClampLocal`)는 이
  잘못된(훨씬 큰) 박스를 그대로 써서 이미 계산된 정상 경로의 첫 걸음부터 계속 되돌려버림 -
  "길찾기는 맞는데 실제로는 한 발짝도 못 감"이 정확히 이 증상이었음.
- 이 버그는 지금까지의 모든 테스트가 못 잡고 있었음 - 테스트용 `TestObjects.BuildRoom`이
  실제 `RoomObstacleSet`을 한 번도 연결한 적이 없어서(`obstacleSet == null`이면
  `BuildObstacles`가 아예 조기 반환) 이 크기 계산 코드 자체가 지금까지 단 한 번도 실행된
  적이 없는, 실제 게임 씬에서만 재현되는 사각지대였음.
- 수정: `alongX`(동서 방향 Connector) 분기와 그 반대 분기의 `size` 값을 맞바꿈 - 이제
  동서 방향은 X축으로 얇고 Z축으로 칸 폭 그대로, 남북 방향은 그 반대로 정확히 표현됨.
- 회귀 테스트 추가를 위해 `TestObjects`에 `MakeObstacleSet()`(더미 `ConnectorPrefab` 하나만
  꽂은 최소 `RoomObstacleSet`) 헬퍼와 `BuildRoom`의 `obstacleSet` 선택 인자를 신규 추가 -
  이제부터 이 클램프 박스 생성 경로도 테스트로 실제 실행 가능. 신규 테스트
  `ClampToInterior_PositionWellInsideNeighbourCellAcrossAConnector_IsNotPushedBack`: 이웃
  칸 안쪽 깊숙한(경계 벽과는 전혀 안 닿는) 좌표가 클램프에 의해 밀려나지 않는지 검증 - 수정
  전 크기였다면 밀려났을 위치를 정확히 골라서 확인.
- [x] 수정 후 재검증: EditMode 115/115, PlayMode 33/33(신규 테스트 포함) 전부 첫 실행에
      바로 통과.

**네 번째 원인이자 아마 가장 근본적인 원인: 웨이포인트 "도착" 판정이 Y축까지 포함해서 계산되고 있었음**
- 유저가 세 번째 수정 직후 또 재현: 앞선 세 버그와 무관해 보이는 상황(장애물도 특별할 것
  없는 평범한 방)인데도 여전히 "안 움직인다"는 보고. 처음 조회했을 때는 속도가 정확히 0이
  아니고(`vel=(-0.19,0,0)`) 경로도 정상(`Room.FindPath`가 4개짜리 유효 경로 반환)이라 일단
  "멈추기 직전 스냅샷을 잘못 잡았을 수 있다"고 보류했으나, 유저가 "그래도 안 움직인다"고 재차
  확인해줘서 재조사 → 프레임이 실제로 약 1800틱 진행되는 동안 좌표가 소수점까지 완전히
  동일하게 고정되어 있음을 확인, 진짜 정지 상태로 확정.
- 정확한 진단: 적의 실제 위치(45.92, **1.00**, 4.01)와 경로의 첫 웨이포인트(46.00, **0.00**,
  4.00) 사이 수평(XZ) 거리는 0.08유닛뿐인데, 도착 판정에 쓰이는 `Vector3.Distance`(Y축 포함
  3D 거리)로 계산하면 약 1.003유닛이 나옴 - 도착 기준(`WaypointArriveDistance = 1f`)을 근소하게
  못 넘김.
- 근본 원인: `EnemyChaseState.NextWaypoint`의 "이 웨이포인트에 도착했는가" 판정이
  `Vector3.Distance(body.position, waypoint)`로 Y축까지 포함해서 계산되고 있었음. 그런데
  `RoomController.CellToLocal`이 만드는 경로 웨이포인트는 항상 Y=0인 반면, 실제 적
  Rigidbody는 항상 Y=1에 서 있음(이번 세션 내내 라이브로 찍힌 좌표 전부 y=1.00으로 일관).
  이 고정된 Y축 차이 1.0유닛만으로 이미 도착 판정 기준(1유닛)과 거의 같아서, 수평으로 아무리
  가까이 가도 3D 거리가 그 기준보다 작아질 방법이 없었음 - `_pathIndex`가 **영원히 0에서
  못 넘어가고** 적은 항상 자기 출발 칸만 다시 목표로 삼는 것을 무한 반복. 마침 `PhysicsTick`
  자신의 별도 정지 조건(Y를 무시하는 수평 거리 기준)은 이 근접을 정상 감지해 그 자리에서
  멈춰버려, 결과적으로 "출발 칸 바로 앞에서 굳어버림"이 되었음.
  이건 이번 세션에서 찾은 것 중 가장 파급력이 큰 버그로 보임 - RelentlessMelee뿐 아니라
  **경로가 2칸 이상인 모든 추격(장애물을 낀 모든 근접 적 포함)에서 실제 게임 빌드 기준으로는
  거의 항상 재현됐을 결함**이기 때문.
- 왜 지금까지 안 잡혔나: 이번 세션에서 작성한 회귀 테스트 세 개를 포함해 `TestObjects.
  BuildEnemy`를 쓰는 모든 기존 테스트가 예외 없이 적을 **Y=0**에 배치해서 만들었음(웨이포인트와
  정확히 같은 높이) - 그래서 이 Y축 격차 자체가 테스트에서는 단 한 번도 노출된 적이 없었음.
- 수정: `EnemyChaseState.cs`에 `HorizontalDistance(a, b)` 헬퍼(Y를 0으로 지운 뒤
  `Vector3.Distance`)를 추가하고, `NextWaypoint`의 도착 판정을 이걸로 교체.
- 회귀 테스트 추가: `ChaseState_EnemyBodyRestsAboveWaypointHeight_StillAdvancesPastEachWaypoint`
  - 이번엔 일부러 적을 실제 프리팹과 같은 **Y=1**에 배치해서(다른 모든 테스트는 Y=0) 이 격차를
  드러나게 하고, 그래도 경로를 끝까지 따라가 거리를 좁히는지 검증.
- [x] 수정 후 재검증: EditMode 115/115, PlayMode 34/34(신규 테스트 포함) 전부 첫 실행에
      바로 통과.

**다섯 번째 원인: `Repath()`가 매번 자기 자신이 서 있는 칸으로 되돌아가 진행을 스스로 취소함 ("떨리는" 증상)**
- 유저가 네 번째 수정 직후 또 재현: 이번엔 "안 움직인다"가 아니라 "위치가 떨리는 느낌으로
  달라진다"는 새로운 표현. 창 포커스가 없어 시뮬레이션이 멈춰있는 상태를 몇 차례 잘못 짚은 뒤,
  `Physics.simulationMode = SimulationMode.Script`로 바꿔서 `Tick`/`PhysicsTick`을 직접 강제
  호출하며 궤적을 30틱 단위로 추적 - 실제로 `clearLine=False`가 계속 유지된 채로 x좌표가
  2.77~3.01 사이를 주기적으로(약 15~16틱, `RepathInterval` 0.3초와 정확히 일치) 왕복하는
  진짜 진동임을 확인.
- 근본 원인: `EnemyChaseState.Repath()`가 0.3초마다 무조건 `_pathIndex = 0`으로 리셋하고 경로를
  처음부터 재계산함. 그런데 `Room.FindPath`가 반환하는 경로의 **0번 인덱스는 항상 "지금 서
  있는 칸 자신"**(A* 경로 복원 방식상 시작 노드가 항상 포함됨) - 이미 그 자리에 있으니 목표로서
  아무 의미가 없음. RelentlessMelee처럼 느린 적(`moveSpeed=1.5`)은 0.3초 동안 0.45유닛밖에 못
  움직여서, 다음 리패스가 돌아올 때도 여전히 같은 칸 안에 있는 경우가 흔함 - 그러면 새로 계산된
  경로도 "0번=지금 서 있는 그 칸"으로 또 나오고, 방금 1번 웨이포인트(진짜 다음 진행 방향)로
  넘어갔던 진행이 통째로 취소되어 다시 0번 목표로 되돌아감. 이게 매 리패스 주기(0.3초)마다
  반복되며 "떨리는" 것처럼 보임 - 실질적 순이동은 거의 0.
- 수정: `Repath()`가 새 경로 계산 직후, 경로가 2칸 이상이면 `_pathIndex`를 0이 아니라 **1**로
  바로 건너뛰도록 변경(0번=이미 도착해있는 자기 자신은 애초에 목표로 삼을 이유가 없음).
- 회귀 테스트 추가: `ChaseState_SlowEnemyAcrossManyRepathCycles_MakesSteadyProgressInsteadOf
  Oscillating` - 실제 RelentlessMelee 속도(1.5)로 여러 리패스 주기에 걸쳐 순이동이 실제로
  발생하는지 검증. 작성 과정에서 이 버그와 무관한 테스트 자체의 문제 세 가지를 연달아 발견/수정:
  (1) 처음엔 시간 예산(8초)이 느린 속도로 우회 경로를 완주하기엔 부족해서 실패 → 24초로 확장.
  (2) 그래도 기준(시작 거리의 절반 이하)이 너무 빡빡해 실패 → "시작보다 최소 1유닛은 확실히
  가까워졌는가"로 완화. (3) 그래도 실행마다 결과가 들쭉날쭉(8→6.54, 8→7.70 등) - 방 중앙
  장애물 하나만으로는 남쪽/북쪽 우회 경로 비용이 정확히 동률이라, A*가 매 리패스마다 그때그때
  다른 쪽을 택하면서(버그와 무관한 별도의 비효율) 진행 속도가 실행마다 달라졌던 것 → 장애물을
  하나 더(북쪽 경로만) 막아 남쪽이 확실히 더 짧도록 비대칭화해서 동률 자체를 없앰. 이 세 가지
  모두 프로덕션 버그가 아니라 테스트 자체의 시간/기준/지형 설계 문제였음.
- [x] 최종 재검증: EditMode 115/115, PlayMode 35/35(신규 테스트 3연속 통과) - 확인 과정에서
      기존에 알려진 Patrol/Idle 타이밍 플레이키 테스트 2종(`Chase_VisitsEveryCheckpoint_With
      NobodyThere_GivesUpToPatrol`, `Chase_SearchTimesOut_GivesUpToPatrol_WhenLastKnownSpot
      Unreachable`)이 번갈아 가며 각각 1회씩 실패했으나 재실행 시 항상 통과 - 이번 수정과
      무관한 기존 결함성 플레이키니스로 확정.

### 이슈/메모
- `TestRunnerApi.Execute()`에 직접 `RegisterCallbacks`한 `ICallbacks` 인스턴스는 실행을 발사한
  `TestRunnerApi` 자체가 어디에도 참조로 안 남아 있으면(로컬 변수뿐) 테스트는 정상 진행되는데도
  내 콜백(`RunFinished` 등)이 전혀 호출되지 않음 - 실제 원인 확정은 못 했지만(GC 추정) 결과
  파일이 끝까지 안 생김. 우회: 커스텀 콜백에 의존하지 말고, 엔진이 항상 남기는
  `%APPDATA%/../LocalLow/DefaultCompany/RogueLike/TestResults.xml`의 mtime 변화를 폴링해서
  그 안의 `total/passed/failed`와 `fullname` 목록으로 결과를 읽는 편이 훨씬 안정적.
- `TestRunnerApi.Execute()`를 호출한 뒤 같은 RunCommand 안에서 `Thread.Sleep` 루프로 완료를
  기다리면 절대 완료를 감지 못함 - 실제 테스트 실행은 이 RunCommand 호출이 리턴하고 에디터가
  자기 업데이트 루프로 돌아간 "이후"에 비동기로 진행되기 때문에, 같은 호출 안에서 블로킹
  대기하면 그 사이 어떤 진행도 없음. 해결: 테스트를 그냥 발사만 하고 즉시 리턴, 이후 별도 호출
  (또는 셸에서 `TestResults.xml` mtime 변화를 폴링)로 완료를 확인하는 방식으로 전환.
- Unity_RunCommand가 만드는 동적 어셈블리는 프로젝트 코드의 `internal` 멤버를 볼 수 없음
  (InternalsVisibleTo 없음) → 디버그/검증용 스크립트에서 `internal static` 헬퍼를 호출하려면
  실패하므로, 필요한 로직을 스크립트 안에 그대로 복제해서 검증할 것.
- Unity_RunCommand 동적 어셈블리에서 `HashSet<T>` 컬렉션 이니셜라이저(`new HashSet<T>{...}`)가
  `ISet<>` 참조 누락으로 컴파일 실패 - `.Add()` 호출을 개별로 풀어써서 우회.
- RunCommand에서 클래스를 `CommandScript` 안에 `private class`로 중첩하면 자동 수정기가 같은
  클래스를 네임스페이스 최상위에도 중복 생성해 `private` 접근제한자 충돌(CS1527)을 냄 →
  헬퍼 클래스는 애초에 `CommandScript`와 같은 레벨의 별도 `internal class`로 작성할 것.
- `Chase_SearchTimesOut_GivesUpToPatrol_WhenLastKnownSpotUnreachable`이 드물게 Patrol
  대신 Idle을 관측해 실패하는 타이밍 플레이키니스 확인(재실행하면 통과) - PatrolState가
  진입 직후 매우 짧게 Idle로 넘어갈 수 있는 기존 폴링 경쟁상태로 보임, 언젠가 시간 나면
  프레임 단위 폴링으로 안정화할 것(급하지 않음, 로직 결함 아님).
- Play Mode에서 `GameObject.FindFirstObjectByType<RoomController>()`로 "아무 방이나" 집으면
  안 됨 - `StageManager`가 층의 모든 방을 각자의 그리드 오프셋 월드 좌표에 동시에
  인스턴스화해두므로, 진단/재현 스크립트는 항상 "플레이어와 가장 가까운 방"처럼 명시적으로
  올바른 방을 찾아써야 함. 실제 게임 코드(`EnemySpawner`)는 애초에 올바른 참조를 인자로
  받으므로 이 함정에 걸리지 않음.
- PlayMode 테스트 스위트를 돌린 직후 곧바로 수동으로 Play 버튼을 눌러 라이브 검증하면
  `Time.timeScale`이 `0`으로 남아있어 아무것도 안 움직이는 것처럼 보일 수 있음 - 라이브
  검증 전에 `Time.timeScale`을 확인/복구할 것.
- `ObstaclePlacement` 생성자는 `(shape, localCell, variantIndex:int, rotationY:float)` 순서 -
  Connector를 수동으로 만들 때 뒤 두 인자(정수/실수) 순서를 착각하기 쉬우니 주의.
- `TestObjects.ResolveField`는 `target.GetType().GetField(...)` 한 단계만 보므로, 부모
  클래스에 선언된 `private` 필드(예: `AttackPatternData.range`를 `MeleeAttackPatternData`
  인스턴스에)는 타입 계층을 타고 올라가며 찾도록 고치기 전까진 못 찾았음 - 지금은 수정됨.
- 이 세션 중 에디터 자체가 한동안 심하게 느려진 구간 관측(`EditorApplication.timeSinceStartup`
  이 실제 경과 시간 대비 거의 정지 수준으로 느리게 흐름, Play Mode를 켜도 `Time.frameCount`가
  몇 분간 그대로 멈춰 있었음) - 에디터 창이 포커스/리페인트를 못 받는 상태로 방치되면 생기는
  것으로 추정(원인 미확정). 라이브 Play Mode 검증 중 아무 반응이 없다면 먼저 `Time.frameCount`
  가 실제로 진행되는지부터 확인할 것 - 로직 버그로 오인하기 쉬움.

### 다음 작업
- 로드맵 §3(방/장애물 배치 + A* 경로탐색) 완료. 다음은 §4 입력 버퍼링 + 프레임레이트
  독립성, 또는 §5 커스텀 에디터 툴링(SO 검증 패스) - `ClaudeSessionSummary.md` §6 순서 참고.
- (튜닝 여지, 급하지 않음) `EnemyChaseState`의 재탐색 주기(0.3초)와 웨이포인트 도달 판정
  거리(1유닛)는 첫 구현값 - 실제 플레이로 손맛 확인 후 조정 여지 있음.

---

## 2026-09-12

### 진행 내용

**`cyberpunk_game_kit` 장애물 배치 (A* 이전 절반) — 시드 기반 배치 + 플레이어/적/발사체 차단**
- 설계는 대안 A(절차 생성 단계에서 그리드 셀 단위로 결정, RoomController는 그 데이터로 조립만)로
  진행 - 사용자 승인 하에 시작. 신규 `ObstaclePlacement`(방 로컬 타일 셀 좌표 + 변형 인덱스 +
  회전), `RoomObstacleSet`(RoomTileSet과 동일 패턴의 SO). `RoomData.Obstacles`는 옵션 생성자
  파라미터(기본값 빈 리스트)로 추가해 기존 `RoomData` 생성 지점(GameFloors 폴백, JSON 방 등)은
  전혀 안 건드림.
- `ProceduralFloorGenerator.BuildObstacles`: 전투방 한정, 문/입구 주변 2칸(진입점 인셋 1칸 +
  여유 1칸)을 제외한 내부 타일 셀 중 시드로 섞어서 고르고, 후보 하나를 확정할 때마다 남은
  빈 칸 전체가 여전히 하나로 연결되는지(BFS) 확인 후 채택 — 채택 안 되면 다음 후보 시도.
  전용 명명 RNG 스트림(`obstacles.f{층}`)이라 기존 시드의 방 그래프/적 배치는 전혀 안 바뀜.
  개수는 `FloorRule.MinObstaclesPerCombatRoom/Max`(`ProceduralRules.json`에 층별로 추가,
  1층 1-2개~3층 2-4개).
- `RoomController.BuildObstacles`: `RoomData.Obstacles`를 받아 `RoomObstacleSet`의 프리팹을
  같은 4유닛 타일 그리드 좌표로 인스턴스화. 각 장애물은 자체 콜라이더를 이미 갖고 있다고
  가정(별도 콜라이더 부착 로직 없음). `ClampToInterior`(적 이동의 유일한 컨테인먼트 지점)에
  `RoomObstacleBounds.ClampLocal` 단계를 추가해 장애물 박스(4x4 그리드 셀 기준, 실제 메시보다
  넉넉함) 밖으로 밀어내도록 확장 - `RoomBounds.PushOutOfNotch`와 같은 역할.
- 장애물 프리팹 4종(`Assets/Prefabs/RoomObstacles/`): `cyberpunk_game_kit`의 AC/Fence/Computer/
  Support_Short를 원본 스케일 그대로 사용(측정 결과 이미 1~2.2m 실사이즈였음 - 처음에 임의로
  2.4~2.6배 키우려다 FBX 루트에 이미 임포트 스케일(~100배)이 박혀 있는 걸 놓쳐 오히려 1/40
  크기로 작아지는 버그를 만들었다가 원인 파악 후 원본 스케일 유지로 정정), 콜라이더는
  자식 렌더러 전체를 부모 로컬 공간으로 역변환해 계산(루트 회전이 있어도 축이 안 뒤바뀌게 -
  단순히 월드 바운드를 lossyScale로 나누기만 했다가 Y/Z가 뒤바뀌는 버그를 겪고 수정).
  콜라이더는 트리거 아님 + Rigidbody 없음(벽과 동일 조합) - `WallClamp`(플레이어)와
  `Projectile.OnTriggerEnter`(발사체)가 이미 이런 콜라이더를 벽처럼 취급하므로 코드 변경 없이
  플레이어/발사체 차단이 자동으로 됨. `RoomObstacleSet_Cyberpunk.asset` 생성 후 `Room_Base.prefab`
  의 `RoomController.obstacleSet`에 연결.
- 가로직사각형(WideRect) 방은 짧은 변이 4칸이라 위 클리어런스 규칙상 후보 칸이 0개 - 이번
  버전에서는 장애물이 아예 안 나옴(의도된 제한, 문서화함).

**같은 세션 (2) — 유저 요청으로 "1x1칸이 이어지는" 장애물(울타리 군집) 추가, `cube_world_kit` 도입**
- 유저가 `cyberpunk_game_kit`/`modular_space`에는 아이작처럼 1칸씩 이어지는 장애물용 리소스가
  없다고 지적 → `Assets/Art/Props/prototype_kit`(1유닛 모듈, `wall`/`crate`/`shape-cube`)와
  `Assets/Art/Props/cube_world_kit`(라이선스 파일은 없지만 유저가 "포트폴리오용이라 상업적 이용
  아니니 상관없다"고 확인) 검토 후, 후자의 `Rock1`/`Rock2`(진짜 바위)와 `Fence_Center/Corner/
  T/End`(코너·T자·막다른끝까지 갖춘 완전한 모듈형 울타리 세트)로 진행 - 비주얼 불일치도 유저가
  "아트 포지션이 아닌 포트폴리오라 상관없다"고 확인.
- **설계는 이전과 동일한 골격 유지**(Option A: 절차 생성 단계가 그리드 셀 단위로 결정,
  `RoomController`는 조립만) - `ObstaclePlacement`에 `ObstacleKind`(Solo/FenceStraight/
  FenceCorner/FenceT/FenceEnd) 필드만 추가해 기존 데이터 모델·`RoomController.BuildObstacles`
  루프·연결성 검사(`LeavesRoomConnected`)를 전부 재사용. 신규는 `FenceObstacleKit`(4개 명명
  필드 SO - `RoomTileSet`과 같은 패턴, 이 4개는 서로 교체 불가능한 구조적 역할이라 `RoomObstacleSet`
  같은 인덱스 배열 대신 이름 있는 필드로 분리) 하나뿐.
- **모양을 다양하게 하기 위해 직선 전용이 아니라 랜덤워크로 군집을 키우는 방식 채택**
  (`ProceduralFloorGenerator.BuildFenceCluster`/`GrowCluster`): 클리어런스 후보 칸 중 시작점을
  고르고, 매 스텝 클러스터의 아무 칸에서나 인접한 미사용 칸으로 확장 - 한 칸이 4방향 모두
  연결되는 경우(교차로 조각이 없음)와 루프가 생기는 경우를 차단해 항상 나무 형태(1~3개 연결)로
  유지. 완성된 클러스터 전체를 한 번에 막았을 때도 방이 여전히 연결되는지 확인 후 채택, 실패하면
  다른 시작점으로 최대 12회 재시도. 전용 명명 스트림(`fenceClusters.f{층}`)이라 소품 배치/적/방
  그래프와 서로 안 건드림. `FloorRule.FenceClusterChance/MinSize/MaxSize` 신규(`ProceduralRules.json`
  층별 0.3~0.5 확률, 3~6칸).
- **조각-회전 매핑은 실측 기반**: `Fence_Center/Corner/T/End`를 씬에 배치해 SceneView 캡처 +
  월드 바운드 min/max를 직접 측정해서 기본(회전 0°) 연결 방향을 확정(Center={서,동},
  Corner={동,북}, T={서,동,북}, End={동}) - 이후 90°(Quaternion.Euler Y+90) 회전이
  남→서→북→동→남 순으로 순환한다는 걸 기존 `RoomController.BuildGeometry` 컨벤션에서 재확인,
  이 순환을 이용해 실제 인접 방향 집합에 맞는 (조각, 회전) 조합을 역산하는
  `PieceFor`/`RotateDir` 작성. 손 계산으로 4×4가지 케이스 전부 교차검증 후 구현.
- 프리팹 6종(`Assets/Prefabs/FenceObstacles/`): 울타리 4종은 원본 스케일의 2배(팔 길이가
  기본 1유닛이라 2배 해야 인접 칸 중앙까지 정확히 닿아 이음매 없이 연결됨), 바위 2종은 2.5배.
  콜라이더는 지난 세션에 만든 "자식 렌더러 전체를 부모 로컬 공간으로 역변환" 방식 그대로 재사용.
  `RoomObstacleSet_Cyberpunk.asset`에 바위 2종 추가(4→6개), `FenceObstacleKit_CubeWorld.asset`
  신규 생성 후 `Room_Base.prefab`에 배선.

### 테스트 결과
- [x] EditMode 전체 102개 그린(장애물 세션 시작 시 99개 + 신규 클러스터 테스트 4개 - 디버그용
      임시 테스트 1개 제거 = 102). 신규: 클러스터 내 칸이 4연결을 절대 안 넘는지, 여러 세션
      120시드 전체에서 코너/T자가 실제로 등장하는지(직선만 나오는 회귀 방지), 배치된
      (조각,회전)이 실제 이웃 방향과 진짜로 맞는지(생성기 내부 상태가 아니라 `LocalCell` 인접
      관계로 독립 재검증) 확인.
- [x] `RunCommand`로 알려진 정답 클러스터(코너+직선+끝단×2 - 실제 seed 3 출력과 동일 모양)를
      방에 직접 만들어 `Initialize()` 호출 → 프리팹 생성 확인, SceneView 멀티앵글 캡처로 위에서
      본 모양이 끊김 없이 이어지는 것 육안 확인.
- [ ] 실제 Play Mode 육안 확인은 여전히 미확인.

### 이슈/메모
- **실제 버그 발견·수정**: `GrowCluster`의 루프 방지 조건이 `ClusterDegree(next, cluster) >= 1`
  로 돼 있었는데, `next`는 확장 중인 셀 자신과 항상 인접하므로 이 조건은 **항상 참** — 결과적으로
  모든 확장 후보가 매번 걸러져 클러스터가 1칸을 절대 못 넘고, `BuildFenceCluster`가 항상 빈
  결과를 반환하고 있었음. `> 1`(자기 자신 말고 다른 클러스터 칸에도 닿는 경우만 차단)로 수정.
  EditMode의 "FenceEnd가 한 번도 안 나온다"는 회귀 테스트가 이걸 정확히 잡아냈고, 강제로
  확률 100%를 준 디버그 테스트(성공 0/30 → 수정 후 30/30, End 67·Corner 26·Straight 14·T 7)로
  재현·수정 확인.
- **테스트 실행 중 Unity가 멈춘 것처럼 보였던 해프닝**: `TestRunnerApi`로 4번째 테스트를 돌리는
  중 몇 분간 모든 명령이 응답 없어서 무한루프를 의심했으나, 실제로는 씬 저장 확인 팝업이 떠서
  자동화 명령이 전부 그 뒤에서 대기하고 있던 것 — 유저가 Save를 눌러 팝업을 닫자 즉시 정상화됐고
  큐에 쌓여 있던 테스트 결과도 그대로 받아졌음(101/102, 실패 1건 = 위의 진짜 버그). 이후
  `TestRunnerApi.RegisterCallbacks`를 쓸 때마다 `UnregisterCallbacks`도 짝을 맞추는 습관으로 변경.

**같은 세션 (3) — 유저 실플레이 피드백으로 "패턴 라이브러리" 방식으로 전면 전환**
- 유저가 1층 실플레이 후 "정사각형 방에만 장애물 있고 이어진 블록은 못 봤다, 아이작은 이것보다
  덜 썰렁했다"고 지적. 원인 분석: 확률(장애물 개수 min 1~2, 군집 확률 30%)이 낮게 잡혀 있었고,
  근본적으로 "매 시드 라이브로 계산" 방식이라 아이작처럼 "미리 검증된 배치"의 밀도감을 못 냄.
  **아이작이 실제로 하는 방식(디스크 스캔/커뮤니티 자료 기반 추정)을 재현**하기로 결정 — 던전
  그래프는 절차적으로 생성하되, 방 "내용물"(장애물 배치)은 매번 새로 계산하지 않고 미리 만든
  방 모양별 패턴 뭉치 중 하나를 시드로 골라 그대로 찍는 방식.
- **랜덤워크 절차 생성(같은 세션 (2)에서 만든 `BuildFenceCluster`/`GrowCluster`/`ClusterDegree`
  /`BuildSoloObstacles`/`BuildRoomObstacles`)을 전부 삭제**하고 패턴 선택으로 교체 — 유저가
  ㄱ자 방도 패턴화하자고 확정해서 정사각형/ㄱ자 둘 다 대상(가로직사각형은 여전히 장애물 없음,
  변경 없음). 단, 조각-회전 산출 로직(`PieceFor`/`RotateDir`/`DirOffset`)은 실측 검증까지
  끝난 정확한 코드라 **그대로 재사용** — 패턴은 "어떤 칸이 서로 붙어있는지"만 정의하면 되고
  실제 조각 종류·회전은 여전히 인접 방향으로 자동 계산됨.
- **데이터 형식**: 신규 `Assets/StreamingAssets/Balance/Stage/ObstaclePatterns.json` — 정사각형
  전투방(7x7)/ㄱ자 전투방(11x11, NE 노치 기준으로 작성) 각각 패턴 배열. 패턴은 사람이 읽기 쉬운
  ASCII 그리드(`.`=빈칸, `#`=울타리, `O`=소품)로 표현, 프로그램으로 좌표→그리드 문자열 생성 후
  파일로 저장(손으로 7/11칸 문자열 세면 실수하기 쉬워서 자동 생성 후 검수). `ObstaclePatternLibrary`
  신규 클래스가 `ProceduralRules`/`GameFloors`와 동일한 패턴(지연 로드, 파싱 실패 시 빈 패턴으로
  안전 폴백)으로 로드. 정사각형 8종(빈 패턴 포함), ㄱ자 7종(빈 패턴 포함) 작성 — 빈 패턴 비중은
  가중치로 조절(실측 결과 정사각 12.6%, ㄱ자 13.3% 빈 방 - 유저가 말한 "적지만 있다"에 근접).
- **ㄱ자 방 노치 방향 대응**: 패턴은 NE 노치 기준으로만 작성하고, 런타임에 방의 실제 노치
  방향(NE/SE/SW/NW)에 맞춰 그리드 좌표를 90°씩 회전(`RotateCell`) — 노치의 대각선 부호벡터가
  NE(1,1)→SE(1,-1)→SW(-1,-1)→NW(-1,1) 순으로 90°마다 회전한다는 걸 유도해 `NotchRotationSteps`
  테이블로 확정. 회전은 홀수 그리드 크기(7,11) 기준 정수 연산이라 반올림 오차 없음. 정사각형
  방은 노치가 없어 정렬 제약이 없으므로, 대신 매번 랜덤 회전(0~270°)을 적용해 패턴 8개만으로도
  체감 다양성을 4배 늘림.
- `FloorRule.MinObstaclesPerCombatRoom/Max`, `FenceClusterChance/MinSize/MaxSize` 필드 전부
  삭제(더 이상 쓰는 곳 없음) - `ProceduralRules.json`/`ProceduralRules.cs`에서도 해당 필드 제거.
- 테스트 파일 재구성: 신규 `ObstaclePatternLibraryTests.cs` — 패턴이 이제 고정 데이터라 시드
  스캔 대신 **패턴 개수만큼만** 검사(정사각 8개, ㄱ자 7개×4가지 노치 방향) - 클리어런스/노치
  회피, 연결성(BFS), 울타리 그룹이 트리 구조(사이클 없음)이고 4연결을 안 넘는지, 고립된 단일
  칸이 없는지. 기존 `ObstaclePlacementTests.cs`는 전체 파이프라인 회귀 테스트로 남기고 삭제된
  함수를 참조하던 테스트만 제거.

### 테스트 결과 (같은 세션 (3))
- [x] EditMode 전체 108개 그린. 패턴 검증 과정에서 **실제 버그 1건 추가 발견**: `Cross` 패턴이
      소품 4개로 중앙 칸을 사방으로 완전히 둘러싸 그 칸을 고립시킴 - 연결성 테스트가 정확히
      잡아냄, 한쪽을 열어서 수정.
- [x] 실제 생성기로 여러 시드를 스캔해 ㄱ자 방의 4가지 노치 방향 전부에서 같은 패턴(예: 7칸짜리
      큰 꺾인 벽)이 올바르게 회전 배치되는 것을 원본 텍스트 로그로 확인.
- [x] 실제 `RoomController.Initialize()`로 SE 노치 ㄱ자 방(꺾인 벽 7칸+바위 1개)과 정사각형 방
      (T자 울타리)을 직접 생성해 SceneView 멀티앵글 캡처로 육안 확인 - 벽이 끊김 없이 이어지고
      노치 방향에 맞게 정확히 배치됨.
- [x] 60시드 스캔으로 빈 방 비율 실측: 정사각 12.6%(67/530), ㄱ자 13.3%(12/90) - "적지만 있다"는
      목표에 부합.
- [ ] 실제 Play Mode 육안 확인은 여전히 유저 몫.

### 이슈/메모 (같은 세션 (3))
- JSON 파일만 수정하고 C# 스크립트는 안 바꾸면 **도메인 리로드가 안 일어나** `ObstaclePatternLibrary`
  의 캐시가 갱신 안 됨 - 패턴 수정 후 즉시 테스트를 돌렸는데 이전 버전이 계속 나와서 한 번 헷갈림.
  `EditorUtility.RequestScriptReload()`로 강제 리로드해서 해결.

### 다음 작업
- 그리드 A* 경로탐색 (장애물 셀 데이터가 이미 `RoomData.Obstacles`에 타일 그리드 좌표로 있으니
  A*의 "막힌 칸" 입력으로 그대로 재사용 가능 - 패턴 기반 배치도 셀 단위라 동일하게 적용됨).
- 실제 Play Mode에서 장애물/울타리 패턴 시각적 배치·발사체 차단 확인.
- 패턴 라이브러리 자체는 앞으로도 계속 늘릴 수 있음(정사각형/ㄱ자 각각 몇 개씩 더 추가하는 정도).

**같은 세션 (4) — "이어진 장애물이 1층에서 안 보인다" 원인 확인 + `cyberpunk_game_kit` 소품 제거**
- **원인은 버그가 아니라 확률+층 구조**: 60시드 스캔 결과 1층 전투방은 압도적으로 정사각형
  (167/204, 82%)이고 ㄱ자는 거의 안 나옴(2/204, 1%) — 병합 조건(2x2 블록 중 한 칸만 빈)을
  만족하는 인접 셀 배치가 1층처럼 방이 6~8개뿐인 작은 층에서는 실제로 드물기 때문(가중치상
  20%로 설정돼 있어도 실제 등장률은 그보다 훨씬 낮음, 병합 알고리즘 자체는 안 건드림). 게다가
  정사각형 패턴 중 절반만 울타리를 포함해서(가중치 균등 2일 때), 방 3~4개짜리 1층에서 시드
  60개 중 16개(27%)는 울타리 있는 방이 층 전체에 하나도 없었음 - 유저가 이걸 본 것.
- **수정**: `ObstaclePatterns.json`에서 울타리를 포함하는 패턴들의 가중치를 2→3으로 올림(빈
  패턴/소품 전용 패턴은 2 유지) - 정사각형 P(울타리)=50%→60%, ㄱ자 P(울타리)=71%→79%. 재측정
  결과 1층에서 울타리가 하나도 없는 시드 비율이 27%(16/60)→17%(10/60)로 개선. 0%까지는 안
  만들었음 - 아예 없애면 오히려 "항상 있음"이 기계적으로 느껴질 수 있어 약간의 확률적 여지는
  의도적으로 남김.
- **`cyberpunk_game_kit` 장애물 소품(AC/Fence/Computer/Support) 완전 제거**: 유저가 인게임에서
  보니 애매하다고 판단. `RoomObstacleSet_Cyberpunk.asset`에서 해당 4개 프리팹 참조 제거, 남은
  바위 2종만 남기고 `RoomObstacleSet_Solo.asset`으로 개명(GUID 유지돼 `Room_Base.prefab` 참조는
  안 깨짐). 실제 프리팹 파일(`Assets/Prefabs/RoomObstacles/Obstacle_{AC,Fence,Computer,Support}.prefab`)
  과 빈 폴더까지 삭제. `ProceduralFloorGenerator.ObstacleVariantCount`를 6→2로 수정. 원본
  `cyberpunk_game_kit` 애셋 폴더 자체(다른 용도로 재사용할 수도 있어서)는 그대로 둠 - 이번에
  지운 건 우리가 직접 만든 장애물 프리팹뿐.

### 테스트 결과 (같은 세션 (4))
- [x] EditMode 전체 108개 그린(변경 후에도 그대로).
- [x] 60시드 재스캔으로 가중치 조정 효과 확인(위 수치).
- [x] `RoomObstacleSet_Solo.asset` 내용 직접 조회로 Rock1/Rock2 2개만 남은 것 확인.

### 다음 작업
- 그리드 A* 경로탐색.
- 실제 Play Mode에서 최종 확인(장애물 배치 밀도, 발사체 차단, 소품이 바위 2종만 나오는지).

**같은 세션 (5) — 소품 다양성 복구 + "정사각형 방도 안 보인다" 질문 확인**
- 유저 요청으로 **울타리 조각 4종(Straight/Corner/T/End)을 단일 소품 풀에도 추가** — 삭제한
  cyberpunk_game_kit 소품을 대신할 다양성 확보. 새 에셋 제작 없이 기존 `Fence_*.prefab`을
  그대로 재사용(회전은 기존 소품 로직 그대로 랜덤). `RoomObstacleSet_Solo`가 바위 2종 + 울타리
  조각 4종 = 6개로 늘어남, `ObstacleVariantCount` 2→6.
- 유저가 "정사각형 방에도 이어진 블록이 안 보이던데 그냥 운이 좋아서 빈 방만 들어간 걸까"라고
  질문 → **실제 floor1 시드(0)의 정사각형 전투방(room 1005)을 직접 `Initialize()`로 만들어
  SceneView로 렌더링 확인** - 코너 모양 울타리(End-Corner-End)와 새로 추가한 소품용 울타리
  조각이 정상적으로 화면에 나타남. 렌더링 파이프라인 자체는 정상 동작 확인 - 유저 본인의
  추측대로 **단순히 운(그 세션에 들어간 방들이 마침 비-울타리 패턴을 뽑음)**일 가능성이 높음.
  정사각형 방 하나당 울타리 패턴 확률은 재조정 후에도 60%라 특정 몇 개 방에서 연달아 안 뜨는
  것 자체는 통계적으로 드물지 않음(단일 방 기준 미출현 확률 40%).

### 테스트 결과 (같은 세션 (5))
- [x] EditMode 108개 그대로 유지.
- [x] 실제 floor1 정사각형 방(seed 0, room 1005)을 SceneView로 렌더링 확인 - 코너형 울타리 +
      소품용 울타리 조각 정상 표시.

**같은 세션 (6) — 단일 소품을 `cube_world_kit`의 순수 블록 메시 1종으로 통일**
- 유저가 씬에 남아있던 실제 오브젝트(`Obstacle_3_4`/`Obstacle_4_2`/`Obstacle_2_3`)를 지목하며
  "네모 블록이 아니다"라고 지적 → 실제 씬 조회로 메시 확인(각각 `Fence_Center`/`Fence_Corner`/
  `Rock2`) → 방금 세션 (5)에서 추가한 "울타리 조각을 단일 소품으로 재사용"이 연결 상대 없이
  혼자 놓이면 "잘린 레일" 처럼 보인다는 게 원인으로 확인됨.
- 유저 결정으로 **바위 2종 + 울타리 조각 4종을 전부 빼고, `cube_world_kit`의 순수 정육면체
  블록 메시 1종(`Blocks/FBX/Block_Stone.fbx`)만 쓰는 것으로 단순화**. 같은 킷 안에 Minecraft풍
  블록 세트(`Blocks/`, `Pixel Blocks/` 폴더 - Stone/Brick/Crate/Dirt/Grass 등 다수)가 있다는 걸
  이번에 처음 발견 - 그중 테마 중립적인 `Block_Stone` 선택. `RoomObstacleSet_Solo`를 프리팹
  1개짜리로 교체, `ObstacleVariantCount` 6→1. 이제 쓰이지 않게 된 `Rock1`/`Rock2` 프리팹은
  삭제(울타리 조각 4종은 `FenceObstacleKit`이 여전히 씀 - 안 지움).

### 테스트 결과 (같은 세션 (6))
- [x] EditMode 108개 그대로 유지.
- [x] 실제 floor1 방(seed 0, room 1005)에 새 블록 소품을 실제로 생성해 콜라이더(논트리거,
      Rigidbody 없음) 확인 + SceneView로 순수 정육면체 모양 육안 확인.

**같은 세션 (7) — 근본 오해 확인: "이어지는 장애물" = 조각 자동선택이 아니라 블록 반복 배치**
- 유저가 "cube_world_kit 여기 있는게 거의 다 큐브 같은 오브젝트일 거라 생각해서 긍정했던
  거였다, 울타리 오브젝트를 말한 게 아니었다"고 명확히 함 - 실제로 원했던 건 **같은 정육면체를
  여러 칸에 이어 붙여서 다양한 모양을 만드는 것**이었지, 방향(직선/코너/T자/끝단)마다 다른
  메시를 자동으로 골라 붙이는 지금 방식이 아니었음. 근본적으로 다른 그림이었다는 걸 확인.
- **방향 기반 조각 선택 시스템을 통째로 제거**: `ObstacleKind` enum(Fence* 값들), `FenceObstacleKit`,
  `PieceFor`/`RotateDir`/`DirOffset`/`PlacementsForGroup`/`FloodFill` 전부 삭제. `ObstaclePlacement`도
  칸+변형 인덱스+회전만 남기는 원래 형태로 단순화. 패턴에서 `#`/`O`로 표시된 칸 전부 **완전히
  동일한 방식**(같은 프리팹, 회전만 랜덤)으로 배치 — 모양은 순전히 "어떤 칸이 마킹돼 있는가"로만
  결정됨. 노치 방향 회전(`RotateCell`/`NotchRotationSteps`)은 방 정렬 문제라 조각 선택과 무관해서
  그대로 유지. 기존 패턴 JSON의 칸 배치는 전혀 안 바꿔도 됨(#=연결형, O=단독형 표시는 그대로
  유지하되 런타임에서는 구분 없이 처리).
- 소품도 `cube_world_kit`에서 이번에 처음 발견한 **Minecraft풍 블록 세트**(`Blocks/` 폴더 -
  Stone/Brick/Crate/Dirt/Grass 등)의 `Block_Stone` 1종으로 통일. Fence 4종 메시와 Rock1/Rock2는
  전부 삭제(Fence 4종은 이제 정말 아무 데도 안 쓰임).
- 코드가 크게 단순해짐(약 150줄 삭제) — 4연결 제한/루프 금지 같은 "메시가 없어서 생기던 제약"
  자체가 사라짐(정육면체는 사방으로 몇 개가 붙어도, 심지어 고리 모양이 돼도 문제 없음).

### 테스트 결과 (같은 세션 (7))
- [x] EditMode 104개 그린(108개에서 더 이상 의미 없어진 4개 테스트 제거 - Kind 기반 검증 3개 +
      울타리 그룹 트리 구조 검증 1개).
- [x] 이전에 확인했던 같은 방(seed 1, floor 2, room 2003, SE 노치)을 다시 렌더링해서 같은 꺾인
      모양이 이제 정육면체 블록 반복으로 정확히 나오는 것을 SceneView로 육안 재확인.

**같은 세션 (8) — 블록 사이 빈틈 수정(시각 + 실제 충돌 버그)**
- 유저가 1층 실플레이 중 "이어진 것처럼 보이는 큐브 사이에 틈이 있어서 안 막혀있다"고 지적.
  원인: `Block_Stone`이 원본 크기(2x2x2)라서 4유닛 격자 중심에 놓으면 칸마다 2유닛의 빈틈이
  생김 - 단순히 시각적 문제가 아니라 **플레이어가 실제로 그 틈을 통과할 수 있는 진짜 버그**였음
  (적 컨테인먼트는 칸 전체(4x4)를 막힌 것으로 취급하는 굵은 판정이라 안 뚫리지만, 플레이어의
  `WallClamp`는 실제 콜라이더 크기만 보므로 2유닛 틈이 그대로 뚫려 있었음).
- `Block_Stone` 프리팹을 2배 스케일(2→4유닛)로 재제작해 4유닛 타일을 정확히 꽉 채우도록 수정 -
  같은 경로에 덮어써서 GUID 유지, 참조 재배선 불필요. 이제 이어진 칸은 완전히 밀착돼 하나의
  벽처럼 보이고, 플레이어 충돌도 실제로 막힘.

### 테스트 결과 (같은 세션 (8))
- [x] EditMode 104개 그대로 유지(스케일만 바뀌어 데이터 모델 무변화).
- [x] 같은 방(seed 1, floor 2, room 2003)을 다시 렌더링해 이어진 블록들이 완전히 밀착된 것을
      SceneView로 육안 확인(콜라이더 크기도 4x4x4로 실측 확인).

---

## 2026-09-11

### 진행 내용

**유저 실플레이 피드백 반영 — 가로직사각형 방 크기 축소 + 문 오프셋 + 미니맵 모양 반영**
- 유저가 3층 보스까지 실플레이 확인 후 요청. `WideRectDepth`(세로 폭) 28 → 24 → 20 → 16으로
  단계별 축소(매번 재빌드 검증) — 16이 사실상 하한(코너 2개(4씩)+문폭(8)=16, 이보다 작으면 그
  축엔 문 있는 방을 못 만듦). 현재 가로직사각형 = 44×16.
- **직사각형/ㄱ자 방의 문을 중앙이 아닌 좌우(또는 상하) 한쪽으로 오프셋**: `RoomController.BuildSide`에
  `offsetDoor` 파라미터 추가. 처음엔 가로직사각형의 South/North에만 적용했다가, 유저 요청으로
  ㄱ자 방의 4변 전부(노치로 짧아진 변 포함)로 확장 — `offsetNorthSouthDoor = HasNotch || Width≠Depth`,
  `offsetEastWestDoor = HasNotch`(가로직사각형은 East/West가 그대로 중앙 유지, ㄱ자만 전 방향
  오프셋). 정사각형은 계속 중앙 고정. 어느 쪽에 붙을지는 `(RoomId*31 + direction) & 1`로 결정 —
  새 RNG 스트림 없이 이미 시드로 결정된 RoomId를 그대로 써서 "같은 시드 = 같은 던전" 원칙 유지,
  방마다 달라짐. 노치로 짧아진 변에서도 `runStart/runEnd` 기준으로 동일 공식이 그대로 성립함을
  `RunCommand`로 4변(노치 짧아진 North/East + 전체 South/West) 전부 손계산과 대조해 확인.
- **(별도 문의) 문이 한쪽 변에 2개(예: 북쪽만 2개) 있는 경우**: 유저가 물어봐서 규모를 설명함 —
  이건 오프셋과 달리 `ProceduralFloorGenerator`의 격자 그래프 모델 자체를 바꿔야 하는 대공사(방
  하나가 같은 방향에 서로 다른 이웃 2개를 가지려면 "방=칸 1개" 가정을 깨야 함). 구현은 보류,
  유저 판단 대기 중.
- **미니맵 방 모양 반영**: `MinimapUI`가 지금까지 모든 방을 24×24 고정 정사각형 아이콘으로
  표시하던 것을 `RoomController.Footprint` 기반으로 교체. 정사각형/가로직사각형은 아이콘 1개를
  Width:Depth 비율로 리사이즈(긴 변은 기존과 동일한 24, 짧은 변만 축소 — 인접 셀과 안 겹침).
  ㄱ자는 `RoomController.BuildNotchWalls`/`BuildNotchCorners`와 같은 방식으로 노치를 뺀 2개의
  겹치지 않는 직사각형으로 분해해 아이콘 조각 2개로 표시. `_iconsByRoomId`를 `Image`→`Image[]`로
  바꿔 여러 조각의 색/표시 상태를 한번에 갱신.

**Stage 2.5 후속 — §6 #3 착수 전 "방/장애물" 세부 계획 확정 + Phase 1(방 껍데기 타일화) 구현**
- 유저 요청으로 "방/장애물 배치"와 "A* 경로탐색"을 분리(§6 #3), 그중 "방/장애물 배치"를 다시
  "방 구성"과 "장애물 배치"로 나눠 이번 세션은 방 구성만 진행. 장애물(`cyberpunk_game_kit`)과
  A* 경로탐색은 완전히 별도 작업으로 보류.
- **설계 논의(Plan Mode)**: `RoomController.BuildGeometry`의 primitive(Plane/Cube) 방 껍데기를
  `Assets/Art/Props/modular_space`(Kenney Modular Space Kit, CC0) 타일로 교체하는 안(A안: 런타임
  절차적 타일 배치)과, 전투방 한정 가로직사각형/ㄱ자(L자) 모양 추가 안(대안2: 사각형 1~2개
  합집합 모델, 바운딩박스+선택적 노치로 구체화)을 각각 2~3개 대안과 비교해 유저 승인받음.
  계획 파일: `squishy-spinning-shore.md`(로컬 플랜 캐시). **이번 세션은 Phase 1(A안, 정사각형
  방 껍데기 타일화)까지만 구현** — Phase 2(모양 다양화)는 다음 세션.
- **타일 실측**(`RunCommand`로 임시 인스턴스화 → `Renderer.bounds` 측정): `template-floor`는
  피벗이 중심(4×0×4), `template-corner`는 X/Z 모두 중심(4×4.25×4, 곡면 쿼터 코너 — 직선 2변이
  아니라 라운드 벽이었음, `SceneView_CaptureMultiAngleSceneView`로 확인). `template-wall`(4×4.25×1)/
  `template-wall-half`(2×4.05×1)는 두께축(로컬 Z)이 피벗 기준 -1~0으로 치우쳐 있어(중앙 정렬
  아님) 배치 시 `outwardNormal * -0.5` 보정 필요 — 안 하면 벽 두께가 방 경계선에 안 맞고 밀림.
- **DoorWidth 6→8 확정**: 어제 계산한 대로 3개 방 크기(16/20/40) 전부 코너+벽(4)+반벽(2) 조합으로
  깔끔히 떨어짐(16: 문이 벽 구간 전체, 20: 반벽 1개씩, 40: 벽 3개씩). `WallHeight` 상수는 완전히
  제거 — 이제 벽 높이가 코드 값이 아니라 타일 메시 자체(4.25)로 결정됨.
- **신규 에셋**: `RoomTileSet`(신규 ScriptableObject, `Assets/Scripts/Room/RoomTileSet.cs`) —
  코너/벽/반벽/바닥 프리팹을 직렬화 필드로 참조(Resources.Load 문자열 경로 대신 — unity63-architecture
  스킬의 authored-definition 기준). `Assets/Prefabs/RoomTiles/`에 4개 타일 프리팹 생성, 각각
  `BoxCollider` 추가(Kenney FBX는 콜라이더 없음) — 바닥은 얇은 콜라이더(Ground 레이어는 인스턴스
  단위로 코드에서 부여), 벽/반벽은 실측 바운드 그대로, 코너는 곡면 벽을 두 개의 겹치는 박스(북쪽 변
  + 서쪽 변)로 근사(`RoomBounds`의 클램프가 실질 충돌 안전망이라 정밀 피팅은 불필요 판단).
  `RoomTileSet_ModularSpace.asset`으로 4개 프리팹 묶어 `Room_Base.prefab`에 배선.
- **`RoomController.BuildGeometry`/`BuildSide` 전면 재작성**: 체커 바닥 텍스처/머티리얼 코드
  전부 제거, 바닥은 코너 4칸을 제외한 나머지 그리드 셀에 `template-floor` 타일링, 코너는
  `PlaceCorner`(4곳, 회전 0/90/180/270 — 기본 방향이 북+서쪽 벽이라 NE/SE/SW는 90도씩 회전),
  벽은 `PlaceWallRun`(문 없는 변은 통짜 타일링, 문 있는 변은 문 폭 8 좌우로 벽/반벽 채움 후 기존
  문 트리거/블로커/마커/EntryPoint 로직 재사용 — 이 부분은 안 건드림).

**Phase 2(전투방 모양 다양화 — 가로직사각형/ㄱ자) 같은 세션에 이어서 구현**
- **`RoomFootprint`**(신규 값 타입, `Assets/Scripts/Room/RoomFootprint.cs`): 바운딩박스(Width,
  Depth) + 선택적 모서리 노치(`RoomCorner? NotchCorner`, NotchWidth, NotchDepth)로 방 모양을
  표현. `Square`/`Rectangle`/`LShape` 팩토리. `RoomData.RoomSize`(float)는 `Footprint.Width`를
  반환하는 하위호환 프로퍼티로 유지, 실제 지오메트리 코드는 전부 `Footprint`를 읽도록 전환
  (`RoomData`/`RoomController`/`ProceduralFloorGenerator`/`GameFloors`/`TestHelpers` 전부 수정).
- **`RoomBounds.cs`에 footprint 오버로드 추가**(기존 스칼라 오버로드는 그대로 유지, 회귀 없음):
  1단계 바운딩박스 클램프(Width/Depth 축별) → 2단계 결과가 노치 내부면 가장 가까운 노치 안쪽
  면으로 밀어냄. `RunCommand`로 직접 검증(정사각형 케이스가 기존 스칼라 API와 완전히 동일한
  값을 내는지, 노치 안쪽 점이 정확한 안쪽 면으로 밀려나는지) 후 `RoomBoundsTests.cs`에 영구
  테스트로 반영.
- **`RoomController` 지오메트리 함수 확장**(`BuildFloor`/`BuildCorners`/`BuildSide`가 전부
  `RoomFootprint`를 받도록): 노치가 있으면 바운딩박스 코너 4개 중 노치 쪽 1개는 스킵하고, 노치의
  두 바깥 경계에 새 볼록 코너 2개(`BuildNotchCorners` — 회전은 노치 코너와 동일한 값을 재사용,
  Phase 1 회전 매핑과 같은 원리로 유도됨) + 안쪽 오목(reflex) 코너 1개(전용 타일 없음 —
  `template-wall`/`-half` 두 run이 직각으로 맞물리는 것으로 처리, `BuildNotchWalls`)를 추가.
  노치에 걸리는 2변은 벽 run 길이가 짧아지고(문이 있으면 그 짧아진 구간 안에서 중앙 정렬 —
  `BuildSide`를 대칭 `half` 대신 비대칭 `runStart/runEnd` 받도록 일반화), 바닥 타일링도 노치
  영역 전체 + 새 코너 2칸을 스킵하도록 확장.
- **`RoomShapeWeights`/`RoomShapeKind`**(`Assets/Scripts/Stage/`, `VariantWeights`와 동일 패턴):
  전투방 한정, `ProceduralFloorGenerator`가 층별 규칙에서 뽑음(정사각형 40/가로직사각형 44×28/
  ㄱ자 40×40+16×16 노치, 전부 4의 배수·이전에 확정한 문폭 8과 맞물림). ㄱ자의 노치 방향(4곳 중
  하나)도 별도 시드 스트림(`roomShape.f{n}`)에서 랜덤 — 그래프/적 스트림과 분리돼 있어 모양
  가중치를 바꿔도 던전 구조나 적 배치는 안 흔들림. `FloorRule`에 `RoomShapeWeights` 필드 추가,
  `ProceduralRules.json`에 `roomShapeWeights` 신규 필드(없으면 정사각형 100%), 층별 실제 값 기입
  (1층 square:6/wideRect:2/lShape:2 → 3층 square:3/wideRect:3/lShape:4, 층이 갈수록 다양해짐).
  전투방 적 원형 배치 수식(`BuildCombatEnemies`)은 `Mathf.Min(Width,Depth)`로 보수적으로 유지 —
  노치를 완전히 피하는 배치는 이번 범위 밖(안전망은 `ClampToInterior`, 후속 과제로 명시).
  JSON 수동 방 정의(`GameFloors.cs`)는 이번엔 정사각형만 유지(신규 필드 추가 안 함) — 아무도 JSON
  경로로 비정사각형 방을 만들 계획이 없어 범위를 줄임(계획 문서와의 유일한 의도적 축소).
- **발견/수정**: `Assets/Tests/PlayMode/TestHelpers.cs`의 `BuildRoom`이 `AddComponent<RoomController>()`로
  맨 컴포넌트만 만들고 `tileSet`을 안 채워주고 있었음 — Phase 1 때부터 있던 잠재 회귀(자동화
  PlayMode 테스트가 `tileSet` null로 `BuildGeometry`에서 NRE 났을 것, 이번 세션 전까지 실행을
  안 해서 못 잡았음). `RoomTileSet_ModularSpace.asset`을 `AssetDatabase.LoadAssetAtPath`로 로드해
  리플렉션으로 주입하도록 수정.

### 테스트 결과
- [x] 재컴파일 콘솔 에러 0(`AssetDatabase.Refresh()` 후 확인).
- [x] **좌표/회전 정확성 검증(`RunCommand`, Edit Mode)**: 두 크기(16, 20)로 합성 `RoomData`를
      만들어 실제 `Initialize()` 호출 후 자식 오브젝트 로컬 좌표를 전수 확인.
      - 20(문 있음): 코너 4곳 (±8,±8) 회전 0/90/180/270 정확, 문 없는 3변은 각 3개 벽타일
        (x −4/0/4, z=±9.5 — 두께축 −0.5 보정 반영됨), 문 있는 변은 반벽 2개(x=±5)+문(x=0,
        z=10) — 계산과 정확히 일치.
      - 16(문 있음, "문이 벽 구간 전체를 차지"하는 경계 케이스): 문 없는 3변은 각 2개 벽타일,
        문 있는 변(East)은 벽타일 0개(코너 바로 옆이 곧장 문) — `PlaceWallRun`의 0-길이 처리
        정상 확인.
      - Edit Mode에서 합성 테스트를 돌리다 보니 `Destroy(marker.GetComponent<BoxCollider>())`가
        "Destroy may not be called from edit mode" 경고를 냄 — 기존 코드(안 건드림)가 Edit Mode
        전용 API 오용, 실제 게임은 항상 Play Mode에서 호출되므로 무해. 별도 후속 과제로 남김
        (사소하지만 `DestroyImmediate` 분기 추가가 정석).
    - [x] **실제 Play Mode 확인**: 씬 진입 → 절차적 생성으로 8개 방(Start 16, Combat 40×5, Item 20,
      Boss 40) 전부 타일로 생성, 콘솔 에러/경고 0. Start room `SceneView_CaptureMultiAngleSceneView`로
      캡처 — 4×4 타일 바닥, 라운드 코너 4곳 전부 정상, 벽 이음새 시각적으로 깨끗함.
    - [ ] **미확인**: 플레이어가 문을 실제로 통과하는지, 벽 콜라이더가 물리적으로 막는지는 이번
      세션에서 자동 검증 못함(Play Mode 캐릭터 이동은 실유저 조작 필요) — 다음 실플레이 때 확인 필요.
    - [ ] **미확인**: `RoomBoundsTests`/`ProceduralInvariantsTests` 등 자동화 테스트 스위트는
      이번 세션에서 실행 안 함(Editor가 이미 MCP로 연결돼 있어 배치모드 2중 실행을 피함) — 다만
      `RoomBounds.cs`/`RoomData.cs`는 이번 Phase에서 손대지 않아 회귀 위험은 낮다고 판단.

**Phase 2 검증**
- [x] 재컴파일 콘솔 에러 0(두 차례 — 코드 변경 직후, `ProceduralRules.json` 수정 직후).
- [x] **`RoomBounds` footprint 클램프 직접 검증(`RunCommand`)**: 정사각형 footprint 결과가 기존
      스칼라 API와 완전히 동일(회귀 없음 확인), 노치 안쪽 깊은 점이 정확한 안쪽 면 좌표로 밀려남,
      노치 바로 바깥 점은 안 건드림, 메인 인테리어/남쪽벽 바깥 점은 기존과 동일하게 동작 —
      `RoomBoundsTests.cs`에 동일 케이스 영구 테스트로 반영.
- [x] **좌표 전수 검증(`RunCommand`, Edit Mode)**: 가로직사각형(44×28, 문 2개)과 ㄱ자(40×40,
      NE 노치 16×16, 문 2개 — 하나는 노치로 짧아진 North변, 하나는 안 짧아진 West변)를 합성
      `RoomData`로 만들어 자식 오브젝트 좌표 전수 확인. 코너 4곳(+ㄱ자는 노치 코너 2곳) 위치·회전,
      각 변 벽/반벽 개수·위치, 짧아진 변의 문 중심이 그 변의 실제 가용 구간 중앙에 오는지, 노치의
      두 안쪽 벽(오목 코너에서 만남) 길이·위치·회전까지 손계산과 전부 정확히 일치.
- [x] **오목 코너 시각 확인(`SceneView_CaptureMultiAngleSceneView`)**: 전용 코너 메시가 없어
      벽 두 run이 직각으로 맞물리는 방식이 실제로 깨끗하게 보이는지 확인 — 약간의 이음선은
      보이지만 구멍/겹침 없이 자연스러운 벽 모서리로 보임(Phase 1의 볼록 코너 콜라이더 근사와
      비슷한 수준의 타협, 문제 없다고 판단).
- [x] **실제 Play Mode 확인**: 씬 진입 → 절차적 생성 층에 실제로 ㄱ자 전투방(NW 노치)이 섞여
      나오는 것 확인, 콘솔 에러/경고 0. 해당 방을 게임 내 실제 컨텍스트에서 캡처 — 모양·문·바닥
      전부 정상.
- [ ] **미확인**(Phase 1과 동일 항목, 여전히 미해결): 플레이어 실조작으로 노치 구간 통과/충돌
      확인, `RoomBoundsTests`/`ProceduralInvariantsTests` 자동화 테스트 스위트 실제 실행.

### 이슈/메모
- `template-corner`가 직선 2변이 아니라 곡면(쿼터 라운드) 코너라는 건 실측 bounds만으로는 안
  드러남 — `SceneView_CaptureMultiAngleSceneView`로 실제 메시를 봐야 알 수 있었음. 좌표만 믿고
  진행했으면 콜라이더 형태를 잘못 잡을 뻔함.
- 코너 콜라이더(북쪽변+서쪽변 박스 근사)는 곡면을 정확히 감싸지 않음 — `RoomBounds` 클램프가
  실질 충돌 방지 역할을 하므로 기능상 문제는 없다고 판단했지만, 코너 근처에서 플레이어가 시각적
  메시보다 살짝 안쪽/바깥쪽에서 막히는 느낌이 들 수 있음. 실플레이 확인 후 필요하면 콜라이더
  조정.
- Ground 레이어: 코너 타일 전체(바닥+벽 아치)에 Ground 레이어를 부여 — 벽 아치 부분도 Ground로
  잡혀서 `GrenadeActiveItem`의 착지 레이캐스트가 코너 벽에서도 히트할 수 있음(사소한 부정확,
  기능적 문제는 아님).

**문 오프셋/미니맵 검증**
- [x] 재컴파일 콘솔 에러 0.
- [x] 문 오프셋: `RunCommand`로 같은 44×16 방을 RoomId만 다르게(1, 2) 두 번 빌드 — 손계산한
      두 분기(문이 서쪽에 붙는 경우/동쪽에 붙는 경우) 좌표와 정확히 일치, 실제로 RoomId에 따라
      반대쪽에 붙는 것 확인.
- [x] 미니맵: 실제 `MinimapIcon`/`MinimapConnector` 프리팹을 배선한 `MinimapUI`를 임시 Canvas에
      만들고 정사각형/가로직사각형(44×16)/ㄱ자(NE 노치) 3방을 `Initialize()`에 넣어 생성된 모든
      아이콘 조각의 `anchoredPosition`/`sizeDelta`를 손계산과 대조 — 전부 정확히 일치(ㄱ자는
      조각 2개의 위치·크기까지).
- [ ] **미확인**: 미니맵을 실제 게임 화면에서 스크린샷으로 확인은 못함 — 현재 `Prototype.unity`에
      `MinimapUI` 컴포넌트가 씬에 존재하지 않음(`StageManager.minimapUI` 필드가 비어있거나 아직
      씬에 배치 안 된 상태로 보임, 이번 변경과 무관한 기존 상태). 실제 화면 확인은 미니맵이 씬에
      배선된 뒤 필요. **(유저가 이후 직접 게임 실행해서 미니맵 정상 표시 확인함 — 이 항목 해결됨.)**

**Phase 3 — 진짜 멀티셀 방(아이작 방식 큰 방) 도입**
- 유저가 실플레이 중 "아이작처럼 한쪽 변에 문이 2개인 방도 있지 않냐"고 질문 → 조사 결과 오늘
  Phase 1/2의 가로직사각형/ㄱ자는 겉모습만 다른 **단일 칸** 방이었고, 아이작의 진짜 큰 방은 격자
  칸을 2개(가로직사각형) 또는 3개(2x2에서 모서리 1개 뺀 ㄱ자)씩 차지하는 **멀티셀** 방이라는 게
  이번 대화에서 명확해짐. Plan Mode로 계획 재수립(`squishy-spinning-shore.md` 덮어씀, 기존
  Phase 1/2 계획은 소진된 상태였음) 후 구현.
- **설계 핵심 — "그래프 재작성"이 아니라 "생성 후 병합 패스"**: `ProceduralFloorGenerator`의
  격자 성장(flood-fill)·데드엔드/보스/아이템 판정은 전혀 안 건드림. 그 로직이 끝나 단일-셀
  그래프가 확정된 *뒤에*, Combat-Combat 인접 쌍(또는 2x2에서 모서리 1개가 "아예 안 깔린" 삼중)을
  골라 하나의 `RoomData`로 합치는 후처리 패스(`MergeCombatCells`)를 추가. 기존에 이미 검증된
  성장/보스판정 로직을 재사용하므로 "대공사"라고 걱정했던 것보다 실제로는 수술 범위가 작았음.
- **데이터 모델**: `RoomData.GridPosition`을 `Vector2Int` → `Vector2`로 승격(병합된 방은 구성
  칸들의 평균 위치, 예: (0,0)+(1,0) 병합 → (0.5,0)). `RoomDoorConnection`에 `SubCellIndex`(0/1,
  기본값 0) 추가 — 병합된 방의 한 변이 원래 칸 2개 몫이면 물리적 문 슬롯이 2개일 수 있어서,
  어느 연결이 어느 슬롯(서/남=0, 동/북=1)인지 표시.
- **`RoomController` — 방향당 문 여러 개**: `_doors`를 `Dictionary<DoorDirection, DoorController>`
  →`Dictionary<(DoorDirection, int SubCellIndex), DoorController>`로 변경. `BuildSide`는 연결
  중 `SubCellIndex!=0`이 하나라도 있으면 그 변을 중점에서 반으로 쪼개 **오늘 만든 "문 0/1개 +
  오프셋" 로직을 각 절반에 독립적으로 재적용**(새 알고리즘이 아니라 기존 로직의 반복 호출).
  `ConnectDoor(direction, subCellIndex, target)`으로 시그니처 확장.
- **진짜 걸림돌 발견 및 수정 — `RoomTransitionController`**: `door.TargetRoom.GetDoor(Opposite(문
  방향))`로 도착 문을 찾던 로직이 "반대 방향엔 문이 항상 하나뿐"이라는, 병합으로 깨지는 가정에
  의존하고 있었음. `DoorController`에 `OwnerRoom`(이 문이 붙어있는 방, `Setup()` 시점에 주입)을
  추가하고, 도착 문 조회를 "방향 기반"에서 "`TargetRoom`이 원래 있던 방을 가리키는 문 찾기"로
  교체 — 방향당 문 개수 제한과 완전히 무관해짐.
- **병합 크기**: 가로직사각형 92×40(칸 2개 몫, 2×50 격자 간격에서 여유 뺀 값), ㄱ자 바운딩박스
  92×92 + 노치 44×44(칸 1개 몫 — 92−44−타일격자코너8=40, 짧은 두 변이 정확히 일반 방 폭과
  같아짐). 오늘 단일-셀 시절의 44×16/40×40+16×16은 전부 폐기(같은 개념의 진짜 버전으로 대체,
  두 갈래로 유지 안 함).

### Phase 3 검증
- [x] 재컴파일 콘솔 에러 0.
- [x] **실제 시드로 병합 발생 확인(`RunCommand`, `ProceduralFloorSource` 직접 호출)**: 2000시드
      스캔 — 가로직사각형(가로/세로 방향 둘 다), ㄱ자(NE/SE 등 여러 노치) 실제로 나옴. 각 사례의
      `GridPosition`(분수 좌표), `Footprint` 치수, `SubCellIndex`까지 전부 손계산과 정확히 일치.
      모든 사례에서 `FloorGraph.Validate` 통과(연결 재매핑이 실제로 올바르게 동작함을 확인).
- [x] **문 왕복 조회 핵심 시나리오 직접 검증**: "한쪽 변에 같은 방향 문 2개"는 지금 격자 성장
      알고리즘(성기게 자라는 방식)에서는 48,867개 방을 스캔해도 자연 발생 안 함(구조적으로
      드묾/거의 없음 — 버그 아니라 성장 알고리즘의 성질) → 실제로 그 상황을 만들 실플레이
      시나리오가 없어서, 직접 합성으로 검증: 병합 방 하나에 North 문 2개(SubCellIndex 0/1, 서로
      다른 타겟)를 만들고 `ConnectDoor`로 연결 → 각 문의 `localPosition`이 정확히 서/동 절반에
      위치, `RoomTransitionController.FindDoorTo`와 동일한 로직("TargetRoom으로 문 찾기")을
      재현해 두 타겟 각각에 대해 정확히 반대쪽이 아닌 올바른 문을 찾아내는 것 확인 — 핵심 리스크
      해소.
- [x] **미니맵**: 실제 병합 방(분수 GridPosition, 92×40 footprint)을 `MinimapUI`에 넣어 아이콘
      위치/크기가 손계산과 정확히 일치 확인.
- [x] **EditMode 자동화 테스트 스위트 실제 실행**(오늘 계속 미뤄온 부채 — `TestRunnerApi`를
      `RunCommand`로 직접 구동해 인프로세스로 실행, 별도 Unity 프로세스 안 띄움): 1차 실행 89개 중
      2개 실패 — 둘 다 실제 문제 발견:
      1. `EverySeed_RoomCounts_MatchTheRulesInUse_NoTrivialFallback`: 병합이 방 개수를 줄이므로
         "개수가 규칙 범위 안"이라는 옛 가정이 깨짐(의도된 동작). 테스트를 상한 체크 + "트리비얼
         폴백이면 모든 방 적 0마리"라는 진짜 판별 기준으로 교체(`EverySeed_RoomCounts_
         StayWithinMax_AndNoTrivialFallback`).
      2. `RoomBoundsTests`의 가로직사각형 클램프 테스트: 오늘 오전 Phase 2 때 내가 직접 쓴 테스트의
         손계산 실수(`WallThickness/2` 항을 빼먹음, 21.4 아닌 20.9가 맞음) — 실제 코드 버그
         아니었음, 테스트만 수정.
      재실행 결과 89/89 전부 통과.
- [x] **실제 Play Mode**: 절차적 생성 스캔으로 확인한 병합 사례들이 실제 게임 실행에서도 동일하게
      나옴, 콘솔 에러 0.

### 이슈/메모 (Phase 3)
- **Unity MCP RunCommand 캐시 버그로 큰 시간 소모**: `RoomDoorConnection`/`RoomData` 두 파일에
  새 생성자 인자/타입을 추가했는데 `RunCommand`가 몇 차례의 `AssetDatabase.Refresh()`/
  `RequestScriptReload()`/Play Mode 재진입/강제 동기 리임포트에도 계속 옛 시그니처로 컴파일 →
  나중에 알고 보니 **진짜 원인은 별개의 실제 컴파일 에러**(`ProceduralInvariantsTests.cs`가
  `HashSet<Vector2Int>`에 새 `Vector2` `GridPosition`을 넣으려다 실패)였고, 이 에러가 콘솔에
  `Type: "Log"`로 찍혀서 그동안 `Types: ["Error"]`로만 걸러보던 확인 방식이 계속 놓치고 있었음.
  Play Mode도 이 에러 때문에 진입하자마자 자동으로 빠져나오고 있었음(증상: `ManageEditor
  GetState`가 매번 `IsPlaying:false`). **교훈**: 컴파일 이상 의심될 때는 `Types` 필터링 없이
  전체(`Error`+`Warning`+`Log`)를 봐야 함 — 다음에도 같은 함정에 빠지지 않도록 메모리에 남김.
- 병합 후보 탐색은 Combat-Combat 인접 쌍만 보고, 2x2-빠진-모서리는 "4번째 칸이 아예 안 깔림"만
  인정(다른 방이 거기 있는 경우는 후보에서 제외) — 더 공격적으로 병합하려면 나중에 확장 가능.
- `BuildCombatEnemies`의 원형 적 배치 수식은 여전히 footprint 비인지형(`Mathf.Min(Width,Depth)`
  사용, Phase 2부터 있던 known limitation) — 92×40처럼 가로세로 비율이 더 커진 병합 방에서는
  적들이 짧은 축(40) 기준 원에 몰릴 수 있음. 안전망(`ClampToInterior`)은 여전히 작동하므로
  기능적 버그는 아니지만, 후속 과제로 남음.

**가로직사각형 좁은 폭 축소 + 좁은 면 문 중앙 고정 (유저 요청)**
- `MergedWideShort` 40→16(가로직사각형의 짧은 축 — 코너 2개(8)+문폭(8)의 최소치, 오늘 세션 초반에
  이미 확인한 하한과 동일한 값).
- **버그 발견 및 수정**: 문 오프셋 여부를 "North/South면 오프셋, East/West면 중앙"으로 방향에
  고정해서 판단하고 있었는데, 이건 가로 병합(긴 축=Width)에서만 맞고 **세로 병합(긴 축=Depth)에서는
  거꾸로였음** — 세로 병합 방의 진짜 좁은 면(North/South, 길이=Width)이 오프셋되고 진짜 긴 면
  (East/West, 길이=Depth)이 중앙 고정되는 반대 상황이었음. `Width>Depth`로 실제 긴 축을 판별해서
  그 축을 따라 도는 두 변만 오프셋하도록 수정 — 방향이 아니라 실제 치수로 판단. ㄱ자(4변 전부
  오프셋)는 그대로 안 건드림.
- `RunCommand`로 가로(92×16)/세로(16×92) 두 방향 다 긴 축 문은 오프셋, 짧은 축 문은 중앙에 오는 것
  손계산과 대조해 확인. 세로 병합 케이스는 이번에 고친 버그가 실제로 고쳐졌다는 증거이기도 함
  (수정 전이었으면 반대로 나왔을 것).
- EditMode 테스트 스위트 재실행 89/89 통과, 실제 Play Mode 콘솔 에러 0 재확인.

**방 크기 추가 축소 (유저 요청 — 이동 시간이 너무 길다)**
- `MergedWideLong`(가로직사각형 긴 축) 92 → 56 (40% 축소, 여전히 4단위 배수).
- `CombatRoomSize`(일반 정사각형 전투방) 40 → 28 (30% 축소, 4단위 배수라 계산이 딱 떨어짐).
  Boss/Start/Item 방은 요청 범위 밖이라 안 건드림. ㄱ자 병합 바운딩박스(92)/노치(44)도 이번엔
  언급 안 하셔서 그대로 둠.
- `RunCommand`로 56×16/28×28 새 크기 방을 직접 빌드해 문 오프셋·중앙 배치 좌표 손계산과 대조,
  EditMode 테스트 89/89 재확인, 실제 Play Mode 콘솔 에러 0.

**ㄱ자 방 축소 + 직사각형 긴 변 문 위치 개선 (유저 요청)**
- 호칭 정리: 유저 요청으로 이 방 모양을 앞으로 "ㄱ자"로 통일해서 부르기로 함(메모리에도 기록,
  `feedback_giyeok_room_naming.md`) — 코드/DevLog의 `RoomCorner`/"L-shape" 식별자는 그대로 유지.
- **ㄱ자 방 축소**: `LMergeBounding` 92→44, `LMergeNotch` 44→20 — "일반 전투방(28) 크기의 150%
  정도"로 해석해 28×1.5=42를 타일격자(4의 배수)에 맞춰 44로 반올림. 노치는 바운딩박스 대비 이전과
  비슷한 비율(약 45%)로 유지해 ㄱ자 형태가 뚜렷하게 보이도록 20으로 설정 — 짧은 두 변은
  44−20−8=16(문폭 최소치, `MergedWideShort`와 동일 값).
- **직사각형 긴 변 문 배치 방식 변경**: 기존엔 문이 한쪽 벽에 완전히 붙는(반대쪽 여유가 전부 쏠리는)
  방식이라 "양 끝에 너무 가깝다"는 피드백 → `BuildSideSegment`의 `offsetDoor` 분기를 "변을 반으로
  나눠 그중 한쪽 구역의 중앙에 문 배치"로 교체(문 기준 좌우 여유가 항상 남게 됨). 이 로직은
  ㄱ자 방의 오프셋 문(오늘 세션에서 4변 전부로 확장해뒀던 것)과 공유되는 코드라 ㄱ자 방의 문
  배치도 같이 개선됨(정사각형은 원래도 중앙 고정이라 무관).
- `RunCommand`로 새 ㄱ자(44×44, 노치 20×20, 4변 문)와 가로직사각형(56×16, RoomId 다른 두 개로
  양쪽 절반 다 검증) 좌표 전부 손계산과 대조 확인. EditMode 테스트 89/89, 실제 Play Mode 콘솔
  에러 0.

**ㄱ자 방 출현빈도 문의 → 병합 알고리즘 2단계 재구성 + 가로직사각형 긴 변 36으로 조정 (유저 요청)**
- 유저가 "3층까지 돌았는데 ㄱ자 방이 한 번도 안 나왔다, 확률을 낮게 잡아둔 거냐"고 문의 →
  `RunCommand`로 200시드×3층 전수 스캔한 결과 원인은 확률 자체가 아니라 **뽑는 순서**였음:
  기존 `MergeCombatCells`가 칸을 하나씩 순회하면서 그 자리에서 바로 Square/WideRect/LShape를
  뽑다 보니, 앞서 뽑힌 WideRect가 옆 칸을 먼저 잡아먹어 버려서 뒤에 오는 칸이 ㄱ자에 필요한
  "2x2 중 3칸이 비어있는" 조건을 만족 못 하게 되는 경우가 흔했음(특히 좁은 층일수록 심함 —
  1층 적중률이 특히 낮았던 이유).
- **2단계로 재구성(유저가 "2번"으로 승인)**: Phase A에서 병합 전 전체 Combat 칸을 먼저 스캔해
  ㄱ자로 묶을 수 있는 모든 (모서리, 변 A, 변 B) 후보를 확률과 무관하게 전부 나열하고, 결정론적
  순서(y→x→Notch)로 정렬한 뒤 각 후보를 독립적으로 `weights.LShape` 확률로 굴림 — 나머지
  Square/WideRect는 기존처럼 Phase B에서 (LShape 제외하고 재정규화한 가중치로) 남은 칸에 한
  번씩 순회. 핵심 알고리즘(격자 성장/데드엔드/보스판정)은 그대로, 병합 후처리 패스 내부
  순서만 바뀐 것.
- **재측정(200시드×3층, 총 2907개 Combat 방)**: ㄱ자 전체 출현율 10.9%(316개). 층별로는
  1층 1.5%, 2층 9.2%, 3층 17.2% — 1층은 여전히 낮지만(칸이 적어 후보 자체가 드묾) 이전보다
  뚜렷하게 개선됐고, 3개 층 전부에서 ㄱ자가 한 번도 안 나온 시드는 200개 중 26개(13%)뿐이라
  "3층까지 못 봤다"는 피드백이 나올 확률 자체가 대폭 줄었음. `FloorGraph.Validate`는 200시드×3층
  전부 통과(불변식 위반 0건).
- **가로직사각형 긴 변 37→36 확정**: 유저가 37을 제안했으나 타일격자(4의 배수) 제약 때문에 36으로
  조정 요청, 승인받음. `MergedWideLong` 56 → 36.
- **타일 정렬 버그 발견 및 수정**: 36 적용 검증 중 기존 "문 중앙 배치(변을 반으로 나눠 그중
  한쪽 구역 중앙)" 공식이 `L/4`(quarter-point) 기반이라 `L`이 8의 배수일 때만 타일(4/2단위) 정렬이
  맞고, 8의 배수가 아니면(36 폭 방의 한 변인 28 등) `leftLength`가 3처럼 2의 배수가 아닌 값이
  나와 벽 타일링에서 1유닛 틈이 생기는 걸 발견 — 이전에 "검증 완료"라고 보고했던 44 바운딩
  ㄱ자도 문/코너 좌표만 확인했지 전체 벽 타일 커버리지는 확인 안 해서 같은 결함이 잠재해 있었음
  (유저에게 사실대로 알리고 재작업). `BuildSideSegment`의 `offsetDoor` 공식을
  `Mathf.Round(rawNearSideLength / HalfTileSize) * HalfTileSize`로 반올림-스냅하는 방식으로
  교체 — 특정 배수 조건을 요구하는 대신 4의 배수이기만 하면 어떤 방 치수에도 안전하게 작동하는
  일반해로 수정.
- `RunCommand`로 36폭 가로직사각형과 44 바운딩 ㄱ자 양쪽 다 North/South/East/West 4변의
  Wall_/WallHalf_/Door_ 자식 오브젝트 좌표를 전부 덤프해 `leftLength+DoorWidth+rightLength`가
  각 변 전체 길이와 정확히 일치하는지(틈 0) 재확인 — 이번엔 문/코너뿐 아니라 벽 타일 전체를 검증.
- EditMode 테스트 스위트 재실행 89/89 통과, 실제 Play Mode 콘솔 에러 0 재확인(진입 로그만 확인됨).

**[버그 수정] 병합 방 잠긴 문 옆으로 빠져나갈 수 있는 벽 틈 (유저 실플레이 신고)**
- 유저 신고: ㄱ자/가로직사각형 방에서 적이 있어 문이 잠겨 있을 때, 문 자체가 아니라 문 근처에서
  캐릭터로 "비비면"(벽을 따라 비스듬히 밀면) 방 밖으로 빠져나갈 수 있다.
- **원인 조사**: `RunCommand`로 실제 절차적 생성 결과(병합된 WideRect/ㄱ자 방)를 그대로
  인스턴스화하고 모든 문을 강제로 `Lock()`한 뒤, 벽/문 콜라이더의 실제 월드 AABB를
  변(side)별로 정렬해 인접한 두 콜라이더 사이에 물리적으로 빈틈이 있는지 전수 대조하는 방식으로
  확인(처음엔 `Collider.bounds`를 그대로 썼다가 Edit Mode에서는 PhysX 바운드가 지연 갱신돼
  전부 프리팹 기본값(-2~2)으로 나오는 함정에 걸려, `BoxCollider.center/size`를 트랜스폼으로
  직접 월드 변환하는 방식으로 교체).
- **실제 원인**: `RoomController.BuildSide`가 병합 방의 한 변을 2개 서브셀로 나눠 각각
  `BuildSideSegment`를 호출할 때, 서브셀 내부의 문 배치는 `offsetDoor`를 항상 `false`로 넘겨
  "그 절반 구간 안에서 중앙 배치"(`leftLength = rightLength = remain / 2f`)로 처리하는데, 이
  분기는 결과값을 타일 격자(`HalfTileSize=2`)에 맞춰 반올림하지 않았음(오늘 세션 앞서 고친
  `offsetDoor==true` 분기의 반올림 로직은 여기엔 적용 안 돼 있었음). 서브셀 절반 구간의 남는
  길이(`remain`)가 4의 배수가 아닌 경우(예: 36폭 가로직사각형의 서브셀 절반 run=14 → remain=6 →
  half=3) `PlaceWallRun`의 4단위/2단위 그리디 타일링이 2단위까지만 채우고 남은 1유닛을 그냥
  건너뛰어서, **문 옆에 벽 타일이 아예 존재하지 않는 1유닛 폭의 실제 구멍**이 생겼음(문 콜라이더
  자체는 정확한 위치/크기로 잠겨 있었음 — 문이 아니라 문과 벽 사이의 이 틈으로 빠져나간 것).
  이 분기(서브셀 분할)는 한쪽 변에 서브셀 0/1 중 하나에만 문이 있어도(즉 "한 변에 문 2개"가
  아니라 흔한 한 변에 문 1개짜리 병합 방에서도) 무조건 타는 경로라 영향 범위가 넓었음 — 현재
  `MergedWideLong=36`/`LMergeBounding=44` 둘 다 8의 배수가 아니라서(서브셀 절반의 remain이
  4의 배수가 되려면 전체 변 길이가 8의 배수여야 함) 사실상 모든 병합 가로직사각형/ㄱ자 방에서
  재현 가능한 상태였음.
- **수정**: `BuildSideSegment`의 `else`(서브셀 중앙 배치) 분기도 `offsetDoor` 분기와 동일하게
  `Mathf.Round((remain/2f)/HalfTileSize)*HalfTileSize`로 반올림 후 나머지를 빼는 방식으로 교체 —
  어떤 변 길이에도 안전한 일반해(문 위치가 절반 구간 정중앙에서 최대 1유닛 미세하게 밀릴 수
  있지만 육안으로는 구분 안 됨, 틈은 항상 0).
- **검증**: 수정 전 재현(200시드 내 WideRect/ㄱ자 실표본 1개씩 직접 빌드 → 서브셀 분할 변에서
  실제로 1유닛 GAP 로그 확인) → 수정 후 300시드에서 병합 Combat 방 401개(그중 서브셀 분할 변을
  가진 방 176개) 전수 재검사, 벽/문 콜라이더 틈 0건. EditMode 테스트 89/89 재통과, 실제 Play
  Mode 콘솔 에러 0.

### 다음 작업
- ~~플레이어 실조작으로 문 통과·벽 충돌·ㄱ자 노치 구간 이동·문 오프셋·병합 방 진입/문 왕복,
  "잠긴 문 옆 틈" 버그 수정 확인~~ — 유저 실플레이로 확인 완료.
- **Phase 1/2/3(방 구성)은 오늘로 완전히 종료.** 이번 세션에서 `Assets/Art/Props/`(무료 CC0
  에셋 — `modular_space`/`cyberpunk_game_kit`)까지 함께 커밋 완료.
- **다음 작업 시간 예정**: §6 #3의 나머지 절반 — **방 안 장애물 배치**(`cyberpunk_game_kit`
  소품, Fence/Support/Pipe/Computer/AC/TV 등 — `Platform_*`류는 원래 플랫포머용이라 제외
  예정, 2026-09-10 (2) 에셋 선정 결정 참고) + **적 경로탐색(그리드 A*, `EnemyChaseState`
  이동 교체)**. 오늘 방 지오메트리가 프리미티브에서 타일 그리드(4유닛) 기반으로 바뀌었으니
  장애물 배치도 이 좌표계 위에서 진행.
- 후속 과제(급하지 않음, 장애물/A* 작업 중 필요해지면 재검토): 코너 콜라이더를 곡면에 더
  가깝게 다듬을지, `Destroy()`→`DestroyImmediate()` Edit Mode 분기 추가할지, 전투방 적 원형
  배치 수식을 footprint(특히 노치·병합 방) 인지형으로 개선할지, 2x2 통짜 메가룸(칸 4개)
  추가할지.

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

**§6 #3(A* 경로탐색 + 방 장애물) 착수 전 — 장애물/방 에셋 논의 (순수 리서치, 코드·씬 변경 없음)**
- 유저가 Kenney.nl/Quaternius에서 에셋을 받아 `Assets/Art/Props/`에 배치(`cyberpunk_game_kit`,
  `modular_space` 폴더). 각 폴더 `License.txt`로 실제 팩 정체 확인:
  - `cyberpunk_game_kit` = Quaternius **"Ultimate Platformer Pack"**(폴더명과 실제 팩 이름이 다름
    — 플랫포머 게임용 팩).
  - `modular_space` = Kenney **"Modular Space Kit"**.
- **1차 평가**: 둘 다 스타일이 기존 캐릭터 일러스트(코스믹/골드톤 애니메이션풍, `Assets/Art/UI/Character/`)와
  거리가 있고, 스코프도 과함(장애물 하나 필요한데 캐릭터·적·복도까지 포함된 풀 킷) — 부적합 판정.
- 유저가 "우주 컨셉만 느슨하게 맞으면 된다"로 조건 완화 → 재검토: 2D 포트레이트(메뉴)와 3D
  인게임(현재 회색 캡슐)이 다른 레이어라 정합성 요구를 낮춰도 무방하다고 판단. 다만
  `modular_space`의 완제품 방/복도(`room-*`/`corridor-*`)는 `RoomController`의 가변 크기+방향별
  문 시스템과 구조적으로 충돌해 배제, 박스형 소품만 장애물로 한정 권장.
- 유저가 GPT에게 같은 질문 → "`Ultimate Platformer Pack`의 Fence/Support/Pipe/Computer/AC/TV를
  장애물로 쓰고 `Platform_*`(뜬 발판류)는 피하라"는 취지의 답변(장르 적합성 8/10, A* 테스트용
  9/10 등 자체 평가 포함). `Platforms` 폴더 실제 파일명(`Platform_1x1_Empty` 등) 확인 결과 GPT
  설명과 일치 — Claude의 1차 판정(부적합)과 결과적 사용 범위(장애물 소품 한정)는 거의 수렴.
- Kenney/Quaternius 던전·SF 모듈러 킷 추가 검색(Modular Dungeon Kit, Tiny/Mini Dungeon, Modular
  Sci-Fi MEGAKIT, Space Station Kit) 중 `modular_space`가 이미 Kenney Modular Space Kit임을
  재확인(위 오분류 정정 계기).
- **`modular_space`의 `template-*` 타일 실측 + 육안 확인**(`RunCommand`로 임시 임포트 →
  `Renderer.bounds` 측정 + `SceneView_CaptureMultiAngleSceneView` 캡처 → 확인 후 전부 삭제, 씬
  저장 안 함):
  - `template-wall` 4.0×4.25×1.0 / `template-wall-half` 2.0×4.05×1.0 / `template-wall-corner`
    1.0×4.05×1.0 / `template-floor` 4.0×~0×4.0 / `template-corner`(바닥+벽 통짜 코너 블록)
    4.0×4.25×4.0.
  - 방 크기(16/20/40)가 전부 4의 배수 → 타일 그리드(4유닛)와 정확히 맞음. `WallThickness`(게임
    상수 1) ≈ 타일 실측(1.0), 거의 일치.
  - **미해결 디테일 2가지**: (1) `WallHeight`(게임 3) vs 타일 실측(~4.25) 불일치 — 조정 필요.
    (2) `DoorWidth`(6) 기준 세그먼트 길이가 16짜리 방에서 5가 나와 4/2/1 타일 조합으로 안 떨어짐
    — `DoorWidth`를 8로 바꾸거나 방 크기를 재조정해야 타일이 깔끔히 맞음.
  - 비주얼: 흰/회색 + 주황 포인트 + 남색 트림, 밝고 깔끔한 플랫셰이딩 톤 — 캐릭터 아트와 크게
    안 튐(지난번 Quaternius 플랫포머 팩보다 톤 궁합 좋음).
- **결론**: 새 에셋을 더 찾을 필요 없이 지금 갖고 있는 두 팩으로 진행 가능해 보임 —
  `modular_space`(Kenney Modular Space Kit)의 `template-*` 타일로 방 벽/바닥,
  `cyberpunk_game_kit`(Quaternius Ultimate Platformer Pack)의 Fence/Support/Pipe/Computer/AC/TV/
  Sign을 장애물 소품으로(`Platform_*`/`Character`/`Enemies` 폴더는 배제). 대안 비교 상세는
  `AI_Decision_Log.md` 2026-09-10 (2) 참고.

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
- 타이틀 시드 입력 → 던전 재현 실플레이 확인 (위 미확인 항목, 이월)
- **다음 세션에서 "뭐부터 할까" 물으면 이렇게 답할 것 — §6 #3(A* 경로탐색 + 방 장애물) 착수 전
  장애물/방 에셋 논의가 위 "장애물/방 에셋 논의" 항목만큼 이미 끝나 있으므로, 에셋을 또 찾지
  말고 바로 구현 단계로 들어갈 것**:
  1. `RoomController.BuildGeometry`를 `modular_space`의 `template-wall`/`template-wall-half`/
     `template-wall-corner`/`template-floor`/`template-corner` 타일 조합으로 교체(또는 병행) —
     `WallHeight`(3→~4.25)와 `DoorWidth`(6→8 검토) 조정 필요, 위 실측치 참고.
  2. `cyberpunk_game_kit`의 Fence/Support/Pipe/Computer/AC/TV/Sign 중 일부를 콜라이더 포함
     장애물 프리팹으로 만들어 방에 배치(`Platform_*`/`Character`/`Enemies` 폴더는 쓰지 않음).
  3. 장애물 배치 후 기존 직선 추격(`EnemyChaseState`)이 실제로 막히는 상황을 만든 뒤 A*
     길찾기 구현 착수 — 이 순서로 가면 "장애물 때문에 직선 추격의 한계가 드러나서 길찾기를
     도입했다"는 자연스러운 개발 서사가 됨.

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
