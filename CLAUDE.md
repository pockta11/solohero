# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**SoloHero** — 오프라인 보상과 장비 가챠 기반의 1인 개발 모바일 육성 RPG.
앱을 종료한 시간에 비례해 골드를 획득하고, 그 재화로 정규분포 기반 장비 가챠를 즐기는 쿼터뷰 3D 방치형 액션 RPG.

- **Engine**: Unity 2022.3.62f3 LTS, C#
- **Render Pipeline**: URP 14.0.12
- **View**: 쿼터뷰 3D (Camera Rot X:60, Y:7, Z:−5)
- **Screen**: Landscape 1920×1080
- **Platform**: Android (AAB output)
- **Backend**: Firebase Realtime Database, Authentication, Analytics
- **Asset delivery**: Unity Addressables with Firebase Hosting as remote path
- **Monetization**: Google AdMob 보상형 광고 (Rewarded Interstitial)

## Build Commands

### Jenkins (local Windows)
Jenkins runs Unity in batchmode. The Jenkinsfile calls:
```
"C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -quit -batchmode -projectPath "%WORKSPACE%" -executeMethod BuildAutomator.Build -logFile Builds/build.log
```
Output: `Builds/game.aab`

### GitHub Actions (CI)
Triggered on push to `main` or manually. Uses GameCI `unity-builder@v4` on `ubuntu-latest`.
Requires repository secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`.
Build artifact uploaded as `android-aab` with 7-day retention.

### Manual build in Unity Editor
`Tools > Build > Android AAB` runs `BuildAutomator.Build` which builds LoginScene + GameScene into `Builds/game.aab` with AAB mode enabled.

## Architecture

### Key custom scripts
- `Assets/Editor/BuildAutomator.cs` — Unity Editor build pipeline entry point; called by both Jenkins and GitHub Actions via `-executeMethod BuildAutomator.Build`
- `Assets/Scripts/UI/SafeAreaAdjuster.cs` — handles notch/cutout safe area for mobile UI

### Architecture patterns
- **UI**: MVP (Model/Presenter/View) separation; ScriptableObjects as Models
- **Combat**: FSM via `StateMachineBehaviour`, `IDamageable` interface, NavMeshAgent + Physics.OverlapSphere, object pooling for monsters and effects
- **Gacha**: Gaussian distribution (Box-Muller) with pity (ceiling) system; drop tables per equipment slot
- **Offline rewards**: Save UTC on `OnApplicationPause`/`OnApplicationQuit`, calculate elapsed seconds on resume (max 21,600 s = 6 hours)
- **Data persistence**: DTO pattern → JSON → Firebase Realtime Database; auto-save on pause/quit
- **Attributes**: `[Flags]` enum with bitwise operations for 8-bit element/attribute system
- **Async**: UniTask throughout (not Unity coroutines)
- **Tweening**: DOTween for all animations
- **Stage scaling**: formula-based (no JSON arrays) — kill count: `base(5) + (ch−1)×10 + (stage−1)×2`

### Scenes
- `Assets/Scenes/LoginScene.unity` — entry scene (Addressables patch check, auth)
- `Assets/Scenes/GameScene.unity` — main gameplay scene

### SDKs & packages
- **Firebase** (Auth, Realtime DB, Analytics) — managed via EDM4U; `google-services.json` must be in `Assets/`
- **Google Mobile Ads** v25.0.0 — AdMob; App ID in `Assets/Plugins/Android/AndroidManifest.xml`
- **Addressables** 1.22.3 — remote load path points to Firebase Hosting URL
- **UniTask** — async/await (added via Git URL in `Packages/manifest.json`)
- **DOTween** — requires running setup (Window > DOTween Utility Panel > Setup DOTween)
- **URP** 14.0.12 — Universal Render Pipeline
- **Input System** 1.14.2 — configured in **Both** mode (old + new simultaneously)
- **Newtonsoft.Json** — StreamingAssets/JSON/ 로컬 데이터 로드

## Environment Setup Requirements

| Requirement | Note |
|---|---|
| Unity 2022.3.62f3 | Via Unity Hub with Android Build Support + SDK/NDK + **OpenJDK (JDK 11)** |
| JDK version | **Use JDK 11 only** — JDK 21 causes Firebase compatibility issues |
| `google-services.json` | Must be placed in `Assets/` (not committed; get from Firebase Console) |
| DOTween setup | Run setup wizard after import or after clean Library |
| CanvasScaler reference | **1920×1080 Landscape** |

## Android Build Configuration

Custom Gradle templates are in `Assets/Plugins/Android/`:
- `mainTemplate.gradle` / `settingsTemplate.gradle` — modified by EDM4U to inject Firebase and AdMob dependencies
- `AndroidManifest.xml` — contains AdMob Application ID (`ca-app-pub-1435934257467286~9895276357`)
- `FirebaseApp.androidlib`, `GoogleMobileAdsPlugin.androidlib` — pre-built Android libraries

Java compatibility is set to **Java 11** in the Gradle template. Do not change this to Java 17/21 — it breaks Firebase.

## Implemented Systems

### ✅ Jenkins + GitHub Actions 이중 빌드 파이프라인
- `BuildAutomator.Build()` — Jenkins batchmode 및 GitHub Actions 양쪽에서 동일 메서드 호출
- GitHub Actions: `UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD` 시크릿 기반 인증

### ✅ 모바일 빌드 세팅 (SafeArea / CanvasScaler / Input)
- `Screen.safeArea`로 기기별 안전 영역 계산 → RectTransform 보정 (노치/홈 버튼 대응)
- `CanvasScaler` Scale With Screen Size — 기준 해상도 **1920×1080 Landscape**
- Input System **Both 모드** (Legacy + New 동시 활성) — 가상 조이스틱(모바일) / WASD(에디터) 동시 지원
- Canvas 구성: HUD(sortOrder 10) · Menu(15) · Joystick(20), 조이스틱 반투명 처리 (alpha 0.15 / 0.4)

### ✅ 정규분포 곡선 기반 가챠 시스템
- Box-Muller 변환으로 정규분포 샘플 생성, 장비 부위별 ScriptableObject DropTable 관리
- 장비 슬롯: **Sword / Helm / Armor / Boots** — 16종 ScriptableObject (4등급 × 4슬롯)
- 등급 가중치: Common=0.20, Rare=0.45, Epic=0.68, Legendary=0.90
- **천장 시스템(Pity)**: 100회 누적 시 Legendary 보장, 달성 후 카운터 리셋
- 진행도(pity 누적)에 따라 정규분포 평균(μ) 이동: 초반 0.3 → 천장 직전 0.8

### ✅ NavMeshAgent + FSM 기반 자동 전투 시스템
- **뷰**: 쿼터뷰 3D, Physics.OverlapSphere 공격 판정 (Physics2D 미사용)
- PlayerController: Space 근거리 공격 (OverlapSphere r=1.8), J 스킬 (SP 50 소모, r=3.5, 데미지×3)
- EnemyController: NavMeshAgent 추적, Physics.OverlapSphere 피격 감지
- PlayerAutoController: 자동 전투 모드 (가장 가까운 적 자동 추적·공격)
- SpawnManager: 스테이지 시작 시 StartStageSpawn(count) 코루틴 순차 스폰
- IDamageable 인터페이스로 플레이어·몬스터 간 데미지 전달 표준화
- DOTween으로 피격 플래시, 콤보 텍스트, 사망/스테이지 클리어 패널 연출

### ✅ Chapter×Stage 진행 시스템
- 공식 기반 스케일링 — JSON 배열 없이 상수만으로 전 스테이지 수치 생성
  - 필요 킬수: `base(5) + (ch−1)×10 + (stage−1)×2`
  - 골드 보상: `base(50) + (ch−1)×100 + (stage−1)×20`
- JsonDataManager: Newtonsoft.Json + StreamingAssets/JSON/ 로컬 로드 (PlayerData·EnemyData·SpawnData·StageData)
- StageManager.OnEnemyKilled() → 킬 충족 시 StageClear() 코루틴 → 3초 후 다음 스테이지 자동 진행

### Planned: Addressables + Firebase Hosting 다운로드 관리
- `patchSize == 0` 이면 씬 즉시 전환, `patchSize > 0` 이면 다운로드 UI 활성화
- UniTask 기반 비동기 진행률 UI 실시간 업데이트

### Planned: UTC 유닉스 타임스탬프 기반 오프라인 보상
- `OnApplicationPause` / `OnApplicationQuit`에서 UTC 시간 Firebase 저장
- `DateTimeOffset.UtcNow.ToUnixTimeSeconds()`로 경과 시간 계산 (최대 21,600초)
- AdMob 보상형 광고 시청 시 2배 보상

### Planned: 비트플래그 + StateMachineBehaviour 속성 시스템
- `[Flags] enum ElementType : byte` — 8비트 최대 8개 속성 (Fire, Water, Wind, ...)
- 비트 연산(|, &, ~)으로 속성 추가·제거·확인
- 이펙트에 오브젝트 풀링(스택 구조) 적용

### ✅ 인벤토리 + 업그레이드 + Firebase 저장
- `SaveManager`: Firebase RTDB 읽기/쓰기 + PlayerPrefs 로컬 백업 (강제 종료 대응)
- `PlayerData`: 업그레이드 레벨(HP/ATK/DEF/SPD), 보유장비 CSV, lastQuitTimeUtc 포함
- `PlayerEquipmentService`: 가챠 결과 자동 장착/인벤 처리, GetEquippedId/SetEquippedId
- `PlayerEquipmentApplier`: 장비+업그레이드 보너스 합산 → PlayerController 스탯 적용
- `UpgradeService`: 레벨업 비용 공식 `baseCost × (level + 1)`, 최대 레벨 50
- `InventoryPanelUI` + `InventoryItemSlotUI`: 보유 장비 스크롤 목록, 슬롯 클릭 장착
- `EquipmentPanelUI`: 장착 슬롯 4개 + 합산 스탯 요약 + 인벤토리 토글
- `UpgradePanelUI`: HP/ATK/DEF/SPD 행별 골드 소비 강화
- `MenuSetupWizard`: `Tools > Setup` 메뉴로 ScriptableObject·Canvas·프리팹 자동 생성
- `GameManager`: Firebase 익명 로그인 → SaveManager 로드 → 오프라인 보상 계산 → GameScene 전환

### ✅ 오프라인 보상 시스템
- `OfflineRewardSystem`: 경과 시간 계산, 최대 6시간(21600초) 제한, 초당 1골드 기본값
- `OfflineRewardPopup`: DOTween 팝업 애니메이션, 일반 수령 / 2배 수령(AdMob TODO)

### Planned: 비트플래그 + StateMachineBehaviour 속성 시스템
- `[Flags] enum ElementType : byte` — 8비트 최대 8개 속성 (Fire, Water, Wind, ...)
- 비트 연산(|, &, ~)으로 속성 추가·제거·확인
- 이펙트에 오브젝트 풀링(스택 구조) 적용

## Development Milestones

| Milestone | Status |
|---|---|
| 핵심 시스템 설계 및 환경 구축 (빌드, Firebase/AdMob, CI/CD) | ✅ Done |
| 전투 + 스테이지 + 가챠 시스템 | ✅ Done |
| UI / 인벤토리 / Firebase 저장 연동 (MVP, DTO, Realtime DB) | ✅ Done |
| 오프라인 보상 시스템 (로직 + UI) | ✅ Done |
| AdMob 보상형 광고 연동 (오프라인 보상 2배) | 📋 Planned |
| 3D 모델 + Animator Controller 교체 (캡슐 → 실제 모델) | 🔄 In progress |
| QA · 밸런싱 · Google Play 출시 | 📋 Planned |
