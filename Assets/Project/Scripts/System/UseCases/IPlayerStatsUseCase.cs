using System;

namespace Project.Scripts.System.UseCases
{
    public interface IPlayerStatsUseCase
    {
        int Gold { get; }
        int Wave  { get; }
        int SelectedTowerLevel { get; }

        event Action<int> OnGoldChanged;
        event Action<int> WaveChanged;
        event Action<int> SelectedTowerLevelChanged;
        
        bool CanSpend(int amount);
        bool TrySpend(int amount);
        void AddGold(int amount);
        void SetWave(int amount);
        void SetSelectedTowerLevel(int level);
    }
}