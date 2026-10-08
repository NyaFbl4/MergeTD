using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Reward;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;
using VContainer.Unity;

namespace Project.Scripts.Gameplay.Quests
{
    public readonly struct DailyQuestScheduleState
    {
        public bool ShouldReset { get; }
        public long QuestDay { get; }
        public int CycleDay { get; }

        public DailyQuestScheduleState(bool shouldReset, long questDay, int cycleDay)
        {
            ShouldReset = shouldReset;
            QuestDay = questDay;
            CycleDay = cycleDay;
        }
    }

    public static class DailyQuestSchedule
    {
        public const int CycleLength = 7;

        public static DailyQuestScheduleState Resolve(
            long currentDay,
            long savedQuestDay,
            int savedCycleDay)
        {
            if (currentDay < 0)
                throw new ArgumentOutOfRangeException(nameof(currentDay));

            if (savedQuestDay <= 0 || savedCycleDay < 0)
                return new DailyQuestScheduleState(true, currentDay, 0);

            var safeCycleDay = Math.Clamp(savedCycleDay, 0, CycleLength - 1);
            var daysPassed = currentDay - savedQuestDay;

            if (daysPassed <= 0)
                return new DailyQuestScheduleState(false, savedQuestDay, safeCycleDay);

            var nextCycleDay = daysPassed == 1
                ? (safeCycleDay + 1) % CycleLength
                : 0;
            return new DailyQuestScheduleState(true, currentDay, nextCycleDay);
        }
    }

    public readonly struct WeeklyQuestScheduleState
    {
        public bool ShouldReset { get; }
        public long QuestWeek { get; }

        public WeeklyQuestScheduleState(bool shouldReset, long questWeek)
        {
            ShouldReset = shouldReset;
            QuestWeek = questWeek;
        }
    }

    public static class WeeklyQuestSchedule
    {
        public static long FromMoscowDay(long moscowDay)
        {
            if (moscowDay < 0)
                throw new ArgumentOutOfRangeException(nameof(moscowDay));

            return (moscowDay + 3) / 7;
        }

        public static WeeklyQuestScheduleState Resolve(long currentWeek, long savedQuestWeek)
        {
            if (currentWeek < 0)
                throw new ArgumentOutOfRangeException(nameof(currentWeek));

            if (savedQuestWeek <= 0)
                return new WeeklyQuestScheduleState(true, currentWeek);

            if (currentWeek <= savedQuestWeek)
                return new WeeklyQuestScheduleState(false, savedQuestWeek);

            return new WeeklyQuestScheduleState(true, currentWeek);
        }
    }

    public class QuestService : IInitializable, IDisposable, IGameStartListener
    {
        private const int MaxDailyQuests = 2;
        private const int MaxWeeklyQuests = 2;
        private const int MaxAchievements = 4;
        private const int WeeklyTargetMultiplier = 7;
        private const int AchievementTargetMultiplier = 50;
        private static readonly TimeSpan PersistDelay = TimeSpan.FromSeconds(1);

        private readonly QuestCatalog _questCatalog;
        private readonly IPlayerStatsUseCase _playerStats;
        private readonly IRunSelectionService _runSelection;
        private readonly IWorldService _world;
        private readonly IDailyRewardTimeProvider _timeProvider;
        private readonly ISubscriber<EnemyKilledQuestEventDTO> _enemyKilledSubscriber;
        private readonly ISubscriber<TowerBoughtQuestEventDTO> _towerBoughtSubscriber;
        private readonly ISubscriber<DamageDealtQuestEventDTO> _damageSubscriber;
        private readonly ISubscriber<WaveCompletedQuestEventDTO> _waveSubscriber;
        private readonly IAudioManager _audioManager;

        private readonly List<IQuestRuntime> _quests = new();
        private readonly List<IQuestRuntime> _weeklyQuests = new();
        private readonly List<IQuestRuntime> _achievements = new();
        private readonly List<BaseQuestConfig> _availableConfigs = new();
        private readonly List<BaseQuestConfig> _activeConfigs = new();
        private readonly List<BaseQuestConfig> _weeklyConfigs = new();
        private readonly List<BaseQuestConfig> _achievementConfigs = new();

        private long _dailyQuestDay = -1;
        private int _dailyCycleDay = -1;
        private long _weeklyQuestWeek = -1;
        private bool _isTimeAvailable;
        private CancellationTokenSource _persistCancellation = new();
        private bool _persistScheduled;

        public IReadOnlyList<IQuestRuntime> Quests => _quests;
        public IReadOnlyList<IQuestRuntime> DailyQuests => _quests;
        public IReadOnlyList<IQuestRuntime> WeeklyQuests => _weeklyQuests;
        public IReadOnlyList<IQuestRuntime> Achievements => _achievements;
        public int DailyCycleDay => _dailyCycleDay;
        public bool IsTimeAvailable => _isTimeAvailable;
        public event Action QuestsChanged;

        public QuestService(
            QuestCatalog questCatalog,
            IPlayerStatsUseCase playerStats,
            IRunSelectionService runSelection,
            IWorldService world,
            IDailyRewardTimeProvider timeProvider,
            ISubscriber<EnemyKilledQuestEventDTO> enemyKilledSubscriber,
            ISubscriber<TowerBoughtQuestEventDTO> towerBoughtSubscriber,
            ISubscriber<DamageDealtQuestEventDTO> damageSubscriber,
            ISubscriber<WaveCompletedQuestEventDTO> waveSubscriber,
            IAudioManager audioManager)
        {
            _questCatalog = questCatalog;
            _playerStats = playerStats;
            _runSelection = runSelection;
            _world = world;
            _timeProvider = timeProvider;
            _enemyKilledSubscriber = enemyKilledSubscriber;
            _towerBoughtSubscriber = towerBoughtSubscriber;
            _damageSubscriber = damageSubscriber;
            _waveSubscriber = waveSubscriber;
            _audioManager = audioManager;

            IGameListener.Register(this);
        }

        public void Initialize()
        {
            EnsureAllQuests();
        }

        public void OnStartGame()
        {
            if (EnsureAllQuests())
                QuestsChanged?.Invoke();
        }

        public void EnsureActiveQuests()
        {
            if (EnsureAllQuests())
                QuestsChanged?.Invoke();
        }

        public bool TryClaimReward(IQuestRuntime quest)
        {
            if (EnsureAllQuests())
                QuestsChanged?.Invoke();

            if (quest == null || !ContainsQuest(quest) || !quest.TryClaimReward())
                return false;

            FlushPendingState();
            _audioManager.PlaySound(ESoundId.TakeQuest);
            return true;
        }

        public List<QuestSaveData> CaptureState()
        {
            var result = new List<QuestSaveData>(_quests.Count);

            foreach (var quest in _quests)
            {
                result.Add(new QuestSaveData
                {
                    id = quest.Id,
                    currentValue = quest.CurrentValue,
                    targetValue = quest.TargetValue,
                    rewardGold = quest.RewardGold,
                    rewardGems = quest.RewardGems,
                    isRewardClaimed = quest.IsRewardClaimed
                });
            }

            return result;
        }

        public void RestoreQuests(IReadOnlyList<QuestSaveData> savedQuests)
        {
            if (EnsureAllQuests())
                QuestsChanged?.Invoke();
        }

        private bool EnsureAllQuests()
        {
            var changed = EnsureDailyQuests();
            changed |= EnsureWeeklyQuests();
            changed |= EnsureAchievements();

            if (changed)
                PersistState();

            return changed;
        }

        private bool EnsureDailyQuests()
        {
            LoadAvailableConfigs();
            var previousTimeAvailable = _isTimeAvailable;

            if (!_timeProvider.TryGetCurrentUnixMilliseconds(out var unixMilliseconds))
            {
                _isTimeAvailable = false;
                var restored = RestoreSavedDailyQuestsIfNeeded();
                return restored || previousTimeAvailable != _isTimeAvailable;
            }

            _isTimeAvailable = true;
            var currentDay = DailyRewardDayCalculator.FromUnixMilliseconds(unixMilliseconds);
            var schedule = DailyQuestSchedule.Resolve(
                currentDay,
                _world.DailyQuestDay,
                _world.DailyQuestCycleDay);

            if (schedule.ShouldReset)
            {
                _dailyQuestDay = schedule.QuestDay;
                _dailyCycleDay = schedule.CycleDay;
                ClearActiveQuests();
                FillDailyQuests(schedule.QuestDay);
                return true;
            }

            _dailyQuestDay = schedule.QuestDay;
            _dailyCycleDay = schedule.CycleDay;
            var changed = RestoreSavedDailyQuestsIfNeeded();

            if (_quests.Count == 0)
            {
                FillDailyQuests(schedule.QuestDay);
                changed = true;
            }

            return changed || previousTimeAvailable != _isTimeAvailable;
        }

        private bool EnsureWeeklyQuests()
        {
            if (!_timeProvider.TryGetCurrentUnixMilliseconds(out var unixMilliseconds))
            {
                _weeklyQuestWeek = _world.WeeklyQuestWeek;
                return RestoreSavedCategoryIfNeeded(
                    _world.WeeklyQuests,
                    _weeklyQuests,
                    _weeklyConfigs,
                    EQuestCategory.Weekly);
            }

            var currentDay = DailyRewardDayCalculator.FromUnixMilliseconds(unixMilliseconds);
            var currentWeek = WeeklyQuestSchedule.FromMoscowDay(currentDay);
            var schedule = WeeklyQuestSchedule.Resolve(currentWeek, _world.WeeklyQuestWeek);
            _weeklyQuestWeek = schedule.QuestWeek;

            if (schedule.ShouldReset)
            {
                ClearCategory(_weeklyQuests, _weeklyConfigs);
                FillRandomCategory(
                    _weeklyQuests,
                    _weeklyConfigs,
                    EQuestCategory.Weekly,
                    MaxWeeklyQuests,
                    schedule.QuestWeek,
                    0x5EED);
                return true;
            }

            var changed = RestoreSavedCategoryIfNeeded(
                _world.WeeklyQuests,
                _weeklyQuests,
                _weeklyConfigs,
                EQuestCategory.Weekly);
            if (_weeklyQuests.Count >= MaxWeeklyQuests)
                return changed;

            var countBeforeFill = _weeklyQuests.Count;
            FillRandomCategory(
                _weeklyQuests,
                _weeklyConfigs,
                EQuestCategory.Weekly,
                MaxWeeklyQuests,
                schedule.QuestWeek,
                0x5EED);
            return changed || _weeklyQuests.Count != countBeforeFill;
        }

        private bool EnsureAchievements()
        {
            var changed = RestoreSavedCategoryIfNeeded(
                _world.AchievementQuests,
                _achievements,
                _achievementConfigs,
                EQuestCategory.Achievement);
            if (_achievements.Count >= MaxAchievements)
                return changed;

            var countBeforeFill = _achievements.Count;
            var usedTypes = new HashSet<Type>();
            foreach (var config in _achievementConfigs)
                usedTypes.Add(config.GetType());

            foreach (var config in _availableConfigs)
            {
                if (!usedTypes.Add(config.GetType()))
                    continue;

                AddCategoryQuest(
                    config,
                    _achievements,
                    _achievementConfigs,
                    EQuestCategory.Achievement);
                if (_achievements.Count == MaxAchievements)
                    break;
            }

            return changed || _achievements.Count != countBeforeFill;
        }

        private bool RestoreSavedCategoryIfNeeded(
            IReadOnlyList<QuestSaveData> savedQuests,
            List<IQuestRuntime> destination,
            List<BaseQuestConfig> activeConfigs,
            EQuestCategory category)
        {
            if (destination.Count > 0 || savedQuests.Count == 0)
                return false;

            foreach (var saved in savedQuests)
            {
                var config = FindConfigById(saved.id);
                if (config == null)
                    continue;

                var quest = CreateQuest(
                    config,
                    category,
                    saved.targetValue,
                    saved.rewardGold,
                    saved.rewardGems);
                if (quest == null)
                    continue;

                quest.RestoreState(saved.currentValue, saved.isRewardClaimed);
                AddQuestRuntime(config, quest, destination, activeConfigs);
            }

            return destination.Count > 0;
        }

        private bool RestoreSavedDailyQuestsIfNeeded()
        {
            if (_quests.Count > 0 || _world.DailyQuests.Count == 0)
                return false;

            _dailyQuestDay = _world.DailyQuestDay;
            _dailyCycleDay = _world.DailyQuestCycleDay;

            foreach (var saved in _world.DailyQuests)
            {
                var config = FindConfigById(saved.id);
                if (config == null)
                    continue;

                var quest = CreateQuest(config, saved.targetValue, saved.rewardGold);
                if (quest == null)
                    continue;

                quest.RestoreState(saved.currentValue, saved.isRewardClaimed);
                AddQuestRuntime(config, quest);
            }

            return _quests.Count > 0;
        }

        private void LoadAvailableConfigs()
        {
            _availableConfigs.Clear();

            foreach (var config in _questCatalog.Quests)
            {
                if (config != null)
                    _availableConfigs.Add(config);
            }
        }

        private void FillDailyQuests(long questDay)
        {
            var candidates = new List<BaseQuestConfig>();
            foreach (var config in _availableConfigs)
            {
                if (!_activeConfigs.Contains(config))
                    candidates.Add(config);
            }

            var seed = unchecked((int)(questDay ^ questDay >> 32));
            var random = new Random(seed);

            while (_quests.Count < MaxDailyQuests && candidates.Count > 0)
            {
                var index = random.Next(candidates.Count);
                var config = candidates[index];
                candidates.RemoveAt(index);
                AddQuestFromConfig(config);
            }
        }

        private void FillRandomCategory(
            List<IQuestRuntime> destination,
            List<BaseQuestConfig> activeConfigs,
            EQuestCategory category,
            int count,
            long period,
            int salt)
        {
            var candidates = new List<BaseQuestConfig>();
            foreach (var config in _availableConfigs)
            {
                if (!activeConfigs.Contains(config))
                    candidates.Add(config);
            }
            var seed = unchecked((int)(period ^ period >> 32) ^ salt);
            var random = new Random(seed);

            while (destination.Count < count && candidates.Count > 0)
            {
                var index = random.Next(candidates.Count);
                var config = candidates[index];
                candidates.RemoveAt(index);
                AddCategoryQuest(config, destination, activeConfigs, category);
            }
        }

        private BaseQuestConfig FindConfigById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            foreach (var config in _availableConfigs)
            {
                if (config.Id == id)
                    return config;
            }

            return null;
        }

        private void AddQuestRuntime(BaseQuestConfig config, IQuestRuntime quest)
        {
            AddQuestRuntime(config, quest, _quests, _activeConfigs);
        }

        private void AddQuestRuntime(
            BaseQuestConfig config,
            IQuestRuntime quest,
            List<IQuestRuntime> destination,
            List<BaseQuestConfig> activeConfigs)
        {
            quest.ProgressChanged += OnQuestProgressChanged;
            destination.Add(quest);
            activeConfigs.Add(config);
        }

        private void AddQuestFromConfig(BaseQuestConfig config)
        {
            var quest = CreateQuest(config);
            if (quest != null)
                AddQuestRuntime(config, quest);
        }

        private void AddCategoryQuest(
            BaseQuestConfig config,
            List<IQuestRuntime> destination,
            List<BaseQuestConfig> activeConfigs,
            EQuestCategory category)
        {
            var rewardGems = category switch
            {
                EQuestCategory.Weekly => Math.Max(5, config.RewardGold / 10),
                EQuestCategory.Achievement => Math.Max(25, config.RewardGold / 2),
                _ => 0
            };
            var quest = CreateQuest(
                config,
                category,
                GetCategoryTargetValue(config, category),
                0,
                rewardGems);
            if (quest != null)
                AddQuestRuntime(config, quest, destination, activeConfigs);
        }

        private IQuestRuntime CreateQuest(BaseQuestConfig config)
        {
            return CreateQuest(
                config,
                GetScaledTargetValue(config),
                GetScaledRewardGold(config));
        }

        private IQuestRuntime CreateQuest(BaseQuestConfig config, int targetValue, int rewardGold)
        {
            return CreateQuest(
                config,
                EQuestCategory.Daily,
                targetValue,
                rewardGold,
                0);
        }

        private IQuestRuntime CreateQuest(
            BaseQuestConfig config,
            EQuestCategory category,
            int targetValue,
            int rewardGold,
            int rewardGems)
        {
            if (config is KillEnemyQuestConfig killEnemyConfig)
            {
                return new KillEnemyQuest(
                    killEnemyConfig,
                    _playerStats,
                    _world,
                    _enemyKilledSubscriber,
                    category,
                    targetValue,
                    rewardGold,
                    rewardGems);
            }

            if (config is BuyTowerQuestConfig buyTowerConfig)
            {
                return new BuyTowerQuest(
                    buyTowerConfig,
                    _playerStats,
                    _world,
                    _towerBoughtSubscriber,
                    category,
                    targetValue,
                    rewardGold,
                    rewardGems);
            }

            if (config is DealDamageQuestConfig dealDamageConfig)
            {
                return new DealDamageQuest(
                    dealDamageConfig,
                    _playerStats,
                    _world,
                    _damageSubscriber,
                    category,
                    targetValue,
                    rewardGold,
                    rewardGems);
            }

            if (config is CompleteWaveQuestConfig completeWaveConfig)
            {
                return new CompleteWaveQuest(
                    completeWaveConfig,
                    _playerStats,
                    _world,
                    _waveSubscriber,
                    category,
                    targetValue,
                    rewardGold,
                    rewardGems);
            }

            return null;
        }

        private int GetCategoryTargetValue(BaseQuestConfig config, EQuestCategory category)
        {
            var multiplier = category switch
            {
                EQuestCategory.Weekly => WeeklyTargetMultiplier,
                EQuestCategory.Achievement => AchievementTargetMultiplier,
                _ => 1
            };
            var target = (long)Math.Max(1, config.TargetValue) * multiplier;
            return target >= int.MaxValue ? int.MaxValue : (int)target;
        }

        private int GetScaledTargetValue(BaseQuestConfig config)
        {
            var baseTargetValue = Math.Max(1, config.TargetValue);
            var wave = GetCurrentWave();
            var scalePerWave = Math.Max(0f, config.TargetScalePerWave);
            var scale = 1f + (wave - 1) * scalePerWave;
            var targetValue = Math.Max(1, (int)Math.Ceiling(baseTargetValue * scale));

            if (config is CompleteWaveQuestConfig)
                targetValue = Math.Min(targetValue, GetRemainingWavesCount(wave));

            return Math.Max(1, targetValue);
        }

        private int GetScaledRewardGold(BaseQuestConfig config)
        {
            var baseRewardGold = Math.Max(0, config.RewardGold);
            if (baseRewardGold <= 0)
                return 0;

            var wave = GetCurrentWave();
            var scalePerWave = Math.Max(0f, config.RewardScalePerWave);
            var scale = 1f + (wave - 1) * scalePerWave;
            return Math.Max(1, (int)Math.Round(baseRewardGold * scale));
        }

        private int GetCurrentWave()
        {
            return Math.Max(1, _playerStats.Wave);
        }

        private int GetRemainingWavesCount(int currentWave)
        {
            var wavesCount = Math.Max(1, _runSelection.SelectedRun.Waves.Count);
            return Math.Max(1, wavesCount - currentWave + 1);
        }

        private void OnQuestProgressChanged()
        {
            SchedulePersist();
            QuestsChanged?.Invoke();
        }

        private void SchedulePersist()
        {
            if (_persistScheduled)
                return;

            _persistScheduled = true;
            PersistAfterDelayAsync(_persistCancellation.Token).Forget();
        }

        private async UniTaskVoid PersistAfterDelayAsync(CancellationToken cancellationToken)
        {
            var canceled = await UniTask.Delay(
                    PersistDelay,
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken)
                .SuppressCancellationThrow();
            if (canceled)
                return;

            _persistScheduled = false;
            PersistState();
        }

        private void FlushPendingState()
        {
            _persistCancellation.Cancel();
            _persistCancellation.Dispose();
            _persistCancellation = new CancellationTokenSource();
            _persistScheduled = false;
            PersistState();
        }

        private void PersistState()
        {
            _world.SaveQuests(
                _dailyQuestDay,
                _dailyCycleDay,
                CaptureState(_quests),
                _weeklyQuestWeek,
                CaptureState(_weeklyQuests),
                CaptureState(_achievements));
        }

        private static List<QuestSaveData> CaptureState(IReadOnlyList<IQuestRuntime> quests)
        {
            var result = new List<QuestSaveData>(quests.Count);
            foreach (var quest in quests)
            {
                result.Add(new QuestSaveData
                {
                    id = quest.Id,
                    currentValue = quest.CurrentValue,
                    targetValue = quest.TargetValue,
                    rewardGold = quest.RewardGold,
                    rewardGems = quest.RewardGems,
                    isRewardClaimed = quest.IsRewardClaimed
                });
            }

            return result;
        }

        private bool ContainsQuest(IQuestRuntime quest)
        {
            return _quests.Contains(quest)
                   || _weeklyQuests.Contains(quest)
                   || _achievements.Contains(quest);
        }

        private void ClearActiveQuests()
        {
            foreach (var quest in _quests)
            {
                quest.ProgressChanged -= OnQuestProgressChanged;
                quest.Dispose();
            }

            _quests.Clear();
            _activeConfigs.Clear();
        }

        private void ClearCategory(
            List<IQuestRuntime> quests,
            List<BaseQuestConfig> activeConfigs)
        {
            foreach (var quest in quests)
            {
                quest.ProgressChanged -= OnQuestProgressChanged;
                quest.Dispose();
            }

            quests.Clear();
            activeConfigs.Clear();
        }

        public void Dispose()
        {
            IGameListener.Unregister(this);
            FlushPendingState();
            _persistCancellation.Cancel();
            _persistCancellation.Dispose();
            ClearActiveQuests();
            ClearCategory(_weeklyQuests, _weeklyConfigs);
            ClearCategory(_achievements, _achievementConfigs);
        }
    }
}
