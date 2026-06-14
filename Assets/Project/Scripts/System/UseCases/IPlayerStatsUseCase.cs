using System;
using System.Collections.Generic;
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
        IReadOnlyDictionary<string, int> UpgradeLevels { get; }
        
        event Action<int> OnGoldChanged;
        event Action<int> WaveChanged;
        event Action<int> SelectedTowerLevelChanged;
        event Action UpgradesChanged;
        
        bool CanSpend(int amount);
        bool TrySpend(int amount);
        void AddGold(int amount);
        void SetWave(int amount);
        void SetSelectedTowerLevel(int level);
        void ResetState();
        void ApplyState(
            int gold,
            int wave,
            int selectedTowerLevel,
            float towerDamageBonus,
            float towerAttackSpeedBonus,
            float towerCritChanceBonus,
            float towerCritDamageBonus,
            IReadOnlyList<UpgradeLevelSaveData> upgradeLevels);
        
        int GetUpgradeLevel(string upgradeId);
        void SetUpgradeLevel(string upgradeId, int level);
        void AddTowerCritChanceBonus(float value);
        void AddTowerDamageBonus(float value);
        void AddTowerAttackSpeedBonus(float value);
        void AddTowerCritDamageBonus(float value);
    }
}