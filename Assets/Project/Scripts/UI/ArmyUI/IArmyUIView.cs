using System;
using Project.Scripts.Gameplay.Towers;

namespace Project.Scripts.UI.ArmyUI
{
    public interface IArmyUIView : IDisposable
    {
        event Action BuyTowerButtonClicked;
        event Action BuyGeneratorButtonClicked;
        event Action<int, int> TowerDropped;

        void SetTower(int slotIndex, bool isLocked, ETowerType? towerType, int towerLevel);
        void SetPurchaseInfo(int towerLevel, int towerPrice, int generatorPrice);
        void SetPurchaseButtonsEnabled(bool canBuyTower, bool canBuyGenerator);
        void SetVisible(bool visible);
    }
}
