# Legacy (quarantine) — TEMPORARY

Scripts inherited from the 3D prototype (see `game-architecture.md` › As-Is). They compile but do
**not** follow the architecture conventions (no `Result`, static singletons, `long` currency, v1 save
schema, etc.). They exist only as porting references for the stories listed below.

Rules

- New code under `Scripts/Core` or `Scripts/Game` must **never** reference a type from this folder.
  Doing so would create a cycle once the assemblies (asmdef) are introduced.
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
