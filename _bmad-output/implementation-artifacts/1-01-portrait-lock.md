---
baseline_commit: ac398a7a1e5b79ed6cb4c0f1574c1d8f51709e1b
---
# Story 1.01: Portrait 고정 (1080×1920)

Status: done

<!-- Epic E1-01 · Must · 선행 없음. 픽셀 퍼펙트(1-02)와 부트(1-04)보다 먼저 화면 크기와 회전을 고정한다 -->

## Story

As a 1인 개발자,
I want Android 플레이어가 가로 1920×1080이 아니라 세로 1080×1920으로 고정되게 하고,
so that 이후 레이아웃·픽셀 퍼펙트·부트가 같은 화면 크기를 전제로 한다.

## Acceptance Criteria

1. **방향은 Portrait 고정이다.** `defaultScreenOrientation`은 Portrait(`0`)이고, 가로 autorotate 두 값은 0이다. 커스텀 `AndroidManifest.xml`의 `UnityPlayerActivity`에 `android:screenOrientation="portrait"`가 있다.
2. **기준 해상도는 1080×1920이다.** `defaultScreenWidth/Height`와 `androidDefaultWindowWidth/Height`가 1080 / 1920이다. 1920×1080 가로 기본값을 남기지 않는다.
3. **회전이 액티비티를 재생성하지 않는다.** activity에 `android:configChanges`가 orientation·screenSize·smallestScreenSize를 포함한다.
4. **기존 AAB 빌드가 성공하고**, 생성된 매니페스트의 런처 액티비티가 `portrait`이다. PS4 등 다른 플랫폼의 1920 값은 건드리지 않는다.

## Tasks / Subtasks

- [x] **T1. PlayerSettings** (AC 1, 2)
  - [x] `ProjectSettings/ProjectSettings.asset`: width 1080, height 1920 (default + android window)
  - [x] orientation은 Portrait. landscape autorotate는 0 유지
- [x] **T2. 매니페스트** (AC 1, 3)
  - [x] `Assets/Plugins/Android/AndroidManifest.xml` activity에 `screenOrientation="portrait"`, `configChanges`, `resizeableActivity="false"`, `exported="true"`
- [x] **T3. 빌드 확인** (AC 4)
  - [x] `BuildAutomator.Build` 성공
  - [x] 생성 매니페스트에서 런처 액티비티 `screenOrientation`이 portrait

## Dev Notes

- Unity `UIOrientation.Portrait = 0`. 현재 값은 이미 0이었다. 깨져 있던 것은 해상도(1920×1080)와, 커스텀 매니페스트가 `screenOrientation`을 선언하지 않던 점이다.
- `useOSAutorotation`은 Auto Rotation일 때만 쓰인다. Portrait 고정에서는 바꾸지 않는다.
- 픽셀 퍼펙트 카메라(270×480)는 1-02. 이 스토리에서 카메라·CanvasScaler를 만들지 않는다.
- 코드 안 텍스트는 영문. 매니페스트 속성 값만 바꾼다.

### References

- [Source: epics.md#E1 — E1-01]
- [Source: gdd.md — 화면 방향 Portrait 고정, 기준 해상도 1080×1920]
- [Source: game-architecture.md — 화면 Portrait 1080×1920]
- [Source: project-context.md — Android only. Portrait locked]

## Dev Agent Record

### Agent Model Used

Grok 4.7

### Debug Log References

- `BuildAutomator.Build`: result=Succeeded errors=0 warnings=1 time=00:00:37. Packaged launcher manifest has `android:screenOrientation="portrait"`.

### Completion Notes List

- 방향은 원래 Portrait(`0`)였다. 기준 해상도만 1920×1080이라 1080×1920으로 바꿨다.
- 커스텀 매니페스트에 portrait, configChanges, resizeableActivity=false, exported=true를 넣었고, 패키징된 매니페스트에서 portrait를 확인했다.
- 실기기에서 돌려 보는 검증은 없다. 빌드 산출 매니페스트가 세로 고정을 담고 있다.

### File List

- `ProjectSettings/ProjectSettings.asset`
- `Assets/Plugins/Android/AndroidManifest.xml`

### Change Log

- 2026-09-22: Portrait 1080×1920 lock. AAB packaged manifest is portrait.
