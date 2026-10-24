using System.Linq;
using NUnit.Framework;
using Project.Scripts.Gameplay.Field;
using UnityEngine;
using YG;

namespace Project.Scripts.System.Save.Editor
{
    public sealed class WorldSlotDefaultsTests
    {
        private const string SaveKey = "merge_td_world";
        private bool _hadLocalSave;
        private string _localSave;
        private WorldSaveData _originalWorld;
        private string _originalWorldJson;

        [SetUp]
        public void SetUp()
        {
            _hadLocalSave = PlayerPrefs.HasKey(SaveKey);
            _localSave = PlayerPrefs.GetString(SaveKey);
            _originalWorld = YG2.saves.world;
            _originalWorldJson = YG2.saves.worldJson;
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [TearDown]
        public void TearDown()
        {
            YG2.saves.world = _originalWorld;
            YG2.saves.worldJson = _originalWorldJson;
            if (_hadLocalSave)
                PlayerPrefs.SetString(SaveKey, _localSave);
            else
                PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void NewWorld_OpensTopTwoRowsAndLocksBottomRow()
        {
            AssertStartingSlots(new WorldService(new WorldSaveService()));
        }

        [Test]
        public void Reset_RestoresStartingSlotsAndPersistsThem()
        {
            var data = ExistingWorld();
            foreach (var slot in data.towerSlots)
                slot.unlockedSlot = WorldTowerSlotSaveData.Unlocked;
            YG2.saves.world = data;
            var world = new WorldService(new WorldSaveService());

            world.Reset();

            AssertStartingSlots(world);
            var stored = JsonUtility.FromJson<WorldSaveData>(PlayerPrefs.GetString(SaveKey));
            Assert.That(stored.towerSlots.Select(s => s.unlockedSlot),
                Is.EqualTo(world.TowerSlots.Select(s => s.unlockedSlot)));
            AssertStartingSlots(new WorldService(new WorldSaveService()));
        }

        [Test]
        public void ExistingWorld_PreservesSavedLocksIncludingUnlockedBottomSlot()
        {
            var data = ExistingWorld();
            data.towerSlots.Single(s => s.slotId == "11").unlockedSlot = WorldTowerSlotSaveData.Unlocked;
            data.towerSlots.Single(s => s.slotId == "34").unlockedSlot = WorldTowerSlotSaveData.Unlocked;
            var expected = data.towerSlots.Select(s => s.unlockedSlot).ToArray();
            YG2.saves.world = data;

            var world = new WorldService(new WorldSaveService());

            Assert.That(world.TowerSlots.Select(s => s.unlockedSlot), Is.EqualTo(expected));
            Assert.That(new WorldService(new WorldSaveService()).TowerSlots.Select(s => s.unlockedSlot),
                Is.EqualTo(expected));
        }

        [Test]
        public void LegacyWorldWithoutSlotStates_KeepsExistingMigrationBehavior()
        {
            var data = ExistingWorld();
            data.version = 1;
            data.towerSlots.Clear();
            YG2.saves.world = data;

            var world = new WorldService(new WorldSaveService());

            Assert.That(world.TowerSlots.Where(s => s.unlockedSlot == WorldTowerSlotSaveData.Unlocked)
                .Select(s => s.slotId), Is.EqualTo(new[] { "11" }));
        }

        private static WorldSaveData ExistingWorld()
        {
            var data = new WorldSaveData { version = 2, gold = 250, maxBaseHealth = 10, maxEnergy = 12 };
            for (var i = 0; i < TowerSlotGrid.SlotCount; i++)
                data.towerSlots.Add(new WorldTowerSlotSaveData(TowerSlotGrid.GetSlotId(i),
                    WorldTowerSlotSaveData.Locked));
            return data;
        }

        private static void AssertStartingSlots(WorldService world)
        {
            Assert.That(world.TowerSlots.Count, Is.EqualTo(15));
            Assert.That(world.TowerSlots.Where(s => s.unlockedSlot == WorldTowerSlotSaveData.Unlocked)
                .Select(s => s.slotId), Is.EqualTo(new[]
                {
                    "11", "12", "13", "14", "15", "21", "22", "23", "24", "25"
                }));
            Assert.That(world.TowerSlots.Where(s => s.unlockedSlot == WorldTowerSlotSaveData.Locked)
                .Select(s => s.slotId), Is.EqualTo(new[] { "31", "32", "33", "34", "35" }));
        }
    }
}
