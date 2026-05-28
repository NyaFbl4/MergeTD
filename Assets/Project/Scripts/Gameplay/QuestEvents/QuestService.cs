using System;
using System.Collections.Generic;
using MessagePipe;
using Project.Scripts.Gameplay.QuestEvents;
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

        private readonly List<IQuestRuntime> _quests = new();

        public IReadOnlyList<IQuestRuntime> Quests => _quests;
        public event Action QuestsChanged;

        public QuestService(
            QuestCatalog questCatalog,
            IPlayerStatsUseCase playerStats,
            ISubscriber<EnemyKilledQuestEventDTO> enemyKilledSubscriber,
            ISubscriber<TowerBoughtQuestEventDTO> towerBoughtSubscriber,
            ISubscriber<DamageDealtQuestEventDTO> damageSubscriber,
            ISubscriber<WaveCompletedQuestEventDTO> waveSubscriber)
        {
            _questCatalog = questCatalog;
            _playerStats = playerStats;
            _enemyKilledSubscriber = enemyKilledSubscriber;
            _towerBoughtSubscriber = towerBoughtSubscriber;
            _damageSubscriber = damageSubscriber;
            _waveSubscriber = waveSubscriber;
        }

        public void Initialize()
        {
            foreach (var config in _questCatalog.Quests)
            {
                var quest = CreateQuest(config);
                if (quest == null)
                    continue;

                quest.ProgressChanged += OnQuestProgressChanged;
                _quests.Add(quest);
            }

            QuestsChanged?.Invoke();
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