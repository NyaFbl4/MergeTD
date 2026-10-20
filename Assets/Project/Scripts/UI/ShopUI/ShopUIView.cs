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
    public class ShopUIView : LayoutViewBase, IShopUIView
    {
        private const string OfferTemplateResourcePath = "UIToolkit/ShopOfferCard";

        [SerializeField] private VisualTreeAsset _shopItemTemplate;
        [SerializeField] private UpgradeItemConfig _damageUpgradeConfig;
        [SerializeField] private UpgradeItemConfig _attackSpeedUpgradeConfig;
        [SerializeField] private UpgradeItemConfig _healthUpgradeConfig;
        [SerializeField] private UpgradeItemConfig _critChanceUpgradeConfig;
        [SerializeField] private UpgradeItemConfig _critDamageUpgradeConfig;
        [SerializeField] private UpgradeItemConfig _towerLevelUpgradeConfig;

        private readonly List<string> _offerOrder = new();
        private readonly Dictionary<string, ShopOfferItemView> _offerViews = new(StringComparer.Ordinal);

        private VisualTreeAsset _offerItemTemplate;
        private Label _titleLabel;
        private Label _emptyLabel;
        private Button _closeButton;
        private VisualElement _tabsContainer;
        private ScrollView _scrollView;

        public UpgradeItemConfig DamageUpgradeConfig => _damageUpgradeConfig;
        public UpgradeItemConfig AttackSpeedUpgradeConfig => _attackSpeedUpgradeConfig;
        public UpgradeItemConfig HealthUpgradeConfig => _healthUpgradeConfig;
        public UpgradeItemConfig CritChanceUpgradeConfig => _critChanceUpgradeConfig;
        public UpgradeItemConfig CritDamageUpgradeConfig => _critDamageUpgradeConfig;
        public UpgradeItemConfig TowerLevelUpgradeConfig => _towerLevelUpgradeConfig;

        public event Action CloseButtonClicked;
        public event Action<string> TabClicked;
        public event Action<string> OfferPurchaseClicked;

        public override void Awake()
        {
            base.Awake();

            _closeButton = _root.Q<Button>("CloseButton")
                           ?? throw new InvalidOperationException(
                               "ShopUIView: element 'CloseButton' was not found.");
            _scrollView = _root.Q<ScrollView>("ScrollView")
                          ?? throw new InvalidOperationException(
                              "ShopUIView: element 'ScrollView' was not found.");
            _titleLabel = _root.Q<Label>("TitleLabel")
                          ?? throw new InvalidOperationException(
                              "ShopUIView: element 'TitleLabel' was not found.");
            _tabsContainer = _root.Q<VisualElement>("TabsContainer")
                             ?? throw new InvalidOperationException(
                                 "ShopUIView: element 'TabsContainer' was not found.");
            _emptyLabel = _root.Q<Label>("EmptyLabel")
                          ?? throw new InvalidOperationException(
                              "ShopUIView: element 'EmptyLabel' was not found.");
            _offerItemTemplate = Resources.Load<VisualTreeAsset>(OfferTemplateResourcePath)
                                 ?? throw new MissingReferenceException(
                                     $"Shop offer template was not found at Resources/{OfferTemplateResourcePath}.");

            _closeButton.clicked += OnCloseButtonClicked;
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
            _scrollView.Clear();
            _offerOrder.Clear();
            _offerViews.Clear();
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
            _scrollView.Add(itemRoot);
        }

        public void SetOffers(IReadOnlyList<ShopOfferUIData> offers)
        {
            _emptyLabel.style.display = DisplayStyle.None;
            if (RequiresOfferRebuild(offers))
                RebuildOffers(offers);

            for (var i = 0; i < offers.Count; i++)
                _offerViews[offers[i].Id].Bind(offers[i]);
        }

        public void ShowEmptyState(string text)
        {
            ClearItems();
            _emptyLabel.text = text;
            _emptyLabel.style.display = DisplayStyle.Flex;
        }

        private bool RequiresOfferRebuild(IReadOnlyList<ShopOfferUIData> offers)
        {
            if (_offerOrder.Count != offers.Count)
                return true;

            for (var i = 0; i < offers.Count; i++)
            {
                if (!string.Equals(_offerOrder[i], offers[i].Id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void RebuildOffers(IReadOnlyList<ShopOfferUIData> offers)
        {
            _scrollView.Clear();
            _offerOrder.Clear();
            _offerViews.Clear();

            for (var i = 0; i < offers.Count; i++)
            {
                var offer = offers[i];
                var offerRoot = _offerItemTemplate.Instantiate();
                var offerView = new ShopOfferItemView(offerRoot, OnOfferPurchaseClicked);
                _offerOrder.Add(offer.Id);
                _offerViews.Add(offer.Id, offerView);
                _scrollView.Add(offerRoot);
            }
        }

        private void OnDestroy()
        {
            _closeButton.clicked -= OnCloseButtonClicked;
        }

        private void OnCloseButtonClicked() => CloseButtonClicked?.Invoke();
        private void OnOfferPurchaseClicked(string offerId) => OfferPurchaseClicked?.Invoke(offerId);
    }
}
