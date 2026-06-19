using System;
using System.Collections.Generic;
using MessagePipe;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.System.Audio;
using Project.Scripts.System.UseCases;
using VContainer.Unity;

namespace Project.Scripts.Gameplay.Quests
{
    public class QuestService : IInitializable, IDisposable, IGameStartListener
    {
        private readonly QuestCatalog _questCatalog;
        private readonly IPlayerStatsUseCase _playerStats;
        private readonly LevelConfig _levelConfig;
        private readonly ISubscriber<EnemyKilledQuestEventDTO> _enemyKilledSubscriber;
        private readonly ISubscriber<TowerBoughtQuestEventDTO> _towerBoughtSubscriber;
        private readonly ISubscriber<DamageDealtQuestEventDTO> _damageSubscriber;
        private readonly ISubscriber<WaveCompletedQuestEventDTO> _waveSubscriber;
        private readonly IAudioManager _audioManager;

        private readonly List<IQuestRuntime> _quests = new();
        private readonly List<BaseQuestConfig> _availableConfigs = new();
        private readonly List<BaseQuestConfig> _activeConfigs = new();

        private const int MaxActiveQuests = 4;
        private readonly Random _random = new();

        public IReadOnlyList<IQuestRuntime> Quests => _quests;
        public event Action QuestsChanged;

        public QuestService(
            QuestCatalog questCatalog,
            IPlayerStatsUseCase playerStats,
            LevelConfig levelConfig,
            ISubscriber<EnemyKilledQuestEventDTO> enemyKilledSubscriber,
            ISubscriber<TowerBoughtQuestEventDTO> towerBoughtSubscriber,
            ISubscriber<DamageDealtQuestEventDTO> damageSubscriber,
            ISubscriber<WaveCompletedQuestEventDTO> waveSubscriber,
            IAudioManager audioManager)
        {
            _questCatalog = questCatalog;
            _playerStats = playerStats;
            _levelConfig = levelConfig;
            _enemyKilledSubscriber = enemyKilledSubscriber;
            _towerBoughtSubscriber = towerBoughtSubscriber;
            _damageSubscriber = damageSubscriber;
            _waveSubscriber = waveSubscriber;
            _audioManager = audioManager;

            IGameListener.Register(this);
        }

        public void Initialize()
        {
            LoadAvailableConfigs();
        }

        public void OnStartGame()
        {
            LoadAvailableConfigs();
            ClearActiveQuests();
            FillActiveQuests();
            QuestsChanged?.Invoke();
        }

        public void EnsureActiveQuests()
        {
            LoadAvailableConfigs();

            var previousCount = _quests.Count;
            FillActiveQuests();

            if (_quests.Count != previousCount)
                QuestsChanged?.Invoke();
        }

        public bool TryClaimReward(IQuestRuntime quest)
        {
            if (quest == null)
                return false;

            if (!quest.TryClaimReward())
                return false;

            _audioManager.PlaySound(ESoundId.TakeQuest);
            RemoveQuest(quest);
            FillActiveQuests();
            QuestsChanged?.Invoke();
            return true;
        }

        private void LoadAvailableConfigs()
        {
            _availableConfigs.Clear();

            if (_questCatalog?.Quests == null)
                return;

            foreach (var config in _questCatalog.Quests)
            {
                if (config == null)
                    continue;

                _availableConfigs.Add(config);
            }
        }

        private void FillActiveQuests()
        {
            while (_quests.Count < MaxActiveQuests)
            {
                var nextConfig = GetRandomAvailableConfig();
                if (nextConfig == null)
                    break;

                AddQuestFromConfig(nextConfig);
            }
        }

        private void RemoveQuest(IQuestRuntime quest)
        {
            var index = _quests.IndexOf(quest);
            if (index < 0)
                return;

            quest.ProgressChanged -= OnQuestProgressChanged;
            quest.Dispose();

            _quests.RemoveAt(index);
            _activeConfigs.RemoveAt(index);
        }

        private BaseQuestConfig GetRandomAvailableConfig()
        {
            var candidates = new List<BaseQuestConfig>();

            foreach (var config in _availableConfigs)
            {
                if (config == null)
                    continue;

                if (_activeConfigs.Contains(config))
                    continue;

                candidates.Add(config);
            }

            if (candidates.Count == 0)
                return null;

            var index = _random.Next(0, candidates.Count);
            return candidates[index];
        }

        private void AddQuestFromConfig(BaseQuestConfig config)
        {
            var quest = CreateQuest(config);
            if (quest == null)
                return;

            quest.ProgressChanged += OnQuestProgressChanged;
            _quests.Add(quest);
            _activeConfigs.Add(config);
        }

        private IQuestRuntime CreateQuest(BaseQuestConfig config)
        {
            var targetValue = GetScaledTargetValue(config);
            var rewardGold = GetScaledRewardGold(config);

            if (config is KillEnemyQuestConfig killEnemyConfig)
                return new KillEnemyQuest(killEnemyConfig, _playerStats, _enemyKilledSubscriber, targetValue, rewardGold);

            if (config is BuyTowerQuestConfig buyTowerConfig)
                return new BuyTowerQuest(buyTowerConfig, _playerStats, _towerBoughtSubscriber, targetValue, rewardGold);

            if (config is DealDamageQuestConfig dealDamageConfig)
                return new DealDamageQuest(dealDamageConfig, _playerStats, _damageSubscriber, targetValue, rewardGold);

            if (config is CompleteWaveQuestConfig completeWaveConfig)
                return new CompleteWaveQuest(completeWaveConfig, _playerStats, _waveSubscriber, targetValue, rewardGold);

            return null;
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
            var wavesCount = _levelConfig?.Waves == null ? currentWave : Math.Max(1, _levelConfig.Waves.Count);
            return Math.Max(1, wavesCount - currentWave + 1);
        }

        private void OnQuestProgressChanged()
        {
            QuestsChanged?.Invoke();
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

        public void Dispose()
        {
            IGameListener.Unregister(this);
            ClearActiveQuests();
        }
    }
}