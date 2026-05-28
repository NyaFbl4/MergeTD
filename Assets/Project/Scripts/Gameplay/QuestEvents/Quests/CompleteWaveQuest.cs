using System;
using MessagePipe;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.System.UseCases;

namespace Project.Scripts.Gameplay.Quests
{
    public class CompleteWaveQuest : QuestRuntimeBase<CompleteWaveQuestConfig>
    {
        private readonly IDisposable _subscription;

        public CompleteWaveQuest(
            CompleteWaveQuestConfig config,
            IPlayerStatsUseCase playerStats,
            ISubscriber<WaveCompletedQuestEventDTO> subscriber) : base(config, playerStats)
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