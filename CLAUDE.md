# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**SoloHero** — 세로 화면 픽셀아트 2D 횡스크롤 **방치형 RPG** (1인 개발, Android 단독).
히어로가 오른쪽으로 자동 전진하며 적을 처치하고, 플레이어는 전투를 조작하지 않는다. 입력은 성장 결정(강화·가챠·스킬·파밍 스테이지·광고)뿐이다. 앱을 끈 시간이 골드가 되는 오프라인 축적(최대 6시간)과, 확률을 공시하는 100회 천장 장비 가챠가 두 축이다.

**설계 기조: 장르 표준 모방.** 버섯커 키우기·세븐나이츠 키우기·Soul Strike의 관습을 따르고 고유 설계는 두지 않는다.

- **Engine**: Unity **2022.3.62f3** — **Personal/Pro로 받을 수 있는 마지막 2022.3 패치** (63f1+는 Industry/Enterprise 전용 xLTS). Unity 6 아님, `Awaitable` 없음, C# 9
- **Render**: URP 14.0.12, `Renderer2D`, Pixel Perfect (기준 270×480 정수 4배, PPU 32)
- **Screen**: **Portrait 1080×1920** 고정
- **Platform**: Android AAB, 타깃 API 36, minSdk 24
- **Backend**: Firebase Auth(익명) / Realtime Database / Analytics
- **Ads**: Google Mobile Ads Unity 11.5.0 (Android `play-services-ads` 25.4.0), 보상형만
- **Async**: UniTask v2.5.11 · **Tween**: DOTween · **JSON**: Newtonsoft 3.2.1 · **UI**: uGUI + TMP

## 현재 상태 (2026-10-08)

**2D 리빌드 — E1~E8 구현 완료(대부분 review), E9 출시 준비·QA 진행 중.** 3D 쿼터뷰 구현은 2026-09-19에 전면 폐기했다. 1-09(빌드 파이프라인·API 36·16 KB, JDK 11 확정)와 1-03(정리·구조 이행) 완료. 레거시 격리 폴더는 2026-09-28 이식 완료로 삭제.

| 문서 | 경로 | 역할 |
|---|---|---|
| **GDD** | `_bmad-output/planning-artifacts/gdds/gdd-solohero-2026-09-20/gdd.md` | 설계 단일 출처. `Number Balancing` 상수표가 모든 수치의 원본 |
| Epics | 같은 폴더 `epics.md` | 9 에픽 / 132 스토리 |
| Decision log | 같은 폴더 `decision-log.md` | D-001~D-126 |
| **Architecture** | `_bmad-output/planning-artifacts/architecture/arch-solohero-2026-09-20/game-architecture.md` | 결정 D1~D15, ADR 1~7, 구조, 패턴(코드 예시), 검증 |
| **Project context** | `_bmad-output/project-context.md` | **코드를 만지기 전에 읽는 규칙 62개.** 아키텍처와 충돌 시 아키텍처가 우선 |

코드 작업은 `project-context.md` → 해당 절의 `game-architecture.md` 순으로 읽고 시작한다.

### 2026-09-27 진행
- **9-01 밸런스 시뮬레이터** `Tools > Balance > Run Simulation` (Core/Balance, 실제 `StageRunner` 헤드리스). 결과 `_bmad-output/implementation-artifacts/balance/`
- **9-02 튜닝** D-053~D-061 반영 — 무광고 1일 2-8 · 3일 3-9~4-2 · 7일 5-7. 남은 FAIL은 경계값 V-6a·V-7뿐. 중복 강화(D-062)·V-3a 가치 균형(D-063)·광고 부스터 1회(D-064)
- **성장 패널** 하단 탭바 + 캐릭터·장비·소환·스킬 패널. `Tools > Setup > Build Growth UI`로 씬 재생성
- Core 규칙 누락 수정: 보스 골드·EXP ×5, 스킬 해금 레벨, 기본 공격 히트 연결, 클리어 저장, 재실행 후 후퇴 유지, v1 장비 id 변환
- **E8 연출·오디오·적 다양화** (D-068~D-071): 적 8종·보스 3종(당시 돼지 5종 + 색 변형 — D-082로 교체), 풀 VFX·카메라 셰이크·크리티컬 숫자·골드 카운트업·강화 펀치·레벨업 링, 가챠 카드 연출, BGM 6 + SFX 18(CC0), 설정 팝업(BGM/SFX/이펙트 축소/30fps). `Tools > Setup > Build Art`가 전부 재생성. 보스 공격 배율 1.1(D-069) — 무광고 남은 FAIL은 V-7(플레이어 모델 의존)뿐
- **2026-09-28 출시 준비·QA** (D-072~D-076): 분석 이벤트(`GameplayTelemetry` + Firebase Analytics), 스토어 자료 `_bmad-output/implementation-artifacts/store/`, 저장 리비전(강제 종료 유실 방지), 스테이지 선택·뒤로가기·세이프 영역·보스 등장 배너·젬 골드 패키지·장비 아이콘, Legacy 폴더 삭제. QA 보고서 `implementation-artifacts/qa/`
- **에뮬레이터 QA**: `pwsh tools/qa/Qa.ps1 smoke|forcekill|idle|coldstart|scenarios|firstsession` (adb root로 저장 판독). 광고 WebView가 에뮬레이터에서 약 85초 후 앱을 죽이므로 QA는 `SOLOHERO_NO_ADS=1` 빌드로 (실기기 확인 필요, release-gate B-001)
- **2026-09-28 스킬 확장** (D-078~D-080): 스킬 3종 고정 → 24종 도감·스킬 소환(소환 탭의 장비/스킬 전환)·6슬롯·보유 효과. 컬러 스킬 VFX 14종·아이콘 24종·SFX 5종(절차 생성, `Art/Vfx`, `Art/Icons/Skills`), 화면 플래시·스킬명 팝업·적 상태 색. 스킬 패널 = 장착 슬롯 + 도감 그리드 + 상세(강화/장착). 무광고 곡선 유지(7일차 5-7~5-8)
- **2026-09-28 몹 무리·캐릭터 교체** (D-081~D-082): 몹 4마리씩 무리 등장(`SPAWN_WAVE_SIZE/GAP/SPACING`), 캐릭터 아트 Kings and Pigs → LuizMelo CC0(영웅 Hero Knight, 적 4종+색 변형, 보스 5종), 발밑 그림자. 변환 규칙: 여백 자르고 발 바닥 중앙, 적·보스 좌우 반전
- **2026-09-28 특성 트리·스킬 수동 발동·2등신 영웅** (D-085~D-088): 스킬 패널을 "스킬 / 특성" 두 페이지로 나누고 특성 트리(공격·수호·비전 3갈래 × 4단계, 레벨당 1포인트, 초기화 100젬, `Core/Talents`)를 추가. 스킬바 위 AUTO 토글(수동이면 슬롯 탭 발동). 시뮬 무광고 7일차 5-6~5-8. 탭 = 캐릭터·장비·소환·스킬·특성, 설정은 오른쪽 메뉴 레일, 광고 보상은 왼쪽 레일(D-089)
- **2026-09-29 귀여운 캐릭터·2.5D 전투** (D-090~D-092): 영웅·적 8종·보스 5종을 운빨존많겜식 절차 생성 도트로(`tools/art/charkit.py` + `hero.py` + `cast.py`, 보스도 1배). 전투는 원근 바닥(`floorgen.py`) + 몹 깊이 레인·레인별 크기·정렬(`Game/Combat/DepthLanes`), 카메라 y -0.6. 판정은 X축 그대로. 에뮬레이터 설치가 멈추면 `bundletool build-apks` 후 `adb install-multiple`
- **2026-10-01~02 3D 캐릭터·게임 품질** (D-096~D-099): 캐릭터 15종을 Blender 3D → 도트로(`python tools/art/build3d.py`, Blender 5.2), 클립 12장·20 fps·적 달리기 클립. 카메라는 `PixelCameraFit`(정수 배율; 2D Pixel Perfect 패키지 컴포넌트는 URP에서 동작 안 함). 전투 손맛(히트스톱·넉백·처치 튕김·흰 플래시 셰이더 `SoloHero/SpriteFlash`·처치 코인·숫자 쌓기). 배경·바닥·앱 아이콘 자체 제작(`bggen.py`·`floorgen.py`·`appicon.py`). 스킬 콤보(파쇄·점화), 고유 VFX 4종, 출석·일일 미션(`Core/Daily`, 메뉴 "일일 보상"), 일일 던전 골드·경험치(D-100, 메뉴 "던전"), 영웅 승급 4단계(D-101 → D-104 전직으로 대체), 동료 4종(D-102 → D-114 펫으로 확장). 골렘 = 3장 보스. 개발 빌드는 FPS 표시. 진행표 `implementation-artifacts/qa/quality-plan-2026-10-01.md`
- **2026-10-02 전직** (D-104): 메이플식 1·2차 전직(`Core/Jobs` `JobCatalog`·`JobService`, 저장 `jobId`). 초보자는 섬광 베기 + 기본 스킬 3종만, Lv 10 전사·마법사·궁수, Lv 30 2차 6종(나이트·버서커·화염술사·빙뢰술사·레인저·저격수). 직업이 주공격(섬광 베기 대체)·숙련 패시브·궁극기(2차, 스킬바 옆 별도 슬롯)를 정하고, 뽑기 스킬 21종은 계열 7종씩 같은 계열만 장착. 캐릭터 패널 초상화 아래 전직 버튼 → 선택 팝업(두 번 눌러 확정). 외형 `chibi3d.py` `JOB_LOOKS`(jobmage·jobpyro·jobcryo·jobarcher·jobranger·jobsniper), 전사 계열은 knight1·2·4 재사용. 시뮬은 `SimSettings.Job1/Job2` 경로로 돈다. 자동 스킬은 무리가 다 사거리에 들어오거나 영웅이 멈춰 교전할 때만 발동(D-105). UI 글꼴은 `Art/Fonts/SoloHeroJua.ttf`(둥근 Jua, 테두리+그림자), 상단바는 초상·체력·경험치 얇은 줄, 캐릭터 창 2×2 카드, 장비 창은 영웅 좌우 장비 칸(D-106)
- **2026-10-06 UI·아이콘·연출 다듬기** (D-107~D-108): 전직 후 자기 계열 스킬만·계열당 12종·몹 6마리 무리(D-107). 스킨 v3: 크림 판넬·금테 팝업·광택 버튼·남색 HUD/탭바(`tools/art/uigen3.py` → `Art/UI/Hd`, 1px = 1 캔버스 단위, `UiSkin.Hd`), 매끈한 UI 아이콘 30·장비 16·스킬 55종(`icongen3.py`·`equipgen3.py`·`skillgen3.py`, `UiSkin.Icon`·ArtBuilder가 Hd 우선), 판넬 항상 열림·팝업 리본/X/바깥 탭 닫기·눌림 탄성, 로딩 화면(`LoadingScreen`, 게임 씬 비동기 로드), 굵은 기본 VFX(`vfxgen2.py`), 무기 키운 캐릭터 재렌더. 판넬 글자색은 `UiPalette`(크림 위 진한 갈색), HUD 글자는 흰색+외곽선
- **2026-10-07 장신구·스킬·몹 확장** (D-109): 장비 8슬롯(장갑·목걸이·반지·귀걸이 추가, 32종, `EquippedSlots`)·장비 보유 효과(ATK +0.5/1/2/4%×강화), 장비 창 좌 장비 2×2·우 장신구 2×2. 스킬 계열당 16종(51종) + 표식(받는 피해 +%, `vfxmark`)·가속(쿨타임 가속) 효과. 몹 8마리 무리 × 2 = 스테이지 16마리, 적 성장률 1.155로 곡선 유지(무광고 7일 5-4~5-9). 밸런스 반복은 Mono 시뮬 드라이버(`BalanceSimulator.Run` 직접 호출), Core 테스트도 Mono로 빠르게 돌릴 수 있다
- **2026-10-07 전투 자연스러움·리텐션** (D-110~D-111): 몹이 걸어와 앞 3마리가 동시에 공격하고 나머지는 줄 서서 대기(`CombatWorld.AssignStands`), 역할 4종(고블린 일반·눈알 돌격·버섯 탱커·해골 뼈 투척, `EnemyRole`/`EnemyWaves`, 무리 HP 정규화). 적 HP 45·공격 2.6·성장 1.185, 보스 공격 2.5배·1장 보스 HP 6배, 오프라인 제수 2000, 스테이지 목표 12~25초. 첫 1분: 로딩 직후 환영 선물 + 무료 소환 카드 연출 + 강화 버튼 손가락. 가이드 퀘스트 바(`GuideQuestCatalog`/`Service`, 저장 `guideQuest`)와 전투력 표시·"+N" 팝업(`CombatPower`). 시뮬 무광고 1일 2-5~2-8 · 7일 5-3~5-9
- **2026-10-07 테두리·타격감** (D-112): `SoloHero/SpriteFlash` 셰이더가 캐릭터 실루엣 바깥 1텍셀 테두리(남색, 보스 진홍)를 그리고 피격 번쩍임에 함께 번쩍인다. UI 큰 영웅은 uGUI Outline. 피격 찌그러짐(치명타·콤보 더 세게 + 넉백 2배)·영웅 휘두름 늘어남·픽셀 파편(`HitParticles`, 원점 파티클은 `AlwaysSimulate` 필수 — 자동 컬링이면 카메라가 멀어질 때 멈춰 안 보인다)·영웅 피격 빨간 번쩍임·타격음 ±8% 음높이
- **2026-10-07 장비 7등급** (D-113): 장비만 일반·고급·희귀·영웅·전설·신화·고대 7단계(`GearGrade`; 스킬·동료·직업은 4단계 `Grade`, UI는 `ToGearGrade()`로 같은 색·테두리). 확률 60/28/9/2.5/0.45/0.045/0.005 %(`GEAR_RATE_*`, `GearTableValues`), 천장 200회 전설(`GEAR_PITY`), 뽑기 150골드/10연 1,350, 환영 선물 무료 10연. 장비 56종 아이콘(`equipgen3.py`의 `VARIANTS`: 기존 모양에 재질 교체)·테두리 7종(`uigen3.py` `hd9_slot{c,u,r,e,l,m,a}`). 시뮬 무광고 7일 5-3~6-6. 보스 실패 후 직전 일반 스테이지 반복은 D-077 그대로
- **2026-10-07 펫** (D-114): 동료를 펫으로 이름을 바꾸고 16종으로(`Core/Pets` `PetCatalog`·`PetService`·`PetSummonService`, 전투 `PetCaster`, 뷰 `PetView`, 창 `PetPresenter`; 저장 키 `companionEquipped`·`companionLevels`는 유지, 새 키 `petOwned`·`petEnhance`·`petPityCount`). 소환 화면 세 번째 탭 "펫 소환"(300 / 10연 2,700 / 젬 200, 장비 7등급표·200회 천장, 1-10 처치 후 개방), 중복 강화·레벨업·보유 효과, 펫 창은 상세 + 4×4 도감. 신규 12종 그림은 `chibi3d.py` `build_pet`(`PET_BODY`·`PET_WINGS`·`HOPPERS`; 꼬리는 화면 왼쪽인 몸 −Y로 펼쳐야 보인다), `python tools/art/build3d.py pet...`. 시뮬 무광고 7일 5-6~6-8
- **2026-10-07~08 성장 축·리텐션** (D-115~D-126): 소환 단계(장비·스킬·펫 각각, 누적 뽑기로 Lv 10까지, `Core/Gacha/SummonLevel`)가 **메이플 키우기식으로 등급을 연다**(D-123: 1단계 일반만, 단계마다 한 등급 — 장비·펫 고급 2·희귀 3·영웅 4·전설 5·신화 6·고대 7, 스킬 희귀 2·영웅 3·전설 4; 장비 단계 100회 단위로 누적 100/300/600/1,000…) 희귀 이상 확률도 올린다. 확률표는 `AtLevel`에서만 계산 — 새 뽑기 경로도 반드시 `AtLevel`을 거칠 것. 천장은 전설이 열린 단계부터만 센다(`PityOpen`). 시뮬 플레이어는 지출의 30 %를 장비·10 %를 스킬 소환에 쓴다(`GearPullShare`·`SkillPullShare`; 한 번의 가치로는 단계 투자를 못 본다). 환급은 소환 레벨 최대에서도 10연 단가의 ½ 미만으로(그 이상이면 뽑을수록 골드가 남는다). 장비 승급(+10 → 한 등급 위 +5, 영웅까지, 장비 창 `일괄 승급`, `Core/Equipment/EquipPromotion`), 업적 10계열(`Core/Progression/Achievement*`, 메뉴 "업적"), 로컬 알림(Unity Mobile Notifications 2.3.2, `Game/Infrastructure/LocalNotifications`, 설정 `알림`), 광고 무료 소환(장비 10회 ×2·펫 10회 ×1/일, `AdSlot.FreeGearSummon/FreePetSummon`). 골드만으로는 조금 더디게: `STAGE_GOLD_GROWTH` 1.035(D-122). 보스는 체력의 벽(D-124: 공격 ×0.5·체력 ×26, 1장 ×14, 실패 대부분 시간 초과 — 판정 V-8). 일반 스테이지는 이어 달린다(D-125: 클리어 대기 중 `HeroBrain.RunOn`, 다음 일반 스테이지는 `Reset(stats, keepPosition)`으로 x 유지; 보스·새 챕터·재시작은 0). 환생은 제외(D-126: 코드·예약 저장 필드 삭제, 후반은 소환 단계·승급·펫·업적). 시뮬 무광고 1일 2-7~2-9 · 3일 3-6~3-9 · 7일 4-10~5-6, 광고 7일 5-4~6-1, 14일 5-6~6-9
- 빌드는 `libFirebaseCppApp` 포함을 자동 검사하고, EDM4U가 pom을 `srcaar`로 바꾸면 빌드 전에 되돌린다
- 빠른 검증: Unity 없이 Mono로 Core+테스트 컴파일 가능(`Editor/Data/MonoBleedingEdge` csc). 공식 검증은 Unity `-runTests -testPlatform EditMode`

## Build Commands

### GitHub Actions (주 CI)
Push to `main` 또는 수동. `unityci/editor:ubuntu-2022.3.62f3-android-3` 이미지를 직접 `docker run`하고 `.github/scripts/unity-build.sh`가 **Unity Licensing Client로 Personal 시트 활성화 → `BuildAutomator.Build` → 시트 반환**을 수행한다. Secrets: `UNITY_EMAIL` / `UNITY_PASSWORD`만 (Unity가 Personal `.ulf` 수동 활성화를 폐지해 `game-ci/unity-builder`·`UNITY_LICENSE`는 쓸 수 없다). Artifact `android-aab` (7일). 빌드 전 `Free disk space` 단계 필수. 계정 2FA는 꺼져 있어야 하고 비밀번호가 `-`로 시작하면 안 된다(옵션으로 파싱됨). 취소·타임아웃에도 시트를 반환하도록 `--init` + trap, 동시 실행은 `concurrency`로 직렬화, `**.md`·`_bmad-output/**`·`tools/**` 변경은 빌드를 트리거하지 않는다. ~22분/빌드.

### Jenkins (휴면)
로컬 Jenkins는 2026-09-21 현재 운영하지 않음. `Jenkinsfile`은 참고용:
```
"C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -quit -batchmode -projectPath "%WORKSPACE%" -executeMethod BuildAutomator.Build -logFile Builds/build.log
```

### Manual build in Unity Editor
`Tools > Build > Android AAB` → `BuildAutomator.Build` — `EditorBuildSettings`의 활성 씬으로 `Builds/game.aab` 생성 (현재 스파이크 `Boot.unity` 1개; 활성 씬이 0개일 때만 `SpikeSceneSetup`이 생성·등록).

## Architecture (요약 — 상세는 game-architecture.md)

### 어셈블리와 경계
```
SoloHero.Tests.EditMode ──▶ SoloHero.Core          (noEngineReferences: true — UnityEngine 참조 불가)
SoloHero.Editor ──▶ SoloHero.Game ──▶ SoloHero.Core
```
- **Core**: 순수 C#. `*Service`, `HeroBrain`/`EnemyBrain`/`StageRunner`(enum + `Tick(dt)` 상태 머신), `Formulas`/`StatAggregator`/`DamageCalc`, `SaveDataV2`/`MigrationV1ToV2`, `Result`, `Log`, `Services`, `IClock`/`IRandom`
- **Game**: MonoBehaviour만 — Boot, Infrastructure(Firebase/AdMob/PlayerPrefs 어댑터), 뷰·풀, UI Presenter, Audio, Debug(`#if`)
- **Editor**: `BuildAutomator`, DebugWindow, BalanceSimulator, GachaVerifier, SpriteImportPreset

### 핵심 규약
- **변경 → 이벤트 → `RequestSave()`** 순서. 저장 트리거 7종(스테이지 클리어·가챠 확정·장착·강화·스킬 레벨업·오프라인 수령·일시정지/종료)만 저장 요청
- 예상 실패(골드 부족·MAX·쿨다운)는 `Result.Fail(reason)` — 예외 금지. 외부 I/O 실패는 항상 폴백(원격→로컬, 광고→일반 수령)
- **런타임 `Instantiate` 0** — 적·데미지 텍스트·VFX·SFX는 `UnityEngine.Pool` 풀
- 서비스 로케이터(`Services.Register` in Boot 1회, `Services.Get<T>()`) + 서비스 소유 C# `event`
- 데이터는 **ScriptableObject 단일 출처**. `BalanceConfig` 필드명 = GDD 상수명(`ENEMY_HP_GROWTH`). 매직 넘버 금지
- 수치는 `double`(재화·스탯), `int`(레벨·카운트). 표기 K/M/B/T → aa/ab
- 저장: `users/{uid}/v2` 노드, Newtonsoft, 로컬 백업 + 250 ms 디바운스. v1 강화 레벨은 골드 **환급**(복사 금지)
- 물리 미사용(모듈은 유지) — 판정은 X축 거리. Animator는 Trigger/Bool만 받고 상태를 소유하지 않음
- **코드 안 텍스트는 영문만** (식별자·주석·로그·키). 한국어는 `Strings` 테이블 값에만
- 제거되어 재도입 금지: Addressables, Input System 패키지, `StreamingAssets/JSON`, `Resources/`, `SingletonMB`, `JsonDataManager`, Box-Muller `GachaSystem`

### 프로젝트 구조 (목표)
```
Assets/SoloHero/            # 프로젝트 소유 전부. 벤더(Firebase·GoogleMobileAds·EDM4U·Plugins·TextMesh Pro)는 루트 유지
├── Scripts/{Core,Game,Editor,Tests/EditMode}/
├── Data/{Config,Equipment,Enemies,Bosses,Chapters,Skills,Gacha}/   # SO 인스턴스
├── Art/{Hero,Enemies,Bosses,Backgrounds,Icons,UI,Vfx,Placeholder,Fonts,Atlases}/
├── Animation/  Audio/  Prefabs/
└── Scenes/{Boot,Game}.unity
```
E1-03(2026-09-22)에서 이동 완료. 3D 시절 스크립트는 `Scripts/Legacy/`에 격리했다가 2026-09-28 전부 이식·삭제했다(장비 SO 16종도 코드 카탈로그 `GachaCatalog`로 대체되어 삭제). Addressables·Input System·collab-proxy 패키지, `StreamingAssets/JSON`, `AddressableAssetsData`는 제거됨. `Assets/Resources/DOTweenSettings.asset`은 벤더 필수 예외.

### 게임 규칙 요약 (GDD 상수표가 원본)
- 스테이지: 전역 인덱스 `g = (챕터−1)×10 + 스테이지`, 챕터당 10, 10번째가 보스(26배 HP, 30초, D-124). 킬 목표 16 고정(8마리 무리 × 2, D-109). 적은 걸어와 앞 3마리가 동시에 공격(D-110). 적 HP 기준 45·ATK 기준 2.6, 성장 HP 1.165·ATK 1.175 (D-123), 보스 ATK ×0.5·HP ×26·1장 보스 HP ×14 (D-124), 골드 `50×1.035^(g−1)` (D-122), 보스 격파 골드·EXP ×5 (D-053·D-059). 일반 스테이지 사망 → 자동 한 칸 아래 파밍, `도전`으로 복귀 (D-058)
- 강화: HP·ATK·DEF **승산 ×1.16/레벨·상한 없음**, 공격속도 가산 +0.02·최대 100. 비용 `baseCost × 1.12^level` (base 300/450/450/600)
- 방어: `DEF_REF = 3 × 적ATK`, `피격 = max(1, 적ATK × DEF_REF / (DEF_REF + DEF))`
- 가챠 (D-113): 150골드 / 10연 1,350 / 젬 200. 장비 확률 **일반 60 / 고급 28 / 희귀 9 / 영웅 2.5 / 전설 0.45 / 신화 0.045 / 고대 0.005 %**, 200회 천장(전설), 전설 이상 획득 시 pity 리셋. 중복 = 장비 강화 +1(최대 10, ×1.10/레벨), 최대 레벨만 환급 20~15,000 (D-062, D-115). **소환 단계**(장비 단계 100·펫 50·스킬 20, Lv 10): 1단계 일반만, 단계마다 한 등급 개방(고급 2·희귀 3·영웅 4·전설 5·신화 6·고대 7, 스킬 희귀 2·영웅 3·전설 4), 희귀 이상 ×(1 + 0.12×(L−1)), 천장은 전설 개방 후부터 (D-115, D-123). 승급 +10 → 윗등급 +5, `500 × 3^등급`, 영웅까지 (D-116). 광고 무료 소환 장비 10회 ×2·펫 10회 ×1/일 (D-120). 장비 **8슬롯 × 7등급 = 56종**(검·투구·갑옷·신발 + 장신구 장갑·목걸이·반지·귀걸이), 보유 효과 ATK +0.5/0.75/1/2/4/8/15 %×강화 (D-109)
- 스킬 (D-078~D-080, D-107, D-109): **시작 3종 + 계열당 16종(등급별 4) = 51종 도감** + 스킬 소환(4등급 표 C 55 / R 33 / E 10 / L 2 %·100회 천장, 카운터 별도, 5,000 / 10연 45,000 / 젬 200) + **6슬롯**(해금 Lv 1/5/12/20/30/45) 슬롯 순 자동 발동. 중복 = 레벨 +1(최대 10, ×1.9), 골드 레벨업 병행, 보유 효과 ATK +0.25/0.5/1/2 %×레벨 배율. 스킬 수치 원본은 `Core/Skills/SkillCatalog`. 화상/중독·기절(보스 ½)·버프 5종(가속 포함)·보호막·표식(받는 피해 +%)
- 펫 (D-114): 16종(일반 3·고급 3·희귀 3·영웅 3·전설 2·신화 1·고대 1), 펫 소환 300 / 10연 2,700 / 젬 200(장비 확률표·천장 공유, 카운터 별도, 1-10 처치 후). 장착 1마리가 공격(영웅 공격력 × 배율 × 1.1^레벨 × 1.1^강화), 보유 효과 ATK +2/3/5/8/15/30/60 % × 강화 × 레벨
- 오프라인: `파밍스테이지_골드 / 2000` 초당 (D-055, D-110), 상한 21,600초, 60초 미만 팝업 없음, 음수·상한 2배 초과 → 0. 광고 2배
- 이동 속도 상수 2.0 u/s — 스탯 아님. SP 없음(쿨다운만). 콤보 없음
- 평타 없음 (D-093): 기본 스킬 "섬광 베기"가 공격속도 간격으로 앞의 가까운 적 3명(2.4u)에게 ATK 300%. 몹 HP 기준 45 (D-094 → D-110)

## Environment Setup

| Requirement | Note |
|---|---|
| Unity 2022.3.62f3 (고정) | Android Build Support + SDK/NDK + **OpenJDK 11**. 63f1+는 xLTS(유료 라이선스)라 상향 불가 |
| Android SDK Platform 36 | Target API 36 (2026-08-31부터 신규 앱 필수) |
| JDK | **JDK 11 확정** (Unity 번들 OpenJDK 11.0.14.1). E1-09 스파이크 결과 A — 2022.3.62f3 + AGP 7.4.2로 16 KB 정렬 통과, JDK 17 불필요 |
| `Assets/google-services.json` | 커밋 금지. 없으면 부트가 `local` 모드로 진입해야 함 |
| DOTween | Utility Panel > Setup + **Create ASMDEF** |
| Android 에뮬레이터 (실기기 없음) | `pwsh tools/android/Emu.ps1 start` → AVD `solohero_fast` (Android 15 · Google APIs · 4 KB · 1080×2400 · host GPU RTX 4060 · 60 fps, 바탕화면 바로가기도 이것). 16 KB 확인은 `-Avd solohero16k35` (SwiftShader 전용, host GPU면 게임 화면이 검다). `install` / `run` / `shot`. SDK 루트 `C:\Users\user\android-sdk-tools` (bundletool·build-tools 35 포함) |
| MCP (선택) | `CoplayDev/unity-mcp`(에디터 조작), Context7(2022.3 API 문서), Higgsfield(배경·아이콘 생성) — 설치는 아키텍처 Development Environment 절 |

## Android Build Configuration

`Assets/Plugins/Android/`: `mainTemplate.gradle` / `settingsTemplate.gradle` / `gradleTemplate.properties`(EDM4U가 갱신), `AndroidManifest.xml`, `FirebaseApp.androidlib`, `GoogleMobileAdsPlugin.androidlib`. **AdMob App ID(`ca-app-pub-1435934257467286~9895276357`)는 GMA 11부터 `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`이 원본**이며 빌드 시 매니페스트에 주입된다 (`AndroidManifest.xml`의 동일 항목은 레거시).
Gradle Java 호환성 11. 16 KB 페이지 정렬: `pwsh tools/spike/Check16Kb.ps1`로 로컬 검증 (2026-09-21 통과). 새 네이티브 SDK를 추가하면 다시 돌린다.

## Next Steps

1. **실기기 확인** — 광고 켠 릴리스 빌드 30분 무크래시(release-gate B-001), 기기 매트릭스(E9-12), FPS·드로우콜(E9-19)
2. **콘솔 작업(개발자)** — RTDB 보안 규칙 적용(`tools/firebase/database.rules.json`), AdMob 계정 승인·스토어 연결·app-ads.txt, 개인정보처리방침 공개 URL
3. **Play Console** — 비공개 테스트(개인 계정이면 12명 × 14일) → 프로덕션
4. 출시 후 분석 이벤트로 V-7(재도전 간격)·보스 첫 도전 실패율 판정

## Development Milestones

| Milestone | Status |
|---|---|
| 3D 프로토타입 (전투·가챠·저장·오프라인) | ✅ 완료 후 폐기 (2026-09-19) |
| 2D 리빌드 GDD · 아키텍처 · project-context | ✅ 2026-09-20 |
| E1 2D 전환 기반 (스파이크·정리·골격·저장 v2·이관) | ✅ |
| E2~E8 전투 → 성장 → 가챠 → 경제 → UI → 아트 | ✅ 구현 (review) 2026-09-28 |
| E9 밸런싱·QA·Google Play 출시 | 🔶 시뮬·QA 진행, 실기기·콘솔 작업 대기 |
