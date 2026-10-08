using System;
using MessagePipe;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.System.UseCases;
using Project.Scripts.System.Save;

namespace Project.Scripts.Gameplay.Quests
{
    public class KillEnemyQuest : QuestRuntimeBase<KillEnemyQuestConfig>
    {
        private readonly IDisposable _subscription;

        public KillEnemyQuest(
            KillEnemyQuestConfig config,
            IPlayerStatsUseCase playerStats,
            IWorldService world,
            ISubscriber<EnemyKilledQuestEventDTO> subscriber,
            EQuestCategory category,
            int targetValue,
            int rewardGold,
            int rewardGems) : base(config, playerStats, world, category, targetValue, rewardGold, rewardGems)
        {
            _subscription = subscriber.Subscribe(OnEnemyKilled);
        }

        private void OnEnemyKilled(EnemyKilledQuestEventDTO dto)
        {
            /*if (dto.EnemyType != _config.EnemyType)
                return;*/

            AddProgress(1);
        }

        public override void Dispose()
        {
            _subscription.Dispose();
        }
    }
}
