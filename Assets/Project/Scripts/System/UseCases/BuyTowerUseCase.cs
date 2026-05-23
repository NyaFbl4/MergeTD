using System;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.System.Enums;

namespace Project.Scripts.System.UseCases
{
    public class BuyTowerUseCase : IBuyTowerUseCase
    {
        private readonly LevelConfig _levelConfig;
        private readonly TowerConfig _towerConfig;
        private readonly BattlefieldContext _battlefieldContext;
        private readonly IUnitsCatalog _unitsCatalog;
        private readonly IPlayerStatsUseCase _playerStats;
        
        private int _currentTowerCost;

        public int TowerCost => _currentTowerCost;
        public event Action<int> TowerCostChanged;

        public BuyTowerUseCase(BattlefieldContext battlefieldContext, IPlayerStatsUseCase playerStatsUseCase,
            TowerConfig towerConfig, IUnitsCatalog unitsCatalog, LevelConfig levelConfig)
        {
            _battlefieldContext = battlefieldContext;
            _playerStats = playerStatsUseCase;
            _towerConfig = towerConfig;
            _unitsCatalog = unitsCatalog;
            _levelConfig = levelConfig;
            
            _currentTowerCost = _towerConfig.StartTowerPrice;
        }

        public EBuyTowerResult TryBuyTower()
        {
            var slot = _battlefieldContext.FindFirstFreeSlot(ETowerSlotType.SpawnOnly);
            if (slot == null)
                return EBuyTowerResult.NoFreeSpawnSlot;

            if (!_playerStats.CanSpend(TowerCost))
                return EBuyTowerResult.NotEnoughGold;

            var towerPrefab = _unitsCatalog.GetTowerPrefabByLevel(_playerStats.SelectedTowerLevel);
            
            if (towerPrefab == null)
                return EBuyTowerResult.PlaceFailed;

            if (!slot.TryPlaceTower(towerPrefab))
                return EBuyTowerResult.PlaceFailed;

            _playerStats.TrySpend(TowerCost);
            IncreaseTowerCost();
            return EBuyTowerResult.Success;
        }
        
        private void IncreaseTowerCost()
        {
            _currentTowerCost += _levelConfig.TowerPriceIncreaseOnBuy;
            TowerCostChanged?.Invoke(_currentTowerCost);
        }
    }
}