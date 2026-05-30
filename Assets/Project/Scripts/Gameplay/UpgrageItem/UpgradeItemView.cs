using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.System.Localization;
using UnityEngine.UIElements;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public class UpgradeItemView
    {
        private readonly Label _descriptionLabel;
        private readonly Label _priceLabel;
        private readonly Button _buyButton;
        private readonly VisualElement _icon;
        private readonly List<VisualElement> _stars;

        public UpgradeItemView(VisualElement root)
        {
            _descriptionLabel = root.Q<Label>("DescriptionUpgradeLabel");
            _priceLabel = root.Q<Label>("PriceLabel");
            _buyButton = root.Q<Button>("BuyUpgradeButton");
            _icon = root.Q<VisualElement>("UpgradeIcon");

            _stars = new List<VisualElement>
            {
                root.Q<VisualElement>("Level_1"),
                root.Q<VisualElement>("Level_2"),
                root.Q<VisualElement>("Level_3"),
                root.Q<VisualElement>("Level_4"),
                root.Q<VisualElement>("Level_5")
            };
        }

        public void Bind(IUpgradeItem item, Action onBuy, ILocalizationService localizationService)
        {
            _descriptionLabel.text = BuildDescription(item, localizationService);
            _priceLabel.text = item.IsMaxLevel ? "MAX" : item.NextPrice.ToString();
            _buyButton.SetEnabled(!item.IsMaxLevel);

            if (_icon != null)
                _icon.style.backgroundImage = new StyleBackground(item.Config.Icon);

            for (var i = 0; i < _stars.Count; i++)
            {
                if (_stars[i] == null)
                    continue;

                _stars[i].style.opacity = i < item.CurrentLevel ? 1f : 0.35f;
            }

            _buyButton.clicked += onBuy;
        }

        private static string BuildDescription(IUpgradeItem item, ILocalizationService localizationService)
        {
            var levelIndex = item.IsMaxLevel
                ? item.MaxLevel - 1
                : item.CurrentLevel;

            var nextValue = item.Config.Levels[levelIndex].Value;
            var displayValue = FormatValue(item, nextValue);
            return localizationService.Format(item.Config.Description, displayValue);
        }

        private static string FormatValue(IUpgradeItem item, float value)
        {
            switch (item)
            {
                case TowerDamageUpgradeItem:
                case TowerAttackSpeedUpgradeItem:
                case TowerCritChanceUpgradeItem:
                case TowerCritDamageUpgradeItem:
                    return $"{value * 100f:0.#}%";

                default:
                    return value % 1f == 0f ? ((int)value).ToString() : value.ToString("0.##");
            }
        }
    }
}
