using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.PurchaseWindowUI
{
    public interface IPurchaseWindowView : IDisposable
    {
        event Action<ETowerType, int> PurchaseRequested;

        void SetOptions(int towerLevelCount, int generatorLevelCount);
        void SetOption(
            ETowerType towerType,
            int level,
            int cost,
            ETowerPurchaseCurrency currency,
            bool canBuy);
        void SetVisible(bool visible);
    }

    public sealed class PurchaseWindowView : IPurchaseWindowView
    {
        private const string OptionTemplateResourcePath = "UIToolkit/New/PurchaseOption";

        private readonly UIElements _uiElements;
        private readonly VisualElement _window;
        private readonly ScrollView _scrollView;
        private readonly VisualElement _rowsContainer;
        private readonly Button _closeButton;
        private readonly Button _footerCloseButton;
        private readonly VisualTreeAsset _optionTemplate;
        private readonly List<PurchaseOptionView> _options = new();

        public event Action<ETowerType, int> PurchaseRequested;

        public PurchaseWindowView(VisualElement root, UIElements uiElements)
        {
            _uiElements = uiElements;
            _window = RequireFrom<VisualElement>(root, "PurchaseWindowRoot");
            _scrollView = RequireFrom<ScrollView>(_window, "PurchaseScrollView");
            _rowsContainer = RequireFrom<VisualElement>(_window, "PurchaseRows");
            _closeButton = RequireFrom<Button>(_window, "PurchaseCloseButton");
            _footerCloseButton = RequireFrom<Button>(_window, "PurchaseFooterCloseButton");
            _optionTemplate = Resources.Load<VisualTreeAsset>(OptionTemplateResourcePath)
                              ?? throw new InvalidOperationException(
                                  $"PurchaseWindowView: Resources/{OptionTemplateResourcePath} was not found.");

            _closeButton.clicked += Close;
            _footerCloseButton.clicked += Close;
            UIButtonAnimationUtility.EnableDefault(_closeButton);
            UIButtonAnimationUtility.EnableDefault(_footerCloseButton);
            SetVisible(false);
        }

        public void SetOptions(int towerLevelCount, int generatorLevelCount)
        {
            ClearOptions();
            _rowsContainer.Clear();

            var rowCount = Math.Max(towerLevelCount, generatorLevelCount);
            for (var level = 1; level <= rowCount; level++)
            {
                var row = new VisualElement();
                row.AddToClassList("purchase-window-row");

                row.Add(level <= towerLevelCount
                    ? CreateOption(ETowerType.Combat, level)
                    : CreatePlaceholder());
                row.Add(level <= generatorLevelCount
                    ? CreateOption(ETowerType.Generator, level)
                    : CreatePlaceholder());

                _rowsContainer.Add(row);
            }
        }

        public void SetOption(
            ETowerType towerType,
            int level,
            int cost,
            ETowerPurchaseCurrency currency,
            bool canBuy)
        {
            var option = GetOption(towerType, level);
            option.NameLabel.text = towerType == ETowerType.Generator
                ? $"Генератор T{level}"
                : $"Башня T{level}";
            option.PriceLabel.text = cost.ToString();
            option.Button.SetEnabled(canBuy);
            option.PriceBar.EnableInClassList(
                "purchase-window-price--gems",
                currency == ETowerPurchaseCurrency.Gems);
            option.CurrencyIcon.EnableInClassList(
                "purchase-window-currency--gems",
                currency == ETowerPurchaseCurrency.Gems);
            option.UnitIcon.style.backgroundImage = new StyleBackground(
                GetTowerIcon(towerType, level));
        }

        public void SetVisible(bool visible)
        {
            _window.EnableInClassList("purchase-window-overlay--visible", visible);
            if (!visible)
                return;

            _scrollView.scrollOffset = Vector2.zero;
            _window.BringToFront();
        }

        public void Dispose()
        {
            _closeButton.clicked -= Close;
            _footerCloseButton.clicked -= Close;
            ClearOptions();
        }

        private VisualElement CreateOption(ETowerType towerType, int level)
        {
            var host = _optionTemplate.Instantiate();
            host.AddToClassList("purchase-option-host");

            var button = RequireFrom<Button>(host, "PurchaseOptionButton");
            button.name = towerType == ETowerType.Generator
                ? $"GeneratorPurchaseT{level}"
                : $"TowerPurchaseT{level}";
            Action clickHandler = () => PurchaseRequested?.Invoke(towerType, level);
            button.clicked += clickHandler;
            UIButtonAnimationUtility.EnableDefault(button);

            _options.Add(new PurchaseOptionView(
                towerType,
                level,
                button,
                RequireFrom<VisualElement>(host, "UnitIcon"),
                RequireFrom<Label>(host, "UnitName"),
                RequireFrom<VisualElement>(host, "PriceBar"),
                RequireFrom<VisualElement>(host, "CurrencyIcon"),
                RequireFrom<Label>(host, "PriceLabel"),
                clickHandler));
            return host;
        }

        private static VisualElement CreatePlaceholder()
        {
            var placeholder = new VisualElement();
            placeholder.AddToClassList("purchase-option-placeholder");
            return placeholder;
        }

        private PurchaseOptionView GetOption(ETowerType towerType, int level)
        {
            foreach (var option in _options)
            {
                if (option.TowerType == towerType && option.Level == level)
                    return option;
            }

            throw new InvalidOperationException(
                $"PurchaseWindowView: option {towerType} T{level} was not found.");
        }

        private Sprite GetTowerIcon(ETowerType towerType, int level)
        {
            var icons = towerType == ETowerType.Generator
                ? _uiElements.GeneratorIcons
                : _uiElements.TowerIcons;
            var iconIndex = level - 1;
            if (iconIndex < 0 || iconIndex >= icons.Count)
                throw new InvalidOperationException(
                    $"PurchaseWindowView: icon {towerType} T{level} was not found.");

            return icons[iconIndex];
        }

        private void ClearOptions()
        {
            foreach (var option in _options)
                option.Button.clicked -= option.ClickHandler;

            _options.Clear();
        }

        private void Close() => SetVisible(false);

        private static T RequireFrom<T>(VisualElement root, string elementName)
            where T : VisualElement
        {
            return root.Q<T>(elementName)
                   ?? throw new InvalidOperationException(
                       $"PurchaseWindowView: element '{elementName}' was not found.");
        }

        private sealed class PurchaseOptionView
        {
            public ETowerType TowerType { get; }
            public int Level { get; }
            public Button Button { get; }
            public VisualElement UnitIcon { get; }
            public Label NameLabel { get; }
            public VisualElement PriceBar { get; }
            public VisualElement CurrencyIcon { get; }
            public Label PriceLabel { get; }
            public Action ClickHandler { get; }

            public PurchaseOptionView(
                ETowerType towerType,
                int level,
                Button button,
                VisualElement unitIcon,
                Label nameLabel,
                VisualElement priceBar,
                VisualElement currencyIcon,
                Label priceLabel,
                Action clickHandler)
            {
                TowerType = towerType;
                Level = level;
                Button = button;
                UnitIcon = unitIcon;
                NameLabel = nameLabel;
                PriceBar = priceBar;
                CurrencyIcon = currencyIcon;
                PriceLabel = priceLabel;
                ClickHandler = clickHandler;
            }
        }
    }
}
