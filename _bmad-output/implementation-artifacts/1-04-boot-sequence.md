---
baseline_commit: b3b73695e75347df036baad5a0358f2a4df451c0
---
# Story 1.04: 부트 시퀀스

Status: done

<!-- Epic E1-04 · Must · 선행: 1-11, 1-05, 1-06, 1-07. 로드 실패 UI(1-08)와 오프라인 팝업(E6)은 여기서 그리지 않는다 -->

## Story

As a 1인 개발자,
I want 앱이 인증, 로드, 이관, 오프라인 계산을 거친 뒤 전투 씬으로 들어가게 하고,
so that 어느 단계가 실패해도 프로세스가 멈추지 않는다.

## Acceptance Criteria

1. **`BootFlow`는 Core에서 순서를 고정한다.** 인증 → 저장 생성 → 로드(이관 포함) → 오프라인 계산. 인증 예외는 `local`. 로드 예외는 신규 유저와 `LoadFailed`.
2. **오프라인 계산은 GDD 규칙이다.** 첫 실행은 0. 60초 미만은 바로 지급. 60초 이상은 팝업용 금액만 계산하고 `lastQuitTimeUtc`를 움직이지 않는다. 음수 또는 상한 2배 초과는 0이고 시각을 지금으로 되돌린다.
3. **`BootSequence`가 서비스를 한 번 등록하고 `Game` 씬을 연다.** 씬이 없으면 부트에 남는다. 일시정지·종료 때 `FlushAsync`. 미수령 오프라인 팝업이 있으면 종료 시각을 덮어쓰지 않는다.
4. **인증 실패는 Firebase를 호출하지 않는 로컬 저장이다.** `AuthService`만 Firebase를 안다.
5. **`OfflineRewardTests`와 `BootFlowTests`가 EditMode에서 녹색이다.**

## Tasks / Subtasks

- [x] **T1. Core** (AC 1, 2)
  - [x] `OfflineReward`, `BootFlow`, `IAuthGateway`, `BootReport`
- [x] **T2. Game** (AC 3, 4)
  - [x] `AuthService`, `BootSequence`, Boot 씬 연결, `Game` 씬과 빌드 설정
- [x] **T3. 테스트** (AC 5)
  - [x] EditMode 테스트 작성
  - [x] EditMode 녹색

## Dev Notes

- 방어 UI와 오프라인 수령 버튼은 다음 스토리다. 부트는 `ShowPopup`과 `LoadFailed`만 남긴다.
- 레거시 `GameManager`는 씬에 없으므로 지우지 않는다.
- 60초 자동 저장 루프는 저장 트리거 7종에 없으므로 넣지 않는다.

### References

- [Source: epics.md#E1 — E1-04]
- [Source: gdd.md#오프라인 진행 규칙]
- [Source: game-architecture.md#게임 흐름]

## Dev Agent Record

### Agent Model Used

Grok 4.7

### Debug Log References

- EditMode `Temp/story-1-04-editmode.xml`: result=Passed total=34 passed=34 failed=0.

### Completion Notes List

- Auth failure enters local mode. Load failure starts a new user and sets `LoadFailed`.
- Offline gold under 60 seconds is granted immediately. Longer absences keep `lastQuitTimeUtc` until the popup is claimed.
- `Game` is an empty camera scene so boot can enter it. The defense UI is still 1-08.

### File List

- `Assets/SoloHero/Scripts/Core/Economy/OfflineReward.cs`
- `Assets/SoloHero/Scripts/Core/Boot/IAuthGateway.cs`
- `Assets/SoloHero/Scripts/Core/Boot/BootReport.cs`
- `Assets/SoloHero/Scripts/Core/Boot/BootFlow.cs`
- `Assets/SoloHero/Scripts/Game/Infrastructure/AuthService.cs`
- `Assets/SoloHero/Scripts/Game/Boot/BootSequence.cs`
- `Assets/SoloHero/Scenes/Boot.unity`
- `Assets/SoloHero/Scenes/Game.unity`
- `ProjectSettings/EditorBuildSettings.asset`
- `Assets/SoloHero/Scripts/Tests/EditMode/OfflineRewardTests.cs`
- `Assets/SoloHero/Scripts/Tests/EditMode/BootFlowTests.cs`

### Change Log

- 2026-09-22: Boot flow with local fallback, offline calc, and Game scene entry. EditMode 34/34.
