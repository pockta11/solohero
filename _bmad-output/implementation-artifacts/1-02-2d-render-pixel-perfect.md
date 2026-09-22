---
baseline_commit: 656515ed71caae671ccbcf30424e762dce41228e
---
# Story 1.02: 2D 렌더와 픽셀 퍼펙트

Status: review

<!-- Epic E1-02 · Must · URP Renderer2D는 이미 연결되어 있다. 이 스토리는 Boot 카메라의 Pixel Perfect 설정이다 -->

## Story

As a 1인 개발자,
I want 32×32 스프라이트가 270×480 기준의 정수 배율로만 그려지게 하고,
so that 1080×1920에서는 4배, 더 긴 폰에서는 세로로 더 많은 월드를 보여주고 늘리지 않는다.

## Acceptance Criteria

1. **렌더 경로는 Renderer2D다.** `UniversalRP.asset`의 기본 렌더러가 `Renderer2D`이다. 3D 렌더러로 되돌리지 않는다.
2. **Boot `Main Camera`에 Pixel Perfect Camera가 있다.** `assetsPPU` 32, 기준 270×480, `upscaleRT` true, `cropFrameX/Y` false, `stretchFill` false. 직교 크기는 7.5(480/32/2)를 유지한다.
3. **카메라 MSAA·HDR은 꺼 둔다.** 픽셀 경계를 흐리지 않기 위해서다. URP 에셋의 다른 품질 값은 바꾸지 않는다.
4. **Unity가 씬을 열었을 때 Missing Script가 없다.** 패키지 `com.unity.2d.pixel-perfect` 5.1.0의 스크립트 guid를 쓴다.

## Tasks / Subtasks

- [x] **T1. 렌더러 확인** (AC 1)
  - [x] `Assets/Settings/Renderer2D.asset` guid가 `UniversalRP` 기본 렌더러와 같다
- [x] **T2. 카메라** (AC 2, 3)
  - [x] `Assets/SoloHero/Scenes/Boot.unity` Main Camera에 Pixel Perfect Camera
  - [x] PPU 32, ref 270×480, upscaleRT on, crop/stretch off, ortho 7.5, HDR/MSAA off
- [x] **T3. 임포트 확인** (AC 4)
  - [x] batchmode로 프로젝트를 열어 Missing Script / CS 오류가 없는지 본다

## Dev Notes

- 더 긴 폰이 세로 월드를 더 보게 하려면 `cropFrameY`를 켜면 안 된다. `stretchFill`은 늘리므로 끈다.
- `upscaleRT`가 켜져 있으면 `pixelSnapping`은 적용되지 않는다. 끈다.
- 스프라이트 임포트 프리셋·아틀라스는 이 스토리가 아니다.
- Game 씬은 아직 없다. 카메라는 Boot에만 둔다.

### References

- [Source: epics.md#E1 — E1-02]
- [Source: gdd.md — PIXEL_REF_RESOLUTION 270×480, 정수 4배]
- [Source: project-context.md — taller phones show more vertical world, never stretch]
- [Source: game-architecture.md — D12 Pixel Perfect Camera 270×480]

## Dev Agent Record

### Agent Model Used

Grok 4.7

### Debug Log References

- Unity batchmode import exited successfully. Log has no `error CS` and no Missing Script.

### Completion Notes List

- URP 기본 렌더러는 이미 `Renderer2D`였다. Boot Main Camera에 Pixel Perfect Camera를 붙였다.
- PPU 32, 기준 270×480, upscaleRT on, crop/stretch off. 직교 크기 7.5. 카메라 HDR·MSAA off.
- 더 긴 화면은 `cropFrameY`가 꺼져 있어 세로 월드를 더 본다.

### File List

- `Assets/SoloHero/Scenes/Boot.unity`

### Change Log

- 2026-09-22: Boot camera Pixel Perfect 270×480, PPU 32. Import clean.
