using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Towers;
using UnityEngine;

namespace Project.Scripts.UI.LevelUI
{
    public class LevelUIUseCase : ILevelUIUseCase
    {
        private readonly BattlefieldContext _battlefieldContext;
        private readonly UnitsConfig _unitsConfig;

        public LevelUIUseCase(BattlefieldContext battlefieldContext, UnitsConfig unitsConfig)
        {
            _battlefieldContext = battlefieldContext;
            _unitsConfig = unitsConfig;
        }
        
        public void TryBuyTower()
        {
            Debug.Log("TryBuyTower");
            
            var slot = _battlefieldContext.FindFirstFreeSlot(ETowerSlotType.SpawnOnly);

            slot.TryPlaceTower(_unitsConfig.Tower);
        }

        public void OpenShop()
        {
            Debug.Log("OpenShop");
        }
    }
}