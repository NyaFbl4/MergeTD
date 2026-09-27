using System.Collections.Generic;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.System.Save;

namespace Project.Scripts.UI.ArmyUI
{
    public sealed class ArmyUIPresenter : IArmyUIPresenter
    {
        private readonly IArmyUIView _view;
        private readonly IWorldService _world;

        public ArmyUIPresenter(IArmyUIView view, IWorldService world)
        {
            _view = view;
            _world = world;
        }

        public void Initialize()
        {
            _world.TowerSlotsChanged += RefreshSlots;
            _world.TowersChanged += RefreshSlots;
            RefreshSlots();
        }

        public void Dispose()
        {
            _world.TowerSlotsChanged -= RefreshSlots;
            _world.TowersChanged -= RefreshSlots;
        }

        private void RefreshSlots()
        {
            var towersBySlot = new Dictionary<string, WorldTowerSaveData>();
            foreach (var tower in _world.Towers)
            {
                if (tower != null && !string.IsNullOrEmpty(tower.slotId))
                    towersBySlot[tower.slotId] = tower;
            }

            for (var i = 0; i < TowerSlotGrid.SlotCount; i++)
            {
                var slotId = TowerSlotGrid.GetSlotId(i);
                towersBySlot.TryGetValue(slotId, out var tower);
                _view.SetTower(
                    i,
                    !_world.IsTowerSlotUnlocked(slotId),
                    tower?.towerType,
                    tower?.towerLevel ?? 0);
            }
        }
    }
}
