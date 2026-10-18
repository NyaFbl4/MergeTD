using System;
using Project.Scripts.Systems.UI;
using Project.Scripts.System.Localization;
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
        private readonly VisualElement _selectedElement;
        private readonly Button _selectionButton;
        private readonly Label _selectionLabel;
        private readonly VisualElement _infoElement;

        private string _spellId;

        public SpellItemView(
            VisualElement root,
            Action<string> onSelectionClicked,
            Action<string> onInfoClicked = null)
        {
            _nameLabel = root.Q<Label>("SpellNameLabel");
            _descriptionLabel = root.Q<Label>("SpellDescriptionLabel");
            _levelLabel = root.Q<Label>("SpellLevelLabel");
            _cooldownLabel = root.Q<Label>("SpellCooldownLabel");
            _icon = root.Q<VisualElement>("TowerIcon");
            _selectedElement = root.Q<VisualElement>("SelectedElement");
            _selectionButton = root.Q<Button>("SetSpellButton");
            _selectionLabel = _selectionButton?.Q<Label>();
            _infoElement = root.Q<VisualElement>("TowerPanel");
            UIButtonAnimationUtility.EnableDefault(_selectionButton);

            if (_selectionButton != null)
            {
                _selectionButton.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
                _selectionButton.clicked += () => onSelectionClicked?.Invoke(_spellId);
            }

            if (_infoElement != null && onInfoClicked != null)
                _infoElement.RegisterCallback<ClickEvent>(_ => onInfoClicked(_spellId));
        }

        public void Bind(
            SpellUIItemData spell,
            ILocalizationService localizationService)
        {
            _spellId = spell.SpellId;
            _nameLabel.text = localizationService.Get(spell.DisplayName);
            _descriptionLabel.text = localizationService.Get(spell.Description);
            _levelLabel.text = localizationService.Format(
                LocalizationKeys.SpellsLevelFormat,
                spell.Level);
            _cooldownLabel.text = localizationService.Format(
                LocalizationKeys.SpellsCooldownFormat,
                spell.Cooldown,
                spell.ManaCost);

            if (_icon != null)
            {
                if (spell.Icon != null)
                    _icon.style.backgroundImage = new StyleBackground(spell.Icon);
                else
                    _icon.style.backgroundImage = StyleKeyword.None;
            }

            if (_selectionLabel != null)
                _selectionLabel.text = spell.IsSelected
                    ? localizationService.Get(LocalizationKeys.SpellsSelected)
                    : spell.CanSelect
                        ? localizationService.Get(LocalizationKeys.SpellsSelect)
                        : localizationService.Get(LocalizationKeys.SpellsLimit);

            _selectedElement.style.display = spell.IsSelected
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            _selectionButton.SetEnabled(spell.IsSelected || spell.CanSelect);
            _selectionButton.style.opacity = 1f;
        }
    }
}
