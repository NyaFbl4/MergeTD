using System;
using MessagePipe;
using Project.Scripts.Gameplay.Enemies;
using Project.Scripts.Gameplay.QuestEvents;
using VContainer.Unity;

namespace Project.Scripts.System.UseCases
{
    public class EnemyDeathUseCase : IInitializable, IDisposable
    {
        private readonly IPublisher<EnemyKilledQuestEventDTO> _publisherEnemyKilledDTO;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;

        public EnemyDeathUseCase(IPlayerStatsUseCase playerStatsUseCase, IPublisher<EnemyKilledQuestEventDTO>  playerKilledDTO)
        {
            _playerStatsUseCase = playerStatsUseCase;
            _publisherEnemyKilledDTO = playerKilledDTO;
        }

        public void Initialize()
        {
            EnemyUnit.DieEnemy += OnEnemyDie;
        }

        public void Dispose()
        {
            EnemyUnit.DieEnemy -= OnEnemyDie;
        }

        private void OnEnemyDie(EnemyUnit enemy, int rewardGold)
        {
            _playerStatsUseCase.AddGold(rewardGold);
            _publisherEnemyKilledDTO.Publish(new EnemyKilledQuestEventDTO(
                enemy.EnemyType, 
                rewardGold));
        }
    }
}