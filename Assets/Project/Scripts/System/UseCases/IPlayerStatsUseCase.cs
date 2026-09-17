using System;
using Project.Scripts.System.Save;

namespace Project.Scripts.System.UseCases
{
    public interface IPlayerStatsUseCase
    {
        int Gold { get; }
        int Wave  { get; }
        int SelectedTowerLevel { get; }
        float TowerCritChanceBonus { get; }
        float TowerDamageBonus { get; }
        float TowerAttackSpeedBonus { get; }
        float TowerCritDamageBonus { get; }
        
        event Action<int> OnGoldChanged;
        event Action<int> WaveChanged;
        event Action<int> SelectedTowerLevelChanged;
        event Action UpgradesChanged;
        
        bool CanSpend(int amount);
        bool TrySpend(int amount);
        void AddGold(int amount);
        void SetWave(int amount);
        void ResetState();
        void ApplyState(int wave);
        
        int GetUpgradeLevel(string upgradeId);
        bool TryPurchaseUpgrade(string upgradeId, int expectedLevel, int price, EWorldUpgradeType type, float value);
    }
}
