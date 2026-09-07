using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Towers;

namespace Project.Scripts.Gameplay
{
    public class UnitsCatalog : IUnitsCatalog
    {
        private readonly UnitsConfig _unitsConfig;

        public UnitsCatalog(UnitsConfig unitsConfig)
        {
            _unitsConfig = unitsConfig;
        }

        public bool HasTowerLevel(int level, ETowerType towerType = ETowerType.Combat)
        {
            switch (towerType)
            {
                case ETowerType.Combat:
                    return level >= 1 && level <= _unitsConfig.Towers.Count;
                case ETowerType.Generator:
                    return level == 1;
                default:
                    return false;
            }
        }
        
        public TowerConfig GetTowerConfigByLevel(int level, ETowerType towerType = ETowerType.Combat)
        {
            var towerPrefab = GetTowerPrefabByLevel(level, towerType);

            if (towerPrefab == null)
                return null;

            return towerPrefab.TowerConfig;
        }

        public TowerUnit GetTowerPrefabByLevel(int level, ETowerType towerType = ETowerType.Combat)
        {
            if (!HasTowerLevel(level, towerType))
                return null;

            return towerType == ETowerType.Generator
                ? _unitsConfig.GeneratorLevelOne
                : _unitsConfig.Towers[level - 1];
        }
    }
}