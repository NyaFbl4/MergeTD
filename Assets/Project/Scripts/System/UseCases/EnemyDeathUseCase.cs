using System;
using Project.Scripts.Gameplay.Enemies;
using VContainer.Unity;

namespace Project.Scripts.System.UseCases
{
    public class EnemyDeathUseCase : IInitializable, IDisposable
    {
        private readonly IPlayerStatsUseCase _playerStatsUseCase;

        public EnemyDeathUseCase(IPlayerStatsUseCase playerStatsUseCase)
        {
            _playerStatsUseCase = playerStatsUseCase;
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
        }
    }
}