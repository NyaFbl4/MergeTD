using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.System.Save;
using UnityEngine;
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
        private float _towerCritChanceBonus;
        private float _towerCritDamageBonus;

        private readonly Dictionary<string, int> _upgradeLevels = new();

        public int Gold => _gold;
        public int Wave => _currentWave;
        public int SelectedTowerLevel => _selectedTowerLevel;
        public float TowerCritChanceBonus => _towerCritChanceBonus;
        public float TowerDamageBonus => _towerDamageBonus;
        public float TowerAttackSpeedBonus => _towerAttackSpeedBonus;
        public float TowerCritDamageBonus => _towerCritDamageBonus;
        public IReadOnlyDictionary<string, int> UpgradeLevels => _upgradeLevels;

        public event Action<int> OnGoldChanged;
        public event Action<int> WaveChanged;
        public event Action<int> SelectedTowerLevelChanged;
        public event Action UpgradesChanged;

        public PlayerStatsUseCase(LevelConfig levelConfig)
        {
            _levelConfig = levelConfig;
            ResetValues();
        }

        public void Initialize()
        {
            NotifyStateChanged();
        }

        public void ResetState()
        {
            ResetValues();
            NotifyStateChanged();
        }

        public void ApplyState(
            int gold,
            int wave,
            int selectedTowerLevel,
            float towerDamageBonus,
            float towerAttackSpeedBonus,
            float towerCritChanceBonus,
            float towerCritDamageBonus,
            IReadOnlyList<UpgradeLevelSaveData> upgradeLevels)
        {
            _gold = Math.Max(0, gold);
            _currentWave = Math.Max(1, wave);
            _selectedTowerLevel = Math.Max(1, selectedTowerLevel);
            _towerDamageBonus = Mathf.Max(0f, towerDamageBonus);
            _towerAttackSpeedBonus = Mathf.Max(0f, towerAttackSpeedBonus);
            _towerCritChanceBonus = Mathf.Clamp01(towerCritChanceBonus);
            _towerCritDamageBonus = Mathf.Max(0f, towerCritDamageBonus);

            _upgradeLevels.Clear();
            if (upgradeLevels != null)
            {
                for (var i = 0; i < upgradeLevels.Count; i++)
                {
                    var upgrade = upgradeLevels[i];
                    if (upgrade == null || string.IsNullOrWhiteSpace(upgrade.id) || upgrade.level <= 0)
                        continue;

                    _upgradeLevels[upgrade.id] = upgrade.level;
                }
            }

            NotifyStateChanged();
        }

        public int GetUpgradeLevel(string upgradeId)
        {
            return _upgradeLevels.TryGetValue(upgradeId, out var level) ? level : 0;
        }

        public void SetUpgradeLevel(string upgradeId, int level)
        {
            if (string.IsNullOrWhiteSpace(upgradeId))
                return;

            if (level <= 0)
                _upgradeLevels.Remove(upgradeId);
            else
                _upgradeLevels[upgradeId] = level;

            UpgradesChanged?.Invoke();
        }

        public void AddTowerCritDamageBonus(float value)
        {
            _towerCritDamageBonus += value;
        }
        
        public void AddTowerCritChanceBonus(float value)
        {
            _towerCritChanceBonus = Mathf.Clamp01(_towerCritChanceBonus + value);
        }
        
        public void AddTowerDamageBonus(float value)
        {
            _towerDamageBonus = Mathf.Clamp01(_towerDamageBonus  + value);
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

        private void ResetValues()
        {
            _gold = _levelConfig.StartGold;
            _selectedTowerLevel = Math.Max(1, _levelConfig.LevelTowerSelected);
            _currentWave = 1;
            _towerDamageBonus = 0f;
            _towerAttackSpeedBonus = 0f;
            _towerCritChanceBonus = 0f;
            _towerCritDamageBonus = 0f;
            _upgradeLevels.Clear();
        }

        private void NotifyStateChanged()
        {
            OnGoldChanged?.Invoke(_gold);
            SelectedTowerLevelChanged?.Invoke(_selectedTowerLevel);
            WaveChanged?.Invoke(_currentWave);
            UpgradesChanged?.Invoke();
        }

        public void Dispose()
        {
        }
    }
}