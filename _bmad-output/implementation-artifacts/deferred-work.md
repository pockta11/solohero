# Deferred Work

## Deferred from: code review of 1-09-build-pipeline-portrait (2026-09-22)

- `BuildAutomator.ApplyToolchainOverrides`가 전역 `EditorPrefs`(JdkUseEmbedded/GradleUseEmbedded)를 영구 변경하고 복원하지 않음 — 결정 A(JDK 11)에서는 미사용 경로. JDK 17 전환이 실제로 필요해질 때 빌드 후 복원 로직과 함께 처리
- `.github/scripts/unity-build.sh`가 `UNITY_PASSWORD`를 argv로 전달(`/proc/*/cmdline` 노출, 컨테이너 내부 한정) — Unity.Licensing.Client에 stdin/파일 입력 옵션이 문서화되어 있지 않음. 옵션이 생기면 교체
- `tools/spike/Check16Kb.ps1`·`tools/android/Emu.ps1` 기본 경로가 `C:\Users\user\android-sdk-tools` 고정 — 파라미터로 덮어쓰기 가능. 다른 PC에서 쓰게 되면 `SOLOHERO_ANDROID_TOOLS` 환경변수로 전환
