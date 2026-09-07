using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Towers;

namespace Project.Scripts.Gameplay
{
    public interface IUnitsCatalog
    {
        bool HasTowerLevel(int level, ETowerType towerType = ETowerType.Combat);
        TowerUnit GetTowerPrefabByLevel(int level, ETowerType towerType = ETowerType.Combat);
        TowerConfig GetTowerConfigByLevel(int level, ETowerType towerType = ETowerType.Combat);
    }
}