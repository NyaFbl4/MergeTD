using System;
using MessagePipe;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.System.UseCases;
using Project.Scripts.System.Save;

namespace Project.Scripts.Gameplay.Quests
{
    public class CompleteWaveQuest : QuestRuntimeBase<CompleteWaveQuestConfig>
    {
        private readonly IDisposable _subscription;

        public CompleteWaveQuest(
            CompleteWaveQuestConfig config,
            IPlayerStatsUseCase playerStats,
            IWorldService world,
            ISubscriber<WaveCompletedQuestEventDTO> subscriber,
            EQuestCategory category,
            int targetValue,
            int rewardGold,
            int rewardGems) : base(config, playerStats, world, category, targetValue, rewardGold, rewardGems)
        {
            _subscription = subscriber.Subscribe(OnWaveCompleted);
        }

        private void OnWaveCompleted(WaveCompletedQuestEventDTO dto)
        {
            if (_config.OnlyLastWave && !dto.IsLastWave)
                return;

            AddProgress(1);
        }

        public override void Dispose()
        {
            _subscription.Dispose();
        }
    }
}
