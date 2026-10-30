using NUnit.Framework;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.System.Save;
using UnityEngine;
using YG;

namespace Project.Scripts.Gameplay.Tutorial.Editor
{
    public sealed class SpellTutorialTests
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
        public void NewWorld_QueuesSpellTutorialWithoutStartingBeforeTowerLesson()
        {
            var world = new WorldService(new WorldSaveService());
            Assert.That(world.SpellTutorial.step, Is.EqualTo(SpellTutorialStep.WaitingForPreparation));
            Assert.That(SpellTutorialRules.CanBegin(world.TowerTutorial.step, "default-run-1", 2, ERunPhase.Preparation), Is.False);
        }

        [Test]
        public void OldWorldWithoutSpellLesson_SkipsItWithoutChangingArmyOrWallet()
        {
            var legacy = new WorldSaveData { gold = 1234, maxBaseHealth = 10, maxEnergy = 12 };
            legacy.towers.Add(new WorldTowerSaveData("11", 3, ETowerType.Combat, 600));
            YG2.saves.world = legacy;
            var world = new WorldService(new WorldSaveService());
            Assert.That(world.SpellTutorial.step, Is.EqualTo(SpellTutorialStep.Completed));
            Assert.That(world.Gold, Is.EqualTo(1234));
            Assert.That(world.Towers[0].towerLevel, Is.EqualTo(3));
        }

        [TestCase(SpellTutorialStep.BuyGenerator)]
        [TestCase(SpellTutorialStep.StartWave)]
        [TestCase(SpellTutorialStep.AccumulateEnergy)]
        [TestCase(SpellTutorialStep.SelectSpell)]
        [TestCase(SpellTutorialStep.SelectTarget)]
        [TestCase(SpellTutorialStep.Completed)]
        public void SavedProgress_ReloadsAndDoesNotChangeFirstTutorial(SpellTutorialStep step)
        {
            var world = new WorldService(new WorldSaveService());
            world.SaveTowerTutorial(TowerTutorialStep.Completed, 1);
            world.SaveSpellTutorial(step);
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            var reloaded = new WorldService(new WorldSaveService());
            Assert.That(reloaded.SpellTutorial.step, Is.EqualTo(step));
            Assert.That(reloaded.TowerTutorial.step, Is.EqualTo(TowerTutorialStep.Completed));
        }

        [TestCase(false, SpellTutorialStep.WaitingForPreparation)]
        [TestCase(true, SpellTutorialStep.Completed)]
        public void ExistingTowerLesson_ContinuesWithNewSpellLessonUntilFirstVictory(bool wonFirstRun, SpellTutorialStep expected)
        {
            var data = new WorldSaveData
            {
                gold = 200,
                maxBaseHealth = 10,
                maxEnergy = 12,
                towerTutorial = new TowerTutorialSaveData { step = TowerTutorialStep.Completed }
            };
            if (wonFirstRun) data.completedRunIds.Add("default-run-1");
            YG2.saves.world = data;
            var world = new WorldService(new WorldSaveService());
            Assert.That(world.SpellTutorial.step, Is.EqualTo(expected));
            Assert.That(world.TowerTutorial.step, Is.EqualTo(TowerTutorialStep.Completed));
            Assert.That(world.Gold, Is.EqualTo(200));
        }

        [Test]
        public void Reset_RestartsBothIndependentScenarios()
        {
            var world = new WorldService(new WorldSaveService());
            world.SaveTowerTutorial(TowerTutorialStep.Completed, 1);
            world.SaveSpellTutorial(SpellTutorialStep.Completed);
            world.Reset();
            Assert.That(world.TowerTutorial.step, Is.EqualTo(TowerTutorialStep.BuyFirst));
            Assert.That(world.SpellTutorial.step, Is.EqualTo(SpellTutorialStep.WaitingForPreparation));
        }

        [TestCase(1, ERunPhase.Preparation, false)]
        [TestCase(2, ERunPhase.Preparation, true)]
        [TestCase(3, ERunPhase.Preparation, true)]
        [TestCase(2, ERunPhase.Wave, false)]
        [TestCase(2, ERunPhase.Defeat, false)]
        public void StartRequiresPreparationAfterFirstWave(int wave, ERunPhase phase, bool expected)
        {
            Assert.That(SpellTutorialRules.CanBegin(TowerTutorialStep.Completed, "default-run-1", wave, phase), Is.EqualTo(expected));
            Assert.That(SpellTutorialRules.CanBegin(TowerTutorialStep.Completed, "default-run-2", wave, phase), Is.False);
        }

        [TestCase(4, 5, true, true, true, false)]
        [TestCase(5, 5, false, true, true, false)]
        [TestCase(5, 5, true, false, true, false)]
        [TestCase(5, 5, true, true, false, false)]
        [TestCase(5, 5, true, true, true, true)]
        public void PromptWaitsForEnergyCooldownEnemyAndRunningBattle(int energy, int cost, bool ready, bool enemy, bool running, bool expected)
        {
            Assert.That(SpellTutorialRules.CanPrompt(energy, cost, ready, enemy, running), Is.EqualTo(expected));
        }

        [Test]
        public void GeneratorStep_BlocksTowerPurchasesAndEarlyWave()
        {
            var input = new SpellTutorialInput();
            input.Set(SpellTutorialStep.BuyGenerator);
            Assert.That(input.CanStartWave, Is.False);
            Assert.That(input.AllowsPurchase(ETowerType.Generator, 1), Is.True);
            Assert.That(input.AllowsPurchase(ETowerType.Generator, 2), Is.False);
            Assert.That(input.AllowsPurchase(ETowerType.Combat, 1), Is.False);
            Assert.That(input.AllowsTarget(Vector2.zero), Is.False);
        }

        [Test]
        public void ExistingGenerator_LeadsToStartWithoutAnotherPurchase()
        {
            var input = new SpellTutorialInput();
            input.Set(SpellTutorialRules.PreparationStep(true));
            Assert.That(input.CanStartWave, Is.True);
            Assert.That(input.AllowsPurchase(ETowerType.Generator, 1), Is.False);
            Assert.That(SpellTutorialRules.PreparationStep(false), Is.EqualTo(SpellTutorialStep.BuyGenerator));
        }

        [Test]
        public void TargetStep_OnlyAllowsImpactNearHighlightedEnemyAndSkipReleasesIt()
        {
            var input = new SpellTutorialInput();
            input.Set(SpellTutorialStep.SelectSpell);
            Assert.That(input.AllowsTarget(new Vector2(2, 3)), Is.False);
            input.Set(SpellTutorialStep.SelectTarget, new Vector2(2, 3), 2);
            Assert.That(input.AllowsTarget(new Vector2(2, 3)), Is.True);
            Assert.That(input.AllowsTarget(new Vector2(4, 3)), Is.True);
            Assert.That(input.AllowsTarget(new Vector2(4.01f, 3)), Is.False);
            input.Clear();
            Assert.That(input.CanStartWave, Is.True);
            Assert.That(input.AllowsPurchase(ETowerType.Combat, 1), Is.True);
            Assert.That(input.AllowsTarget(new Vector2(100, 100)), Is.True);
        }

        [TestCase(SpellTutorialStep.BuyGenerator, false)]
        [TestCase(SpellTutorialStep.AccumulateEnergy, true)]
        [TestCase(SpellTutorialStep.SelectSpell, true)]
        [TestCase(SpellTutorialStep.SelectTarget, true)]
        [TestCase(SpellTutorialStep.Completed, false)]
        public void OnlyActualCastsDuringActiveBattleCanCompleteLesson(SpellTutorialStep step, bool expected)
        {
            Assert.That(SpellTutorialRules.CanCountCast(step, ERunPhase.Wave), Is.EqualTo(expected));
            Assert.That(SpellTutorialRules.CanCountCast(step, ERunPhase.Preparation), Is.False);
        }

        [Test]
        public void InvalidSavedProgress_DoesNotBlockGame()
        {
            var progress = new SpellTutorialSaveData { version = 99, step = (SpellTutorialStep)(-1) };
            progress.Normalize();
            Assert.That(progress.step, Is.EqualTo(SpellTutorialStep.Completed));
        }

        [TestCase(.5f, .7f, 1f, true)]
        [TestCase(.5f, .5f, 1f, false)]
        [TestCase(.99f, .7f, 1f, false)]
        [TestCase(.5f, .95f, 1f, false)]
        [TestCase(.5f, .7f, -1f, false)]
        public void TargetMustStayVisibleAboveMessageAndBelowHud(float x, float y, float z, bool expected)
        {
            Assert.That(SpellTutorialRules.IsTargetInView(new Vector3(x, y, z)), Is.EqualTo(expected));
        }
    }
}
