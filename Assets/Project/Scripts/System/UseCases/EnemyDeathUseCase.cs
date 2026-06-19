using System;
using MessagePipe;
using Project.Scripts.Gameplay.Enemies;
using Project.Scripts.Gameplay.QuestEvents;
using VContainer.Unity;

namespace Project.Scripts.System.UseCases
{
    public class EnemyDeathUseCase : IInitializable, IDisposable
    {
        private const int MoneyBagRewardMultiplier = 5;

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
            var finalRewardGold = enemy.EnemyType == EEnemyType.MoneyBag
                ? rewardGold * MoneyBagRewardMultiplier
                : rewardGold;

            _playerStatsUseCase.AddGold(finalRewardGold);
            _publisherEnemyKilledDTO.Publish(new EnemyKilledQuestEventDTO(
                enemy.EnemyType,
                finalRewardGold));
        }
    }
}