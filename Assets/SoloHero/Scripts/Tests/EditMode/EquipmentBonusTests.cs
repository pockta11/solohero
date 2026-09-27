using NUnit.Framework;
using SoloHero.Core.Config;
using SoloHero.Core.Equipment;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class EquipmentBonusTests
    {
        [Test]
        public void Resolve_EmptySlots_MultipliersAreOneAndBootsAreZero()
        {
            var balance = new BalanceValues();
            var data = SaveDataV2.CreateNew();

            EquipmentBonus bonus = EquipmentBonus.Resolve(balance, data);

            Assert.AreEqual(1d, bonus.SwordMult, 1e-9);
            Assert.AreEqual(1d, bonus.ArmorMult, 1e-9);
            Assert.AreEqual(1d, bonus.HelmMult, 1e-9);
            Assert.AreEqual(0d, bonus.BootsSpeedBonus, 1e-9);
            Assert.AreEqual(0d, bonus.BootsCritBonus, 1e-9);
        }

        [Test]
        public void Resolve_SwordLegendary_AtkMultIs2_40()
        {
            var balance = new BalanceValues();
            var data = SaveDataV2.CreateNew();
            data.equippedSword = "Equipment_Sword_Legendary";

            EquipmentBonus bonus = EquipmentBonus.Resolve(balance, data);

            Assert.AreEqual(2.40d, balance.SWORD_ATK_L, 1e-9);
            Assert.AreEqual(balance.SWORD_ATK_L, bonus.SwordMult, 1e-9);
            Assert.AreEqual(1d, bonus.ArmorMult, 1e-9);
            Assert.AreEqual(1d, bonus.HelmMult, 1e-9);
            Assert.AreEqual(0d, bonus.BootsSpeedBonus, 1e-9);
            Assert.AreEqual(0d, bonus.BootsCritBonus, 1e-9);
        }

        [Test]
        public void Resolve_BootsEpic_SpeedFractionIs0_15AndCritIs6()
        {
            var balance = new BalanceValues();
            var data = SaveDataV2.CreateNew();
            data.equippedBoots = "Equipment_Boots_Epic";

            EquipmentBonus bonus = EquipmentBonus.Resolve(balance, data);

            Assert.AreEqual(0.15d, balance.BOOTS_ATKSPD_E, 1e-9);
            Assert.AreEqual(6d, balance.BOOTS_CRIT_E, 1e-9);
            Assert.AreEqual(balance.BOOTS_ATKSPD_E, bonus.BootsSpeedBonus, 1e-9);
            Assert.AreEqual(balance.BOOTS_CRIT_E, bonus.BootsCritBonus, 1e-9);
            Assert.AreEqual(1d, bonus.SwordMult, 1e-9);
            Assert.AreEqual(1d, bonus.ArmorMult, 1e-9);
            Assert.AreEqual(1d, bonus.HelmMult, 1e-9);
        }

        [Test]
        public void Resolve_GarbageId_DoesNotThrow()
        {
            var balance = new BalanceValues();
            var data = SaveDataV2.CreateNew();
            data.equippedSword = "garbage";
            data.equippedHelm = null;
            data.equippedArmor = "Equipment_Helm_Legendary";
            data.equippedBoots = "Equipment_Boots_Mythic";

            EquipmentBonus bonus = EquipmentBonus.Resolve(balance, data);

            Assert.AreEqual(1d, bonus.SwordMult, 1e-9);
            Assert.AreEqual(1d, bonus.ArmorMult, 1e-9);
            Assert.AreEqual(1d, bonus.HelmMult, 1e-9);
            Assert.AreEqual(0d, bonus.BootsSpeedBonus, 1e-9);
            Assert.AreEqual(0d, bonus.BootsCritBonus, 1e-9);
        }
    }
}
