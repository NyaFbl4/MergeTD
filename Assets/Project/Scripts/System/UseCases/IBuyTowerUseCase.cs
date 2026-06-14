using System;
using Project.Scripts.System.Enums;

namespace Project.Scripts.System.UseCases
{
    public interface IBuyTowerUseCase
    {
        event Action<int> TowerCostChanged;
        int TowerCost { get; }
        EBuyTowerResult TryBuyTower();
        void SetTowerCost(int cost);
        void ResetTowerCost();
    }
}