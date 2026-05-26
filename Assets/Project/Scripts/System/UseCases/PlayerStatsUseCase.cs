using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using VContainer.Unity;

namespace Project.Scripts.System.UseCases
{
    public class PlayerStatsUseCase : IPlayerStatsUseCase, IInitializable, IDisposable
    {
        private readonly LevelConfig _levelConfig;

        private int _gold;
        private int _currentWave;
        private int _selectedTowerLevel;

        private float _towerDamageBonus;
        private float _towerAttackSpeedBonus;

        private readonly Dictionary<string, int> _upgradeLevels = new();

        public int Gold => _gold;
        public int Wave => _currentWave;
        public int SelectedTowerLevel => _selectedTowerLevel;

        public float TowerDamageBonus => _towerDamageBonus;
        public float TowerAttackSpeedBonus => _towerAttackSpeedBonus;

        public event Action<int> OnGoldChanged;
        public event Action<int> WaveChanged;
        public event Action<int> SelectedTowerLevelChanged;
        public event Action UpgradesChanged;

        public PlayerStatsUseCase(LevelConfig levelConfig)
        {
            _levelConfig = levelConfig;
            _gold = _levelConfig.StartGold;
            _selectedTowerLevel = Math.Max(1, _levelConfig.LevelTowerSelected);
            _currentWave = 1;
        }

        public void Initialize()
        {
            OnGoldChanged?.Invoke(_gold);
            SelectedTowerLevelChanged?.Invoke(_selectedTowerLevel);
            WaveChanged?.Invoke(_currentWave);
            UpgradesChanged?.Invoke();
        }

        public int GetUpgradeLevel(string upgradeId)
        {
            return _upgradeLevels.TryGetValue(upgradeId, out var level) ? level : 0;
        }

        public void SetUpgradeLevel(string upgradeId, int level)
        {
            _upgradeLevels[upgradeId] = level;
            UpgradesChanged?.Invoke();
        }

        public void AddTowerDamageBonus(float value)
        {
            _towerDamageBonus += value;
        }

        public void AddTowerAttackSpeedBonus(float value)
        {
            _towerAttackSpeedBonus += value;
        }

        public void SetSelectedTowerLevel(int level)
        {
            var safeLevel = Math.Max(1, level);
            if (_selectedTowerLevel == safeLevel)
                return;

            _selectedTowerLevel = safeLevel;
            SelectedTowerLevelChanged?.Invoke(_selectedTowerLevel);
        }

        public bool CanSpend(int amount) => amount > 0 && _gold >= amount;

        public bool TrySpend(int amount)
        {
            if (!CanSpend(amount))
                return false;

            _gold -= amount;
            OnGoldChanged?.Invoke(_gold);
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            _gold += amount;
            OnGoldChanged?.Invoke(_gold);
        }

        public void SetWave(int amount)
        {
            var safeWave = Math.Max(1, amount);
            if (_currentWave == safeWave)
                return;

            _currentWave = safeWave;
            WaveChanged?.Invoke(_currentWave);
        }

        public void Dispose()
        {
        }
    }
}