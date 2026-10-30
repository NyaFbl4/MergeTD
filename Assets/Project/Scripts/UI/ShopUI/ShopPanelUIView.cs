using Project.Scripts.System.Analytics;
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

        private readonly Dictionary<string, (VisualElement Root, string Placement)> _analyticsOffers = new();
        private readonly HashSet<string> _seenOffers = new();
        private readonly IVisualElementScheduledItem _analyticsSchedule;
        private bool _shopWasVisible;
        private readonly VisualElement _root;
        private readonly IUpgradeConfigProvider _upgradeConfigs;
        private readonly VisualTreeAsset _shopItemTemplate;
        private readonly VisualTreeAsset _offerItemTemplate;
        private readonly VisualTreeAsset _gemOfferItemTemplate;
        private readonly VisualTreeAsset _energyOfferItemTemplate;
        private readonly VisualTreeAsset _acceleratorOfferItemTemplate;
        private readonly VisualTreeAsset _goldOfferItemTemplate;
        private readonly Label _titleLabel;
        private readonly Label _emptyLabel;
        private readonly ScrollView _scrollView;
        private readonly VisualElement _offerPanel;
        private readonly VisualElement _offerContainer;
        private readonly VisualElement _gemPanel;
        private readonly VisualElement _gemContainer;
        private readonly VisualElement _energyPanel;
        private readonly VisualElement _energyContainer;
        private readonly VisualElement _acceleratorPanel;
        private readonly VisualElement _acceleratorContainer;
        private readonly VisualElement _goldPanel;
        private readonly VisualElement _goldContainer;
        private readonly List<VisualElement> _upgradeItemRoots = new();
        private readonly List<string> _offerOrder = new();
        private readonly List<string> _gemOfferOrder = new();
        private readonly List<string> _energyOfferOrder = new();
        private readonly List<string> _acceleratorOfferOrder = new();
        private readonly List<string> _goldOfferOrder = new();
        private readonly Dictionary<string, IShopOfferItemView> _offerViews =
            new(StringComparer.Ordinal);

        public UpgradeItemConfig DamageUpgradeConfig => _upgradeConfigs.DamageUpgradeConfig;
        public UpgradeItemConfig AttackSpeedUpgradeConfig => _upgradeConfigs.AttackSpeedUpgradeConfig;
        public UpgradeItemConfig HealthUpgradeConfig => _upgradeConfigs.HealthUpgradeConfig;
        public UpgradeItemConfig CritChanceUpgradeConfig => _upgradeConfigs.CritChanceUpgradeConfig;
        public UpgradeItemConfig CritDamageUpgradeConfig => _upgradeConfigs.CritDamageUpgradeConfig;
        public UpgradeItemConfig TowerLevelUpgradeConfig => _upgradeConfigs.TowerLevelUpgradeConfig;

        public event Action<string> OfferPurchaseClicked;

        public ShopPanelUIView(
            VisualElement root,
            IUpgradeConfigProvider upgradeConfigs,
            UIElements uiElements,
            VisualTreeAsset upgradeItemTemplate = null)
        {
            _root = root;
            _upgradeConfigs = upgradeConfigs;
            _titleLabel = Require<Label>("TitleLabel");
            _scrollView = Require<ScrollView>("ScrollView");
            _offerPanel = Require<VisualElement>("OfferPanel");
            _offerContainer = Require<VisualElement>("OfferContainer");
            _gemPanel = Require<VisualElement>("GemPanel");
            _gemContainer = Require<VisualElement>("GemContainer");
            _energyPanel = Require<VisualElement>("EnergyOffersPanel");
            _energyContainer = Require<VisualElement>("EnergyContainer");
            _acceleratorPanel = Require<VisualElement>("AcceleratorOffersPanel");
            _acceleratorContainer = Require<VisualElement>("AcceleratorContainer");
            _goldPanel = Require<VisualElement>("GoldPanel");
            _goldContainer = Require<VisualElement>("GoldContainer");
            _emptyLabel = Require<Label>("EmptyLabel");
            _shopItemTemplate = upgradeItemTemplate ?? LoadTemplate(UpgradeTemplateResourcePath);
            _offerItemTemplate = uiElements.ShopOfferCard;
            _gemOfferItemTemplate = uiElements.ShopGemOfferCard;
            _energyOfferItemTemplate = uiElements.ShopEnergyOfferCard;
            _acceleratorOfferItemTemplate = uiElements.ShopAcceleratorOfferCard;
            _goldOfferItemTemplate = uiElements.ShopGoldOfferCard;
            _analyticsSchedule = _root.schedule.Execute(TrackVisibleOffers).Every(250);
        }

        public void SetVisible(bool visible)
        {
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetTitle(string title) => _titleLabel.text = title;

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
            var panel = layout switch
            {
                EShopOfferPlacementLayout.GemGrid => _gemPanel,
                EShopOfferPlacementLayout.EnergyGrid => _energyPanel,
                EShopOfferPlacementLayout.AcceleratorGrid => _acceleratorPanel,
                EShopOfferPlacementLayout.GoldGrid => _goldPanel,
                _ => _offerPanel
            };
            panel.style.display = offers.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
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
            _analyticsSchedule.Pause();
            ClearRenderedItems();
            OfferPurchaseClicked = null;
        }

        private bool RequiresOfferRebuild(
            IReadOnlyList<ShopOfferUIData> offers,
            EShopOfferPlacementLayout layout)
        {
            var order = GetOfferOrder(layout);
            if (order.Count != offers.Count)
                return true;

            for (var i = 0; i < offers.Count; i++)
            {
                if (!string.Equals(order[i], offers[i].Id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void RebuildOffers(
            IReadOnlyList<ShopOfferUIData> offers,
            EShopOfferPlacementLayout layout)
        {
            var useGemGrid = layout == EShopOfferPlacementLayout.GemGrid;
            var useEnergyGrid = layout == EShopOfferPlacementLayout.EnergyGrid;
            var useAcceleratorGrid = layout == EShopOfferPlacementLayout.AcceleratorGrid;
            var useGoldGrid = layout == EShopOfferPlacementLayout.GoldGrid;
            var targetContainer = layout switch
            {
                EShopOfferPlacementLayout.GemGrid => _gemContainer,
                EShopOfferPlacementLayout.EnergyGrid => _energyContainer,
                EShopOfferPlacementLayout.AcceleratorGrid => _acceleratorContainer,
                EShopOfferPlacementLayout.GoldGrid => _goldContainer,
                _ => _offerContainer
            };
            var template = layout switch
            {
                EShopOfferPlacementLayout.GemGrid => _gemOfferItemTemplate,
                EShopOfferPlacementLayout.EnergyGrid => _energyOfferItemTemplate,
                EShopOfferPlacementLayout.AcceleratorGrid => _acceleratorOfferItemTemplate,
                EShopOfferPlacementLayout.GoldGrid => _goldOfferItemTemplate,
                _ => _offerItemTemplate
            };
            var order = GetOfferOrder(layout);
            for (var i = 0; i < order.Count; i++)
            {
                _offerViews.Remove(order[i]);
                _analyticsOffers.Remove(order[i]);
            }

            order.Clear();
            targetContainer.Clear();

            for (var i = 0; i < offers.Count; i++)
            {
                var offer = offers[i];
                var offerRoot = template.Instantiate();
                IShopOfferItemView offerView;
                if (useAcceleratorGrid)
                {
                    offerView = new ShopAcceleratorOfferItemView(offerRoot, OnOfferPurchaseClicked);
                }
                else if (useEnergyGrid)
                {
                    offerView = new ShopEnergyOfferItemView(offerRoot, OnOfferPurchaseClicked);
                }
                else if (useGemGrid)
                {
                    offerRoot.AddToClassList("shop-gem-card-container");
                    offerView = new ShopGemOfferItemView(offerRoot, OnOfferPurchaseClicked);
                }
                else if (useGoldGrid)
                {
                    offerRoot.AddToClassList("shop-gem-card-container");
                    offerView = new ShopGoldOfferItemView(offerRoot, OnOfferPurchaseClicked);
                }
                else
                {
                    offerView = new ShopOfferItemView(offerRoot, OnOfferPurchaseClicked);
                }

                order.Add(offer.Id);
                _offerViews.Add(offer.Id, offerView);
                targetContainer.Add(offerRoot);
                _analyticsOffers[offer.Id] = (offerRoot, layout.ToString());
            }
        }

        private void ClearRenderedItems()
        {
            for (var i = 0; i < _upgradeItemRoots.Count; i++)
                _upgradeItemRoots[i].RemoveFromHierarchy();

            _upgradeItemRoots.Clear();
            _offerContainer.Clear();
            _gemContainer.Clear();
            _energyContainer.Clear();
            _acceleratorContainer.Clear();
            _goldContainer.Clear();
            _offerPanel.style.display = DisplayStyle.None;
            _gemPanel.style.display = DisplayStyle.None;
            _energyPanel.style.display = DisplayStyle.None;
            _acceleratorPanel.style.display = DisplayStyle.None;
            _goldPanel.style.display = DisplayStyle.None;
            _offerOrder.Clear();
            _gemOfferOrder.Clear();
            _energyOfferOrder.Clear();
            _acceleratorOfferOrder.Clear();
            _goldOfferOrder.Clear();
            _offerViews.Clear();
            _analyticsOffers.Clear();
        }

        private List<string> GetOfferOrder(EShopOfferPlacementLayout layout) =>
            layout switch
            {
                EShopOfferPlacementLayout.GemGrid => _gemOfferOrder,
                EShopOfferPlacementLayout.EnergyGrid => _energyOfferOrder,
                EShopOfferPlacementLayout.AcceleratorGrid => _acceleratorOfferOrder,
                EShopOfferPlacementLayout.GoldGrid => _goldOfferOrder,
                _ => _offerOrder
            };

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

        private void TrackVisibleOffers()
        {
            if (!IsVisible(_root))
            {
                _shopWasVisible = false;
                _seenOffers.Clear();
                return;
            }
            if (!_shopWasVisible)
            {
                _shopWasVisible = true;
                GameAnalytics.Track("shop_open", ("source", _root.name));
            }
            foreach (var pair in _analyticsOffers)
            {
                if (!_seenOffers.Contains(pair.Key) && IsVisible(pair.Value.Root))
                {
                    _seenOffers.Add(pair.Key);
                    GameAnalytics.Track("offer_view", ("offer_id", pair.Key), ("placement", pair.Value.Placement));
                }
            }
        }

        private static bool IsVisible(VisualElement element)
        {
            if (element.panel == null) return false;
            var visible = element.worldBound;
            if (visible.width <= 0f || visible.height <= 0f || float.IsNaN(visible.width)) return false;
            for (var ancestor = element; ancestor != null; ancestor = ancestor.parent)
            {
                var style = ancestor.resolvedStyle;
                if (style.display == DisplayStyle.None || style.visibility == Visibility.Hidden || style.opacity <= 0f)
                    return false;
                if (ancestor is not ScrollView && ancestor != element.panel.visualTree) continue;
                var clip = ancestor is ScrollView scroll ? scroll.contentViewport.worldBound : ancestor.worldBound;
                visible = Rect.MinMaxRect(Mathf.Max(visible.xMin, clip.xMin), Mathf.Max(visible.yMin, clip.yMin),
                    Mathf.Min(visible.xMax, clip.xMax), Mathf.Min(visible.yMax, clip.yMax));
                if (visible.width <= 0f || visible.height <= 0f) return false;
            }
            return true;
        }

        private void OnOfferPurchaseClicked(string offerId) =>
            OfferPurchaseClicked?.Invoke(offerId);
    }
}
