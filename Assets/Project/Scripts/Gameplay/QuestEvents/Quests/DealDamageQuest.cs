using System;
using MessagePipe;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.System.UseCases;
using Project.Scripts.System.Save;

namespace Project.Scripts.Gameplay.Quests
{
    public class DealDamageQuest : QuestRuntimeBase<DealDamageQuestConfig>
    {
        private readonly IDisposable _subscription;

        public DealDamageQuest(
            DealDamageQuestConfig config,
            IPlayerStatsUseCase playerStats,
            IWorldService world,
            ISubscriber<DamageDealtQuestEventDTO> subscriber,
            EQuestCategory category,
            int targetValue,
            int rewardGold,
            int rewardGems) : base(config, playerStats, world, category, targetValue, rewardGold, rewardGems)
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
