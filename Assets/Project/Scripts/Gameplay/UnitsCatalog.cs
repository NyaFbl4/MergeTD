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

        public bool HasTowerLevel(int level)
        {
            return level >= 1 && level <= _unitsConfig.Towers.Count;
        }

        public TowerUnit GetTowerPrefabByLevel(int level)
        {
            if (!HasTowerLevel(level))
                return null;

            return _unitsConfig.Towers[level - 1];
        }
    }
}