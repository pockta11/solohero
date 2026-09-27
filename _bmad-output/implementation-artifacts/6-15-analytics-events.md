# Story 6-15: Analytics 이벤트 연결

Status: review (2026-09-28)

## 결과

Firebase Analytics로 GDD `Gameplay Metrics` 12개를 측정할 이벤트를 발화한다. 리텐션·세션 수·세션 길이는 Firebase 자동 수집(`first_open`, `session_start`, `user_engagement`)으로 보고 따로 만들지 않는다.

| 구성 | 파일 |
|---|---|
| 인터페이스·파라미터·이름 | `Core/Analytics/IAnalytics.cs`, `AnalyticsParam.cs`, `AnalyticsEvents.cs` |
| 스테이지 흐름·성장·골드 흐름 | `Core/Analytics/GameplayTelemetry.cs` (StageRunner·SaveDataV2 관찰, 게임에 영향 없음) |
| Firebase 어댑터 | `Game/Infrastructure/AnalyticsService.cs` — Firebase 의존성 확인 실패 시(로컬 모드·에디터) 전부 무시 |
| UI 결정 이벤트 | `Game/UI/Common/GameAnalytics.cs` → 가챠·강화·스킬·광고·오프라인 프레젠터 |
| 테스트 | `Tests/EditMode/GameplayTelemetryTests.cs` |

## 이벤트 집합

| 이벤트 | 파라미터 | 발화 시점 |
|---|---|---|
| `stage_reach` | stage, chapter, boss | 개척 스테이지를 처음 시작 (세션 내 새 최고) |
| `stage_clear` | stage, boss, seconds, attempt | 개척 스테이지 첫 클리어 (파밍 클리어는 기록 안 함) |
| `stage_fail` | stage, boss, seconds, attempt | 모든 실패 |
| `retreat_enter` | stage, boss | 후퇴 파밍 진입 (실패한 스테이지 기준) |
| `challenge` | stage, seconds | 파밍 중 `도전`으로 복귀, 파밍 시간 |
| `tutorial_step` | step | 튜토리얼 단계 변화 (3 = 완료) |
| `hero_level_up` | level | 히어로 레벨 상승 |
| `gold_session` | earned, spent, seconds | 일시정지·종료 시 세션 골드 흐름 |
| `gacha_pull` | kind(gold_single/gold_ten/gem_ten), count, best_grade(0~3), pity | 뽑기 확정 |
| `upgrade` | lane(0 HP·1 ATK·2 DEF·3 SPD), level | 강화 성공 |
| `skill_level` | slot, level | 스킬 레벨업 |
| `ad_reward` / `ad_fail` | slot(OfflineDouble/Gem/GoldBooster) | 광고 보상 지급 / 실패·조기 종료 |
| `offline_claim` | gold, doubled | 오프라인 보상 수령 |
| 사용자 속성 `highest_chapter` | — | 도달 챕터 |

## 지표 대응 (GDD Gameplay Metrics)

| 지표 | 계산 |
|---|---|
| 튜토리얼 3단계 완료율 | `tutorial_step` step=3 사용자 / `first_open` 사용자 |
| 첫 세션 길이 · 1일차 세션 수 · D1/D7 | Firebase 자동 (참여 시간, 리텐션 코호트) |
| 1일차 1-10 보스 도달률 | 설치 1일 내 `stage_reach` stage=10 사용자 비율 |
| 일반 스테이지 평균 소요 | `stage_clear` boss=0 의 seconds 중앙값 (V-2와 같은 정의: 개척 첫 클리어) |
| 보스 첫 도전 실패율 | 보스 스테이지별 사용자의 첫 `stage_fail`/`stage_clear` 중 fail 비율 |
| 후퇴 파밍 진입 후 재도전율 | `challenge` 수 / `retreat_enter` 수. seconds 분포 = 실제 재도전 간격 → **V-7 판정 근거 (D-069)** |
| 광고 시청 / DAU | AdMob 보고서 + `ad_reward` |
| 24시간 골드 소비율 | 사용자·일 단위 `gold_session` spent 합 / earned 합 |
| 7일차 챕터 5 진입률 | 설치 7일 내 `stage_reach` chapter=5 사용자 비율, 또는 `highest_chapter` |

## 한계

- 새 저장은 `highestStage = 1`로 시작하므로 스테이지 1은 개척으로 보지 않는다. 첫 `stage_reach`는 2.
- `attempt`는 세션 안 횟수다. 첫 도전 여부는 사용자별 이벤트 순서로 판정한다.
- 개인정보처리방침과 Play 데이터 보안 양식에 Analytics 수집을 명시해야 한다 (E9-14).
