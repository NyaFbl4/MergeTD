using System;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.System.Enums;
using Project.Scripts.UI.PurchaseWindowUI;

namespace Project.Scripts.System.UseCases
{
    public interface IBuyTowerUseCase : IPurchaseWindowUseCase
    {
        event Action<int> TowerCostChanged;
        int TowerCost { get; }
        int GeneratorCost { get; }
        EBuyTowerResult TryBuyTower();
        EBuyTowerResult TryBuyGenerator();
        void SetTowerCost(int cost);
        void ResetTowerCost();
    }
}
