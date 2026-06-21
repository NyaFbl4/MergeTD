using System;
using System.Collections.Generic;

namespace Project.Scripts.System.Save
{
    [Serializable]
    public class ProgressSaveData
    {
        public int nextWave = 1;
        public int gold;
        public int selectedTowerLevel = 1;
        public int towerCost;
        public int currentBaseHealth;
        public int maxBaseHealth;
        public float towerDamageBonus;
        public float towerAttackSpeedBonus;
        public float towerCritChanceBonus;
        public float towerCritDamageBonus;
        public List<UpgradeLevelSaveData> upgrades = new();
        public List<TowerSlotSaveData> towers = new();
        public List<QuestSaveData> quests = new();
    }

    [Serializable]
    public class UpgradeLevelSaveData
    {
        public string id;
        public int level;

        public UpgradeLevelSaveData()
        {
        }

        public UpgradeLevelSaveData(string id, int level)
        {
            this.id = id;
            this.level = level;
        }
    }

    [Serializable]
    public class TowerSlotSaveData
    {
        public int slotIndex;
        public int towerLevel;

        public TowerSlotSaveData()
        {
        }

        public TowerSlotSaveData(int slotIndex, int towerLevel)
        {
            this.slotIndex = slotIndex;
            this.towerLevel = towerLevel;
        }
    }

    [Serializable] 
    public class QuestSaveData
    { 
        public string id;
        public int currentValue;
        public int targetValue; 
        public int rewardGold;
        public bool isRewardClaimed;
    }
}