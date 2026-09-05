using System;
using MessagePipe;
using Project.Scripts.Gameplay.QuestEvents;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.System;
using VContainer.Unity;

namespace Project.Scripts.System.UseCases
{
    public class QuestWaveEventsUseCase : IInitializable, IDisposable
    {
        private readonly RunBattleRuntime _runBattleRuntime;
        private readonly IPublisher<WaveCompletedQuestEventDTO> _waveCompletedPublisher;

        public QuestWaveEventsUseCase(
            RunBattleRuntime runBattleRuntime,
            IPublisher<WaveCompletedQuestEventDTO> waveCompletedPublisher)
        {
            _runBattleRuntime = runBattleRuntime;
            _waveCompletedPublisher = waveCompletedPublisher;
        }

        public void Initialize()
        {
            _runBattleRuntime.WaveCompleted += OnWaveCompleted;
        }

        public void Dispose()
        {
            _runBattleRuntime.WaveCompleted -= OnWaveCompleted;
        }

        private void OnWaveCompleted(int waveNumber, int rewardCount, ERunPhase phaseAfterComplete)
        {
            _waveCompletedPublisher.Publish(new WaveCompletedQuestEventDTO(
                waveNumber,
                phaseAfterComplete == ERunPhase.Victory));
        }
    }
}
