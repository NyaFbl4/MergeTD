using System;
using System.Collections.Generic;
using MessagePipe;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.System.Audio;
using Project.Scripts.System.UseCases;
using VContainer.Unity;

namespace Project.Scripts.Gameplay.Quests
{
    public class QuestService : IInitializable, IDisposable
    {
        private readonly QuestCatalog _questCatalog;
        private readonly IPlayerStatsUseCase _playerStats;
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
            ISubscriber<EnemyKilledQuestEventDTO> enemyKilledSubscriber,
            ISubscriber<TowerBoughtQuestEventDTO> towerBoughtSubscriber,
            ISubscriber<DamageDealtQuestEventDTO> damageSubscriber,
            ISubscriber<WaveCompletedQuestEventDTO> waveSubscriber,
            IAudioManager audioManager)
        {
            _questCatalog = questCatalog;
            _playerStats = playerStats;
            _enemyKilledSubscriber = enemyKilledSubscriber;
            _towerBoughtSubscriber = towerBoughtSubscriber;
            _damageSubscriber = damageSubscriber;
            _waveSubscriber = waveSubscriber;
            _audioManager = audioManager;
        }

        public void Initialize()
        {
            _availableConfigs.Clear();
            _activeConfigs.Clear();
            _quests.Clear();

            foreach (var config in _questCatalog.Quests)
            {
                if (config == null)
                    continue;

                _availableConfigs.Add(config);
            }

            FillActiveQuests();

            QuestsChanged?.Invoke();
        }
        
        public bool TryClaimReward(IQuestRuntime quest)
        {
            if (quest == null)
                return false;

            if (!quest.TryClaimReward())
                return false;

            _audioManager.PlaySfx(ESoundId.TakeQuest);
            RemoveQuest(quest);
            FillActiveQuests();
            QuestsChanged?.Invoke();
            return true;
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
            if (config is KillEnemyQuestConfig killEnemyConfig)
                return new KillEnemyQuest(killEnemyConfig, _playerStats, _enemyKilledSubscriber);

            if (config is BuyTowerQuestConfig buyTowerConfig)
                return new BuyTowerQuest(buyTowerConfig, _playerStats, _towerBoughtSubscriber);

            if (config is DealDamageQuestConfig dealDamageConfig)
                return new DealDamageQuest(dealDamageConfig, _playerStats, _damageSubscriber);

            if (config is CompleteWaveQuestConfig completeWaveConfig)
                return new CompleteWaveQuest(completeWaveConfig, _playerStats, _waveSubscriber);

            return null;
        }

        private void OnQuestProgressChanged()
        {
            QuestsChanged?.Invoke();
        }

        public void Dispose()
        {
            foreach (var quest in _quests)
            {
                quest.ProgressChanged -= OnQuestProgressChanged;
                quest.Dispose();
            }

            _quests.Clear();
        }
    }
}
