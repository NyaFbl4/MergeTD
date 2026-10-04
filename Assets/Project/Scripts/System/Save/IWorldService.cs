using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Towers;

namespace Project.Scripts.System.Save
{
    public enum EWorldUpgradeType
    {
        TowerLevel,
        TowerDamage,
        TowerAttackSpeed,
        BaseHealth,
        TowerCritChance,
        TowerCritDamage
    }

    public interface IWorldService
    {
        int Gold { get; }
        int Gems { get; }
        long DailyRewardLastClaimDay { get; }
        int DailyRewardIndex { get; }
        int MaxBaseHealth { get; }
        int MaxEnergy { get; }
        int SelectedTowerLevel { get; }
        float TowerDamageBonus { get; }
        float TowerAttackSpeedBonus { get; }
        float TowerCritChanceBonus { get; }
        float TowerCritDamageBonus { get; }
        IReadOnlyList<WorldTowerSlotSaveData> TowerSlots { get; }
        IReadOnlyList<WorldTowerSaveData> Towers { get; }
        IReadOnlyList<SpellProgressSaveData> Spells { get; }
        IReadOnlyList<string> ActiveSpellIds { get; }

        event Action<int> GoldChanged;
        event Action<int> GemsChanged;
        event Action DailyRewardChanged;
        event Action<int> MaxBaseHealthChanged;
        event Action<int> MaxEnergyChanged;
        event Action UpgradesChanged;
        event Action TowersChanged;
        event Action TowerSlotsChanged;
        event Action SpellsChanged;

        bool CanSpendGold(int amount);
        bool TrySpendGold(int amount);
        void AddGold(int amount);
        void AddGems(int amount);
        bool TryClaimDailyReward(long moscowDay, int rewardIndex, int goldAmount, int gemsAmount);
        bool TrySpendGems(int amount);
        void SetMaxBaseHealth(int value);
        void SetMaxEnergy(int value);
        int GetUpgradeLevel(string upgradeId);
        bool TryPurchaseUpgrade(string upgradeId, int expectedLevel, int price, EWorldUpgradeType type, float value);
        bool IsTowerSlotUnlocked(string slotId);
        void UnlockTowerSlot(string slotId);
        void SetTower(string slotId, int towerLevel, ETowerType towerType);
        void MoveTower(string sourceSlotId, string targetSlotId, int towerLevel, ETowerType towerType);
        void RemoveTower(string slotId);
        bool IsSpellUnlocked(string spellId);
        int GetSpellLevel(string spellId);
        void UnlockSpell(string spellId);
        void SetSpellLevel(string spellId, int level);
        bool TryUpgradeSpell(string spellId, int expectedLevel, int price);
        bool IsSpellSelected(string spellId);
        bool SetSpellSelected(string spellId, bool isSelected);
        void Reset();
    }
}
