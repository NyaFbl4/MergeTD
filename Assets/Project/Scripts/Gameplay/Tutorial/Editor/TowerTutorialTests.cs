using NUnit.Framework;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.System.Save;
using UnityEngine;
using YG;

namespace Project.Scripts.Gameplay.Tutorial.Editor
{
    public sealed class TowerTutorialTests
    {
        private const string SaveKey = "merge_td_world";
        private bool _hadLocal;
        private string _local;
        private WorldSaveData _originalWorld;
        private string _originalJson;

        [SetUp]
        public void SetUp()
        {
            _hadLocal = PlayerPrefs.HasKey(SaveKey);
            _local = PlayerPrefs.GetString(SaveKey);
            _originalWorld = YG2.saves.world;
            _originalJson = YG2.saves.worldJson;
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [TearDown]
        public void TearDown()
        {
            YG2.saves.world = _originalWorld;
            YG2.saves.worldJson = _originalJson;
            if (_hadLocal) PlayerPrefs.SetString(SaveKey, _local);
            else PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void NewWorld_StartsTutorialWithEnoughGoldForThreePurchases()
        {
            var world = new WorldService(new WorldSaveService());
            Assert.That(world.TowerTutorial.step, Is.EqualTo(TowerTutorialStep.BuyFirst));
            Assert.That(world.Gold, Is.GreaterThanOrEqualTo(TowerEconomy.CombatPurchaseCost * 3));
        }

        [Test]
        public void MissingTutorialInLegacySave_CompletesWithoutChangingArmyOrGold()
        {
            YG2.saves.world = new WorldSaveData { gold = 875, maxBaseHealth = 10, maxEnergy = 12 };
            YG2.saves.world.towers.Add(new WorldTowerSaveData("11", 3, ETowerType.Combat, 800));
            var world = new WorldService(new WorldSaveService());
            Assert.That(world.TowerTutorial.step, Is.EqualTo(TowerTutorialStep.Completed));
            Assert.That(world.Gold, Is.EqualTo(875));
            Assert.That(world.Towers.Count, Is.EqualTo(1));
            Assert.That(world.Towers[0].towerLevel, Is.EqualTo(3));
        }

        [TestCase(TowerTutorialStep.BuySecond)]
        [TestCase(TowerTutorialStep.BuyThird)]
        [TestCase(TowerTutorialStep.Merge)]
        [TestCase(TowerTutorialStep.StartWave)]
        [TestCase(TowerTutorialStep.Completed)]
        public void Progress_ReloadsFromLocalBackup(TowerTutorialStep step)
        {
            var world = new WorldService(new WorldSaveService());
            world.SaveTowerTutorial(step, 2);
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            var restored = new WorldService(new WorldSaveService());
            Assert.That(restored.TowerTutorial.step, Is.EqualTo(step));
            Assert.That(restored.TowerTutorial.towerLevel, Is.EqualTo(2));
        }

        [Test]
        public void Reset_ReenablesTutorial()
        {
            var world = new WorldService(new WorldSaveService());
            world.SaveTowerTutorial(TowerTutorialStep.Completed, 3);
            world.Reset();
            Assert.That(world.TowerTutorial.step, Is.EqualTo(TowerTutorialStep.BuyFirst));
            Assert.That(world.TowerTutorial.towerLevel, Is.EqualTo(1));
        }

        [Test]
        public void ArmySnapshots_ResumePurchasesAndAdvanceOnlyAfterMerge()
        {
            var progress = new TowerTutorialSaveData();
            var towers = new global::System.Collections.Generic.List<WorldTowerSaveData>();
            Assert.That(TowerTutorialRules.Reconcile(progress, towers), Is.EqualTo(TowerTutorialStep.BuyFirst));
            towers.Add(new WorldTowerSaveData("11", 1, ETowerType.Combat));
            Assert.That(TowerTutorialRules.Reconcile(progress, towers), Is.EqualTo(TowerTutorialStep.BuySecond));
            towers.Add(new WorldTowerSaveData("12", 1, ETowerType.Generator));
            Assert.That(TowerTutorialRules.Reconcile(progress, towers), Is.EqualTo(TowerTutorialStep.BuySecond));
            towers[1].towerType = ETowerType.Combat;
            Assert.That(TowerTutorialRules.Reconcile(progress, towers), Is.EqualTo(TowerTutorialStep.BuyThird));
            towers.Add(new WorldTowerSaveData("13", 1, ETowerType.Combat));
            Assert.That(TowerTutorialRules.Reconcile(progress, towers), Is.EqualTo(TowerTutorialStep.Merge));
            progress.step = TowerTutorialStep.Merge;
            towers.RemoveAt(0);
            towers[0].towerLevel = 2;
            Assert.That(TowerTutorialRules.Reconcile(progress, towers), Is.EqualTo(TowerTutorialStep.StartWave));
            Assert.That(towers.Count, Is.EqualTo(2));
            Assert.That(towers[1].towerLevel, Is.EqualTo(1));
        }

        [TestCase(TowerTutorialStep.BuyFirst)]
        [TestCase(TowerTutorialStep.BuySecond)]
        [TestCase(TowerTutorialStep.BuyThird)]
        public void PurchaseSteps_AllowOnlyMatchingCombatPurchase(TowerTutorialStep step)
        {
            var input = new TowerTutorialInput();
            input.Set(step, 1, "11", "12");
            Assert.That(input.AllowsPurchase(ETowerType.Combat, 1), Is.True);
            Assert.That(input.AllowsPurchase(ETowerType.Combat, 2), Is.False);
            Assert.That(input.AllowsPurchase(ETowerType.Generator, 1), Is.False);
            Assert.That(input.CanStartWave, Is.False);
            Assert.That(input.AllowsDrag("11"), Is.False);
        }

        [Test]
        public void Merge_AllowsBothDirectionsButRejectsSaleAndOtherSlots()
        {
            var input = new TowerTutorialInput();
            input.Set(TowerTutorialStep.Merge, 1, "11", "12");
            Assert.That(input.AllowsDrag("11"), Is.True);
            Assert.That(input.AllowsDrag("12"), Is.True);
            Assert.That(input.AllowsDrag("13"), Is.False);
            Assert.That(input.AllowsDrop("11", "12"), Is.True);
            Assert.That(input.AllowsDrop("12", "11"), Is.True);
            Assert.That(input.AllowsDrop("11", "13"), Is.False);
            Assert.That(input.AllowsDrop("11", "11"), Is.False);
            Assert.That(input.AllowsPurchase(ETowerType.Combat, 1), Is.False);
            Assert.That(input.CanStartWave, Is.False);
        }

        [Test]
        public void StartStepAndSkip_ReleaseCorrectControls()
        {
            var input = new TowerTutorialInput();
            input.Set(TowerTutorialStep.StartWave, 1, null, null);
            Assert.That(input.CanStartWave, Is.True);
            Assert.That(input.AllowsPurchase(ETowerType.Combat, 1), Is.False);
            input.IsDragging = true;
            input.Clear();
            Assert.That(input.Active, Is.False);
            Assert.That(input.IsDragging, Is.False);
            Assert.That(input.AllowsPurchase(ETowerType.Generator, 1), Is.True);
            Assert.That(input.AllowsDrop("11", "14"), Is.True);
        }

        [Test]
        public void PreviousMergeStepWithTwoTowers_RequiresThirdPurchase()
        {
            var progress = new TowerTutorialSaveData { step = (TowerTutorialStep)2 };
            var towers = new[]
            {
                new WorldTowerSaveData("11", 1, ETowerType.Combat),
                new WorldTowerSaveData("12", 1, ETowerType.Combat)
            };
            progress.Normalize();
            Assert.That(TowerTutorialRules.Reconcile(progress, towers), Is.EqualTo(TowerTutorialStep.BuyThird));
        }

        [TestCase(3, TowerTutorialStep.StartWave)]
        [TestCase(4, TowerTutorialStep.Completed)]
        public void SavedFinalSteps_KeepTheirMeaning(int savedValue, TowerTutorialStep expected)
        {
            var progress = new TowerTutorialSaveData { step = (TowerTutorialStep)savedValue };
            progress.Normalize();
            Assert.That(TowerTutorialRules.Reconcile(progress, new WorldTowerSaveData[0]), Is.EqualTo(expected));
        }

        [Test]
        public void UnknownProgressVersion_DoesNotTrapPlayer()
        {
            var data = new TowerTutorialSaveData { version = 99, step = (TowerTutorialStep)87, towerLevel = -5 };
            data.Normalize();
            Assert.That(data.step, Is.EqualTo(TowerTutorialStep.Completed));
            Assert.That(data.towerLevel, Is.EqualTo(1));
        }
    }
}
