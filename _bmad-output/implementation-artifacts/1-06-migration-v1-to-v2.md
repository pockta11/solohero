---
baseline_commit: a7ede6b89d23f580f2dc8cb3e45804e10a6fbf9b
---
# Story 1.06: v1 → v2 이관

Status: done

<!-- Epic E1-06 · Must · 선행: 1-05 SaveDataV2. 저장 파이프라인(1-07)이 이 함수를 호출한다 -->

## Story

As a 1인 개발자,
I want v1 저장을 진행도·장비·강화에 쓴 골드를 유지한 v2 기록으로 바꾸고,
so that 예전 계정이 승산 강화 레벨을 복사당하지 않고 들어온다.

## Acceptance Criteria

1. **`PlayerDataV1` 필드 이름은 레거시 `PlayerData`와 같다.** Core의 순수 클래스이고, JsonUtility로 읽기 위한 `[Serializable]` public 필드만 있다.
2. **`MigrationV1ToV2.Convert`가 강화 레벨을 복사하지 않는다.** v2 강화는 0이고, 환급은 `baseCost * L * (L + 1) / 2`이다. v1 baseCost는 HP 100, ATK 150, DEF 150, SPD 200. 레벨은 0 이하면 0, 50을 넘으면 50까지만 환급한다.
3. **진행도는 v1의 챕터당 5스테이지 인덱스를 유지한다.** `highestStage = farmingStage = max(1, (chapter - 1) * 5 + stageNumber)`. `retreatMode`는 false.
4. **슬롯 이름은 Sword/Helm/Armor/Boots다.** `gachaPullCount`는 `pityCount`와 `totalPullCount`에 같이 복사한다. `lastQuitTimeUtc`는 그대로다. `stageKillsCurrent`는 버린다.
5. **장비 id는 카탈로그가 있으면 그 안의 id만 남긴다.** 카탈로그가 null이면 빈 값이 아닌 id는 유지한다. `ownedEquipmentCsv`는 `|`로 나눈다.
6. **`MigrationV1ToV2Tests`가 EditMode에서 녹색이다.** 레거시 `PlayerData.cs`는 `SaveManager`가 아직 참조하므로 삭제하지 않는다.

## Tasks / Subtasks

- [x] **T1. DTO와 변환** (AC 1–5)
  - [x] `PlayerDataV1.cs`, `MigrationV1ToV2.cs`
  - [x] `link.xml`에 `PlayerDataV1` 추가
- [x] **T2. 테스트** (AC 6)
  - [x] `MigrationV1ToV2Tests`
  - [x] EditMode 녹색

## Dev Notes

- v1 비용 상수는 `BalanceValues`에 넣지 않는다. 라이브 밸런스와 섞이면 환급액이 바뀐다. 출처는 `Scripts/Legacy/README.md`.
- 레거시 파일 삭제는 1-07에서 `SaveManager`를 옮길 때 한다.

### References

- [Source: epics.md#E1 — E1-06]
- [Source: game-architecture.md#v1 → v2 값 변환 규칙]
- [Source: Assets/SoloHero/Scripts/Legacy/README.md]

## Dev Agent Record

### Agent Model Used

Grok 4.7

### Debug Log References

- EditMode `Temp/story-1-06-editmode.xml`: result=Passed total=18 passed=18 failed=0.

### Completion Notes List

- v1 upgrade levels are refunded with the historical linear cost and stored as 0 in v2.
- Equipment ids are kept when no catalog is passed, and dropped when a catalog does not contain them.
- Legacy `PlayerData.cs` stays because `SaveManager` and `GameManager` still reference it.

### File List

- `Assets/SoloHero/Scripts/Core/Save/PlayerDataV1.cs`
- `Assets/SoloHero/Scripts/Core/Save/MigrationV1ToV2.cs`
- `Assets/SoloHero/Scripts/Tests/EditMode/MigrationV1ToV2Tests.cs`
- `Assets/link.xml`

### Change Log

- 2026-09-22: v1 to v2 migration with gold refund. EditMode 18/18.
