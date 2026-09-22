---
baseline_commit: f16e8ce2096ce91ca07675496c8b025771aa9f6e
---
# Story 1.3: 3D 잔재·불필요 패키지 정리 및 Assets/SoloHero 구조 이행

Status: review

<!-- Epic E1-03 · Must · 선행: 1-09 done (파이프라인 확정). 후행: E1 골격(asmdef) 스토리가 이 구조 위에 올라간다 -->
<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a 1인 개발자,
I want 3D 시절 스크립트·데이터·패키지·폴더를 정리하고 살아남을 것만 `Assets/SoloHero/` 아래 아키텍처 구조로 옮겨,
so that 다음 스토리(asmdef 골격, 저장 v2, 부트 시퀀스)가 깨끗한 트리 위에서 시작하고, CI 빌드가 Addressables·Input System 없이도 그대로 녹색이다.

**범위는 "옮기고 지우기"다.** 살아남는 레거시 스크립트를 아키텍처 규약대로 재작성하는 것은 각 담당 스토리(E1-04/05/07, E4, E6, E7)의 몫이며, 이 스토리에서는 컴파일이 되는 상태로 격리만 한다.

## Acceptance Criteria

1. **패키지:** `Packages/manifest.json`에서 `com.unity.addressables`, `com.unity.inputsystem` 제거 (D1·D9). `com.unity.modules.physics`·`physics2d`는 유지 (D10). 에디터를 열면 `packages-lock.json`이 재생성되고 콘솔 오류 0.
2. **Addressables 흔적 0:** `Assets/AddressableAssetsData/` 삭제, `ProjectSettings/EditorBuildSettings.asset`의 `m_configObjects.com.unity.addressableassets` 항목 제거. `grep -ri addressable Assets ProjectSettings Packages/manifest.json`이 벤더 폴더 밖에서 0건.
3. **데이터 정의 정리 (D2):** `Assets/StreamingAssets/JSON/` 5종과 `Assets/Scripts/Models/JsonDataManager.cs` 삭제. `Assets/StreamingAssets/`가 비면 폴더째 삭제.
4. **레거시 스크립트 분류:** 삭제 7종 — `SingletonMB`(D6), `JsonDataManager`(D2), `GachaSystem`(R9 Box-Muller), `PlayerEquipmentService`(GachaSystem 의존), `UpgradeService`(선형 공식 폐기), `IDamageable`·`CameraShake`(3D 전투). 격리 8종 — `PlayerData`, `EquipmentData`, `GameManager`, `SaveManager`, `OfflineRewardSystem`, `AdMobService`, `MainThreadDispatcher`, `SafeAreaAdjuster`를 `Assets/SoloHero/Scripts/Legacy/`로 **`git mv`(.meta 동반, GUID 보존)**. `Legacy/README.md`에 파일별 폐기 담당 스토리를 적는다. `Assets/Scripts/` 폴더는 사라진다.
5. **에디터 스크립트:** `Assets/Editor/FontSetupWizard.cs` → `Assets/SoloHero/Scripts/Editor/FontSetupWizard.cs` (git mv). `Assets/Editor/` 폴더 삭제. (`Assets/Editor Default Resources/`는 Firebase 벤더 — 손대지 않는다.)
6. **데이터 에셋:** `Assets/ScriptableObjects/Equipment/*.asset` 16종 → `Assets/SoloHero/Data/Equipment/` (git mv, 이름은 그대로 — E4-04가 `Equipment_{Slot}_{Grade}`로 재정의). `Assets/ScriptableObjects/` 삭제. `Assets/Prefabs/`(빈 폴더) 삭제.
7. **Resources 예외 기록:** `Assets/Resources/DOTweenSettings.asset`은 DOTween이 `Resources.Load`로 읽는 벤더 필수 파일이라 **유지**한다. 아키텍처 D1 "Resources 폴더 금지"의 예외로 `game-architecture.md`(Project Structure › 정리 대상 표)와 `project-context.md`에 한 줄 기록.
8. **로컬 전용 3D 시절 에셋:** gitignore된 `Assets/Sprites/`(Kenney 플랫포머·UI 팩, 픽셀아트 아님), `Assets/Tiles/`를 로컬에서 삭제. `Assets/Fonts/`(NotoSansKR, 91 MB)는 E8-12 폰트 결정 전까지 로컬 유지. `.gitignore`에서 `Assets/Sprites/`, `Assets/Tiles/`, `Assets/Prefabs/UI/` 행 제거(폴더 소멸), **`Assets/TextMesh Pro/` 행 제거 후 TMP Essential Resources(약 8 MB)를 추적** — CI 체크아웃에 TMP 리소스가 없으면 E7 UI가 CI에서 깨진다.
9. **빌드 회귀 없음:** 에디터 컴파일 오류·경고 0 (레거시 스크립트 격리 후에도), `Tools > Build > Android AAB` 성공, `pwsh tools/spike/Check16Kb.ps1` exit 0, GitHub Actions 녹색. 추적 파일 기준 `Assets/` 용량이 줄었음을 Dev Record에 before/after로 기록 (현재 약 22 MB, LFS 포인터 제외; TMP 추적으로 +8 MB가 더해지므로 순감소가 아닐 수 있다 — 수치만 정직하게 기록).
10. **문서 동기화:** `CLAUDE.md` "프로젝트 구조 (목표)" 아래의 "현재 `Assets/Scripts`·`Assets/Editor`·`Assets/ScriptableObjects` 등은 E1-03에서 위 구조로 이동한다" 문장을 완료형으로 갱신. `game-architecture.md` 정리 대상 표에 완료 표시 + Resources 예외.

## Tasks / Subtasks

- [x] **T1. 패키지 제거** (AC 1, 2)
  - [x] `Packages/manifest.json`에서 `"com.unity.addressables": "1.22.3"`, `"com.unity.inputsystem": "1.14.2"` 두 줄 삭제. `com.unity.collab-proxy`(Unity Version Control, git 사용 중이라 불필요)도 함께 제거 — 런타임 영향 0, 에디터 창 하나 사라짐
  - [x] `ProjectSettings/EditorBuildSettings.asset`에서 `m_configObjects:` 아래 `com.unity.addressableassets:` 행 삭제 → `m_configObjects: {}`
  - [x] `git rm -r Assets/AddressableAssetsData Assets/AddressableAssetsData.meta`
  - [x] **[사용자]** Unity 열기 → Package Manager가 lock 재생성 → 콘솔 오류 0 확인. `ProjectSettings.asset`의 `activeInputHandler: 0`은 1-09에서 이미 설정됨
- [x] **T2. 데이터 정의 정리** (AC 3)
  - [x] `git rm -r Assets/StreamingAssets` (JSON 5종 + meta). 폴더 자체가 비므로 `Assets/StreamingAssets.meta`까지
  - [x] `git rm Assets/Scripts/Models/JsonDataManager.cs{,.meta}` + `Assets/Scripts/Models.meta`
- [x] **T3. 레거시 스크립트 삭제 7종** (AC 4)
  - [x] `git rm` — `Core/SingletonMB.cs`, `Systems/GachaSystem.cs`, `Systems/PlayerEquipmentService.cs`, `Systems/UpgradeService.cs`, `Combat/IDamageable.cs`, `Controllers/Camera/CameraShake.cs` (각 `.meta` 동반), 빈 폴더 `Core/`, `Combat/`, `Controllers/` 및 그 `.meta`
  - [x] 삭제 전 확인: `grep -rn "GachaSystem\|PlayerEquipmentService\|UpgradeService\|CameraShake\|IDamageable\|SingletonMB" Assets/Scripts Assets/SoloHero --include=*.cs`가 삭제 대상 파일 자신들과 `PlayerData.cs`의 **주석 1줄**만 반환해야 한다 (사전 확인 완료 — 다른 참조 없음)
- [x] **T4. 레거시 스크립트 격리 8종** (AC 4)
  - [x] `mkdir Assets/SoloHero/Scripts/Legacy` → `git mv` 8개 (.meta 동반): `Data/PlayerData.cs`, `Data/EquipmentData.cs`, `Managers/GameManager.cs`, `Managers/SaveManager.cs`, `Managers/AdMobService.cs`, `Managers/MainThreadDispatcher.cs`, `Systems/OfflineRewardSystem.cs`, `UI/SafeAreaAdjuster.cs`
  - [x] `Assets/Scripts/` 및 하위 폴더·`.meta` 전부 삭제 (남는 파일 0 확인)
  - [x] `Assets/SoloHero/Scripts/Legacy/README.md` 작성 (영문): 목적(3D 프로토타입에서 계승, 규약 미준수, 참고·이관용) + 표 — 파일 / 계승 대상 / 폐기 스토리: `PlayerData.cs → PlayerDataV1 (E1-05/06 이관 원본)`, `EquipmentData.cs → E4-04 재정의`, `GameManager.cs → BootSequence (E1-04)`, `SaveManager.cs → SaveService+FirebaseSaveStore (E1-07)`, `OfflineRewardSystem.cs → OfflineRewardService (E6-03)`, `AdMobService.cs → AdService (E6-09)`, `MainThreadDispatcher.cs → Infrastructure/MainThreadDispatcher (E1-04)`, `SafeAreaAdjuster.cs → UI/Common (E7-02)`. 규칙: Legacy 안의 코드는 새 코드에서 참조 금지, 담당 스토리가 이식 완료 시 삭제
  - [x] `PlayerData.cs:18` 주석의 `GachaSystem` 언급을 `legacy gacha (removed)`로 수정 — 삭제된 클래스 이름이 코드베이스에 남지 않게
- [x] **T5. 에디터 스크립트·데이터 에셋·빈 폴더** (AC 5, 6)
  - [x] `git mv Assets/Editor/FontSetupWizard.cs{,.meta} Assets/SoloHero/Scripts/Editor/` → `git rm Assets/Editor.meta` (폴더 비면 삭제)
  - [x] `mkdir -p Assets/SoloHero/Data/Equipment` → `git mv Assets/ScriptableObjects/Equipment/*.asset{,.meta}` → `git rm -r Assets/ScriptableObjects{,.meta}`
  - [x] `git rm Assets/Prefabs.meta` (빈 폴더)
  - [x] `Assets/SoloHero/Data.meta`, `Data/Equipment.meta`, `Scripts/Legacy.meta`는 에디터가 생성 → **[사용자]** 에디터 열어 임포트 후 함께 커밋
- [x] **T6. Resources 예외 + 로컬 에셋 + gitignore** (AC 7, 8)
  - [x] `Assets/Resources/DOTweenSettings.asset` 유지. `game-architecture.md` 정리 대상 표의 `Assets/Resources` 행을 "**`DOTweenSettings.asset`만 유지** (DOTween이 `Resources.Load`로 읽는 벤더 필수 파일 — D1 예외). 프로젝트 소유 파일은 두지 않는다"로 수정. `project-context.md` "Removed (do not reintroduce)" 행의 `Resources/`에 `(except vendor-required Assets/Resources/DOTweenSettings.asset)` 추가
  - [x] **[로컬, 사용자 확인 후]** `Assets/Sprites/`, `Assets/Sprites.meta`, `Assets/Tiles/`, `Assets/Tiles.meta` 삭제 (gitignore 대상이라 git 변경 없음, 로컬 14 MB). `Assets/Fonts/`는 유지
  - [x] `.gitignore`에서 `Assets/Sprites/`, `Assets/Sprites.meta`, `Assets/Tiles/`, `Assets/Tiles.meta`, `Assets/Prefabs/UI/` 행 삭제. `Assets/TextMesh Pro/`, `Assets/TextMesh Pro.meta` 행 삭제 → `git add "Assets/TextMesh Pro"` (Essential Resources 약 8 MB. `Examples & Extras`가 있으면 **[사용자]** `Window > TextMeshPro`에서 Examples는 임포트하지 않았는지 확인, 있으면 폴더 삭제 후 추적)
  - [x] `Assets/Fonts/` ignore 행은 유지 (E8-12에서 픽셀 폰트 확정 시 `Art/Fonts/`로 대체)
- [x] **T7. 회귀 검증** (AC 9)
  - [x] **[사용자]** Unity 콘솔: 오류 0, 경고 0 (Legacy 스크립트 격리 후 경고가 남으면 해당 줄만 최소 수정하고 Dev Record에 기록)
  - [x] **[사용자]** `Tools > Build > Android AAB` 성공 (`[Build] result=Succeeded errors=0`)
  - [x] `pwsh tools/spike/Check16Kb.ps1` → exit 0
  - [x] 푸시 → GitHub Actions 녹색 (Addressables 빌드 단계 로그 `Addressable content successfully built`가 **사라졌는지** 확인)
  - [x] 용량: `git ls-files -z Assets | xargs -0 du -cm | tail -1` before/after 기록
- [x] **T8. 문서 동기화 + 마무리** (AC 10)
  - [x] `CLAUDE.md`: "현재 … E1-03에서 위 구조로 이동한다" → "E1-03(2026-09-xx)에서 이동 완료. 레거시 8종은 `Scripts/Legacy/`에 격리, 담당 스토리가 이식 후 삭제". 프로젝트 구조 트리에 `Scripts/Legacy/` 한 줄 추가(임시)
  - [x] `game-architecture.md` 정리 대상 표 각 행에 완료 표시, Legacy 격리 방식 1줄 추가
  - [x] Dev Agent Record: 삭제/이동 목록, 용량, 검증 결과. `sprint-status.yaml` → `review`

## Dev Notes

### 이 스토리가 필요한 이유와 순서

- 아키텍처가 Addressables(D1)·StreamingAssets JSON(D2)·Input System(D9)·`SingletonMB`(D6)·Box-Muller `GachaSystem`(R9) 제거를 결정했고, 1-09 스파이크에서 **Addressables가 아직 빌드 파이프라인에 끼어 있음**을 확인했다 (`Addressable content successfully built` 로그, `AddressableAssetsData/link.xml` 자동 생성). 다음 스토리(asmdef 골격)는 `Assets/SoloHero/Scripts/{Core,Game,Editor,Tests}`에 asmdef를 놓는데, 그 전에 `Assets/Scripts/`의 레거시가 `Assembly-CSharp`에 남아 있으면 Core/Game 경계가 처음부터 흐려진다. [Source: game-architecture.md#Architectural Decisions D1, D2, D6, D8, D9; #Project Structure › 정리 대상]
- 1-09에서 `activeInputHandler`는 이미 `0`(Input Manager)으로 바꿨다 — 패키지 제거만 남았다. [Source: 1-09-build-pipeline-portrait.md#Completion Notes]

### 현재 상태 (사전 조사 완료 — 읽고 시작할 것)

| 대상 | 현재 | 처리 | 근거 |
|---|---|---|---|
| `Packages/manifest.json` | addressables 1.22.3, inputsystem 1.14.2, collab-proxy 2.12.4 포함 | 3개 제거 | D1, D9. collab-proxy는 미사용 |
| `ProjectSettings/EditorBuildSettings.asset` | `m_configObjects.com.unity.addressableassets` 1건 | 제거 | 패키지 제거 후 dangling 참조 경고 방지 |
| `Assets/AddressableAssetsData/` | 그룹 기본 설정 + 빌드가 만든 `link.xml` | 폴더 삭제 | 엔트리 0건 |
| `Assets/StreamingAssets/JSON/` | 5종 (Player/Enemy/Spawn/Weapon/Stage) | 폴더 삭제 | D2 — `BalanceConfig` SO로 대체 예정 |
| `Assets/Scripts/` 15종 | 3D 프로토타입 생존분. 상호 참조: `PlayerEquipmentService → GachaSystem.Instance`, `JsonDataManager : SingletonMB` 외 없음 (확인함) | 7 삭제 / 8 격리 | 아래 분류표 |
| `Assets/Editor/FontSetupWizard.cs` | TMP 한글 폰트 설정 메뉴. `Assets/Fonts/KoreanSDF.asset` 경로 하드코딩 | `SoloHero/Scripts/Editor/`로 이동 (경로는 E8-12에서 수정) | 에디터 전용, E8-12에 유용 |
| `Assets/ScriptableObjects/Equipment/` | 16 SO (`Divine_Blade`, `Iron_Helm` … — GDD 명명과 다름) | `SoloHero/Data/Equipment/`로 이동, 이름 유지 | E4-04가 재정의 |
| `Assets/Prefabs/` | 빈 폴더 (meta만) | 삭제 | — |
| `Assets/Resources/DOTweenSettings.asset` | DOTween 설정 | **유지** | DOTween이 `Resources.Load("DOTweenSettings")` — 벤더 필수 |
| `Assets/Sprites/`, `Assets/Tiles/` | gitignore, 로컬 14 MB, Kenney 플랫포머/UI 팩 (벡터풍, 픽셀아트 아님) | 로컬 삭제 | GDD 픽셀아트 방향과 불일치, 3D 시절 "비주얼 교체 준비" 잔재 |
| `Assets/Fonts/` | gitignore, 로컬 91 MB, NotoSansKR 9종 + SDF | 로컬 유지 | E8-12 한글 픽셀 폰트 결정 전 대안(A-4) |
| `Assets/TextMesh Pro/` | gitignore, 로컬 8 MB, Essential Resources | **추적 시작** | CI 체크아웃에 없으면 TMP 사용 시 런타임 오류 |
| `Assets/Settings/`, `Assets/Plugins/`, `Assets/Firebase/`, `Assets/GoogleMobileAds/`, `Assets/ExternalDependencyManager/`, `Assets/GeneratedLocalRepo/`, `Assets/Editor Default Resources/` | URP·벤더 | 손대지 않음 | 경계 규칙 |

**레거시 스크립트 분류표**

| 파일 | 분류 | 이유 | 이식·폐기 담당 |
|---|---|---|---|
| `Core/SingletonMB.cs` | 삭제 | D6 — `FindObjectOfType` 자동 생성 싱글턴 폐기 | — |
| `Models/JsonDataManager.cs` | 삭제 | D2 | — |
| `Systems/GachaSystem.cs` | 삭제 | R9 — Box-Muller 방식 폐기, 확률표로 재작성 | E5-01 (신규 작성) |
| `Systems/PlayerEquipmentService.cs` | 삭제 | `GachaSystem.Instance` 의존, 정적 클래스 | E4-05 `EquipmentService` (신규) |
| `Systems/UpgradeService.cs` | 삭제 | 선형 비용·가산 보너스 공식 폐기 (GDD) | E4-02 `UpgradeService` (신규) |
| `Combat/IDamageable.cs` | 삭제 | 3D OverlapSphere 전투용 | E2 (신규 설계) |
| `Controllers/Camera/CameraShake.cs` | 삭제 | 3D 카메라용. DOTween `DOShakePosition` 10줄이라 필요 시 재작성 | E8-10 |
| `Data/PlayerData.cs` | **격리** | v1 저장 DTO — 이관 원본으로 반드시 필요 | E1-05/06 → `PlayerDataV1` |
| `Data/EquipmentData.cs` | **격리** | 16 SO 에셋의 스크립트. 삭제하면 에셋이 Missing Script | E4-04 재정의 |
| `Managers/GameManager.cs` | **격리** | 부트 순서(Firebase→인증→로드→오프라인→씬) 참고 | E1-04 `BootSequence` |
| `Managers/SaveManager.cs` | **격리** | 디바운스·로컬 백업·Flush 로직 이식 원본 | E1-07 `SaveService` |
| `Systems/OfflineRewardSystem.cs` | **격리** | 경과 시간 계산 참고 | E6-03 `OfflineRewardService` |
| `Managers/AdMobService.cs` | **격리** | 보상형 광고 로드/표시 흐름 참고 | E6-09 `AdService` |
| `Managers/MainThreadDispatcher.cs` | **격리** | 아키텍처가 "As-Is 계승" 명시 | E1-04 `Infrastructure/` |
| `UI/SafeAreaAdjuster.cs` | **격리** | 아키텍처가 계승 명시 | E7-02 `UI/Common/` |

### 아키텍처 준수 사항

- **`git mv`로 옮긴다** — `.meta`가 함께 움직여야 GUID가 보존되고, 특히 `EquipmentData.cs`의 GUID가 바뀌면 16개 SO 에셋이 전부 Missing Script가 된다. 탐색기 이동·복사 금지. [Source: 1-09 File List — `BuildAutomator.cs.meta` GUID 보존 사례]
- **Legacy 폴더는 임시 격리다.** 새 코드(Core/Game)에서 `Legacy` 타입을 참조하면 asmdef 도입 시 순환 참조가 생긴다. README에 금지 규칙을 적고, 각 담당 스토리가 이식을 끝내면 파일을 지운다. 이 스토리에서 Legacy 코드를 규약대로 고치려 하지 말 것 — 범위 밖. [Source: game-architecture.md#Architectural Boundaries]
- **asmdef는 만들지 않는다.** 다음 스토리(E1 골격). 지금 `Assets/SoloHero/Scripts/Editor/`는 폴더명 규칙으로 에디터 어셈블리, 나머지는 `Assembly-CSharp`. [Source: game-architecture.md D8]
- **Resources 폴더 규칙의 예외**는 DOTween 하나뿐이다. 1-09에서 GMA `Resources/GoogleMobileAdsSettings.asset`이 이미 같은 유형의 벤더 예외로 기록됐다 — 두 사례를 아키텍처 표에 나란히 적는다. [Source: 1-09 Review Findings — "GMA Resources 에셋 편집(벤더 필수 파일)"]
- **코드 안 텍스트는 영문만.** `Legacy/README.md`도 영문. [Source: project-context.md#Code Organization Rules]
- **CI 트리거:** `Assets/**`·`Packages/**`·`ProjectSettings/**` 변경은 빌드를 돌린다(약 9~22분). 문서만 바꾸는 커밋과 분리하면 CI 낭비가 줄지만, 이 스토리는 어차피 빌드 검증이 AC라 한 번은 돌아야 한다. [Source: .github/workflows/build.yml paths-ignore]

### 이전 스토리(1-09)에서 배운 것

- Unity가 새 폴더·스크립트의 `.meta`를 만들면 **그것까지 커밋해야** CI에서 GUID가 안정된다. 에디터를 열지 않고 푸시하면 CI가 매번 새 GUID를 만든다.
- `Assets/Resources/`에 벤더 파일이 있다 — 1-09 리뷰에서 `GoogleMobileAdsSettings.asset` 편집이 "벤더 필수"로 정리됐고, 이번엔 `DOTweenSettings.asset`이 같은 케이스.
- 빌드 로그에 `Addressable content successfully built` / `Running Android Gradle Build Pre-Processor`가 찍힌다 — 전자가 사라지면 Addressables 제거가 빌드까지 반영된 것.
- EDM4U는 에디터를 열 때 `mainTemplate.gradle`·`gradleTemplate.properties`를 다시 쓸 수 있다 — diff에 나타나면 정상, 내용만 확인하고 함께 커밋.
- PowerShell에서 `Remove-Item`에 `C:\Program…` 경로가 같은 명령에 섞이면 도구가 차단한다 — 로컬 폴더 삭제는 `git rm`(추적 파일) 또는 별도 명령으로.

### Project Structure Notes

- 이동 후 트리 (이 스토리 종료 시점):
  ```
  Assets/SoloHero/
  ├── Scripts/
  │   ├── Editor/        BuildAutomator.cs, SpikeSceneSetup.cs, FontSetupWizard.cs
  │   ├── Game/Boot/     BuildSpikeProbe.cs (TEMP)
  │   └── Legacy/        README.md + 8 레거시 스크립트 (TEMP)
  ├── Data/Equipment/    16 SO (이름 변경은 E4-04)
  └── Scenes/Boot.unity
  ```
- 삭제되는 최상위 폴더: `Assets/Scripts`, `Assets/Editor`, `Assets/ScriptableObjects`, `Assets/Prefabs`, `Assets/StreamingAssets`, `Assets/AddressableAssetsData` (+ 로컬 `Sprites`, `Tiles`)
- 새로 추적: `Assets/TextMesh Pro/` (Essential Resources)
- `.gitignore` 정리 후에도 `Assets/Fonts/`, `Assets/google-services.json`, `Builds/`, `*.unitypackage`, `*.apks`, `*.keystore`는 유지

### Project Context Rules

- **Removed (do not reintroduce):** Addressables, Input System package, `StreamingAssets/JSON`, `Resources/`, `SingletonMB`, `JsonDataManager`, Box-Muller `GachaSystem` — 이 스토리가 그 목록을 실제 트리에 적용한다. `Resources/`는 벤더 예외(DOTween·GMA) 한 줄을 project-context에 추가
- **Everything the project owns lives under `Assets/SoloHero/`. Vendor folders are read-only** — 벤더 폴더는 이 스토리에서 건드리지 않는다
- **Four assemblies only … `SoloHero.Tests.EditMode` references Core only** — 아직 asmdef 없음. Legacy 코드가 Core에 들어가면 안 되므로 별도 `Legacy/` 폴더
- **All text inside code is English** — README.md 포함
- **CI = GitHub Actions only … `Free disk space` step is required** — 변경 없음

### 테스트 기준

- 자동 테스트 없음 (asmdef·테스트 어셈블리는 다음 스토리). 검증은 AC 9의 컴파일 0/0 · 로컬 AAB · Check16Kb · CI 녹색 4종.
- 회귀 위험: `EquipmentData.cs` GUID 유실(→ 16 SO Missing Script), Legacy 이동 후 `GameManager`가 참조하는 `SaveManager`·`OfflineRewardSystem`·`AdMobService`는 함께 이동하므로 컴파일 유지. `PlayerData.cs`는 `GachaSystem` 주석만 언급 — 컴파일 영향 없음.

### References

- [Source: _bmad-output/planning-artifacts/gdds/gdd-solohero-2026-09-20/epics.md#E1 — E1-03]
- [Source: _bmad-output/planning-artifacts/architecture/arch-solohero-2026-09-20/game-architecture.md#Architectural Decisions D1, D2, D6, D8, D9, D10; ADR-1, ADR-6, ADR-7]
- [Source: _bmad-output/planning-artifacts/architecture/arch-solohero-2026-09-20/game-architecture.md#Project Structure › Directory Structure, 정리 대상, Architectural Boundaries]
- [Source: _bmad-output/project-context.md#Technology Stack (Removed), Code Organization Rules]
- [Source: _bmad-output/implementation-artifacts/1-09-build-pipeline-portrait.md#Completion Notes, Review Findings]
- [Source: Packages/manifest.json, ProjectSettings/EditorBuildSettings.asset, .gitignore, Assets/Scripts/**, Assets/Editor/FontSetupWizard.cs]

## Dev Agent Record

### Agent Model Used

_(dev-story 실행 시 기록)_

### Debug Log References

### Completion Notes List

- **용량 (추적 파일 기준, `git ls-files -z Assets | xargs -0 du -cm`):** before 22 MB / 612 파일 → after **24 MB / 606 파일**. 순증은 TMP Essential Resources(3 MB) 추적을 새로 시작했기 때문이며, 삭제분(Addressables·StreamingAssets JSON·레거시 스크립트)은 원래 작았다. 로컬 디스크는 `Assets/Sprites`(14 MB) + `Assets/Tiles`(1 MB) + TMP Examples/Documentation(5 MB) = **약 20 MB 감소**
- **AAB 크기·시간:** 69.2 MB / 9분 44초 → **56.3 MB / 4분 20초** (−19% / −55%). Addressables·Input System 제거 효과
- **빌드 결과:** `[Build] result=Succeeded size=511004098 errors=0 warnings=0`. 빌드 로그에서 `Addressable content successfully built` **사라짐** (AC 2 실질 확인)
- **16 KB 검사:** `pwsh tools/spike/Check16Kb.ps1` → RESULT A, exit 0 (정리 후 재확인)
- **CI:** https://github.com/pockta11/solohero/actions/runs/35723982029 (success, Addressables 로그 0건 확인)
- **파일 변동:** 삭제 67 / 이동(R) 60 / 추가 61 / 수정 6 — 이동은 Legacy 8 + Equipment SO 16(각 .meta 포함) + FontSetupWizard, 추가는 대부분 TMP Essential Resources
- **추가 발견·처리 3건:**
  1. `Assets/Scripts/UI/InputActions.inputactions` — 스토리에 없던 Input System 액션 에셋. 패키지 제거 후 고아가 되므로 함께 삭제 (참조 0 확인)
  2. `Assets/StreamingAssets/google-services-desktop.json` — Firebase 에디터가 `google-services.json`에서 자동 생성. 프로젝트 키를 포함하므로 **gitignore 추가** (원본과 동일 취급). 폴더는 재생성되지만 추적 안 됨 → AC 3의 "폴더째 삭제"는 git 기준으로 달성
  3. `GoogleMobileAdsPlugin.androidlib/AndroidManifest.xml` — 빌드가 1-09에서 설정한 App ID를 주입해 재생성. 추적 파일이라 함께 커밋
- **TMP 범위 조정:** 스토리는 Essential Resources를 "약 8 MB"로 예상했으나 실제 추적분은 **3 MB**. `Examples & Extras`·`Documentation`(5 MB)은 개발에 불필요해 로컬 삭제 후 추적하지 않음
- **컴파일:** 오류 0, 경고 0 — Legacy 격리 후에도 수정 불필요 (사전 참조 조사대로)

### File List

- 삭제: `Assets/AddressableAssetsData/**`, `Assets/StreamingAssets/JSON/**`, `Assets/Scripts/**`(7 스크립트 + `InputActions.inputactions` + 폴더 meta), `Assets/Editor.meta`, `Assets/ScriptableObjects/**`, `Assets/Prefabs.meta`
- 이동(git mv, GUID 보존): `Scripts/{Data,Managers,Systems,UI}/*.cs` 8종 → `Assets/SoloHero/Scripts/Legacy/`, `Editor/FontSetupWizard.cs` → `Assets/SoloHero/Scripts/Editor/`, `ScriptableObjects/Equipment/*.asset` 16종 → `Assets/SoloHero/Data/Equipment/`
- 신규: `Assets/SoloHero/Scripts/Legacy/README.md`(+meta), `Assets/SoloHero/{Data,Data/Equipment,Scripts/Legacy}.meta`, `Assets/TextMesh Pro/**`(추적 시작)
- 수정: `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/EditorBuildSettings.asset`, `.gitignore`, `Assets/SoloHero/Scripts/Legacy/PlayerData.cs`(주석), `Assets/Plugins/Android/GoogleMobileAdsPlugin.androidlib/AndroidManifest.xml`
- 문서: `CLAUDE.md`, `_bmad-output/project-context.md`, `game-architecture.md`(정리 대상 표)
