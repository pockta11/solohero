# Deferred Work

## Deferred from: code review of 1-09-build-pipeline-portrait (2026-09-22)

- ~~`BuildAutomator.ApplyToolchainOverrides`가 전역 `EditorPrefs`(JdkUseEmbedded/GradleUseEmbedded)를 영구 변경하고 복원하지 않음~~ — **해결 2026-10-08 (D-138):** 빌드 직전에 적용하고 빌드 뒤 `ToolchainPrefs.Restore()`로 원래 값(키가 없던 경우 삭제)을 돌린다. 버전·서명 설정도 빌드 뒤 원복
- `.github/scripts/unity-build.sh`가 `UNITY_PASSWORD`를 argv로 전달(`/proc/*/cmdline` 노출, 컨테이너 내부 한정) — Unity.Licensing.Client에 stdin/파일 입력 옵션이 문서화되어 있지 않음. 옵션이 생기면 교체. **2026-10-08 재검토: 유지** — 일회용 컨테이너 안에서만 보이고 같은 컨테이너에 다른 사용자·프로세스가 없다
- ~~`tools/spike/Check16Kb.ps1`·`tools/android/Emu.ps1` 기본 경로가 `C:\Users\user\android-sdk-tools` 고정~~ — **해결 2026-10-08:** `Check16Kb.ps1`·`Emu.ps1`·`Play.ps1`·`Qa.ps1`이 `SOLOHERO_ANDROID_TOOLS` 환경변수를 먼저 보고, 없으면 기존 경로

## Deferred from: code review of 1-03-cleanup-3d-assets (2026-09-22)

- ~~AdMob App ID가 두 곳에 선언됨~~ — **해결 2026-10-08 (D-138):** 메인 매니페스트의 레거시 항목 삭제. `GoogleMobileAdsSettings.asset` → `GoogleMobileAdsPlugin.androidlib` 한 곳만
- ~~`Assets/Fonts/`는 git-ignore인데 `Scripts/Editor/FontSetupWizard.cs`는 추적됨~~ — **해결 2026-10-08:** 게임은 TMP를 쓰지 않고(uGUI + `Art/Fonts/SoloHeroJua.ttf`) 마법사는 어디서도 쓰이지 않아 삭제

