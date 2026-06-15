using Project.Scripts.Configs;

namespace Project.Scripts.UI.LevelUI
{
    public interface ILevelUIUseCase
    {
        void OpenShop();
        TowerConfig GetSelectedTowerConfig();
        bool HasUpgradeableTower();
        bool TryUpgradeRandomLowestLevelTower();
    }
}