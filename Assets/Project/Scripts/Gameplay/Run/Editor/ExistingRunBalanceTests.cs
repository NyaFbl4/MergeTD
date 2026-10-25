using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Project.Scripts.Gameplay.Enemies;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.Gameplay.Systems;
using Project.Scripts.Gameplay.Wave;
using Project.Scripts.System.Localization;
using UnityEditor;
using UnityEngine;

namespace Project.Scripts.Gameplay.Run.Editor
{
    public sealed class ExistingRunBalanceTests
    {
        private static readonly int[] WaveCounts = { 5, 5, 7, 7, 7, 10, 10, 10, 10, 10 };
        private static readonly int[] GoldTotals = { 700, 1000, 1500, 2200, 3200, 5200, 8000, 15000, 26000, 40000 };
        private static readonly BindingFlags PrivateFields = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void ExistingRuns_PreserveIdsAndHavePlannedWavesRewardsAndPhases()
        {
            var waves = new HashSet<EnemyWaveConfig>();
            for (var runIndex = 0; runIndex < WaveCounts.Length; runIndex++)
            {
                var run = LoadRun(runIndex);
                Assert.That(run.Id, Is.EqualTo($"default-run-{runIndex + 1}"));
                Assert.That(run.Waves.Count, Is.EqualTo(WaveCounts[runIndex]));
                Assert.That(run.Waves.Sum(w => w.GetRewardAmount(EWaveRewardType.Gold)), Is.EqualTo(GoldTotals[runIndex]));
                for (var index = 0; index < run.Waves.Count; index++)
                {
                    var wave = run.Waves[index];
                    Assert.That(wave.WaveConfig, Is.Not.Null);
                    Assert.That(waves.Add(wave.WaveConfig), Is.True, "Waves must not share mutable spawn configs.");
                    var final = index == run.Waves.Count - 1;
                    Assert.That(wave.IsBossWave, Is.EqualTo(final));
                    Assert.That(wave.PhaseAfterComplete, Is.EqualTo(final
                        ? ERunPhase.Victory
                        : (index + 1) % 3 == 0 ? ERunPhase.CardChoice : ERunPhase.Reward));
                    Assert.That(wave.Rewards.All(r => r.Amount > 0), Is.True);
                    Assert.That(wave.WaveConfig.Sequence.Count, Is.GreaterThan(0));
                    Assert.That(wave.WaveConfig.Sequence.All(s => s.EnemyPrefab != null && s.EnemyConfig != null
                        && s.Count > 0 && s.HealthMultiplier > 0 && s.StartDelay >= 0), Is.True);
                    Assert.That(wave.WaveConfig.Sequence.Count(s => s.EnemyPrefab.EnemyType == EEnemyType.Boss),
                        Is.EqualTo(final ? 1 : 0));
                }
            }
            Assert.That(waves.Count, Is.EqualTo(81));
        }

        [Test]
        public void NewEnemyTypes_AppearInThePlannedRuns()
        {
            Assert.That(EnemyTypes(LoadRun(0)).All(t => t == EEnemyType.Enemy || t == EEnemyType.Boss), Is.True);
            Assert.That(EnemyTypes(LoadRun(1)), Does.Contain(EEnemyType.Fast));
            Assert.That(EnemyTypes(LoadRun(1)).Contains(EEnemyType.Big), Is.False);
            for (var i = 2; i < WaveCounts.Length; i++)
            {
                Assert.That(EnemyTypes(LoadRun(i)), Does.Contain(EEnemyType.Fast));
                Assert.That(EnemyTypes(LoadRun(i)), Does.Contain(EEnemyType.Big));
            }
        }

        [Test]
        public void AverageWaveHealthAndFinalBossHealth_IncreaseBetweenRuns()
        {
            double previousAverage = 0;
            double previousBossHealth = 0;
            for (var i = 0; i < WaveCounts.Length; i++)
            {
                var run = LoadRun(i);
                var average = run.Waves.Where(w => !w.IsBossWave).Average(w => TotalHealth(w.WaveConfig));
                var boss = run.Waves.Last().WaveConfig.Sequence.Single(s => s.EnemyPrefab.EnemyType == EEnemyType.Boss);
                var bossHealth = Health(boss);
                Assert.That(average, Is.GreaterThan(previousAverage));
                Assert.That(bossHealth, Is.GreaterThan(previousBossHealth));
                previousAverage = average;
                previousBossHealth = bossHealth;
            }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void AllSpawnSchedules_FinishAtTheirConfiguredTime_RegardlessOfFrameRate(int fps)
        {
            for (var runIndex = 0; runIndex < WaveCounts.Length; runIndex++)
            {
                foreach (var wave in LoadRun(runIndex).Waves)
                {
                    var expectedEnd = wave.IsBossWave ? (runIndex >= 5 ? 58f : 54f) : 42f;
                    var config = wave.WaveConfig;
                    var frames = new Dictionary<float, int>();
                    var warningStarts = new List<float>();
                    var warningEnds = new List<float>();
                    var active = false;
                    var actualEnd = SimulateSchedule(config.Sequence, fps, (sequence, time) =>
                    {
                        if (sequence.ShowSpawnWarning)
                            Assert.That(active, Is.False, "Warning must hide when reinforcements arrive.");
                        if (!frames.ContainsKey(time)) frames[time] = 0;
                        frames[time]++;
                    }, config, (visible, time) =>
                    {
                        active = visible;
                        (visible ? warningStarts : warningEnds).Add(time);
                    });
                    var expectedTimes = wave.IsBossWave ? new[] { 12f, 32f, 42f } : new[] { 12f, 32f };
                    Assert.That(warningStarts.Count, Is.EqualTo(expectedTimes.Length), config.name);
                    Assert.That(warningEnds.Count, Is.EqualTo(expectedTimes.Length), config.name);
                    var groups = frames.Where(pair => pair.Value > 1).ToArray();
                    Assert.That(groups.Length, Is.EqualTo(expectedTimes.Length), config.name);
                    for (var peak = 0; peak < expectedTimes.Length; peak++)
                    {
                        Assert.That(groups[peak].Value, Is.InRange(5, 10), config.name);
                        Assert.That(groups[peak].Key, Is.EqualTo(expectedTimes[peak]).Within(2f / fps), config.name);
                        Assert.That(warningStarts[peak], Is.EqualTo(expectedTimes[peak] - 2f).Within(2f / fps), config.name);
                        Assert.That(warningEnds[peak], Is.EqualTo(expectedTimes[peak]).Within(2f / fps), config.name);
                    }
                    Assert.That(frames.Values.Sum(), Is.EqualTo(config.Sequence.Sum(s => s.Count)), config.name);
                    if (wave.IsBossWave)
                    {
                        Assert.That(groups.Last().Value, Is.EqualTo(5), config.name);
                        var boss = config.Sequence.Single(s => s.EnemyPrefab.EnemyType == EEnemyType.Boss);
                        Assert.That(boss.StartDelay, Is.EqualTo(42f), config.name);
                        Assert.That(config.Sequence.Count(s => s.StartDelay == boss.StartDelay), Is.EqualTo(5), config.name);
                        Assert.That(config.Sequence.Any(s => s.StartDelay > boss.StartDelay), Is.True, config.name);
                    }
                    Assert.That(actualEnd, Is.InRange(expectedEnd - 0.1f, expectedEnd + 0.35f),
                        $"{wave.WaveConfig.name} at {fps} FPS");
                }
            }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void GlobalSpawnCooldown_DoesNotPauseAnotherSequenceTimer(int fps)
        {
            var dense = Sequence(6, 0.22f, 0f);
            var delayed = Sequence(1, 1f, 1f);
            float delayedSpawn = -1;
            SimulateSchedule(new[] { dense, delayed }, fps, (sequence, time) =>
            {
                if (sequence == delayed)
                    delayedSpawn = time;
            });
            Assert.That(delayedSpawn, Is.InRange(1f - 1f / fps, 1.35f));
        }

        [Test]
        public void NewWaveConfig_DefaultsToSingleSpawnsWithoutWarnings()
        {
            var config = ScriptableObject.CreateInstance<EnemyWaveConfig>();
            try
            {
                Assert.That(config.MaxSpawnsPerTick, Is.EqualTo(1));
                Assert.That(config.SpawnWarningSeconds, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public void CheckingSpawnReadiness_DoesNotAdvanceTimers()
        {
            var sequence = new EnemySpawnSequenceRuntime(Sequence(1, 1f, 2f));
            for (var i = 0; i < 100; i++)
                Assert.That(sequence.CanSpawn(), Is.False);
            sequence.Tick(1f);
            Assert.That(sequence.CanSpawn(), Is.False);
            sequence.Tick(1f);
            Assert.That(sequence.CanSpawn(), Is.True);
            sequence.MarkSpawned();
            Assert.That(sequence.CanSpawn(), Is.False);
        }

        [Test]
        public void AllTenLocations_AreOrderedAndHaveDistinctBackgroundsAndIcons()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RunCatalog>("Assets/Project/Configs/Run Catalog.asset");
            Assert.That(catalog.Runs.Count, Is.EqualTo(10));
            var backgrounds = new HashSet<Sprite>();
            var icons = new HashSet<Sprite>();
            for (var i = 0; i < catalog.Runs.Count; i++)
            {
                var run = catalog.Runs[i];
                Assert.That(run, Is.SameAs(LoadRun(i)));
                Assert.That(run.Background, Is.Not.Null);
                Assert.That(run.Icon, Is.Not.Null);
                Assert.That(backgrounds.Add(run.Background), Is.True);
                Assert.That(icons.Add(run.Icon), Is.True);
            }
        }

        [Test]
        public void NewLocationBackgrounds_MatchExistingWorldSizeAndPivot()
        {
            var reference = LoadRun(4).Background;
            for (var i = 5; i < 10; i++)
            {
                var background = LoadRun(i).Background;
                Assert.That(background.bounds.size.y, Is.EqualTo(reference.bounds.size.y).Within(0.02f));
                Assert.That(background.bounds.size.x, Is.EqualTo(reference.bounds.size.x).Within(0.05f));
                Assert.That(background.pivot.x / background.rect.width,
                    Is.EqualTo(reference.pivot.x / reference.rect.width).Within(0.001f));
                Assert.That(background.pivot.y / background.rect.height,
                    Is.EqualTo(reference.pivot.y / reference.rect.height).Within(0.001f));
            }
        }

        [Test]
        public void NewLocationNames_ArePresentInBothLanguages()
        {
            var table = JsonUtility.FromJson<LocalizationTableData>(
                Resources.Load<TextAsset>("Localization/localization_table").text);
            var names = new[] { "Ржавый пояс", "Выжженные пустоши", "Забытый комплекс", "Чёрный бастион", "Сердце Зоны" };
            for (var i = 0; i < names.Length; i++)
            {
                var key = $"run.default_{i + 6}.name";
                var entry = table.entries.Single(e => e.key == key);
                Assert.That(entry.ru, Is.EqualTo(names[i]));
                Assert.That(entry.en, Is.Not.Empty);
                Assert.That(LoadRun(i + 5).DisplayName, Is.EqualTo(key));
            }
        }

        [Test]
        public void FirstRun_HealthIncreasesFifteenPercent_WithoutAddingEnemies()
        {
            var health = new[] { 161, 207, 253, 299, 322 };
            var counts = new[] { 18, 21, 24, 27, 22 };
            var run = LoadRun(0);
            for (var i = 0; i < run.Waves.Count; i++)
            {
                var wave = run.Waves[i].WaveConfig;
                var ordinary = wave.Sequence.Where(s => s.EnemyPrefab.EnemyType != EEnemyType.Boss).ToArray();
                Assert.That(ordinary.Sum(s => s.Count), Is.EqualTo(counts[i]));
                Assert.That(ordinary.All(s => Health(s) == health[i]), Is.True);
                Assert.That(wave.MaxSpawnsPerTick, Is.EqualTo(10));
                Assert.That(wave.SpawnWarningSeconds, Is.EqualTo(2f));
            }
            var boss = run.Waves.Last().WaveConfig.Sequence.Single(s => s.EnemyPrefab.EnemyType == EEnemyType.Boss);
            Assert.That(Health(boss), Is.EqualTo(2875));
            Assert.That(boss.StartDelay, Is.EqualTo(42f));
            for (var i = 2; i < 10; i++)
            foreach (var wave in LoadRun(i).Waves)
            {
                Assert.That(wave.WaveConfig.MaxSpawnsPerTick, Is.EqualTo(10));
                Assert.That(wave.WaveConfig.SpawnWarningSeconds, Is.EqualTo(2f));
            }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void FirstRun_SpawnsMixedFlowAndGroups_WithWarningsOnlyForGroups(int fps)
        {
            var expectedGroups = new[] { new[] { 5, 5 }, new[] { 5, 5 }, new[] { 5, 5 },
                new[] { 5, 5 }, new[] { 5, 5, 4 } };
            var waves = LoadRun(0).Waves;
            for (var i = 0; i < waves.Count; i++)
            {
                var wave = waves[i].WaveConfig;
                var frames = new Dictionary<float, int>();
                var warningStarts = new List<float>();
                var warningEnds = new List<float>();
                var active = false;
                SimulateSchedule(wave.Sequence, fps, (sequence, time) =>
                {
                    if (sequence.ShowSpawnWarning)
                        Assert.That(active, Is.False, "Warning must hide when the group arrives.");
                    if (sequence.EnemyPrefab.EnemyType == EEnemyType.Boss)
                        return;
                    if (!frames.ContainsKey(time)) frames[time] = 0;
                    frames[time]++;
                }, wave, (visible, time) =>
                {
                    active = visible;
                    (visible ? warningStarts : warningEnds).Add(time);
                });
                Assert.That(frames.Values.Where(count => count > 1).ToArray(), Is.EqualTo(expectedGroups[i]));
                if (i < 4)
                {
                    Assert.That(frames.Values.Count(count => count == 1), Is.EqualTo(8 + i * 3));
                    Assert.That(wave.Sequence.Count(s => s.ShowSpawnWarning), Is.EqualTo(10));
                    Assert.That(wave.Sequence.Where(s => s.ShowSpawnWarning).Select(s => s.StartDelay).Distinct(),
                        Is.EquivalentTo(new[] { 12f, 32f }));
                    Assert.That(wave.Sequence.Max(s => s.StartDelay), Is.EqualTo(42f));
                }
                var spawnTimes = wave.Sequence.Where(s => s.ShowSpawnWarning).Select(s => s.StartDelay).Distinct().OrderBy(x => x).ToArray();
                Assert.That(warningStarts.Count, Is.EqualTo(spawnTimes.Length));
                Assert.That(warningEnds.Count, Is.EqualTo(spawnTimes.Length));
                for (var j = 0; j < spawnTimes.Length; j++)
                {
                    Assert.That(warningStarts[j], Is.EqualTo(spawnTimes[j] - 2f).Within(2f / fps));
                    Assert.That(warningEnds[j], Is.EqualTo(spawnTimes[j]).Within(2f / fps));
                }
            }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void SecondRun_MixedWavesGrowGradually_AndBossArrivesWithEscort(int fps)
        {
            var counts = new[] { 24, 27, 30, 34, 31 };
            var groups = new[] { 5, 6, 6, 7, 6 };
            var run = LoadRun(1);
            for (var wi = 0; wi < run.Waves.Count; wi++)
            {
                var wave = run.Waves[wi].WaveConfig;
                Assert.That(wave.MaxSpawnsPerTick, Is.EqualTo(10));
                Assert.That(wave.SpawnWarningSeconds, Is.EqualTo(2f));
                Assert.That(wave.Sequence.Sum(s => s.Count), Is.EqualTo(counts[wi]));
                var frames = new Dictionary<float, int>();
                var warningStarts = new List<float>();
                var warningEnds = new List<float>();
                var active = false;
                SimulateSchedule(wave.Sequence, fps, (sequence, time) =>
                {
                    if (sequence.ShowSpawnWarning)
                        Assert.That(active, Is.False);
                    if (!frames.ContainsKey(time)) frames[time] = 0;
                    frames[time]++;
                }, wave, (visible, time) =>
                {
                    active = visible;
                    (visible ? warningStarts : warningEnds).Add(time);
                });
                var final = wi == run.Waves.Count - 1;
                Assert.That(frames.Values.Where(count => count > 1), Is.EqualTo(final
                    ? new[] { groups[wi], groups[wi], 5 }
                    : new[] { groups[wi], groups[wi] }));
                var times = final ? new[] { 12f, 32f, 42f } : new[] { 12f, 32f };
                Assert.That(warningStarts.Count, Is.EqualTo(times.Length));
                Assert.That(warningEnds.Count, Is.EqualTo(times.Length));
                for (var i = 0; i < times.Length; i++)
                {
                    Assert.That(warningStarts[i], Is.EqualTo(times[i] - 2f).Within(2f / fps));
                    Assert.That(warningEnds[i], Is.EqualTo(times[i]).Within(2f / fps));
                }
            }
            foreach (var runIndex in new[] { 0, 1 })
            {
                var finalWave = LoadRun(runIndex).Waves.Last().WaveConfig;
                var boss = finalWave.Sequence.Single(s => s.EnemyPrefab.EnemyType == EEnemyType.Boss);
                Assert.That(boss.StartDelay, Is.EqualTo(42f));
                Assert.That(finalWave.Sequence.Count(s => s.StartDelay == boss.StartDelay
                    && s.EnemyPrefab.EnemyType != EEnemyType.Boss), Is.EqualTo(4));
                Assert.That(finalWave.Sequence.Any(s => s.StartDelay > boss.StartDelay), Is.True,
                    "Background must continue while the boss advances.");
            }
        }

        [Test]
        public void SpawnWarning_HidesOnDefeat_AndResumesWithRemainingTime()
        {
            var root = new GameObject("WarningLifecycleTest");
            var runtime = new BattlefieldRuntime(root.AddComponent<BattlefieldContext>(), new RunState(null), null);
            try
            {
                var wave = LoadRun(0).Waves[0].WaveConfig;
                typeof(BattlefieldRuntime).GetMethod("InitializeWave", PrivateFields).Invoke(runtime, new object[] { wave });
                SetField(runtime, "_isGameRunning", true);
                SetField(runtime, "_isWaveRunning", true);
                for (var step = 0; step < 21; step++)
                    runtime.OnUpdate(0.5f);
                Assert.That(runtime.IsSpawnWarningActive, Is.True);
                var state = (RunState)typeof(BattlefieldRuntime).GetField("_runState", PrivateFields).GetValue(runtime);
                state.Defeat();
                runtime.OnFinishGame();
                Assert.That(runtime.IsSpawnWarningActive, Is.False);
                runtime.OnUpdate(5f);
                Assert.That(runtime.IsSpawnWarningActive, Is.False);
                Assert.That(runtime.TryResumeDefeatedWave(), Is.True);
                runtime.OnUpdate(0.1f);
                Assert.That(runtime.IsSpawnWarningActive, Is.True);
                runtime.Dispose();
                Assert.That(runtime.IsSpawnWarningActive, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static float SimulateSchedule(IReadOnlyList<EnemySpawnSequenceConfig> configs, int fps,
            Action<EnemySpawnSequenceConfig, float> spawned = null,
            EnemyWaveConfig wave = null, Action<bool, float> warning = null)
        {
            var root = new GameObject("SpawnScheduleTest");
            try
            {
                var context = root.AddComponent<BattlefieldContext>();
                var runtime = new BattlefieldRuntime(context, new RunState(null), null);
                SetField(runtime, "_isGameRunning", true);
                SetField(runtime, "_isWaveRunning", true);
                var sequences = (IList)typeof(BattlefieldRuntime).GetField("_sequenceRuntimes", PrivateFields).GetValue(runtime);
                foreach (var config in configs)
                    sequences.Add(new EnemySpawnSequenceRuntime(config));
                if (wave != null)
                    typeof(BattlefieldRuntime).GetMethod("InitializeWave", PrivateFields).Invoke(runtime, new object[] { wave });
                var elapsed = 0f;
                runtime.SpawnWarningChanged += active => warning?.Invoke(active, elapsed);
                var previous = new int[configs.Count];
                var dt = 1f / fps;
                for (var step = 0; step < fps * 100; step++)
                {
                    elapsed += dt;
                    runtime.OnUpdate(dt);
                    var complete = true;
                    for (var i = 0; i < sequences.Count; i++)
                    {
                        var sequence = (EnemySpawnSequenceRuntime)sequences[i];
                        var count = (int)typeof(EnemySpawnSequenceRuntime).GetField("_spawnedCount", PrivateFields).GetValue(sequence);
                        if (count > previous[i])
                            spawned?.Invoke(sequence.Config, elapsed);
                        previous[i] = count;
                        complete &= sequence.IsComplete;
                    }
                    if (complete)
                        return elapsed;
                }
                Assert.Fail("Spawn schedule did not complete within 100 simulated seconds.");
                return elapsed;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static EnemySpawnSequenceConfig Sequence(int count, float interval, float delay)
        {
            var config = new EnemySpawnSequenceConfig();
            SetField(config, "_count", count);
            SetField(config, "_spawnInterval", interval);
            SetField(config, "_startDelay", delay);
            return config;
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, PrivateFields).SetValue(target, value);

        private static RunConfig LoadRun(int index) => AssetDatabase.LoadAssetAtPath<RunConfig>(
            $"Assets/Project/Configs/Runs/Run Config {index + 1}.asset");

        private static IEnumerable<EEnemyType> EnemyTypes(RunConfig run) =>
            run.Waves.SelectMany(w => w.WaveConfig.Sequence).Select(s => s.EnemyPrefab.EnemyType);

        private static double Health(EnemySpawnSequenceConfig sequence) => Math.Max(1, Math.Round(
            sequence.EnemyConfig.StartHealth * sequence.HealthMultiplier
            * sequence.EnemyConfig.GetHealthMultiplier(sequence.EnemyPrefab.EnemyType)));

        private static double TotalHealth(EnemyWaveConfig wave) => wave.Sequence.Sum(s => s.Count * Health(s));
    }
}
