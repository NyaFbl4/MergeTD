using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.System.Audio;
using Project.Scripts.System.UseCases;
using UnityEngine;

namespace Project.Scripts.UI.LevelUI
{
    public class LevelUIUseCase : ILevelUIUseCase
    {
        private readonly BattlefieldContext _battlefieldContext;
        private readonly UnitsConfig _unitsConfig;
        private readonly IPlayerStatsUseCase _playerStats;
        private readonly IUnitsCatalog _unitsCatalog;
        private readonly IAudioManager _audioManager;

        public LevelUIUseCase(
            BattlefieldContext battlefieldContext,
            UnitsConfig unitsConfig,
            IUnitsCatalog unitsCatalog,
            IPlayerStatsUseCase playerStats,
            IAudioManager audioManager)
        {
            _battlefieldContext = battlefieldContext;
            _unitsConfig = unitsConfig;
            _playerStats = playerStats;
            _unitsCatalog = unitsCatalog;
            _audioManager = audioManager;
        }
        
        public TowerConfig GetSelectedTowerConfig()
        {
            var towerConfig = _unitsCatalog.GetTowerConfigByLevel(_playerStats.SelectedTowerLevel);
            return towerConfig;
        }

        public bool HasUpgradeableTower()
        {
            return TryGetRandomLowestUpgradeableTower(out _);
        }

        public bool TryUpgradeRandomLowestLevelTower()
        {
            if (!TryGetRandomLowestUpgradeableTower(out var slot))
                return false;

            var currentTower = slot.CurrentTower;
            if (currentTower == null)
                return false;

            var nextPrefab = _unitsCatalog.GetTowerPrefabByLevel(currentTower.CurrentLevel + 1);
            if (nextPrefab == null)
                return false;

            slot.ClearTower();
            return slot.TryPlaceTower(nextPrefab, _playerStats, _audioManager);
        }

        public void OpenShop()
        {
            Debug.Log("OpenShop");
        }

        private bool TryGetRandomLowestUpgradeableTower(out TowerSlot targetSlot)
        {
            targetSlot = null;

            var slots = _battlefieldContext.TowerSlots;
            if (slots == null || slots.Length == 0)
                return false;

            var candidates = new List<TowerSlot>();
            var lowestLevel = int.MaxValue;

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var tower = slot != null ? slot.CurrentTower : null;
                if (tower == null)
                    continue;

                var level = tower.CurrentLevel;
                if (!_unitsCatalog.HasTowerLevel(level + 1))
                    continue;

                if (level < lowestLevel)
                {
                    lowestLevel = level;
                    candidates.Clear();
                }

                if (level == lowestLevel)
                    candidates.Add(slot);
            }

            if (candidates.Count == 0)
                return false;

            targetSlot = candidates[Random.Range(0, candidates.Count)];
            return targetSlot != null;
        }
    }
}