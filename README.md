<div align="center">

# RogueLike

**시드로 절차 생성되는 던전을 문으로 이어진 방마다 돌파하는 3D 쿼터뷰 액션 로그라이트**

![Unity](https://img.shields.io/badge/Unity-6000.3.22f1-222222?logo=unity&logoColor=white)
![CSharp](https://img.shields.io/badge/C%23-Gameplay_Programming-7654A3)
![Platform](https://img.shields.io/badge/Platform-PC-315E82)
![Status](https://img.shields.io/badge/Status-Playable_Demo-B08A48)
[![CI](https://github.com/highfive37/RogueLike/actions/workflows/ci.yml/badge.svg)](https://github.com/highfive37/RogueLike/actions/workflows/ci.yml)

게임 클라이언트 개발 포트폴리오 · 개발자 서영훈

![RogueLike 전투 화면](Docs/Images/main.png)

[프로젝트 소개](#프로젝트-소개) · [핵심 플레이](#핵심-플레이) · [구현 포인트](#구현-포인트) · [코드 탐색](#코드-탐색) · [개발 현황](#개발-현황)

</div>

---

## 프로젝트 소개

RogueLike는 시드마다 다른 모양으로 이어지는 던전을 방 단위로 돌파하는 3D 쿼터뷰
로그라이트입니다. 문으로 연결된 방에 들어가 적을 전부 처치해야 다음 방으로 넘어갈
수 있고, 방마다 달라지는 지형·장애물과 대형을 갖춘 적들을 상대하며 층 끝의 보스까지
도달하는 것이 목표입니다.

**절차적으로 생성되지만 항상 풀 수 있는 던전, 그리고 그 던전 위에서 자연스럽게
움직이는 적 AI**를 만드는 것을 목표로 개발하고 있습니다. 게임 자체의 완성도보다,
이 목표를 이루기 위해 어떤 기술적 선택을 했고 왜 그렇게 했는지를 설명할 수 있는
프로젝트로 만드는 데 초점을 맞췄습니다.

| 항목 | 내용 |
| :--- | :--- |
| 장르 | 3D 쿼터뷰 액션 로그라이트 (바인딩 오브 아이작 / 엔터 더 건전 계열) |
| 플랫폼 | PC (상용 출시 계획 없는 포트폴리오 프로젝트) |
| 개발 환경 | Unity 6000.3.22f1 · C# · Universal Render Pipeline 17.3.0 |
| 현재 단계 | 타이틀 → 캐릭터 선택 → 시드 기반 3층 런 → 보스 → 클리어/게임오버까지 처음부터 끝까지 플레이 가능한 개발 데모 |
| 담당 작업 | 1인 개발 — 시스템 설계, 절차적 생성/AI/전투 구현, 데이터 파이프라인, 자동화 테스트, 프로파일링, Claude Code + Unity MCP 기반 AI 협업 워크플로우 설계 |

> **저장소 범위** — 이 저장소는 코드만이 아니라 씬·프리팹·밸런스 데이터를 포함한
> Unity 프로젝트 전체입니다. Unity 6000.3.22f1로 클론해 열면 `Title` 씬부터 바로
> 실행할 수 있습니다. 아래 화면들은 자리 표시자이며, `Docs/Images/`에 실제 캡처를
> 넣고 파일명을 맞추면 그대로 표시됩니다.

## 핵심 플레이

```text
캐릭터 선택 (근접 / 원거리)
        ↓
방 입장 → 전투 → 클리어 (문 해제)
                    ↓
             다음 방으로 이동
                    ↓
          보스 → 클리어 / 게임오버
```

### 01. 시드마다 다른, 그러나 항상 풀 수 있는 던전

같은 시드는 항상 같은 던전을 만들고, 다른 시드는 매번 다른 방 배치·형태·장애물을
만듭니다. 아이작 스타일로 중앙에서 시작해 큐를 성장시키는 방식으로 방 그래프를 만든
뒤, 연결성을 검증해 갈 수 없는 방이 생기지 않도록 보장합니다.

| 절차 생성 던전 (미니맵) | 전투방 내부 |
| :---: | :---: |
| ![던전 미니맵](Docs/Images/dungeon-map.png) | ![전투방 내부](Docs/Images/combat-room.png) |

### 02. 뭉치지 않고 대형을 갖추는 적, 장애물을 우회하는 추격

여러 적이 한 플레이어에게 몰릴 때 서로 밀치며 뭉개지지 않도록 근접/원거리 링에
균등하게 자리를 배분하고, 시야가 장애물에 막히면 그리드 기반 경로탐색으로 우회해
따라옵니다.

![적 포메이션과 경로 우회](Docs/Images/enemy-formation.png)

*여러 적이 원형으로 자리를 나눠 서고, 시야가 막힌 적은 장애물을 돌아서 추격합니다.*

### 03. 텔레그래프가 있는 보스 돌진

보스의 돌진 공격은 예고 없이 순간적으로 맞는 판정이 아니라, 돌진 내내 유지되는
히트박스로 판정하고 전용 넉백을 적용해 "제대로 맞았다"는 느낌을 주도록 설계했습니다.

| 보스 돌진 예고 | 돌진 판정/넉백 |
| :---: | :---: |
| ![보스 돌진 공격 패턴 및 플레이어 넉백](Docs/Images/boss-charge.png) |

## 구현 포인트

### 시드 기반 절차적 던전 생성

자체 제작한 결정적 RNG가 아이작 스타일의 큐 성장 방식으로 방 그래프를 생성하고,
층별 난이도 규칙과 그래프 검증(연결성, 끊어진 연결 확인)을 함께 처리합니다. 절차
생성과 손 authoring한 JSON 참조 층 중 하나를 고를 수 있도록 `IFloorSource` 뒤에
추상화해뒀고, 층 데이터가 손상되면 내장 비상용 층으로 자동 대체됩니다.

- **살펴볼 부분:** 큐 성장 알고리즘, 시드 재현성, 그래프 연결성 검증과 폴백
- **관련 코드:** [ProceduralFloorGenerator.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Stage/ProceduralFloorGenerator.cs) · [DeterministicRng.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Core/DeterministicRng.cs) · [FloorGraph.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Stage/FloorGraph.cs) · [IFloorSource.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Stage/IFloorSource.cs)

### 그리드 기반 A* 경로탐색과 적 포메이션

Unity NavMesh 대신 방/장애물 생성기가 이미 만들어둔 런타임 그리드를 그대로
재사용하는 자체 A*를 적용했습니다. 시야가 막혔을 때만 경로탐색으로 전환하고
확보되면 직선 추격으로 복귀하며, 여러 적이 한 대상에게 몰릴 때는 근접/원거리 링에
매 프레임 균등하게 슬롯을 재분배해 뭉침을 방지합니다.

- **살펴볼 부분:** 시야 차단 시에만 경로탐색으로 전환하는 조건, 슬롯 예약/해제
  생명주기
- **관련 코드:** [RoomPathfindingGrid.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Room/RoomPathfindingGrid.cs) · [RoomAttackSlots.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Room/RoomAttackSlots.cs) · [RoomController.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Room/RoomController.cs)

### 일반 적과 보스가 공유하는 FSM

하나의 상태머신(Idle/Patrol/Chase/Attack/Stunned/Dead)을 일반 적 6종과 보스가
함께 쓰고, 교체 가능한 공격 패턴으로 개체별 차이를 만듭니다. 보스의 돌진 공격은
단발 거리 체크가 아니라 돌진 내내 유지되는 히트박스로 재설계했고, 공격별로 다른
넉백 세기를 인터페이스 변경 없이 적용할 수 있게 만들었습니다.

- **살펴볼 부분:** 상태 전환 조건, 돌진 히트박스의 활성/해제 시점
- **관련 코드:** [EnemyStateMachine.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Enemy/EnemyStateMachine.cs) · [ChargeStrikePattern.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Enemy/ChargeStrikePattern.cs)

### 데이터 기반 밸런스와 던전 규칙

플레이어 스탯, 아이템 효과, 절차 생성 난이도 규칙이 전부 평문 JSON에서 로드됩니다.
Unity를 열지 않고도 텍스트 에디터로 수치를 조정할 수 있고, 파일이 없거나 파싱에
실패하면 내장 기본값으로 대체됩니다.

- **관련 코드:** [GameBalance.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Balance/GameBalance.cs) · [GameFloors.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Stage/GameFloors.cs)

### 오브젝트 & VFX 풀링

범용 오브젝트 풀 하나를 발사체·적·보스·그레네이드·전투 VFX 전부에 재사용합니다.
VFX는 정적 싱글톤이 피격 등 기존 이벤트를 구독해 트리거하는 방식이라, 각 공격/무기
코드가 이펙트 호출을 직접 알 필요가 없습니다.

- **관련 코드:** [GameObjectPool.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Pooling/GameObjectPool.cs) · [CombatVfxLibrary.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Combat/CombatVfxLibrary.cs)

### 격리 벤치마크로 실측한 성능

주장이 아니라 실측 — 격리된 벤치마크 씬에서 적 50/100/200마리를 동시에 스폰하고
Unity Profiler로 CPU 시간과 GC 할당량을 측정했습니다. 최악의 경우(BurstRanged
200마리)가 60FPS 예산의 44%, VFX 풀링 적용 후에는 적 4종 전부 50% 이하를 유지했고,
보스 돌진의 1회성 38ms 스파이크는 공격 로직이 아니라 그 프레임에 우연히 겹친 GC
수집 사이클이 원인임을 프로파일러로 확인했습니다.

- **관련 코드:** [BenchmarkSpawner.cs](https://github.com/highfive37/RogueLike/blob/main/Assets/Scripts/Benchmark/BenchmarkSpawner.cs)

### 자동화 테스트 + CI

EditMode/PlayMode 테스트 스위트가 GitHub Actions(GameCI)를 통해 매 push마다
자동 실행됩니다. 절차 생성, 경로탐색, 포메이션처럼 씬 없이도 검증 가능한 로직은
전부 순수 C# 클래스로 분리해 EditMode에서 빠르게 회귀를 잡습니다.

- **관련 코드:** [.github/workflows/ci.yml](https://github.com/highfive37/RogueLike/blob/main/.github/workflows/ci.yml) · [Assets/Tests/](https://github.com/highfive37/RogueLike/tree/main/Assets/Tests)

### 의도적으로 하지 않은 것 — Addressables

Addressables는 검토 후 도입하지 않기로 결정했습니다. 런 전체가 하나의 게임플레이
씬 안에서 절차적으로 방을 짓는 구조라 씬 단위 로드/언로드 자체가 없고, 그래서
비동기 로딩으로 얻을 실질적 이득이 없었기 때문입니다. "왜 안 썼는가"도 설계
판단이라고 생각해 남겨둡니다.

## 코드 탐색

처음 살펴볼 때는 **던전 생성 → 방/적 AI → 전투·아이템 → UI** 순서를 권장합니다.

| 경로 | 주요 내용 |
| :--- | :--- |
| [Stage/](https://github.com/highfive37/RogueLike/tree/main/Assets/Scripts/Stage) | 절차적 던전 생성, 층 데이터 소스(절차/JSON), 그래프 검증 |
| [Room/](https://github.com/highfive37/RogueLike/tree/main/Assets/Scripts/Room) | 방/문 그래프, 지오메트리, 장애물, 경로탐색, 적 포메이션 |
| [Enemy/](https://github.com/highfive37/RogueLike/tree/main/Assets/Scripts/Enemy) | FSM, 공격 패턴, 애니메이션 연동 |
| [Combat/](https://github.com/highfive37/RogueLike/tree/main/Assets/Scripts/Combat) | 데미지, 무기, 액티브/패시브 아이템, VFX |
| [Player/](https://github.com/highfive37/RogueLike/tree/main/Assets/Scripts/Player) | 이동, 조준, 대시, 피격 반응 |
| [Pooling/](https://github.com/highfive37/RogueLike/tree/main/Assets/Scripts/Pooling) | 범용 오브젝트 풀 |
| [Benchmark/](https://github.com/highfive37/RogueLike/tree/main/Assets/Scripts/Benchmark) | 격리 성능 측정 |
| [Editor/](https://github.com/highfive37/RogueLike/tree/main/Assets/Editor) | SO 데이터 검증 + 던전 그래프 비주얼라이저 |
| [Tests/](https://github.com/highfive37/RogueLike/tree/main/Assets/Tests) | EditMode/PlayMode 자동화 테스트 |

*코드 탐색 링크는 `main` 브랜치 기준입니다.*

## 개발 현황

**데모에서 구현한 핵심 흐름**

- 시드 기반 절차적 던전 생성 (형태 다양화 + 멀티셀 병합 + 패턴 기반 장애물 배치)
- 그리드 A* 경로탐색 + 적 포메이션(방 공격 슬롯)
- 일반 적 6종 + 보스 FSM, 텔레그래프 있는 보스 돌진 공격
- 액티브/패시브 아이템, 바닥 드롭 + 아이템 방/보스방 보장 지급
- 오브젝트 & VFX 풀링, 격리 벤치마크로 §6.5 Before/After 프로파일링 실측
- 자동화 테스트(EditMode/PlayMode) + GitHub Actions CI
- SO 데이터 검증 툴 + 던전 그래프 비주얼라이저(에디터 확장)
- 타이틀 → 캐릭터 선택 → 3층 런 → 보스 → 클리어/게임오버 전체 루프

**다음 개발 목표**

- 서술형 포트폴리오 문서 작성 — 이 README보다 자세한 설계 근거, AI 제안을 거부·
  수정한 사례, 배운 점 정리 (준비되는 대로 이 자리에 링크 연결 예정)
- 세이브/로드(런 이어하기) — 우선순위 낮음, 마감 임박 시 스킵 가능
- 스크린샷/GIF를 실제 캡처로 교체

---

<div align="center">

**(이름) · Game Client Programmer**
[GitHub](https://github.com/highfive37)

*개발 중인 프로젝트로, 화면과 세부 기능은 변경될 수 있습니다.*

</div>
