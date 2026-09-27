using System;
using Project.Scripts.Gameplay.Towers;

namespace Project.Scripts.UI.ArmyUI
{
    public interface IArmyUIView : IDisposable
    {
        void SetTower(int slotIndex, bool isLocked, ETowerType? towerType, int towerLevel);
        void SetVisible(bool visible);
    }
}
