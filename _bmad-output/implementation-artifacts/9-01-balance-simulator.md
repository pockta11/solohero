# Story 9.1: 밸런스 시뮬레이터 (E9-01)

Status: review

<!-- Epic E9-01 · Must · epics.md: "E4 착수 전에 1차 수행한다". E4 코어가 먼저 들어가 순서가 뒤집혔다 — 이 스토리로 따라잡는다 -->

## Story

As a 1인 개발자,
I want 게임과 같은 Core 코드·같은 상수로 7일 플레이를 헤드리스로 돌려 목표 곡선과 V-1~V-7을 숫자로 판정하고,
so that 상수를 UI·아트에 굳히기 전에 성장 곡선이 성립하는지 확인하고 조정 근거를 남긴다.

## Acceptance Criteria

1. `Tools > Balance > Run Simulation`이 `Data/Config/BalanceConfig` 에셋을 읽어(상수 복사 금지, ADR-2) 무광고·광고 2개 프로필 × 3시드를 돌리고 `_bmad-output/implementation-artifacts/balance/`에 `sim-summary.md`, `sim-stages-*.csv`, `sim-days-*.csv`를 쓴다. 배치 진입점 `SoloHero.Editor.BalanceSimulatorMenu.RunBatch`.
2. 시뮬레이터는 **실제 `StageRunner`**(스폰·타깃팅·피격·스킬·보스 타이머·후퇴)를 고정 스텝(1/30 s)으로 돌린다. 공식을 다시 쓰지 않는다. 플레이어 모델(세션·소비·후퇴 정책)만 시뮬레이터 소유다.
3. 출력: 스테이지별 최초 클리어(누적 플레이 시간, 시도·실패, 클리어 시간, 정체 시간, 레벨·강화·스킬·장비·골드), 일별 합계(수입원별·지출처별 골드, 뽑기 수, 일반/보스 시도·실패).
4. `BalanceChecks`가 목표 곡선·V-1~V-7·E9-04·E9-05를 PASS/FAIL/INFO로 판정한다. GDD가 말로만 정한 범위는 `BalanceChecks` 상수로 고정한다.
5. EditMode 테스트: 같은 시드 → 같은 결과, 1-1 클리어 시간이 목표 근처, 세션 사이 오프라인 수령, 체크 id 중복 없음, CSV 행 수.
6. 시뮬레이터가 GDD 규칙대로 돌도록, 시뮬레이션 중 발견한 Core 규칙 누락을 고친다(아래 "발견한 규칙 누락").

## 플레이어 모델 (가정 — `SimSettings`)

| 항목 | 값 | 근거 |
|---|---|---|
| 세션 | 매일 08:00 8분 · 12:30 5분 · 18:30 5분 · 22:00 12분 = 하루 30분 | GDD 페르소나 P-A (3~5회 × 2~5분 + 한 자리 15분) |
| 오프라인 | 세션 사이 경과를 `OfflineReward.Compute`로 수령. 광고 프로필은 A-1 2배(5회/일)·A-2 젬 5×5·A-3 부스터 10분×3 | GDD 광고 슬롯 표 |
| 소비 | 탐욕: 강화 4레인·스킬 3종·골드 1회 뽑기 중 `Δ(ln DPS + ln EHP) / 골드`가 가장 큰 것을 산다. 못 사면 모은다. 뽑기가 최선이고 4,500 이상이면 10연. 젬 200이 모이면 젬 10연 | 합리적 플레이어 상한. 실제 플레이어는 이보다 느리다 |
| 튜토리얼 | 첫 뽑기는 500골드가 모이는 즉시 | GDD 첫 30분 시나리오 |
| 보스 실패 | 후퇴 → 파밍 4회 클리어 후 재도전 | GDD 후퇴 파밍 |
| 일반 연속 실패 3회 | 한 단계 아래로 이동 | GDD 3회 연속 실패 제안 |
| 히트 프레임 | 공격 발동과 같은 스텝 | 애니메이션 없음 |

## 발견한 규칙 누락 (이 스토리에서 수정)

| 규칙 (GDD) | 기존 코드 | 수정 |
|---|---|---|
| 보스 격파 골드 = 스테이지 골드 × `BOSS_GOLD_MULT`(5) | ×1 (테스트가 ×1을 고정) | `Formulas.StageClearGold`, 테스트 기대값 교정 |
| 보스 EXP = 일반 적 × `BOSS_EXP_MULT`(5) | ×1 | `Formulas.EnemyExp`, `KillExp.Grant(isBoss)` |
| 스킬 해금 Lv 1 / 5 / 12 | 자동 발동이 해금을 보지 않음 | `SkillAutoCaster.SetUnlocked`, `CombatLoadout.Apply` |
| 전투 함성 버프도 스킬 레벨 +10%p | 레벨 무시 | 레벨 계수 적용 |
| 전투 시작 스탯 = 레벨·강화·장비 합산 | `CombatSession`만 장비를 풀고, 스킬 레벨·해금은 전투에 안 들어감 | `CombatLoadout`(Core) 하나로 게임·시뮬레이터 공용 |

**Game 쪽 연결 누락(2단계 코드 리뷰로 이월):** 아무도 `HeroBrain.OnHitFrame`을 호출하지 않아 실제 게임에서 기본 공격이 안 들어간다. 레벨업·강화·장착 후 `CombatLoadout.Apply`를 다시 부르는 곳이 없다. `farmingStage`·`retreatMode`가 전투 진행 중 저장되지 않는다.

## 1차 결과 (GDD 현재 상수) — `balance/sim-summary.md`

13개 판정 중 **10개 FAIL**. 요약:

- **진행이 폭주한다.** 1일차 13-3, 7일차 108-4 (목표 1일 1-10 격파, 7일 챕터 5 진입). MVP 50스테이지를 첫날 16분에 끝낸다.
- 원인 1 — 성장률 부호가 반대다. 파밍 효율 지수 1.31 때문에 골드 성장 1.08은 파워 성장 `1.08^1.31 = 1.106`/스테이지가 되어 적 HP 1.10을 **앞지른다**. GDD "벽이 생기는 이유 1"의 "0.6%p씩 뒤처진다"는 지수를 빼먹은 계산이다.
- 원인 2 — 절대량. g=41에서 ATK 레인만으로 필요한 골드가 누적 수입의 약 1/4이다. 레벨(+2 ATK/+20 HP)·스킬·장비가 주는 무료 파워도 크다.
- 원인 3 — 오프라인이 주 수입이다. 하루 4세션이면 오프라인 약 20시간 × 6% 효율 ≈ 온라인 72분분으로, 온라인 30분의 2~3배다(V-4 "보조 수입" 전제 불성립).
- DEF 레인은 `DEF_REF = 8 × 적 ATK`가 스스로 커지므로 상대 가치가 계속 줄어 합리적 플레이어가 한 번도 사지 않는다.
- 첫 5분 뽑기 1회는 스테이지당 50골드로는 불가능하다(500골드 = 10클리어). 장르 표준인 튜토리얼 무료 뽑기가 필요하다.

조정 후보와 결정은 9-02(`9-02-verify-v1-v7`)에서 기록한다.

## Tasks

- [x] T1. Core/Balance: `SimSettings`, `SimReport`, `SimSpender`, `BalanceSimulator`, `BalanceChecks`, `SimCsv`
- [x] T2. `CombatLoadout`(Core/Stage), `GachaCatalog.Standard`(Core/Gacha), `EquipmentBonus.FromGrades/GradeOrNone`
- [x] T3. 규칙 누락 수정 + 기존 테스트 기대값 교정(`StageRewardTests` 보스 골드 2건)
- [x] T4. Editor `BalanceSimulatorMenu` (`Run` / `RunBatch`)
- [x] T5. `BalanceSimulatorTests` 10건. Mono 러너로 EditMode 156건 녹색
- [ ] T6. **[사용자]** Unity에서 `Tools > Balance > Run Simulation` 1회, Test Runner EditMode Run All 녹색

## File List

- `Assets/SoloHero/Scripts/Core/Balance/*` (신규 6)
- `Assets/SoloHero/Scripts/Core/Stage/CombatLoadout.cs` (신규)
- `Assets/SoloHero/Scripts/Core/Gacha/GachaCatalog.cs` (신규)
- `Assets/SoloHero/Scripts/Core/Formulas.cs`, `Progression/StageReward.cs`, `Progression/KillExp.cs`, `Stage/StageRunner.cs`, `Combat/SkillAutoCaster.cs`, `Equipment/EquipmentBonus.cs`
- `Assets/SoloHero/Scripts/Game/Combat/CombatSession.cs`
- `Assets/SoloHero/Scripts/Editor/BalanceSimulatorMenu.cs` (신규)
- `Assets/SoloHero/Scripts/Tests/EditMode/BalanceSimulatorTests.cs` (신규), `StageRewardTests.cs`
- `_bmad-output/implementation-artifacts/balance/` (1차 결과)
