using Project.Scripts.System.Enums;

namespace Project.Scripts.System.UseCases
{
    public interface IBuyTowerUseCase
    {
        int TowerCost { get; }
        EBuyTowerResult TryBuyTower();
    }
}