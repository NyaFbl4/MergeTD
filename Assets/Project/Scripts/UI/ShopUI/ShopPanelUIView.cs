using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay.UpgradeItem;
using Project.Scripts.System.Localization;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.ShopUI
{
    public sealed class ShopPanelUIView : IShopPanelUIView
    {
        private const string UpgradeTemplateResourcePath = "UIToolkit/UpgradeObjectUI";
        private const string OfferTemplateResourcePath = "UIToolkit/ShopOfferCard";
        private const string GemOfferTemplateResourcePath = "UIToolkit/New/ShopGemOfferCard";

        private readonly VisualElement _root;
        private readonly IUpgradeConfigProvider _upgradeConfigs;
        private readonly VisualTreeAsset _shopItemTemplate;
        private readonly VisualTreeAsset _offerItemTemplate;
        private readonly VisualTreeAsset _gemOfferItemTemplate;
        private readonly Label _titleLabel;
        private readonly Label _emptyLabel;
        private readonly VisualElement _tabsContainer;
        private readonly ScrollView _scrollView;
        private readonly VisualElement _offerPanel;
        private readonly VisualElement _offerContainer;
        private readonly VisualElement _gemPanel;
        private readonly VisualElement _gemContainer;
        private readonly List<VisualElement> _upgradeItemRoots = new();
        private readonly List<string> _offerOrder = new();
        private readonly Dictionary<string, IShopOfferItemView> _offerViews =
            new(StringComparer.Ordinal);

        private EShopOfferPlacementLayout? _offerLayout;

        public UpgradeItemConfig DamageUpgradeConfig => _upgradeConfigs.DamageUpgradeConfig;
        public UpgradeItemConfig AttackSpeedUpgradeConfig => _upgradeConfigs.AttackSpeedUpgradeConfig;
        public UpgradeItemConfig HealthUpgradeConfig => _upgradeConfigs.HealthUpgradeConfig;
        public UpgradeItemConfig CritChanceUpgradeConfig => _upgradeConfigs.CritChanceUpgradeConfig;
        public UpgradeItemConfig CritDamageUpgradeConfig => _upgradeConfigs.CritDamageUpgradeConfig;
        public UpgradeItemConfig TowerLevelUpgradeConfig => _upgradeConfigs.TowerLevelUpgradeConfig;

        public event Action<string> TabClicked;
        public event Action<string> OfferPurchaseClicked;

        public ShopPanelUIView(
            VisualElement root,
            IUpgradeConfigProvider upgradeConfigs)
        {
            _root = root;
            _upgradeConfigs = upgradeConfigs;
            _titleLabel = Require<Label>("TitleLabel");
            _tabsContainer = Require<VisualElement>("TabsContainer");
            _scrollView = Require<ScrollView>("ScrollView");
            _offerPanel = Require<VisualElement>("OfferPanel");
            _offerContainer = Require<VisualElement>("OfferContainer");
            _gemPanel = Require<VisualElement>("GemPanel");
            _gemContainer = Require<VisualElement>("GemContainer");
            _emptyLabel = Require<Label>("EmptyLabel");
            _shopItemTemplate = LoadTemplate(UpgradeTemplateResourcePath);
            _offerItemTemplate = LoadTemplate(OfferTemplateResourcePath);
            _gemOfferItemTemplate = LoadTemplate(GemOfferTemplateResourcePath);
        }

        public void SetVisible(bool visible)
        {
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetTitle(string title) => _titleLabel.text = title;

        public void SetTabs(IReadOnlyList<ShopTabUIData> tabs, string selectedTabId)
        {
            _tabsContainer.Clear();
            for (var i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                var tabButton = new Button(() => TabClicked?.Invoke(tab.Id))
                {
                    text = tab.Title
                };
                tabButton.AddToClassList("shop-tab-button");
                tabButton.EnableInClassList(
                    "shop-tab-button--selected",
                    string.Equals(tab.Id, selectedTabId, StringComparison.Ordinal));
                UIButtonAnimationUtility.EnableDefault(tabButton);
                _tabsContainer.Add(tabButton);
            }
        }

        public void ClearItems()
        {
            ClearRenderedItems();
            _emptyLabel.style.display = DisplayStyle.None;
        }

        public void AddItem(
            IUpgradeItem item,
            Action onBuy,
            ILocalizationService localizationService)
        {
            var itemRoot = _shopItemTemplate.Instantiate();
            var itemView = new UpgradeItemView(itemRoot);
            itemView.Bind(item, onBuy, localizationService);
            _upgradeItemRoots.Add(itemRoot);
            _scrollView.Add(itemRoot);
        }

        public void SetOffers(
            IReadOnlyList<ShopOfferUIData> offers,
            EShopOfferPlacementLayout layout)
        {
            _emptyLabel.style.display = DisplayStyle.None;
            if (RequiresOfferRebuild(offers, layout))
                RebuildOffers(offers, layout);

            for (var i = 0; i < offers.Count; i++)
                _offerViews[offers[i].Id].Bind(offers[i]);
        }

        public void ShowEmptyState(string text)
        {
            ClearItems();
            _emptyLabel.text = text;
            _emptyLabel.style.display = DisplayStyle.Flex;
        }

        public void Dispose()
        {
            _tabsContainer.Clear();
            ClearRenderedItems();
            TabClicked = null;
            OfferPurchaseClicked = null;
        }

        private bool RequiresOfferRebuild(
            IReadOnlyList<ShopOfferUIData> offers,
            EShopOfferPlacementLayout layout)
        {
            if (_offerLayout != layout || _offerOrder.Count != offers.Count)
                return true;

            for (var i = 0; i < offers.Count; i++)
            {
                if (!string.Equals(_offerOrder[i], offers[i].Id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void RebuildOffers(
            IReadOnlyList<ShopOfferUIData> offers,
            EShopOfferPlacementLayout layout)
        {
            ClearRenderedItems();
            _offerLayout = layout;
            var useGemGrid = layout == EShopOfferPlacementLayout.GemGrid;
            var targetPanel = useGemGrid ? _gemPanel : _offerPanel;
            var targetContainer = useGemGrid ? _gemContainer : _offerContainer;
            targetPanel.style.display = offers.Count > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            targetContainer.EnableInClassList("shop-offer-grid", useGemGrid);

            for (var i = 0; i < offers.Count; i++)
            {
                var offer = offers[i];
                var offerRoot = (useGemGrid ? _gemOfferItemTemplate : _offerItemTemplate)
                    .Instantiate();
                IShopOfferItemView offerView;
                if (useGemGrid)
                {
                    offerRoot.AddToClassList("shop-gem-card-container");
                    offerView = new ShopGemOfferItemView(offerRoot, OnOfferPurchaseClicked);
                }
                else
                {
                    offerView = new ShopOfferItemView(offerRoot, OnOfferPurchaseClicked);
                }

                _offerOrder.Add(offer.Id);
                _offerViews.Add(offer.Id, offerView);
                targetContainer.Add(offerRoot);
            }
        }

        private void ClearRenderedItems()
        {
            for (var i = 0; i < _upgradeItemRoots.Count; i++)
                _upgradeItemRoots[i].RemoveFromHierarchy();

            _upgradeItemRoots.Clear();
            _offerContainer.Clear();
            _gemContainer.Clear();
            _offerContainer.EnableInClassList("shop-offer-grid", false);
            _gemContainer.EnableInClassList("shop-offer-grid", false);
            _offerPanel.style.display = DisplayStyle.None;
            _gemPanel.style.display = DisplayStyle.None;
            _offerOrder.Clear();
            _offerViews.Clear();
            _offerLayout = null;
        }

        private T Require<T>(string elementName) where T : VisualElement
        {
            return _root.Q<T>(elementName)
                   ?? throw new InvalidOperationException(
                       $"ShopPanelUIView: element '{elementName}' was not found.");
        }

        private static VisualTreeAsset LoadTemplate(string resourcePath)
        {
            return Resources.Load<VisualTreeAsset>(resourcePath)
                   ?? throw new MissingReferenceException(
                       $"Shop template was not found at Resources/{resourcePath}.");
        }

        private void OnOfferPurchaseClicked(string offerId) =>
            OfferPurchaseClicked?.Invoke(offerId);
    }
}
