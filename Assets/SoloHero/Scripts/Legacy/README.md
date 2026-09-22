# Legacy (quarantine) — TEMPORARY

Scripts inherited from the 3D prototype (see `game-architecture.md` › As-Is). They compile but do
**not** follow the architecture conventions (no `Result`, static singletons, `long` currency, v1 save
schema, etc.). They exist only as porting references for the stories listed below.

Rules

- New code under `Scripts/Core` or `Scripts/Game` must **never** reference a type from this folder.
  Doing so would create a cycle once the assemblies (asmdef) are introduced.
- The project rule "all text inside code is English" is **suspended inside this folder only** -
  these files still carry Korean comments and strings from the prototype. Ported code must be English.
- Do not "fix" these files to match conventions. Port the behaviour into the new class, then delete
  the legacy file in the same story.
- When the last file is gone, delete this folder and its `.meta`.

| File | Ported into | Story | Notes |
|---|---|---|---|
| `PlayerData.cs` | `PlayerDataV1` (read-only v1 DTO for migration) | E1-05 / E1-06 | Keep field names exactly; migration maps them to `SaveDataV2` |
| `EquipmentData.cs` | `EquipmentData` (SO) redefined with grade multipliers | E4-04 | 16 assets in `Data/Equipment/` reference this script by GUID — move, never recreate |
| `GameManager.cs` | `Boot/BootSequence` | E1-04 | Reference for the boot order: Firebase → anonymous auth → load → offline reward → scene |
| `SaveManager.cs` | `Infrastructure/SaveService` + `FirebaseSaveStore` + `LocalBackupStore` | E1-07 | Debounce (250 ms, last-write-wins), local backup first, flush on pause/quit |
| `OfflineRewardSystem.cs` | `Core/Economy/OfflineRewardService` | E6-03 | Elapsed-seconds calc; cap and tamper rules move to `BalanceValues` |
| `AdMobService.cs` | `Infrastructure/AdService` | E6-09 | Rewarded ad load/show flow; callbacks must go through `MainThreadDispatcher` |
| `MainThreadDispatcher.cs` | `Infrastructure/MainThreadDispatcher` | E1-04 | Architecture keeps this as-is (rename namespace only) |
| `SafeAreaAdjuster.cs` | `UI/Common/SafeAreaAdjuster` | E7-02 | Architecture keeps this as-is |

## v1 constants needed by the save migration (E1-06)

`UpgradeService.cs` was deleted in E1-03 (its formula is obsolete), but the migration refunds the
gold a v1 player spent on upgrades, so the **v1** numbers are recorded here. Recovered from
`git show f16e8ce:Assets/Scripts/Systems/UpgradeService.cs`.

| Lane | v1 baseCost | v1 per-level gain |
|---|---|---|
| HP | 100 | +50 (additive) |
| ATK | 150 | +5 |
| DEF | 150 | +5 |
| SPD (= move speed in v1, redefined as attack speed in v2) | 200 | +0.2 |

- v1 cost of the step from level `i` to `i+1`: `baseCost * (i + 1)`; max level 50.
- Refund for a saved level `L`: `sum(baseCost * (i + 1) for i in 0..L-1)` = `baseCost * L * (L + 1) / 2`.
- v2 levels start at 0; the gold goes back to the player (ADR-4).
