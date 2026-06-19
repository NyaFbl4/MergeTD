using System;
using MessagePipe;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.System.UseCases;

namespace Project.Scripts.Gameplay.Quests
{
    public class DealDamageQuest : QuestRuntimeBase<DealDamageQuestConfig>
    {
        private readonly IDisposable _subscription;

        public DealDamageQuest(
            DealDamageQuestConfig config,
            IPlayerStatsUseCase playerStats,
            ISubscriber<DamageDealtQuestEventDTO> subscriber,
            int targetValue,
            int rewardGold) : base(config, playerStats, targetValue, rewardGold)
        {
            _subscription = subscriber.Subscribe(OnDamageDealt);
        }

        private void OnDamageDealt(DamageDealtQuestEventDTO dto)
        {
            if (_config.OnlyCritical && !dto.IsCritical)
                return;

            if (_config.FilterByEnemyType && dto.EnemyType != _config.EnemyType)
                return;

            AddProgress(dto.Damage);
        }

        public override void Dispose()
        {
            _subscription.Dispose();
        }
    }
}