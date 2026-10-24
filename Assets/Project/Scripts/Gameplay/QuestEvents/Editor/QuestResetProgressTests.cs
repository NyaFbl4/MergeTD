using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePipe;
using NUnit.Framework;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Reward;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using YG;

namespace Project.Scripts.Gameplay.Quests.Editor
{
    public sealed class QuestResetProgressTests
    {
        private const string SaveKey = "merge_td_world";
        private string _localSave;
        private bool _hadLocalSave;
        private WorldSaveData _originalWorld;
        private string _originalWorldJson;
        private WorldService _world;
        private QuestService _quests;
        private QuestCatalog _catalog;
        private RunConfig _run;
        private Clock _clock;
        private Events<EnemyKilledQuestEventDTO> _kills;

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
            _catalog = AssetDatabase.LoadAssetAtPath<QuestCatalog>("Assets/Project/Configs/QuestCatalog.asset");
            _run = AssetDatabase.LoadAssetAtPath<RunConfig>("Assets/Project/Configs/Runs/Run Config 1.asset");
            _clock = new Clock();
            _kills = new Events<EnemyKilledQuestEventDTO>();
            _world = new WorldService(new WorldSaveService());
            _quests = CreateService();
        }

        [TearDown]
        public void TearDown()
        {
            _quests?.Dispose();
            YG2.saves.world = _originalWorld;
            YG2.saves.worldJson = _originalWorldJson;
            if (_hadLocalSave)
                PlayerPrefs.SetString(SaveKey, _localSave);
            else
                PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Reset_ClearsPartialCompletedAndClaimedAchievements(bool completed, bool claimed)
        {
            foreach (var quest in _quests.Achievements)
                quest.RestoreState(completed ? quest.TargetValue : 1, claimed);
            Assert.That(_quests.Achievements.Count, Is.EqualTo(4));

            _world.Reset();

            AssertReset(_quests.Achievements);
            AssertSavedReset();
            _quests.EnsureActiveQuests();
            AssertReset(_quests.Achievements);
        }

        [Test]
        public void Reset_NotifiesQuestUiWithFreshState()
        {
            _quests.Achievements[0].RestoreState(17, false);
            var notifications = 0;
            _quests.QuestsChanged += () =>
            {
                notifications++;
                AssertReset(_quests.Achievements);
                AssertSavedReset();
            };

            _world.Reset();

            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void Reset_ClearsDailyWeeklyAndAchievementRuntime()
        {
            foreach (var quest in AllQuests())
                quest.RestoreState(quest.TargetValue, true);

            _world.Reset();

            Assert.That(_quests.DailyQuests.Count, Is.EqualTo(2));
            Assert.That(_quests.WeeklyQuests.Count, Is.EqualTo(2));
            AssertReset(AllQuests());
            AssertSavedReset();
        }

        [Test]
        public void Reset_WithoutServerTime_DoesNotResurrectQuestsWhenTimeReturns()
        {
            foreach (var quest in AllQuests())
                quest.RestoreState(quest.TargetValue, true);
            _clock.Available = false;

            _world.Reset();

            AssertReset(_quests.Achievements);
            Assert.That(_quests.DailyQuests, Is.Empty);
            Assert.That(_quests.WeeklyQuests, Is.Empty);
            _clock.Available = true;
            _quests.EnsureActiveQuests();
            AssertReset(AllQuests());
            AssertSavedReset();
        }

        [UnityTest]
        public IEnumerator Reset_PendingSaveCannotRestoreProgressAfterDelayOrDispose()
        {
            _quests.Achievements[0].RestoreState(23, false);
            _world.Reset();
            yield return new WaitForSecondsRealtime(1.2f);
            AssertSavedReset();
            _quests.Dispose();
            _quests = null;
            AssertSavedReset();
        }

        [Test]
        public void Reset_SaveReloadKeepsAchievementsAtZero()
        {
            foreach (var quest in _quests.Achievements)
                quest.RestoreState(quest.TargetValue, true);
            _world.Reset();
            _quests.Dispose();
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            _world = new WorldService(new WorldSaveService());
            _quests = CreateService();

            AssertReset(_quests.Achievements);
            AssertSavedReset();
        }

        [Test]
        public void Reset_DisposesOldQuestSubscriptionsAndRejectsOldReward()
        {
            var oldQuest = _quests.Achievements.Single(q => q is KillEnemyQuest);
            oldQuest.RestoreState(oldQuest.TargetValue, false);

            _world.Reset();
            _world.Reset();
            _kills.Publish(default);

            var freshQuest = _quests.Achievements.Single(q => q is KillEnemyQuest);
            Assert.That(freshQuest.CurrentValue, Is.EqualTo(1));
            Assert.That(_quests.TryClaimReward(oldQuest), Is.False);
            Assert.That(_world.Gems, Is.Zero);
            Assert.That(_kills.Count, Is.EqualTo(AllQuests().Count(q => q is KillEnemyQuest)));
        }

        [Test]
        public void NormalRefreshAndCalendarRollover_DoNotResetAchievements()
        {
            var quest = _quests.Achievements[0];
            quest.RestoreState(quest.TargetValue, true);
            _quests.EnsureActiveQuests();
            _clock.Now += 8L * 24 * 60 * 60 * 1000;
            _quests.EnsureActiveQuests();

            Assert.That(_quests.Achievements[0], Is.SameAs(quest));
            Assert.That(quest.CurrentValue, Is.EqualTo(quest.TargetValue));
            Assert.That(quest.IsRewardClaimed, Is.True);
        }

        [Test]
        public void Reset_AfterServiceDispose_DoesNotRecreateQuests()
        {
            _quests.Dispose();
            _quests = null;

            _world.Reset();

            Assert.That(_world.AchievementQuests, Is.Empty);
            Assert.That(_kills.Count, Is.Zero);
        }

        private QuestService CreateService()
        {
            var service = new QuestService(_catalog, new PlayerStatsUseCase(_world),
                new Selection(_run), _world, _clock, _kills,
                new Events<TowerBoughtQuestEventDTO>(), new Events<DamageDealtQuestEventDTO>(),
                new Events<WaveCompletedQuestEventDTO>(), new Audio());
            service.Initialize();
            return service;
        }

        private IEnumerable<IQuestRuntime> AllQuests() =>
            _quests.DailyQuests.Concat(_quests.WeeklyQuests).Concat(_quests.Achievements);

        private static void AssertReset(IEnumerable<IQuestRuntime> quests)
        {
            Assert.That(quests, Is.Not.Empty);
            foreach (var quest in quests)
            {
                Assert.That(quest.CurrentValue, Is.Zero, quest.Id);
                Assert.That(quest.IsCompleted, Is.False, quest.Id);
                Assert.That(quest.IsRewardClaimed, Is.False, quest.Id);
            }
        }

        private void AssertSavedReset()
        {
            var saved = JsonUtility.FromJson<WorldSaveData>(PlayerPrefs.GetString(SaveKey));
            Assert.That(saved.achievementQuests.Count, Is.EqualTo(4));
            foreach (var quest in saved.dailyQuests.Concat(saved.weeklyQuests).Concat(saved.achievementQuests))
            {
                Assert.That(quest.currentValue, Is.Zero, quest.id);
                Assert.That(quest.isRewardClaimed, Is.False, quest.id);
            }
        }

        private sealed class Clock : IDailyRewardTimeProvider
        {
            public bool Available = true;
            public long Now = 1792800000000;
            public bool TryGetCurrentUnixMilliseconds(out long milliseconds)
            {
                milliseconds = Now;
                return Available;
            }
        }

        private sealed class Events<T> : ISubscriber<T>
        {
            private readonly List<IMessageHandler<T>> _handlers = new();
            public int Count => _handlers.Count;
            public IDisposable Subscribe(IMessageHandler<T> handler, params MessageHandlerFilter<T>[] filters)
            {
                _handlers.Add(handler);
                return new Subscription(() => _handlers.Remove(handler));
            }
            public void Publish(T message)
            {
                foreach (var handler in _handlers.ToArray())
                    handler.Handle(message);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Action _dispose;
            public Subscription(Action dispose) => _dispose = dispose;
            public void Dispose() => _dispose();
        }

        private sealed class Selection : IRunSelectionService
        {
            public Selection(RunConfig run) => SelectedRun = run;
            public int Count => 1;
            public int SelectedIndex => 0;
            public RunConfig SelectedRun { get; }
            public RunConfig PreviousRun => null;
            public bool IsSelectedRunUnlocked => true;
            public event Action<RunConfig> SelectionChanged { add { } remove { } }
            public void SelectNext() { }
            public void SelectPrevious() { }
        }

        private sealed class Audio : IAudioManager
        {
            public bool IsMusicEnabled => true;
            public bool IsSoundEnabled => true;
            public void PlaySound(ESoundId id) { }
            public void PlayMusic() { }
            public void StopMusic() { }
            public void SetMusicEnabled(bool value) { }
            public void SetSoundEnabled(bool value) { }
        }
    }
}
