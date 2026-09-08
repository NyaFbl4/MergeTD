using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.System.Save;
using UnityEngine;
using VContainer.Unity;

namespace Project.Scripts.System.UseCases
{
    public class PlayerStatsUseCase : IPlayerStatsUseCase, IInitializable, IDisposable
    {
        private readonly RunConfig _runConfig;
        private readonly IWorldService _world;

        private int _currentWave;
        private int _selectedTowerLevel;

        private float _towerDamageBonus;
        private float _towerAttackSpeedBonus;
        private float _towerCritChanceBonus;
        private float _towerCritDamageBonus;

        private readonly Dictionary<string, int> _upgradeLevels = new();

        public int Gold => _world.Gold;
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

        public PlayerStatsUseCase(RunConfig runConfig, IWorldService world)
        {
            _runConfig = runConfig;
            _world = world;
            ResetValues();
        }

        public void Initialize()
        {
            _world.GoldChanged += OnWorldGoldChanged;
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

        public bool CanSpend(int amount) => _world.CanSpendGold(amount);

        public bool TrySpend(int amount)
        {
            return _world.TrySpendGold(amount);
        }

        public void AddGold(int amount)
        {
            _world.AddGold(amount);
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
            _selectedTowerLevel = Math.Max(1, _runConfig.StartSelectedTowerLevel);
            _currentWave = 1;
            _towerDamageBonus = 0f;
            _towerAttackSpeedBonus = 0f;
            _towerCritChanceBonus = 0f;
            _towerCritDamageBonus = 0f;
            _upgradeLevels.Clear();
        }

        private void NotifyStateChanged()
        {
            OnGoldChanged?.Invoke(Gold);
            SelectedTowerLevelChanged?.Invoke(_selectedTowerLevel);
            WaveChanged?.Invoke(_currentWave);
            UpgradesChanged?.Invoke();
        }

        public void Dispose()
        {
            _world.GoldChanged -= OnWorldGoldChanged;
        }

        private void OnWorldGoldChanged(int value) => OnGoldChanged?.Invoke(value);
    }
}
