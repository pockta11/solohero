using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Equipment;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class EquipServiceTests
    {
        private sealed class FakeSaveRequester : ISaveRequester
        {
            public int RequestCount;

            public void RequestSave() => RequestCount++;
        }

        [Test]
        public void TryEquip_NullId_FailsLockedWithoutMutation()
        {
            var data = SaveDataV2.CreateNew();
            data.equippedSword = "keep";
            var save = new FakeSaveRequester();
            var service = new EquipService(save);

            Result result = service.TryEquip(data, EquipmentSlot.Sword, null);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.Locked, result.Reason);
            Assert.AreEqual("keep", data.equippedSword);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void TryEquip_EmptyId_FailsLockedWithoutMutation()
        {
            var data = SaveDataV2.CreateNew();
            data.equippedHelm = "keep";
            var save = new FakeSaveRequester();
            var service = new EquipService(save);

            Result result = service.TryEquip(data, EquipmentSlot.Helm, "");

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.Locked, result.Reason);
            Assert.AreEqual("keep", data.equippedHelm);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void TryEquip_NotOwned_FailsLockedWithoutMutation()
        {
            var data = SaveDataV2.CreateNew();
            data.ownedEquipment.Add("Iron_Sword");
            data.equippedSword = "Iron_Sword";
            var save = new FakeSaveRequester();
            var service = new EquipService(save);

            Result result = service.TryEquip(data, EquipmentSlot.Sword, "Steel_Sword");

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.Locked, result.Reason);
            Assert.AreEqual("Iron_Sword", data.equippedSword);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void TryEquip_Owned_SetsSlotAndRequestsSave()
        {
            var data = SaveDataV2.CreateNew();
            data.ownedEquipment.Add("Iron_Sword");
            var save = new FakeSaveRequester();
            var service = new EquipService(save);

            Result result = service.TryEquip(data, EquipmentSlot.Sword, "Iron_Sword");

            Assert.IsTrue(result.Ok);
            Assert.AreEqual("Iron_Sword", data.equippedSword);
            Assert.AreEqual(1, save.RequestCount);
        }

        [Test]
        public void TryEquip_AllSlots_SetMatchingFieldOnly()
        {
            var data = SaveDataV2.CreateNew();
            data.ownedEquipment.Add("A_Sword");
            data.ownedEquipment.Add("B_Helm");
            data.ownedEquipment.Add("C_Armor");
            data.ownedEquipment.Add("D_Boots");
            var save = new FakeSaveRequester();
            var service = new EquipService(save);

            Assert.IsTrue(service.TryEquip(data, EquipmentSlot.Sword, "A_Sword").Ok);
            Assert.IsTrue(service.TryEquip(data, EquipmentSlot.Helm, "B_Helm").Ok);
            Assert.IsTrue(service.TryEquip(data, EquipmentSlot.Armor, "C_Armor").Ok);
            Assert.IsTrue(service.TryEquip(data, EquipmentSlot.Boots, "D_Boots").Ok);

            Assert.AreEqual("A_Sword", data.equippedSword);
            Assert.AreEqual("B_Helm", data.equippedHelm);
            Assert.AreEqual("C_Armor", data.equippedArmor);
            Assert.AreEqual("D_Boots", data.equippedBoots);
            Assert.AreEqual(4, save.RequestCount);
        }

        [Test]
        public void TryEquip_Accessories_SetTheirOwnFields()
        {
            // D-109: gloves, necklace, ring and earring each have a save field of their own.
            var data = SaveDataV2.CreateNew();
            var service = new EquipService(new FakeSaveRequester());
            EquipmentSlot[] slots = { EquipmentSlot.Gloves, EquipmentSlot.Necklace, EquipmentSlot.Ring, EquipmentSlot.Earring };
            foreach (EquipmentSlot slot in slots)
            {
                string id = GachaCatalog.IdOf(slot, Grade.Rare);
                data.ownedEquipment.Add(id);
                Assert.IsTrue(service.TryEquip(data, slot, id).Ok);
                Assert.AreEqual(id, EquippedSlots.Get(data, slot));
            }

            Assert.AreEqual("Equipment_Gloves_Rare", data.equippedGloves);
            Assert.AreEqual("Equipment_Necklace_Rare", data.equippedNecklace);
            Assert.AreEqual("Equipment_Ring_Rare", data.equippedRing);
            Assert.AreEqual("Equipment_Earring_Rare", data.equippedEarring);
            Assert.AreEqual("", data.equippedSword);

            Assert.IsTrue(service.TryUnequip(data, EquipmentSlot.Ring).Ok);
            Assert.AreEqual("", data.equippedRing);
        }

        [Test]
        public void TryEquip_AlreadyEquipped_SuccessWithoutRequestSave()
        {
            var data = SaveDataV2.CreateNew();
            data.ownedEquipment.Add("Iron_Armor");
            data.equippedArmor = "Iron_Armor";
            var save = new FakeSaveRequester();
            var service = new EquipService(save);

            Result result = service.TryEquip(data, EquipmentSlot.Armor, "Iron_Armor");

            Assert.IsTrue(result.Ok);
            Assert.AreEqual("Iron_Armor", data.equippedArmor);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void TryUnequip_ClearsSlotAndRequestsSave()
        {
            var data = SaveDataV2.CreateNew();
            data.equippedBoots = "Iron_Boots";
            data.equippedSword = "Iron_Sword";
            var save = new FakeSaveRequester();
            var service = new EquipService(save);

            Result result = service.TryUnequip(data, EquipmentSlot.Boots);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual("", data.equippedBoots);
            Assert.AreEqual("Iron_Sword", data.equippedSword);
            Assert.AreEqual(1, save.RequestCount);
        }

        [Test]
        public void TryUnequip_AlreadyEmpty_StillRequestsSave()
        {
            var data = SaveDataV2.CreateNew();
            data.equippedHelm = "";
            var save = new FakeSaveRequester();
            var service = new EquipService(save);

            Result result = service.TryUnequip(data, EquipmentSlot.Helm);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual("", data.equippedHelm);
            Assert.AreEqual(1, save.RequestCount);
        }
    }
}
