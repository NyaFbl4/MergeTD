using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Towers;

namespace Project.Scripts.System.Save
{
    [Serializable]
    public sealed class WorldSaveData
    {
        public int version = 1;
        public int gold;
        public int gems;
        public int maxBaseHealth;
        public int maxEnergy;
        public List<WorldTowerSaveData> towers = new();
        public List<SpellProgressSaveData> spells = new();
    }

    [Serializable]
    public sealed class WorldTowerSaveData
    {
        public string slotId;
        public int towerLevel;
        public ETowerType towerType;

        public WorldTowerSaveData()
        {
        }

        public WorldTowerSaveData(string slotId, int towerLevel, ETowerType towerType)
        {
            this.slotId = slotId;
            this.towerLevel = towerLevel;
            this.towerType = towerType;
        }
    }

    [Serializable]
    public sealed class SpellProgressSaveData
    {
        public string spellId;
        public bool isUnlocked;
        public int level;

        public SpellProgressSaveData()
        {
        }

        public SpellProgressSaveData(string spellId, bool isUnlocked, int level)
        {
            this.spellId = spellId;
            this.isUnlocked = isUnlocked;
            this.level = level;
        }
    }
}
