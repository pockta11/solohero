---
baseline_commit: d255081eeb78dc3c4f721e2fdbd1e3f73266a97a
---
# Story 1.9: 빌드 파이프라인 세로 설정 갱신 + Android API 36 / 16 KB 실빌드 스파이크

Status: in-progress

<!-- Epic E1-09 · Must · epic-1의 첫 스토리 (architecture D13) -->
<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a 1인 개발자,
I want 2022.3.62f3(고정) · 타깃 API 36 · 64-bit IL2CPP · 최신 Firebase/AdMob 조합으로 Jenkins와 GitHub Actions 양쪽에서 AAB가 나오고 Play Console의 16 KB 페이지 크기 검사를 통과하는 것을 **먼저** 확인하고,
so that E1의 나머지 작업(정리·골격·저장)을 JDK 11 유지 / JDK 17 전환이 확정된 안정된 파이프라인 위에서 진행할 수 있다.

**이 스토리는 스파이크다.** 산출물은 (1) 통과하는 빌드 파이프라인, (2) JDK·AGP·Gradle 결정 기록이다. 게임 기능은 넣지 않는다.

## Acceptance Criteria

1. **에디터 고정 + SDK 36:** 프로젝트는 **2022.3.62f3** 그대로 (63f1+는 Industry/Enterprise 전용 xLTS — 상향 불가). Android **SDK Platform 36**이 Unity 번들 SDK에 설치되어 있고 컴파일 오류 0. `Jenkinsfile`·`activation.yml`은 62f3을 가리킨다.
2. **Player Settings (Android):** Scripting Backend **IL2CPP**, Target Architectures **ARM64 + ARMv7**, Target API Level **36 (명시값, "Highest Installed" 아님)**, Minimum API 24, Default Orientation **Portrait** 단독(자동 회전 끔), Aspect Ratio Mode **Custom ≥ 2.5** (20:9 세로 기기 레터박스 방지), Managed Stripping **Low** 이상.
3. **SDK 상향:** Firebase Unity SDK **≥ 13.17.0** (App·Auth·Database·Analytics), Google Mobile Ads Unity **≥ 11.5.0**, EDM4U는 위 패키지에 동봉된 최신. `Assets > External Dependency Manager > Android Resolver > Force Resolve` 재실행으로 `mainTemplate.gradle` 의존성 블록이 재생성된다.
4. **로컬 AAB:** `Tools > Build > Android AAB`로 `Builds/game.aab` 생성 성공. `bundletool build-apks` → 추출한 **arm64-v8a `.so` 전부**가 16 KB 정렬(ELF LOAD 세그먼트 align ≥ `0x4000`)이다 — Unity 라이브러리(`libunity`, `libil2cpp`, `libmain`)와 서드파티(`libFirebaseCppApp`, `libFirebaseCppAuth`, `libFirebaseCppDatabase`, `libFirebaseCppAnalytics` 등) 모두.
5. **Play Console:** 내부 테스트 트랙에 업로드했을 때 **16 KB 페이지 크기 경고 없음**, **타깃 API 레벨 경고 없음**, 64-bit 요구 충족.
6. **실기기:** Android 기기(가능하면 Android 15+, 16 KB 모드 지원 기기 또는 에뮬레이터 16 KB 시스템 이미지)에서 설치·실행. 플레이스홀더 부트 씬이 뜨고 `BuildSpikeProbe`가 Firebase 초기화(`CheckAndFixDependenciesAsync` → `Available`)와 AdMob 초기화(`MobileAds.Initialize` 콜백)를 화면 텍스트로 보고하며 **크래시 없음**. (16 KB 크래시는 `libFirebaseCppApp.so` 로드 시점에 나므로 이 프로브가 필수다.)
7. **CI 양쪽 성공:** Jenkins 로컬 빌드 성공(`Builds/game.aab`), GitHub Actions `Unity Android Build` 성공 + artifact `android-aab` 업로드.
8. **결정 기록:** 아래 결정 트리의 결과(**A: JDK 11 유지** / **B: AGP 8.5+ · Gradle 8.7+ · JDK 17 전환**)를 이 스토리의 Dev Agent Record, `game-architecture.md` D13 행, `CLAUDE.md` Environment Setup JDK 행, `_bmad-output/project-context.md` Build 행에 기록한다. B인 경우 Gradle 템플릿·CI 변경이 커밋에 포함된다.

## Tasks / Subtasks

- [ ] **T1. Unity 2022.3.62f3 고정 + SDK Platform 36** (AC 1)
  - [x] ~~2022.3.76f1 설치~~ **불가 확인** — Hub 라이선스 오류 "part of an Extended LTS release, requires Industry or Enterprise". 62f3 유지로 결정 변경, 아키텍처·CLAUDE.md·project-context 갱신
  - [ ] Unity 번들 SDK(`Edit > Preferences > External Tools > Android > SDK` 경로)에 **`platforms\android-36`** 존재 확인. 없으면 `<SDK>\cmdline-tools\<ver>\bin\sdkmanager.bat "platforms;android-36" "build-tools;35.0.0"`
  - [ ] 62f3에서 프로젝트 열기 → 컴파일 오류 0 확인
  - [x] `Jenkinsfile`·`.github/workflows/activation.yml` — 62f3 유지 (76f1로 바꿨다가 되돌림)
- [ ] **T2. Player Settings 세로·64-bit·API 36** (AC 2)
  - [x] `Edit > Project Settings > Player > Android > Other Settings`: Scripting Backend IL2CPP, Api Compatibility .NET Standard 2.1, Target Architectures ARMv7 + ARM64, Minimum API 24, Target API **36**
  - [x] Resolution and Presentation: Default Orientation Portrait, Auto Rotation 끔, Aspect Ratio Mode Custom → **2.5**
  - [x] Optimization: Managed Stripping Level Low (Firebase 리플렉션 안전). `Assets/link.xml`은 이 스토리에서 만들지 않는다 (E1-05)
  - [ ] Publishing Settings: Custom Main Gradle Template / Custom Gradle Properties Template / Custom Gradle Settings Template 체크 상태 유지 확인 (`Assets/Plugins/Android/*` 사용)
- [ ] **T3. Firebase · AdMob · EDM4U 상향** (AC 3)
  - [ ] Firebase Unity SDK 13.17.0 `.unitypackage` 4종(App은 자동, Auth · Database · Analytics) 임포트. 기존 13.9.0 파일이 남지 않도록 `Assets/Firebase`, `Assets/ExternalDependencyManager` 버전 매니페스트 확인 (`*_version-13.17.0_manifest.txt`만 존재)
  - [ ] Google Mobile Ads Unity 11.5.0 임포트. `Assets/GoogleMobileAds/GoogleMobileAds_version-11.5.0_manifest.txt` 확인
  - [ ] `Assets > External Dependency Manager > Android Resolver > Force Resolve` → `mainTemplate.gradle`의 `// Android Resolver Dependencies Start` 블록 재생성. **`sourceCompatibility JavaVersion.VERSION_11` 줄은 이 시점에 그대로 둔다** (결정 트리 전)
  - [ ] `Assets/Plugins/Android/AndroidManifest.xml`의 AdMob App ID(`ca-app-pub-1435934257467286~9895276357`) 유지 확인
- [ ] **T4. 플레이스홀더 부트 씬 + 프로브** (AC 6)
  - [ ] `Assets/SoloHero/Scenes/Boot.unity` 생성 — Main Camera(Orthographic, 배경 단색) + Canvas(Portrait 1080×1920 CanvasScaler) + TMP Text `StatusLabel`
  - [x] `Assets/SoloHero/Scripts/Game/Boot/BuildSpikeProbe.cs` (MonoBehaviour, **임시** — E1-04 `BootSequence`가 대체하며 삭제). `Start`에서 순서대로: Unity 버전·`SystemInfo`(기기·OS·`Application.targetFrameRate`) 표시 → `FirebaseApp.CheckAndFixDependenciesAsync()` 결과 표시 → `MobileAds.Initialize(status => ...)` 결과 표시. 모든 콜백은 `try/catch`로 감싸고 예외 메시지를 라벨에 출력. 코드 텍스트 영문만
  - [ ] `EditorBuildSettings`에 `Boot.unity` 1개 등록
- [ ] **T5. BuildAutomator 갱신** (AC 4, 7)
  - [x] `Assets/Editor/BuildAutomator.cs` → `Assets/SoloHero/Scripts/Editor/BuildAutomator.cs`로 이동 (`.meta` 함께). 폴더명이 `Editor`이므로 asmdef 없이도 에디터 어셈블리로 컴파일됨
  - [x] 씬 목록을 하드코딩 대신 `EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path)`로 읽기 (LoginScene·GameScene 참조 제거 — 존재하지 않아 빌드 실패의 원인)
  - [x] `EditorUserBuildSettings.buildAppBundle = true` 유지, `BuildOptions`는 환경변수 `SOLOHERO_DEV_BUILD=1`이면 `Development` 추가 (개발 빌드에서 `BuildConfig.useTestAdIds` 강제 규약의 기반)
  - [x] `BuildReport.summary.result != Succeeded`이면 `EditorApplication.Exit(1)` — batchmode에서 실패가 CI 실패로 전파되도록
  - [x] `Builds/` 디렉터리 없으면 생성
- [ ] **T6. 로컬 빌드 + 16 KB 정렬 검사** (AC 4)
  - [ ] `Tools > Build > Android AAB` → `Builds/game.aab`
  - [ ] bundletool(최신 jar 다운로드)로 APK 추출:
        `java -jar bundletool.jar build-apks --bundle=Builds/game.aab --output=Builds/spike.apks --mode=universal` → `spike.apks`를 zip으로 열어 `universal.apk` → 다시 zip으로 열어 `lib/arm64-v8a/*.so` 추출
  - [ ] 정렬 검사 (Windows): NDK의 `llvm-readelf`(`<NDK>/toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-readelf.exe`)로 각 `.so`에 `-l` 실행 → 모든 `LOAD` 행의 `Align`이 `0x4000` 이상인지 확인. 결과를 표로 Dev Record에 기록 (라이브러리명 · align · 통과/실패)
  - [ ] APK 안 `.so`가 **비압축**이고 zip 엔트리 오프셋이 16 KB 배수인지도 확인 (`zipalign -c -P 16 -v 4 universal.apk` — SDK build-tools 35+에 포함)
- [ ] **T7. 결정 트리 실행** (AC 8)
  - [ ] **결과 A — T6 전부 통과:** JDK 11 유지. `mainTemplate.gradle` Java 11 그대로. 결정 A를 기록하고 T8로
  - [ ] **결과 B-1 — 서드파티 `.so`만 실패:** 해당 SDK가 더 최신인지 확인(T3 재확인). 그래도 실패면 Firebase/GMA GitHub 이슈 번호와 함께 기록하고 사용자에게 보고 후 중단
  - [ ] **결과 B-2 — Unity `.so`(libunity/libil2cpp/libmain) 실패:** 62f3(16 KB 지원 56f1+)에서 나오면 안 되는 결과. Unity Discussions 검색 후 사용자에게 보고. 진행 중단
  - [ ] **결과 B-3 — `.so`는 정렬됐으나 `zipalign -P 16` 실패 (패키징 문제, AGP 7.4.2 한계):** AGP·Gradle·JDK 상향 경로 시도 —
        (a) `Edit > Preferences > External Tools > Android`에서 Gradle **8.7+** 설치 경로와 **JDK 17** 경로를 지정(Unity 번들 해제),
        (b) `Assets/Plugins/Android/baseProjectTemplate.gradle`을 커스텀 활성화하고 `com.android.tools.build:gradle:8.5.1`(또는 62f3이 허용하는 최신)로,
        (c) `mainTemplate.gradle` `sourceCompatibility/targetCompatibility` → `VERSION_17`, AGP 8 요구사항(`namespace`, `buildFeatures`) 반영,
        (d) `gradleTemplate.properties`에 `android.bundle.enableUncompressedNativeLibs` 관련 설정 검토,
        (e) 재빌드 → T6 재검사. 통과하면 **결정 B**. Jenkins·GameCI에서도 JDK 17이 잡히도록 T8에서 처리
  - [ ] 어느 결과든 `game-architecture.md` **D13** 행, `CLAUDE.md` JDK 행, `project-context.md` Build 행을 같은 커밋에서 갱신
- [ ] **T8. CI 양쪽 빌드** (AC 7)
  - [ ] Jenkins: `Jenkinsfile` 실행 → `Builds/build.log` 마지막에 `Build succeeded` 확인. 결정 B면 Jenkins 에이전트에 JDK 17 설치 + Unity Preferences 경로가 배치 모드에서도 적용되는지(Preferences는 사용자 단위 — `-executeMethod` 전에 `EditorPrefs`로 `JdkPath`/`GradlePath`를 설정하는 코드를 `BuildAutomator`에 추가) 확인
  - [ ] GitHub Actions: `workflow_dispatch`로 수동 실행 → 성공 + artifact. `androidExportType: androidAppBundle` 유지. `actions/cache@v3` → `@v4`로 상향(v3 deprecated). 결정 B면 `unity-builder` 이전에 `actions/setup-java@v4`(temurin 17)를 넣고 `JAVA_HOME`이 컨테이너 안에서 보이는지 확인 — GameCI 2022.3 이미지는 JDK 11 동봉이라 실패 가능성 있음. 실패 시 Jenkins만 통과로 두고 GameCI는 이슈로 기록(단일 실패점 제거가 목표이므로 두 파이프라인 중 하나라도 살아 있어야 함)
- [ ] **T9. Play Console 검사** (AC 5)
  - [ ] Play Console(기존 앱 항목 사용, 없으면 내부 테스트용 신규 앱 생성)에 `game.aab` 업로드 → 내부 테스트 트랙. 업로드 키스토어가 없으면 이 스토리에서 **디버그 서명으로 업로드 불가** → `Publishing Settings > Keystore Manager`로 업로드 키 생성, 키스토어 파일과 비밀번호는 저장소 밖(비밀번호 관리자)에 보관하고 경로만 기록. `ProjectSettings`에 키스토어 비밀번호가 평문으로 남지 않도록 `androidUseCustomKeystore`만 켜고 비밀번호는 빌드 시 환경변수(`SOLOHERO_KEYSTORE_PASS`, `SOLOHERO_KEYALIAS_PASS`)에서 `BuildAutomator`가 주입
  - [ ] 업로드 후 "App bundle explorer"와 경고 배너에서 16 KB · 타깃 API · 64-bit 관련 경고 없음 스크린샷/문구를 Dev Record에 기록
- [ ] **T10. 실기기 확인** (AC 6)
  - [ ] 내부 테스트 링크 또는 `bundletool install-apks`로 기기 설치. Android 15+ 기기가 있으면 개발자 옵션 "16 KB 페이지 크기로 부팅" 활성화 후 재시도. 없으면 Android Studio 에뮬레이터의 16 KB 시스템 이미지(API 35+ `16k` 태그) 사용
  - [ ] 프로브 라벨 3줄(Unity 버전 / Firebase `Available` / AdMob initialized) 확인, 30초 대기 후 크래시 없음. `adb logcat -s Unity FirebaseApp` 로그 첨부
- [ ] **T11. 마무리** (AC 8)
  - [ ] Dev Agent Record에 결정(A/B), 정렬 검사 표, Play Console 결과, 기기 결과, 변경 파일 목록 기록
  - [ ] `sprint-status.yaml`의 `1-09-build-pipeline-portrait`를 `review`로
  - [ ] 커밋 메시지 예: `build: Unity 2022.3.62f3 · API 36 · IL2CPP ARM64 · Firebase 13.17 / GMA 11.5 — 16 KB 스파이크 결과 A(JDK 11 유지)`

## Dev Notes

### 왜 이 스토리가 epic-1의 첫 작업인가

- Google Play는 **2026-08-31부터 신규 앱에 타깃 API 36(Android 16)을 요구**한다. SoloHero는 신규 앱이다. [Source: game-architecture.md#Engine & Framework › 외부 제약]
- API 35+ 타깃 앱은 **16 KB 페이지 정렬**이 필수이고, 이 검사는 Play Console에 올려 봐야 확실히 안다. 2022.3 내장 AGP 7.4.2 / Gradle 7.5.1로 서드파티 `.so` 패키징 정렬이 실패한 사례(2022.3.71f1, 2026-01)가 보고되어 있다. 실패하면 JDK 17 전환이 필요하고, 그건 `CLAUDE.md`의 "JDK 11 고정" 제약을 뒤집는다. **E1의 다른 작업이 이 결정에 의존하므로 먼저 한다.** [Source: game-architecture.md#Architectural Decisions D13, ADR 없음 — Validation 절 O4]
- 현재 `AndroidTargetArchitectures: 1`(ARMv7 단독)이고 `scriptingBackend: {}`(Mono 기본)이다. **이 상태의 AAB는 64-bit 요구로 Play에서 거부된다.** 16 KB 이전에 이것부터 고쳐야 한다. [Source: ProjectSettings/ProjectSettings.asset:267, :685]
- `AndroidTargetSdkVersion: 0`("Highest Installed")은 CI 머신마다 결과가 달라진다. 36으로 명시한다. [Source: ProjectSettings/ProjectSettings.asset:176]
- `androidMaxAspectRatio: 2.1`은 20:9(2.22) 이상 세로 기기에서 레터박스를 만든다. GDD "다른 종횡비는 세로 기준 스케일 + SafeArea 보정"을 위해 2.5로. [Source: ProjectSettings/ProjectSettings.asset:164, gdd.md#Platform-Specific Details]

### 현재 상태 (읽고 시작할 것)

| 파일 | 현재 | 이 스토리가 바꾸는 것 | 보존할 것 |
|---|---|---|---|
| `Assets/Editor/BuildAutomator.cs` | `LoginScene`·`GameScene` 하드코딩(둘 다 없음 → 빌드 실패), `buildAppBundle = true`, 결과 검사 없음 | 위치 이동, `EditorBuildSettings` 기반 씬, 실패 시 exit 1, dev 빌드 플래그, (결정 B 시) JDK/Gradle 경로 주입 | 메뉴 `Tools/Build/Android AAB`, `-executeMethod BuildAutomator.Build` 시그니처 (Jenkins·GameCI 양쪽이 호출) |
| `Jenkinsfile` | 62f3 경로, `-quit -batchmode -executeMethod BuildAutomator.Build -logFile Builds/build.log` | 에디터 경로만 | 나머지 인자 |
| `.github/workflows/build.yml` | `game-ci/unity-builder@v4`, `androidExportType: androidAppBundle`, artifact `build/Android/*.aab`, `actions/cache@v3` | cache v4, (결정 B 시) JDK 17 단계 | secrets 3종, buildMethod |
| `Assets/Plugins/Android/mainTemplate.gradle` | Java 11, Firebase 13.9.0 / GMA 25.0.0 의존성(EDM4U 생성), `**APIVERSION**`·`**TARGETSDKVERSION**` 플레이스홀더 | Resolver가 의존성 블록 재생성. (결정 B 시) Java 17 | 플레이스홀더, `packagingOptions` |
| `Assets/Plugins/Android/gradleTemplate.properties` | `android.useAndroidX=true`, `enableJetifier=true` | (결정 B 시) 검토 | Resolver 블록 |
| `Assets/Plugins/Android/AndroidManifest.xml` | AdMob App ID | 없음 | 전부 |
| `Assets/Scenes/` | 비어 있음, `EditorBuildSettings` 비어 있음 | `Assets/SoloHero/Scenes/Boot.unity` 신규 + 등록 | — |
| `Assets/Firebase/Editor/*_version-13.9.0_manifest.txt` | 13.9.0 | 13.17.0으로 교체 (구버전 파일 잔존 금지) | — |
| `Assets/GoogleMobileAds/GoogleMobileAds_version-11.0.0_manifest.txt` | 11.0.0 | 11.5.0 | — |

### 아키텍처 준수 사항

- **프로젝트 소유 파일은 `Assets/SoloHero/` 아래.** 이 스토리가 만드는 `Boot.unity`, `BuildSpikeProbe.cs`, 이동된 `BuildAutomator.cs`가 그 첫 파일이다. 벤더 폴더(`Firebase/`, `GoogleMobileAds/`, `ExternalDependencyManager/`, `Plugins/`)는 SDK 임포트 도구가 쓰는 것 외에 손대지 않는다. [Source: game-architecture.md#Project Structure › Architectural Boundaries]
- **asmdef는 아직 만들지 않는다.** E1 골격 스토리(E1-03 이후)에서 4개를 한 번에 만든다. 이 스토리의 스크립트 2개는 `Assembly-CSharp` / `Assembly-CSharp-Editor`에 들어가며 그때 재배치된다. [Source: game-architecture.md#Architectural Decisions D8]
- **`BuildSpikeProbe`는 임시다.** 아키텍처의 `BootSequence`(E1-04)가 대체한다. 파일 상단 주석에 `// TEMP: build spike probe. Replaced by BootSequence in E1-04.` 명시. 서비스 로케이터·이벤트 규약을 여기 적용하려 하지 말 것 — 스파이크의 범위 밖.
- **Managed Stripping Low.** Firebase·Newtonsoft가 리플렉션을 쓴다. `link.xml`은 저장 스키마 스토리(E1-05)에서 `SaveDataV2`와 함께 만든다. 이 스토리에서 High로 올리지 말 것.
- **Input System 제거(D9)·Addressables 제거(D1)는 이 스토리가 아니다** — E1-03. 패키지 상태를 여기서 건드리면 스파이크 변수가 늘어난다. 단, 76f1 업그레이드 후 Addressables 1.22.3이 컴파일 경고를 내면 기록만 한다.
- **코드 안 텍스트는 영문만.** 프로브 라벨 문자열도 영문(`"Firebase: Available"`). [Source: project-context.md#Code Organization Rules]
- **개발 빌드 구분은 컴파일 심볼.** `SOLOHERO_DEV_BUILD=1` → `BuildOptions.Development` → `DEVELOPMENT_BUILD` 심볼. 이후 `BuildConfig.useTestAdIds` 강제, 디버그 패널 컴파일이 이 심볼에 걸린다. [Source: game-architecture.md#Cross-cutting Concerns › Debug Tools › Activation]

### 라이브러리 · 버전 (2026-09-20 확인)

| 항목 | 현재 | 목표 | 근거 |
|---|---|---|---|
| Unity | 2022.3.62f3 | **2022.3.62f3 고정** | 63f1+는 xLTS(Industry/Enterprise 전용) — 상향 불가. 16 KB 엔진 지원은 56f1+라 62f3에 포함. GameCI `ubuntu-2022.3.62f3-android-3` (기존 사용) |
| Firebase Unity SDK | 13.9.0 | **13.17.0** (2026-09-17) | 16 KB 지원 12.6.0+. 12.6~12.9에서 16 KB 기기 시작 크래시 이슈(#1259, `libFirebaseCppApp.so` `SWIGRegisterExceptionCallbacks`)가 있었으므로 **반드시 13.x 최신** + 실기기 프로브로 확인. 최소 Unity 2021 |
| Google Mobile Ads Unity | 11.0.0 (Android SDK 25.0.0) | **11.5.0** (2026-09-03) | 최신. 16 KB 정렬된 `.so` 확인 대상 |
| EDM4U | 1.2.187 | 위 패키지 동봉 최신 | Resolver 재실행 필수 |
| AGP / Gradle (Unity 내장) | 7.4.2 / 7.5.1 | 결정 A: 유지 / 결정 B: 8.5.1+ / 8.7+ | 16 KB 자동 정렬은 AGP 8.5.1+. 2022.3에서 AGP 8은 공식 지원 밖 — 그래서 스파이크 |
| JDK | 11 (Unity 번들) | 결정 A: 11 / 결정 B: 17 | Gradle 8.7+는 JDK 17 필요 |
| bundletool | — | 최신 jar | APK 추출·설치 |
| Android SDK Platform | 35? | **36** | Target API 36 |
| Android SDK Build-Tools | — | 35+ | `zipalign -P 16` 옵션 |

### 16 KB 검사 근거

- Google Play: 2025-11-01부터 API 35+ 타깃 신규 앱·업데이트는 16 KB 지원 필수. 검사는 (1) 각 `.so`의 ELF LOAD align ≥ 16384, (2) APK 안 비압축 `.so`의 zip 정렬 16 KB, 두 가지다.
- `llvm-readelf -l lib.so | findstr LOAD` — `Align` 열이 `0x4000` 또는 `0x10000`이면 통과, `0x1000`이면 실패.
- `zipalign -c -P 16 -v 4 universal.apk` — 종료 코드 0이면 통과.
- 정렬 검사가 전부 통과해도 Firebase #1259처럼 **실기기에서만 나는 크래시**가 있었다. T10을 생략하지 말 것.

### Project Structure Notes

- 신규: `Assets/SoloHero/Scenes/Boot.unity`, `Assets/SoloHero/Scripts/Game/Boot/BuildSpikeProbe.cs`, `Assets/SoloHero/Scripts/Editor/BuildAutomator.cs`(이동)
- 삭제: `Assets/Editor/BuildAutomator.cs`(이동됨). `Assets/Editor/FontSetupWizard.cs`는 그대로 둔다 (E1-03 대상)
- 변경: `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/EditorBuildSettings.asset`, `Jenkinsfile`, `.github/workflows/build.yml`, `.github/workflows/activation.yml`, `Assets/Plugins/Android/mainTemplate.gradle`(Resolver), `Assets/Firebase/**`, `Assets/GoogleMobileAds/**`, `Assets/ExternalDependencyManager/**`, `Packages/manifest.json`(EDM4U가 갱신할 수 있음)
- 결정 B 추가 변경: `Assets/Plugins/Android/baseProjectTemplate.gradle`(신규 커스텀), `mainTemplate.gradle` Java 17, `gradleTemplate.properties`
- `.gitignore`에 `Builds/`가 있는지 확인. `*.apks`, `*.keystore`도 추가
- 대용량 SDK 파일: 이전 커밋 `2a4fc4d`에서 대용량 에셋 git 추적을 제거한 이력이 있다. Firebase `.unitypackage` 자체는 커밋하지 않고 임포트된 `Assets/Firebase/**`만 커밋한다 (기존 방식과 동일)

### Project Context Rules

이 스토리에 적용되는 `project-context.md` 규칙:

- **Not Unity 6.** 2022.3 = C# 9 / .NET Standard 2.1. `Awaitable` 없음 — 프로브는 UniTask(설치됨)나 Task 콜백을 쓴다
- **Removed (do not reintroduce):** 이 스토리는 제거 작업을 하지 않지만, 상향 과정에서 Input System·Addressables 패키지를 **재설치하지도** 않는다
- **JDK 11 today; may become 17 after E1-09 spike (D13)** — 이 스토리가 그 결정을 내리고 세 문서를 갱신한다
- **Test IDs forced in development builds** — 프로브의 AdMob 초기화는 유닛 ID를 요구하지 않는다(`MobileAds.Initialize`만). 광고 로드는 하지 않는다
- **`Assets/google-services.json` is not committed. If missing, boot enters `local` mode** — 프로브는 파일이 없을 때 Firebase 초기화 실패를 **에러가 아니라 라벨 텍스트**로 보고한다. CI 빌드에는 파일이 없으므로 빌드 자체는 성공해야 한다 (Firebase는 런타임에만 실패)
- **All text inside code is English**
- **Everything the project owns lives under `Assets/SoloHero/`**

### 테스트 기준

- 단위 테스트 없음 (스파이크). AC 4·5·6·7이 검증이다.
- 회귀: 없음 — 생존 스크립트 15개는 씬에 배치되지 않아 이 빌드에 영향이 없다. 단 **컴파일은 되어야** 한다. 76f1 + Firebase 13.17 + GMA 11.5 조합에서 `AdMobService.cs`, `SaveManager.cs`, `GameManager.cs`가 API 변경으로 컴파일 실패하면 **최소 수정**(deprecated API 교체)만 하고 Dev Record에 기록한다. 재설계는 E1-03/E1-07의 몫.

### References

- [Source: _bmad-output/planning-artifacts/gdds/gdd-solohero-2026-09-20/epics.md#E1 — E1-09]
- [Source: _bmad-output/planning-artifacts/gdds/gdd-solohero-2026-09-20/gdd.md#Technical Specifications › Platform-Specific Details]
- [Source: _bmad-output/planning-artifacts/architecture/arch-solohero-2026-09-20/game-architecture.md#Engine & Framework › Selected Engine, 외부 제약]
- [Source: _bmad-output/planning-artifacts/architecture/arch-solohero-2026-09-20/game-architecture.md#Architectural Decisions › D13, D8, D9, D1]
- [Source: _bmad-output/planning-artifacts/architecture/arch-solohero-2026-09-20/game-architecture.md#Cross-cutting Concerns › Configuration, Debug Tools]
- [Source: _bmad-output/planning-artifacts/architecture/arch-solohero-2026-09-20/game-architecture.md#Project Structure › Directory Structure, 정리 대상]
- [Source: _bmad-output/planning-artifacts/architecture/arch-solohero-2026-09-20/game-architecture.md#Development Environment › First Steps 1]
- [Source: _bmad-output/project-context.md#Technology Stack & Versions, Platform & Build Rules]
- [Source: Assets/Editor/BuildAutomator.cs, Jenkinsfile, .github/workflows/build.yml, ProjectSettings/ProjectSettings.asset, Assets/Plugins/Android/mainTemplate.gradle]
- [External: Google Play target API 요구 — https://support.google.com/googleplay/android-developer/answer/11926878]
- [External: Firebase Unity SDK 릴리스 노트 — https://firebase.google.com/support/release-notes/unity]
- [External: Firebase 16 KB 크래시 이슈 #1259 — https://github.com/firebase/firebase-unity-sdk/issues/1259]
- [External: Unity 2022.3.71f1 16 KB 패키징 실패 스레드 — https://discussions.unity.com/t/unity-2022-3-71f1-android-playstore-16-kb-page-size-not-working/1706445]
- [External: GameCI 이미지 태그 — https://hub.docker.com/r/unityci/editor/tags?name=2022.3.62f3]

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5) — 코드·설정 부분 (2026-09-20)

### Debug Log References

- 에디터 컴파일은 아직 미실행. API 존재는 DLL 문자열 검색으로 확인: `GoogleMobileAds.dll`에 `RaiseAdEventsOnUnityMainThread`·`getAdapterStatusMap`, `GoogleMobileAds.Core.dll`에 `InitializationState`, `Firebase.App.dll`에 `CheckAndFixDependenciesAsync`.

### Implementation Plan (코드 파트, 완료)

- **2026-09-20 결정 변경:** 76f1 상향은 Unity Hub에서 라이선스 오류로 차단됨 (2022.3.63f1+ = Extended LTS, Industry/Enterprise 전용). **62f3 고정.** 아키텍처 Engine 절·D13·CLAUDE.md·project-context 갱신. 타깃 API 36은 SDK Platform 36 설치로 충족.

- `ProjectSettings.asset` 직접 편집: `AndroidTargetSdkVersion 0→36`, `AndroidTargetArchitectures 1→3`(ARMv7+ARM64), `scriptingBackend {Android: 1}`(IL2CPP), `managedStrippingLevel {Android: 1}`(Low), `androidMaxAspectRatio 2.1→2.5`. **편차:** Aspect Ratio Mode는 `androidSupportedAspectRatio: 1`(Native Aspect Ratio) 그대로 둠 — Custom보다 강한 옵션(어떤 비율에서도 레터박스 없음)이라 AC 2의 목적을 충족. 2.5는 모드를 Custom으로 바꿀 때를 대비한 값
- `BuildAutomator` 재작성: `EditorBuildSettings` 기반 씬, `-customBuildPath`(GameCI) 우선, `SOLOHERO_DEV_BUILD`, 키스토어 env 주입, `SOLOHERO_JDK_PATH`/`SOLOHERO_GRADLE_PATH` → `EditorPrefs`(결정 B 대비), 실패 시 batchmode `Exit(1)`. `SpikeSceneSetup.EnsureBootScene()`를 빌드 전에 호출해 CI 새 체크아웃에서도 씬이 보장됨
- `SpikeSceneSetup`(에디터): 메뉴 `Tools > SoloHero > Spike > Create Boot Scene` — 카메라(ortho 7.5 = 480 px / PPU 32) + `BuildSpikeProbe` GO, `EditorBuildSettings`에 단독 등록. 씬 YAML을 손으로 쓰는 대신 코드로 생성해 GUID·직렬화 오류 위험 제거
- `BuildSpikeProbe`(런타임, TEMP): `OnGUI` 텍스트로 Unity 버전·기기·64-bit 여부·`CheckAndFixDependenciesAsync` 결과·`MobileAds.Initialize` 콜백(어댑터 상태)을 표시. UI 에셋 의존 0. AdMob 콜백은 lock 큐 → `Update`에서 플러시
- `build.yml`: `actions/cache@v4`, artifact 경로에 `Builds/*.aab` 추가 + `if-no-files-found: error` (기존엔 `build/Android/*.aab`만 봐서 커스텀 빌드 메서드 산출물을 못 찾는 상태였음)
- `tools/spike/Check16Kb.ps1`: bundletool universal APK → `.so` 추출 → `llvm-readelf -l` LOAD align 표 → `zipalign -c -P 16` → 결과 A/B-1/B-2/B-3 판정 및 exit code
- `.gitignore`: `*.apks`, `*.keystore`, `*.jks`

### 사람 손이 필요한 남은 작업 (순서대로)

1. **T1** 62f3 그대로 프로젝트 열기 → 콘솔 컴파일 오류 0 확인 → SDK Platform 36 존재 확인(`Preferences > External Tools`의 SDK 경로 `platforms\android-36`; 없으면 sdkmanager로 설치)
2. **T3** Firebase Unity SDK 13.17.0 `.unitypackage` 임포트(App·Auth·Database·Analytics) → GMA Unity 11.5.0 임포트 → `Assets > External Dependency Manager > Android Resolver > Force Resolve` → `Assets/Firebase/Editor/*_version-13.17.0_manifest.txt`, `Assets/GoogleMobileAds/GoogleMobileAds_version-11.5.0_manifest.txt`만 남았는지 확인
3. **T4** `Tools > SoloHero > Spike > Create Boot Scene` 1회 실행 → `Assets/SoloHero/Scenes/Boot.unity` 생성·등록 확인 → 새로 생긴 `.meta` 파일들과 함께 커밋
4. **T2 검증** `Project Settings > Player > Android`에서 IL2CPP / ARMv7+ARM64 / Target API 36 / Portrait / Stripping Low가 인스펙터에 그대로 보이는지 확인 (파일 편집이 에디터에 반영됐는지)
5. **T6** `Tools > Build > Android AAB` → `Builds/game.aab` → bundletool jar 다운로드 후 `pwsh tools/spike/Check16Kb.ps1 -Aab Builds/game.aab -Bundletool <jar> -Ndk <62f3 NDK> -BuildTools <SDK build-tools 35+>` 실행 → 출력 표를 아래 Completion Notes에 붙여넣기
6. **T7** 스크립트 exit code로 결정: 0=A / 2=B-1·B-2 / 3=B-3 (스토리 태스크의 대응 절차)
7. **T8** Jenkins 실행, GitHub Actions `workflow_dispatch` 실행 → run URL 기록
8. **T9** Play Console 내부 테스트 업로드(업로드 키 필요 시 `SOLOHERO_KEYSTORE_*` env로 빌드) → 경고 확인
9. **T10** 기기/에뮬레이터(16 KB) 설치 → 프로브 3줄 + `adb logcat -s Unity` 30초
10. **T11** 결과를 Completion Notes에 기록 → D13·CLAUDE.md·project-context 갱신 → 상태 `review`

### Completion Notes List

- 결정: **A / B** (하나 남기기)
- 16 KB 정렬 검사 표:

| 라이브러리 | LOAD Align | 결과 |
|---|---|---|
| libunity.so | | |
| libil2cpp.so | | |
| libmain.so | | |
| libFirebaseCppApp.so | | |
| libFirebaseCppAuth.so | | |
| libFirebaseCppDatabase.so | | |
| libFirebaseCppAnalytics.so | | |
| (기타) | | |

- `zipalign -P 16` 결과:
- Play Console 결과 (경고 문구 / 없음):
- 실기기 / 에뮬레이터 결과 (기기명, Android 버전, 16 KB 모드 여부, 프로브 3줄, logcat 요약):
- Jenkins 결과 / GitHub Actions run URL:
- 컴파일 최소 수정 목록 (있다면):

### File List

- `ProjectSettings/ProjectSettings.asset` (M — Target SDK 36, ARMv7+ARM64, IL2CPP, Stripping Low, maxAspectRatio 2.5)
- `Jenkinsfile`, `.github/workflows/activation.yml` (변경 없음 — 76f1로 바꿨다가 62f3으로 복원)
- `.github/workflows/build.yml` (M — cache v4, artifact 경로)
- `.gitignore` (M — *.apks, *.keystore, *.jks)
- `Assets/Editor/BuildAutomator.cs` → `Assets/SoloHero/Scripts/Editor/BuildAutomator.cs` (R + 재작성)
- `Assets/SoloHero/Scripts/Editor/SpikeSceneSetup.cs` (A)
- `Assets/SoloHero/Scripts/Game/Boot/BuildSpikeProbe.cs` (A, TEMP)
- `tools/spike/Check16Kb.ps1` (A)
- _(에디터 실행 후 생성 예정)_ `Assets/SoloHero/Scenes/Boot.unity`, 신규 폴더·스크립트 `.meta`, `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/EditorBuildSettings.asset`, `Assets/Plugins/Android/mainTemplate.gradle`(Resolver), `Assets/Firebase/**`, `Assets/GoogleMobileAds/**`, `Assets/ExternalDependencyManager/**`
