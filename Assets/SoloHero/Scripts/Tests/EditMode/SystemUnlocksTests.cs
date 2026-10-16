using NUnit.Framework;
using SoloHero.Core.Config;
using SoloHero.Core.Pets;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-178: the gate list reads the same stages the services enforce, and a first clear names what it opened.</summary>
    public sealed class SystemUnlocksTests
    {
        [Test]
        public void Gates_ReadTheServicesOwnUnlockStages()
        {
            var b = new BalanceValues();
            Assert.AreEqual(b.DUNGEON_UNLOCK_STAGE, SystemUnlocks.StageOf(b, SystemGate.Dungeon));
            Assert.AreEqual(b.TOWER_UNLOCK_STAGE, SystemUnlocks.StageOf(b, SystemGate.Tower));
            Assert.AreEqual(b.PET_UNLOCK_STAGE, SystemUnlocks.StageOf(b, SystemGate.PetSummon));
            Assert.AreEqual(b.MATERIAL_UNLOCK_STAGE, SystemUnlocks.StageOf(b, SystemGate.MaterialDungeon));
            Assert.AreEqual(b.RAID_UNLOCK_STAGE, SystemUnlocks.StageOf(b, SystemGate.Raid));

            var save = new SaveDataV2 { highestStage = b.PET_UNLOCK_STAGE };
            Assert.AreEqual(PetSummonService.IsUnlocked(b, save), SystemUnlocks.IsOpen(b, save, SystemGate.PetSummon));
            Assert.AreEqual(DungeonService.IsUnlocked(b, save), SystemUnlocks.IsOpen(b, save, SystemGate.Dungeon));
            Assert.AreEqual(TowerService.IsUnlocked(b, save), SystemUnlocks.IsOpen(b, save, SystemGate.Tower));
            Assert.AreEqual(RaidService.IsUnlocked(b, save), SystemUnlocks.IsOpen(b, save, SystemGate.Raid));
            Assert.AreEqual(DungeonService.IsOpen(b, save, DungeonKind.Material), SystemUnlocks.IsOpen(b, save, SystemGate.MaterialDungeon));

            save.highestStage = b.PET_UNLOCK_STAGE - 1;
            Assert.IsFalse(SystemUnlocks.IsOpen(b, save, SystemGate.PetSummon));
            foreach (SystemGate gate in SystemUnlocks.All)
                Assert.IsTrue(SystemUnlocks.NameKey(gate).StartsWith("unlock."), gate.ToString());
        }

        [Test]
        public void OpenedBy_NamesEveryGateOfTheFirstBoss_AndNothingForAPlainStage()
        {
            var b = new BalanceValues();
            var buffer = new SystemGate[SystemUnlocks.All.Length];
            int first = SystemUnlocks.OpenedBy(b, b.DUNGEON_UNLOCK_STAGE, buffer);
            Assert.GreaterOrEqual(first, 3, "the first boss opens the dungeon, the tower and the pet summon");
            Assert.AreEqual(SystemGate.Dungeon, buffer[0]);
            Assert.AreEqual(SystemGate.Tower, buffer[1]);
            Assert.AreEqual(SystemGate.PetSummon, buffer[2]);

            Assert.AreEqual(0, SystemUnlocks.OpenedBy(b, b.DUNGEON_UNLOCK_STAGE + 1, buffer));
            Assert.AreEqual(1, SystemUnlocks.OpenedBy(b, b.RAID_UNLOCK_STAGE, buffer));
            Assert.AreEqual(SystemGate.Raid, buffer[0]);
            Assert.AreEqual(0, SystemUnlocks.OpenedBy(b, b.RAID_UNLOCK_STAGE, new SystemGate[0]));
        }
    }
}
