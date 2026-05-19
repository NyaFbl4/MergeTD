using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Towers;

namespace Project.Scripts.Gameplay
{
    public interface IUnitsCatalog
    {
        bool HasTowerLevel(int level);
        TowerUnit GetTowerPrefabByLevel(int level);
        TowerConfig GetTowerConfigByLevel(int level);
    }
}