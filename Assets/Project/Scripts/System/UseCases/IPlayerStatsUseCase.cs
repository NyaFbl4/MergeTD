using System;

namespace Project.Scripts.System.UseCases
{
    public interface IPlayerStatsUseCase
    {
        int Gold { get; }
        int Wave  { get; }
        int SelectedTowerLevel { get; }

        float TowerDamageBonus { get; }
        float TowerAttackSpeedBonus { get; }
        
        event Action<int> OnGoldChanged;
        event Action<int> WaveChanged;
        event Action<int> SelectedTowerLevelChanged;
        event Action UpgradesChanged;
        
        bool CanSpend(int amount);
        bool TrySpend(int amount);
        void AddGold(int amount);
        void SetWave(int amount);
        void SetSelectedTowerLevel(int level);
        
        int GetUpgradeLevel(string upgradeId);
        void SetUpgradeLevel(string upgradeId, int level);

        void AddTowerDamageBonus(float value);
        void AddTowerAttackSpeedBonus(float value);
    }
}