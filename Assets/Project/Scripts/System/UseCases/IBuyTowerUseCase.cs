using System;
using Project.Scripts.System.Enums;

namespace Project.Scripts.System.UseCases
{
    public interface IBuyTowerUseCase
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