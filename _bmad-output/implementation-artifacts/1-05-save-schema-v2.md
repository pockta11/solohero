---
baseline_commit: 42c18ce505e9000694fd3d12966d45d5551ce073
---
# Story 1.05: 저장 스키마 v2

Status: review

<!-- Epic E1-05 · Must · 선행: 1-11 Core. 이관(1-06)·파이프라인(1-07)·부트(1-04)는 이 DTO를 쓴다 -->

## Story

As a 1인 개발자,
I want 신규 유저 저장이 GDD 상태 항목과 같은 이름의 v2 스키마로 만들어지게 하고,
so that 이후 이관·원격 저장·부트가 필드 이름을 다시 짓지 않는다.

## Acceptance Criteria

1. **`SaveDataV2`는 Core에 있다.** 네임스페이스 `SoloHero.Core.Save`. 필드는 GDD `저장 상태와 화면 대응` 표의 식별자와 같다. public 필드만 있고 프로퍼티로만 된 멤버는 없다.
2. **신규 유저 기본값이 고정된다.** `CreateNew()`는 `dataVersion` 2, `heroLevel` 1, `highestStage`/`farmingStage` 1, 재화·강화·프레스티지 0, 장착 문자열은 빈 문자열, 리스트는 빈 리스트(null 아님), `lastQuitTimeUtc` 0이다.
3. **타입은 규약을 따른다.** 재화·경험치·soul은 `double`, 시각은 `long`, 레벨·카운트는 `int`, 설정은 `bool`, 장비 id와 광고 리셋 날짜는 `string`.
4. **`Assets/link.xml`이 `SaveDataV2`를 preserve="all"로 보존한다.** `PlayerDataV1`은 1-06에서 추가한다.
5. **`SaveDataV2Tests.CreateNew_SetsNewUserDefaults`가 EditMode에서 녹색이다.** 테스트는 UnityEngine을 쓰지 않는다.

## Tasks / Subtasks

- [x] **T1. DTO** (AC 1, 2, 3)
  - [x] `Assets/SoloHero/Scripts/Core/Save/SaveDataV2.cs`
- [x] **T2. link.xml** (AC 4)
  - [x] `Assets/link.xml`
- [x] **T3. 테스트** (AC 5)
  - [x] `SaveDataV2Tests`
  - [x] EditMode 녹색

## Dev Notes

- 이관 함수, Firebase, PlayerPrefs, 디바운스, `PlayerState`는 만들지 않는다.
- 스킬 레벨 0은 미해금이다. 구매 후 1이 되는 규칙은 스킬 스토리의 몫이다.
- `chapterFirstClearFlags`는 빈 리스트로 시작한다. 챕터 수가 늘어날 때 채운다.
- 코드 안 텍스트는 영문.

### References

- [Source: epics.md#E1 — E1-05]
- [Source: gdd.md#저장 상태와 화면 대응]
- [Source: game-architecture.md#Data Persistence]
- [Source: project-context.md — Serialization]

## Dev Agent Record

### Agent Model Used

Grok 4.7

### Debug Log References

- EditMode `Temp/story-1-05-editmode.xml`: result=Passed total=11 passed=11 failed=0. Includes `CreateNew_SetsNewUserDefaults` and the existing `FormulasTests`.

### Completion Notes List

- `SaveDataV2.CreateNew()` is the new-user record. Stages and hero level start at 1. Skill levels start at 0 (locked). Lists are empty, not null.
- `Assets/link.xml` preserves `SaveDataV2` only. `PlayerDataV1` waits for the migration story.

### Change Log

- 2026-09-22: Save schema v2 and new-user defaults. EditMode 11/11.

### File List

- `Assets/SoloHero/Scripts/Core/Save/SaveDataV2.cs`
- `Assets/SoloHero/Scripts/Tests/EditMode/SaveDataV2Tests.cs`
- `Assets/link.xml`
- matching `.meta` files
