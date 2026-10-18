using System;
using System.Collections.Generic;
using Project.Scripts.System.Localization;
using Project.Scripts.Systems.UI;
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
            UIButtonAnimationUtility.EnableDefault(_buyButton);

            _stars = new List<VisualElement>();
            for (var i = 1; i <= 9; i++)
                _stars.Add(root.Q<VisualElement>($"Level_{i}"));
        }

        public void Bind(IUpgradeItem item, Action onBuy, ILocalizationService localizationService)
        {
            _descriptionLabel.text = BuildDescription(item, localizationService);
            _priceLabel.text = item.IsMaxLevel 
                ? localizationService.Get(LocalizationKeys.UpgradeMax) 
                : item.NextPrice.ToString();
            _buyButton.SetEnabled(!item.IsMaxLevel);

            if (_icon != null)
                _icon.style.backgroundImage = new StyleBackground(item.Config.Icon);

            for (var i = 0; i < _stars.Count; i++)
            {
                if (_stars[i] == null)
                    continue;

                _stars[i].style.display = i < item.MaxLevel ? DisplayStyle.Flex : DisplayStyle.None;
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

    public sealed class BaseUpgradeItemView
    {
        private readonly Label _titleLabel;
        private readonly Label _levelLabel;
        private readonly Label _currentValueLabel;
        private readonly Label _nextValueLabel;
        private readonly Label _descriptionLabel;
        private readonly Label _buyLabel;
        private readonly Label _priceLabel;
        private readonly VisualElement _icon;
        private readonly VisualElement _arrow;
        private readonly VisualElement _priceIcon;
        private readonly Button _buyButton;

        public BaseUpgradeItemView(VisualElement root)
        {
            _titleLabel = root.Q<Label>("UpgradeTitleLabel");
            _levelLabel = root.Q<Label>("UpgradeLevelLabel");
            _currentValueLabel = root.Q<Label>("CurrentValueLabel");
            _nextValueLabel = root.Q<Label>("NextValueLabel");
            _descriptionLabel = root.Q<Label>("UpgradeDescriptionLabel");
            _buyLabel = root.Q<Label>("BuyLabel");
            _priceLabel = root.Q<Label>("PriceLabel");
            _icon = root.Q<VisualElement>("UpgradeIcon");
            _arrow = root.Q<VisualElement>("ValueArrow");
            _priceIcon = root.Q<VisualElement>("PriceIcon");
            _buyButton = root.Q<Button>("BuyUpgradeButton");
            UIButtonAnimationUtility.EnableDefault(_buyButton);
        }

        public void Bind(
            IBaseUpgradeItem item,
            bool canPurchase,
            Action onBuy,
            ILocalizationService localizationService)
        {
            _titleLabel.text = localizationService.Get(item.Config.Title);
            _levelLabel.text = localizationService.Format(
                LocalizationKeys.BaseUpgradeLevelFormat,
                item.CurrentLevel);
            _currentValueLabel.text = FormatValue(item.CurrentValue, item.ValueFormat);
            _nextValueLabel.text = FormatValue(item.NextValue, item.ValueFormat);
            _descriptionLabel.text = localizationService.Get(item.Config.Details);
            _buyLabel.text = localizationService.Get(LocalizationKeys.BaseUpgradeBuy);
            _priceLabel.text = item.IsMaxLevel
                ? localizationService.Get(LocalizationKeys.UpgradeMax)
                : item.NextPrice.ToString();
            _icon.style.backgroundImage = new StyleBackground(item.Config.Icon);

            var nextValueDisplay = item.IsMaxLevel ? DisplayStyle.None : DisplayStyle.Flex;
            _arrow.style.display = nextValueDisplay;
            _nextValueLabel.style.display = nextValueDisplay;
            _priceIcon.style.display = nextValueDisplay;
            _buyLabel.style.display = nextValueDisplay;

            var canBuy = !item.IsMaxLevel && canPurchase;
            _buyButton.SetEnabled(canBuy);
            _buyButton.style.opacity = canBuy ? 1f : 0.55f;
            _buyButton.clicked += onBuy;
        }

        private static string FormatValue(float value, EBaseUpgradeValueFormat format)
        {
            if (format == EBaseUpgradeValueFormat.Percent)
                return $"{value * 100f:0.#}%";

            return value % 1f == 0f
                ? ((int)value).ToString()
                : value.ToString("0.##");
        }
    }
}
