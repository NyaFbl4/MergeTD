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
        public PlayerEnergySaveData playerEnergy;
        public long dailyRewardLastClaimDay = -1;
        public int dailyRewardIndex = -1;
        public long dailyQuestDay = -1;
        public int dailyQuestCycleDay = -1;
        public long weeklyQuestWeek = -1;
        public int maxBaseHealth;
        public int maxEnergy;
        public int selectedTowerLevel = 1;
        public float towerDamageBonus;
        public float towerAttackSpeedBonus;
        public float towerCritChanceBonus;
        public float towerCritDamageBonus;
        public List<WorldUpgradeSaveData> upgrades = new();
        public List<WorldTowerSaveData> towers = new();
        public List<WorldTowerSlotSaveData> towerSlots = new();
        public List<SpellProgressSaveData> spells = new();
        public List<string> activeSpellIds = new();
        public List<ShopOfferProgressSaveData> shopOffers = new();
        public List<QuestSaveData> dailyQuests = new();
        public List<QuestSaveData> weeklyQuests = new();
        public List<QuestSaveData> achievementQuests = new();
    }

    [Serializable]
    public sealed class PlayerEnergySaveData
    {
        public const int Capacity = 10;
        public int version = 1;
        public int current = Capacity;
        public long nextRestoreUnixMilliseconds;

        public static PlayerEnergySaveData Normalize(PlayerEnergySaveData data)
        {
            if (data == null || data.version < 1)
                return new PlayerEnergySaveData();

            data.current = Math.Max(0, data.current);
            data.nextRestoreUnixMilliseconds = data.current >= Capacity
                ? 0
                : Math.Max(0, data.nextRestoreUnixMilliseconds);
            return data;
        }
    }

    [Serializable]
    public sealed class WorldUpgradeSaveData
    {
        public string id;
        public int level;

        public WorldUpgradeSaveData()
        {
        }

        public WorldUpgradeSaveData(string id, int level)
        {
            this.id = id;
            this.level = level;
        }
    }

    [Serializable]
    public sealed class WorldTowerSlotSaveData
    {
        public const int Unlocked = 0;
        public const int Locked = 1;

        public string slotId;
        public int unlockedSlot = Locked;

        public WorldTowerSlotSaveData()
        {
        }

        public WorldTowerSlotSaveData(string slotId, int unlockedSlot)
        {
            this.slotId = slotId;
            this.unlockedSlot = unlockedSlot;
        }
    }

    [Serializable]
    public sealed class WorldTowerSaveData
    {
        public string slotId;
        public int towerLevel;
        public ETowerType towerType;
        public int purchaseCost;

        public WorldTowerSaveData()
        {
        }

        public WorldTowerSaveData(
            string slotId,
            int towerLevel,
            ETowerType towerType,
            int purchaseCost = 0)
        {
            this.slotId = slotId;
            this.towerLevel = towerLevel;
            this.towerType = towerType;
            this.purchaseCost = purchaseCost;
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

    [Serializable]
    public sealed class ShopOfferProgressSaveData
    {
        public string offerId;
        public long utcDay = -1;
        public int purchasesToday;

        public ShopOfferProgressSaveData()
        {
        }

        public ShopOfferProgressSaveData(string offerId, long utcDay, int purchasesToday)
        {
            this.offerId = offerId;
            this.utcDay = utcDay;
            this.purchasesToday = purchasesToday;
        }
    }
}
