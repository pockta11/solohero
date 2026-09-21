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

## 현재 상태 (2026-09-20)

**2D 리빌드 — 계획 문서 완료, 코드 착수 전.** 3D 쿼터뷰 구현은 2026-09-19에 전면 폐기했다. 생존 스크립트 15개(`Assets/Scripts/`)는 계승 참고용이며 아키텍처 규약에 맞춰 재배치·재작성된다.

| 문서 | 경로 | 역할 |
|---|---|---|
| **GDD** | `_bmad-output/planning-artifacts/gdds/gdd-solohero-2026-09-20/gdd.md` | 설계 단일 출처. `Number Balancing` 상수표가 모든 수치의 원본 |
| Epics | 같은 폴더 `epics.md` | 9 에픽 / 132 스토리 |
| Decision log | 같은 폴더 `decision-log.md` | D-001~D-052 |
| **Architecture** | `_bmad-output/planning-artifacts/architecture/arch-solohero-2026-09-20/game-architecture.md` | 결정 D1~D15, ADR 1~7, 구조, 패턴(코드 예시), 검증 |
| **Project context** | `_bmad-output/project-context.md` | **코드를 만지기 전에 읽는 규칙 62개.** 아키텍처와 충돌 시 아키텍처가 우선 |

코드 작업은 `project-context.md` → 해당 절의 `game-architecture.md` 순으로 읽고 시작한다.

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
현재 `Assets/Scripts`·`Assets/Editor`·`Assets/ScriptableObjects` 등은 E1-03에서 위 구조로 이동한다.

### 게임 규칙 요약 (GDD 상수표가 원본)
- 스테이지: 전역 인덱스 `g = (챕터−1)×10 + 스테이지`, 챕터당 10, 10번째가 보스(15배 HP, 30초). 킬 목표 8 고정. 적 HP `30×1.10^(g−1)`, 골드 `50×1.08^(g−1)`
- 강화: HP·ATK·DEF **승산 ×1.16/레벨·상한 없음**, 공격속도 가산 +0.02·최대 100. 비용 `baseCost × 1.12^level`
- 방어: `DEF_REF = 8 × 적ATK`, `피격 = max(1, 적ATK × DEF_REF / (DEF_REF + DEF))`
- 가챠: 500골드 / 10연 4,500. 확률 **C 55 / R 33 / E 10 / L 2 %**, 100회 천장, Legendary 획득 시 pity 리셋. 중복 자동 환급
- 오프라인: `파밍스테이지_골드 / 400` 초당, 상한 21,600초, 60초 미만 팝업 없음, 음수·상한 2배 초과 → 0. 광고 2배
- 이동 속도 상수 2.0 u/s — 스탯 아님. SP 없음(쿨다운만). 콤보 없음

## Environment Setup

| Requirement | Note |
|---|---|
| Unity 2022.3.62f3 (고정) | Android Build Support + SDK/NDK + **OpenJDK 11**. 63f1+는 xLTS(유료 라이선스)라 상향 불가 |
| Android SDK Platform 36 | Target API 36 (2026-08-31부터 신규 앱 필수) |
| JDK | **JDK 11 확정** (Unity 번들 OpenJDK 11.0.14.1). E1-09 스파이크 결과 A — 2022.3.62f3 + AGP 7.4.2로 16 KB 정렬 통과, JDK 17 불필요 |
| `Assets/google-services.json` | 커밋 금지. 없으면 부트가 `local` 모드로 진입해야 함 |
| DOTween | Utility Panel > Setup + **Create ASMDEF** |
| Android 에뮬레이터 (실기기 없음) | `pwsh tools/android/Emu.ps1 start` → AVD `solohero16k35` (Android 15 · 16 KB 페이지 · Google APIs · SwiftShader). `install` / `run` / `shot`. SDK 루트 `C:\Users\user\android-sdk-tools` (bundletool·build-tools 35 포함) |
| MCP (선택) | `CoplayDev/unity-mcp`(에디터 조작), Context7(2022.3 API 문서), Higgsfield(배경·아이콘 생성) — 설치는 아키텍처 Development Environment 절 |

## Android Build Configuration

`Assets/Plugins/Android/`: `mainTemplate.gradle` / `settingsTemplate.gradle` / `gradleTemplate.properties`(EDM4U가 갱신), `AndroidManifest.xml`, `FirebaseApp.androidlib`, `GoogleMobileAdsPlugin.androidlib`. **AdMob App ID(`ca-app-pub-1435934257467286~9895276357`)는 GMA 11부터 `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`이 원본**이며 빌드 시 매니페스트에 주입된다 (`AndroidManifest.xml`의 동일 항목은 레거시).
Gradle Java 호환성 11. 16 KB 페이지 정렬: `pwsh tools/spike/Check16Kb.ps1`로 로컬 검증 (2026-09-21 통과). 새 네이티브 SDK를 추가하면 다시 돌린다.

## Next Steps

1. **E1-09 스파이크** — 62f3 + SDK Platform 36 + Firebase/AdMob 최신으로 AAB → Play Console 내부 테스트 16 KB 검사
2. **E1-03 정리** — 아키텍처 "정리 대상" 표대로 이동·삭제, 패키지 제거
3. **E1 골격** — asmdef 4개 + Core/Common + `Formulas` + `BalanceConfig` + EditMode 테스트 1개 녹색
4. 이후 E2 전투 코어 → E3/E4 병렬 → E9-01~05 시뮬레이션 1차(E4 착수 전) → E5 → E6 → E7 → E8 → E9

## Development Milestones

| Milestone | Status |
|---|---|
| 3D 프로토타입 (전투·가챠·저장·오프라인) | ✅ 완료 후 폐기 (2026-09-19) |
| 2D 리빌드 GDD · 아키텍처 · project-context | ✅ 2026-09-20 |
| E1 2D 전환 기반 (스파이크·정리·골격·저장 v2·이관) | 📋 다음 |
| E2~E8 전투 → 성장 → 가챠 → 경제 → UI → 아트 | 📋 |
| E9 밸런싱·QA·Google Play 출시 | 📋 |
