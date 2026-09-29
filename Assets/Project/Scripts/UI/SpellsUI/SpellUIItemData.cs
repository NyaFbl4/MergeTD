using UnityEngine;

namespace Project.Scripts.UI.SpellsUI
{
    public sealed class SpellUIItemData
    {
        public string SpellId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public int Level { get; }
        public float Cooldown { get; }
        public bool IsSelected { get; }
        public bool CanSelect { get; }

        public SpellUIItemData(
            string spellId,
            string displayName,
            string description,
            Sprite icon,
            int level,
            float cooldown,
            bool isSelected,
            bool canSelect)
        {
            SpellId = spellId;
            DisplayName = displayName;
            Description = description;
            Icon = icon;
            Level = level;
            Cooldown = cooldown;
            IsSelected = isSelected;
            CanSelect = canSelect;
        }
    }
}
