using System;
using MessagePipe;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.System.UseCases;

namespace Project.Scripts.Gameplay.Quests
{
    public class KillEnemyQuest : QuestRuntimeBase<KillEnemyQuestConfig>
    {
        private readonly IDisposable _subscription;

        public KillEnemyQuest(
            KillEnemyQuestConfig config,
            IPlayerStatsUseCase playerStats,
            ISubscriber<EnemyKilledQuestEventDTO> subscriber,
            int targetValue,
            int rewardGold) : base(config, playerStats, targetValue, rewardGold)
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