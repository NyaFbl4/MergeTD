using System;
using MessagePipe;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.Gameplay.Systems;
using Project.Scripts.System;
using VContainer.Unity;

namespace Project.Scripts.System.UseCases
{
    public class QuestWaveEventsUseCase : IInitializable, IDisposable
    {
        private readonly BattlefieldRuntime _battlefieldRuntime;
        private readonly IPublisher<WaveCompletedQuestEventDTO> _waveCompletedPublisher;

        public QuestWaveEventsUseCase(
            BattlefieldRuntime battlefieldRuntime,
            IPublisher<WaveCompletedQuestEventDTO> waveCompletedPublisher)
        {
            _battlefieldRuntime = battlefieldRuntime;
            _waveCompletedPublisher = waveCompletedPublisher;
        }

        public void Initialize()
        {
            _battlefieldRuntime.WaveCompleted += OnWaveCompleted;
        }

        public void Dispose()
        {
            _battlefieldRuntime.WaveCompleted -= OnWaveCompleted;
        }

        private void OnWaveCompleted(int waveNumber, int rewardCount, bool isLastWave)
        {
            _waveCompletedPublisher.Publish(new WaveCompletedQuestEventDTO(
                waveNumber,
                isLastWave));
        }
    }
}