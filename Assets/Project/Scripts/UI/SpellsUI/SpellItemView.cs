using System;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.SpellsUI
{
    public sealed class SpellItemView
    {
        private readonly Label _nameLabel;
        private readonly Label _descriptionLabel;
        private readonly Label _levelLabel;
        private readonly Label _cooldownLabel;
        private readonly VisualElement _icon;
        private readonly Button _selectionButton;
        private readonly Label _selectionLabel;

        private string _spellId;

        public SpellItemView(VisualElement root, Action<string> onSelectionClicked)
        {
            _nameLabel = root.Q<Label>("SpellNameLabel");
            _descriptionLabel = root.Q<Label>("SpellDescriptionLabel");
            _levelLabel = root.Q<Label>("SpellLevelLabel");
            _cooldownLabel = root.Q<Label>("SpellCooldownLabel");
            _icon = root.Q<VisualElement>("TowerIcon");
            _selectionButton = root.Q<Button>("SetSpellButton");
            _selectionLabel = _selectionButton?.Q<Label>();

            if (_selectionButton != null)
                _selectionButton.clicked += () => onSelectionClicked?.Invoke(_spellId);
        }

        public void Bind(SpellUIItemData spell)
        {
            _spellId = spell.SpellId;
            _nameLabel.text = spell.DisplayName;
            _descriptionLabel.text = spell.Description;
            _levelLabel.text = $"Уровень {spell.Level}";
            _cooldownLabel.text = $"КД {spell.Cooldown:0.#} сек.";

            if (_icon != null)
            {
                if (spell.Icon != null)
                    _icon.style.backgroundImage = new StyleBackground(spell.Icon);
                else
                    _icon.style.backgroundImage = StyleKeyword.None;
            }

            if (_selectionLabel != null)
                _selectionLabel.text = spell.IsSelected
                    ? "ВЫБРАНО"
                    : spell.CanSelect
                        ? "ВЫБРАТЬ"
                        : "ЛИМИТ";

            _selectionButton.SetEnabled(spell.IsSelected || spell.CanSelect);
        }
    }
}
