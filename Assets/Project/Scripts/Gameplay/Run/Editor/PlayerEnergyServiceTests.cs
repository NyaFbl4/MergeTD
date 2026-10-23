using System;
using NUnit.Framework;
using Project.Scripts.System.Reward;
using Project.Scripts.System.Save;
using UnityEngine;

namespace Project.Scripts.Gameplay.Run.Editor
{
    public sealed class PlayerEnergyServiceTests
    {
        private const long StartTime = 1_790_000_000_000;
        private const long Hour = PlayerEnergyService.RestoreIntervalMilliseconds;
        private MemoryStore _store;
        private TestClock _clock;
        private PlayerEnergyService _service;

        [SetUp]
        public void SetUp()
        {
            _store = new MemoryStore();
            _clock = new TestClock { Now = StartTime, Available = true };
            _service = new PlayerEnergyService(_store, _clock);
        }

        [Test]
        public void FullEnergy_DoesNotStartTimerOrWriteSave()
        {
            Assert.That(_service.GetState().Current, Is.EqualTo(10));
            Assert.That(_store.NextRestore, Is.Zero);
            Assert.That(_store.SaveCount, Is.Zero);
        }

        [Test]
        public void StartingRun_SpendsOneAndStartsHour()
        {
            Assert.That(_service.TrySpendForRun(), Is.True);
            Assert.That(_store.Current, Is.EqualTo(9));
            Assert.That(_store.NextRestore, Is.EqualTo(StartTime + Hour));
        }

        [Test]
        public void SecondRun_PreservesAlreadyRunningTimer()
        {
            _service.TrySpendForRun();
            _clock.Now += Hour / 2;
            _service.TrySpendForRun();

            Assert.That(_store.Current, Is.EqualTo(8));
            Assert.That(_store.NextRestore, Is.EqualTo(StartTime + Hour));
        }

        [Test]
        public void BeforeHour_DoesNotRestoreOrRewriteSave()
        {
            _service.TrySpendForRun();
            _clock.Now += Hour - 1;
            var state = _service.GetState();

            Assert.That(state.Current, Is.EqualTo(9));
            Assert.That(state.TimeUntilNextRestore, Is.EqualTo(TimeSpan.FromMilliseconds(1)));
            Assert.That(_store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void AtExactHour_RestoresOneAndStopsAtFull()
        {
            _service.TrySpendForRun();
            _clock.Now += Hour;

            Assert.That(_service.GetState().Current, Is.EqualTo(10));
            Assert.That(_store.NextRestore, Is.Zero);
        }

        [Test]
        public void TimeOutsideGame_RestoresWholeHoursAndKeepsRemainder()
        {
            _store.Current = 3;
            _store.NextRestore = StartTime + Hour;
            _clock.Now += 3 * Hour + Hour / 2;
            var reloadedService = new PlayerEnergyService(_store, _clock);
            var state = reloadedService.GetState();

            Assert.That(state.Current, Is.EqualTo(6));
            Assert.That(state.TimeUntilNextRestore, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(_store.NextRestore, Is.EqualTo(StartTime + 4 * Hour));
        }

        [Test]
        public void LongAbsence_DoesNotExceedCapacity()
        {
            _store.Current = 0;
            _store.NextRestore = StartTime + Hour;
            _clock.Now += 100 * Hour;

            Assert.That(_service.GetState().Current, Is.EqualTo(10));
            Assert.That(_store.NextRestore, Is.Zero);
        }

        [Test]
        public void TimeAtFull_DoesNotAccumulateForLaterSpending()
        {
            _clock.Now += 100 * Hour;
            _service.TrySpendForRun();

            Assert.That(_service.GetState().Current, Is.EqualTo(9));
            Assert.That(_store.NextRestore, Is.EqualTo(_clock.Now + Hour));
        }

        [Test]
        public void EmptyEnergy_BlocksRunWithoutSaving()
        {
            _store.Current = 0;
            _store.NextRestore = StartTime + Hour;

            Assert.That(_service.TrySpendForRun(), Is.False);
            Assert.That(_store.Current, Is.Zero);
            Assert.That(_store.SaveCount, Is.Zero);
        }

        [Test]
        public void RestoreIsAppliedBeforeCheckingRunCost()
        {
            _store.Current = 0;
            _store.NextRestore = StartTime;

            Assert.That(_service.TrySpendForRun(), Is.True);
            Assert.That(_store.Current, Is.Zero);
            Assert.That(_store.NextRestore, Is.EqualTo(StartTime + Hour));
        }

        [Test]
        public void Reload_PreservesSpentEnergyAndDeadline()
        {
            _service.TrySpendForRun();
            _clock.Now += Hour / 4;
            var reloadedService = new PlayerEnergyService(_store, _clock);
            var state = reloadedService.GetState();

            Assert.That(state.Current, Is.EqualTo(9));
            Assert.That(state.TimeUntilNextRestore, Is.EqualTo(TimeSpan.FromMinutes(45)));
        }

        [Test]
        public void UnavailableServerTime_DoesNotAwardEnergyOrChangeDeadline()
        {
            _store.Current = 4;
            _store.NextRestore = StartTime;
            _clock.Available = false;
            var state = _service.GetState();

            Assert.That(state.IsTimeAvailable, Is.False);
            Assert.That(state.Current, Is.EqualTo(4));
            Assert.That(_store.NextRestore, Is.EqualTo(StartTime));
            Assert.That(_store.SaveCount, Is.Zero);
        }

        [Test]
        public void FirstSpendWithoutClock_StartsRecoveryWhenServerTimeArrives()
        {
            _clock.Available = false;
            Assert.That(_service.TrySpendForRun(), Is.True);
            Assert.That(_store.NextRestore, Is.Zero);

            _clock.Available = true;
            var state = _service.GetState();

            Assert.That(state.Current, Is.EqualTo(9));
            Assert.That(_store.NextRestore, Is.EqualTo(StartTime + Hour));
        }

        [Test]
        public void ClockMovesBackward_DoesNotAwardEnergy()
        {
            _service.TrySpendForRun();
            _clock.Now -= Hour;

            Assert.That(_service.GetState().Current, Is.EqualTo(9));
            Assert.That(_store.NextRestore, Is.EqualTo(StartTime + Hour));
        }

        [Test]
        public void LegacySave_ReceivesFullEnergyWithoutChangingOtherProgress()
        {
            var world = JsonUtility.FromJson<WorldSaveData>("{\"version\":2,\"gold\":321,\"maxEnergy\":17}");
            world.playerEnergy = PlayerEnergySaveData.Normalize(world.playerEnergy);

            Assert.That(world.playerEnergy.current, Is.EqualTo(10));
            Assert.That(world.playerEnergy.version, Is.EqualTo(1));
            Assert.That(world.gold, Is.EqualTo(321));
            Assert.That(world.maxEnergy, Is.EqualTo(17));
        }

        [Test]
        public void SaveReload_KeepsEnergyAndRecoveryTimestamp()
        {
            var saved = new WorldSaveData
            {
                playerEnergy = new PlayerEnergySaveData
                {
                    current = 7,
                    nextRestoreUnixMilliseconds = StartTime + Hour
                }
            };
            var loaded = JsonUtility.FromJson<WorldSaveData>(JsonUtility.ToJson(saved));
            loaded.playerEnergy = PlayerEnergySaveData.Normalize(loaded.playerEnergy);

            Assert.That(loaded.playerEnergy.current, Is.EqualTo(7));
            Assert.That(loaded.playerEnergy.nextRestoreUnixMilliseconds, Is.EqualTo(StartTime + Hour));
        }

        [TestCase(-5, -1, 0)]
        [TestCase(20, StartTime, 20)]
        public void InvalidSaveValues_AreClamped(int current, long nextRestore, int expected)
        {
            var data = PlayerEnergySaveData.Normalize(new PlayerEnergySaveData
            {
                current = current,
                nextRestoreUnixMilliseconds = nextRestore
            });

            Assert.That(data.current, Is.EqualTo(expected));
            Assert.That(data.nextRestoreUnixMilliseconds, Is.Zero);
        }

        [Test]
        public void SurplusEnergy_DoesNotRegenerateOrDisappear()
        {
            _store.Current = 25;
            _clock.Now += 100 * Hour;
            Assert.That(_service.GetState().Current, Is.EqualTo(25));
            Assert.That(_store.SaveCount, Is.Zero);
        }

        [Test]
        public void SpendingSurplus_StartsRecoveryOnlyBelowTen()
        {
            _store.Current = 11;
            Assert.That(_service.TrySpendForRun(), Is.True);
            Assert.That(_store.Current, Is.EqualTo(10));
            Assert.That(_store.NextRestore, Is.Zero);
            Assert.That(_service.TrySpendForRun(), Is.True);
            Assert.That(_store.Current, Is.EqualTo(9));
            Assert.That(_store.NextRestore, Is.EqualTo(StartTime + Hour));
        }

        [Test]
        public void SurplusSave_RetainsAmountAfterSerializationAndNormalization()
        {
            var original = new WorldSaveData
            {
                playerEnergy = new PlayerEnergySaveData { current = 37 }
            };
            var loaded = JsonUtility.FromJson<WorldSaveData>(JsonUtility.ToJson(original));
            Assert.That(PlayerEnergySaveData.Normalize(loaded.playerEnergy).current, Is.EqualTo(37));
        }

        private sealed class MemoryStore : IPlayerEnergyStore
        {
            public int Current = 10;
            public long NextRestore;
            public int SaveCount;
            public int PlayerEnergy => Current;
            public long PlayerEnergyNextRestoreUnixMilliseconds => NextRestore;

            public void SavePlayerEnergy(int current, long nextRestoreUnixMilliseconds)
            {
                Current = current;
                NextRestore = nextRestoreUnixMilliseconds;
                SaveCount++;
            }
        }

        private sealed class TestClock : IDailyRewardTimeProvider
        {
            public long Now;
            public bool Available;

            public bool TryGetCurrentUnixMilliseconds(out long unixMilliseconds)
            {
                unixMilliseconds = Available ? Now : 0;
                return Available;
            }
        }
    }
}
