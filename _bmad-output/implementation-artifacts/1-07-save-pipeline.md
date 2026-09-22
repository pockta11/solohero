---
baseline_commit: 725ddc4e9c9bbacda524f88b1bce16618d15175b
---
# Story 1.07: 저장 파이프라인

Status: done

<!-- Epic E1-07 · Must · 선행: 1-05, 1-06. 부트(1-04)가 이 서비스를 호출한다. 트리거 7종의 호출 지점은 각 도메인 서비스 스토리에서 연결한다 -->

## Story

As a 1인 개발자,
I want 저장이 로컬에 먼저 쓰이고 원격 실패를 삼키며, 짧은 시간에 겹친 요청은 마지막 상태만 남기게 하고,
so that 강제 종료와 연속 강화가 기록을 지우거나 두 번 쓰지 않는다.

## Acceptance Criteria

1. **`SaveService`는 Core에 있다.** `RequestSave`는 250ms 디바운스 후 마지막 JSON만 저장한다. `FlushAsync`는 기다리지 않고 바로 저장한다. 저장 중에 들어온 요청은 그 저장이 끝난 뒤 한 번 더 저장한다.
2. **로컬이 원격보다 먼저다.** 원격 저장이 예외를 던져도 로컬 백업은 남고, 호출자는 예외를 받지 않는다.
3. **로드는 원격 v2, 없으면 v1 이관, 그것도 없으면 로컬, 그래도 없으면 신규다.** 원격 로드가 예외면 로컬 v2를 쓴다. `remote == null`이면 로컬 전용이다.
4. **Game 어댑터만 Unity와 Firebase를 안다.** `FirebaseSaveStore`는 `users/{uid}/v2`와 v1 노드, `LocalBackupStore`는 `player_data_v2` / 기존 `player_data_backup`, `NewtonsoftSaveSerializer`는 `MissingMemberHandling.Ignore`.
5. **`SaveServiceTests` 7개가 EditMode에서 녹색이다.** 레거시 `SaveManager`는 아직 지우지 않는다.

## Tasks / Subtasks

- [x] **T1. Core 큐** (AC 1, 2, 3)
  - [x] `ISaveStore`, `ISaveSerializer`, `SaveService`
- [x] **T2. Game 어댑터** (AC 4)
  - [x] `FirebaseSaveStore`, `LocalBackupStore`, `NewtonsoftSaveSerializer`
- [x] **T3. 테스트** (AC 5)
  - [x] `SaveServiceTests`
  - [x] EditMode 녹색

## Dev Notes

- 스테이지 클리어·가챠·장착·강화·스킬·오프라인 수령의 `RequestSave` 호출은 그 서비스가 생길 때 붙인다. 이 스토리는 큐와 로드 분기만 만든다.
- 부트에서 `Services.Register`로 연결하는 일은 1-04다.
- v1 로컬 키 `player_data_backup`은 덮어쓰지 않는다. v2는 `player_data_v2`다.

### References

- [Source: epics.md#E1 — E1-07]
- [Source: game-architecture.md#Data Persistence]
- [Source: Assets/SoloHero/Scripts/Legacy/SaveManager.cs]

## Dev Agent Record

### Agent Model Used

Grok 4.7

### Debug Log References

- EditMode `Temp/story-1-07-editmode.xml`: result=Passed total=25 passed=25 failed=0.

### Completion Notes List

- Overlapping `RequestSave` calls keep the last payload. A request that arrives during a save is written once more after it finishes.
- Remote failures stay in the log. The local v2 backup is written first.
- Missing remote v2 migrates v1, then falls back to local, then creates a new user.
- The seven gameplay save triggers are not wired yet. Legacy `SaveManager` remains until boot replaces it.

### File List

- `Assets/SoloHero/Scripts/Core/Save/ISaveStore.cs`
- `Assets/SoloHero/Scripts/Core/Save/ISaveSerializer.cs`
- `Assets/SoloHero/Scripts/Core/Save/SaveService.cs`
- `Assets/SoloHero/Scripts/Game/Infrastructure/FirebaseSaveStore.cs`
- `Assets/SoloHero/Scripts/Game/Infrastructure/LocalBackupStore.cs`
- `Assets/SoloHero/Scripts/Game/Infrastructure/NewtonsoftSaveSerializer.cs`
- `Assets/SoloHero/Scripts/Tests/EditMode/SaveServiceTests.cs`

### Change Log

- 2026-09-22: Save queue, local-first backup, v2 load with v1 migration fallback. EditMode 25/25.
