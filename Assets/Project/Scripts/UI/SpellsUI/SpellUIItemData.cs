using UnityEngine;

namespace Project.Scripts.UI.SpellsUI
{
    public sealed class SpellUIItemData
    {
        public string SpellId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public Sprite Background { get; }
        public int Level { get; }
        public float Cooldown { get; }
        public int ManaCost { get; }
        public bool IsSelected { get; }
        public bool CanSelect { get; }
        public int MaximumLevel { get; }
        public int UpgradePrice { get; }
        public string UpgradeTitle { get; }
        public string UpgradeDescription { get; }
        public bool HasNextUpgrade { get; }
        public bool CanAffordUpgrade { get; }

        public SpellUIItemData(
            string spellId,
            string displayName,
            string description,
            Sprite icon,
            Sprite background,
            int level,
            float cooldown,
            int manaCost,
            bool isSelected,
            bool canSelect,
            int maximumLevel,
            int upgradePrice,
            string upgradeTitle,
            string upgradeDescription,
            bool hasNextUpgrade,
            bool canAffordUpgrade)
        {
            SpellId = spellId;
            DisplayName = displayName;
            Description = description;
            Icon = icon;
            Background = background;
            Level = level;
            Cooldown = cooldown;
            ManaCost = manaCost;
            IsSelected = isSelected;
            CanSelect = canSelect;
            MaximumLevel = maximumLevel;
            UpgradePrice = upgradePrice;
            UpgradeTitle = upgradeTitle;
            UpgradeDescription = upgradeDescription;
            HasNextUpgrade = hasNextUpgrade;
            CanAffordUpgrade = canAffordUpgrade;
        }
    }
}
