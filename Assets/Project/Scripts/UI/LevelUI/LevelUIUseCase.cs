using Project.Scripts.Configs;
using Project.Scripts.Gameplay;
using Project.Scripts.Gameplay.Field;
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

        public LevelUIUseCase(BattlefieldContext battlefieldContext, UnitsConfig unitsConfig, 
                            IUnitsCatalog  unitsCatalog, IPlayerStatsUseCase playerStats)
        {
            _battlefieldContext = battlefieldContext;
            _unitsConfig = unitsConfig;
            _playerStats = playerStats;
            _unitsCatalog = unitsCatalog;
        }
        
        public void TryBuyTower()
        {
            Debug.Log("TryBuyTower");
            
            var slot = _battlefieldContext.FindFirstFreeSlot(ETowerSlotType.SpawnOnly);
            var currentTower = _unitsConfig.Towers[0];
            
            slot.TryPlaceTower(currentTower);
        }
        
        public TowerConfig GetSelectedTowerConfig()
        {
            var towerConfig = _unitsCatalog.GetTowerConfigByLevel(_playerStats.SelectedTowerLevel);
            return towerConfig;
        }

        public void OpenShop()
        {
            Debug.Log("OpenShop");
        }
    }
}