using Project.Scripts.Configs;
using Project.Scripts.Gameplay;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.System.Enums;
using NotImplementedException = System.NotImplementedException;

namespace Project.Scripts.System.UseCases
{
    public class BuyTowerUseCase : IBuyTowerUseCase
    {
        private readonly TowerConfig _towerConfig;
        private readonly BattlefieldContext _battlefieldContext;
        private readonly IUnitsCatalog _unitsCatalog;
        private readonly IPlayerStatsUseCase _playerStats;

        public int TowerCost => ResolveTowerCost();

        public BuyTowerUseCase(BattlefieldContext battlefieldContext, IPlayerStatsUseCase playerStatsUseCase,
            TowerConfig towerConfig, IUnitsCatalog unitsCatalog)
        {
            _battlefieldContext = battlefieldContext;
            _playerStats = playerStatsUseCase;
            _towerConfig = towerConfig;
            _unitsCatalog = unitsCatalog;
        }

        public EBuyTowerResult TryBuyTower()
        {
            var slot = _battlefieldContext.FindFirstFreeSlot(ETowerSlotType.SpawnOnly);
            if (slot == null)
                return EBuyTowerResult.NoFreeSpawnSlot;

            if (!_playerStats.CanSpend(TowerCost))
                return EBuyTowerResult.NotEnoughGold;

            var towerPrefab = _unitsCatalog.GetTowerPrefabByLevel(1);
            
            if (towerPrefab == null)
                return EBuyTowerResult.PlaceFailed;

            if (!slot.TryPlaceTower(towerPrefab))
                return EBuyTowerResult.PlaceFailed;

            _playerStats.TrySpend(TowerCost);
            return EBuyTowerResult.Success;
        }
        
        private int ResolveTowerCost()
        {
            var towerConfig = _towerConfig.StartTowerPrice;
            return towerConfig;
        }
    }
}