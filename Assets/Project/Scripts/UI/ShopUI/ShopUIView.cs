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
        [SerializeField] private VisualTreeAsset _shopItemTemplate;
        [SerializeField] private UpgradeItemConfig _damageUpgradeConfig;
        [SerializeField] private UpgradeItemConfig _attackSpeedUpgradeConfig;
        [SerializeField] private UpgradeItemConfig _healthUpgradeConfig;
        [SerializeField] private UpgradeItemConfig _critChanceUpgradeConfig;
        [SerializeField] private UpgradeItemConfig _critDamageUpgradeConfig;
        [SerializeField] private UpgradeItemConfig _towerLevelUpgradeConfig;

        private ShopPanelUIView _contentView;
        private Button _closeButton;

        public UpgradeItemConfig DamageUpgradeConfig => _damageUpgradeConfig;
        public UpgradeItemConfig AttackSpeedUpgradeConfig => _attackSpeedUpgradeConfig;
        public UpgradeItemConfig HealthUpgradeConfig => _healthUpgradeConfig;
        public UpgradeItemConfig CritChanceUpgradeConfig => _critChanceUpgradeConfig;
        public UpgradeItemConfig CritDamageUpgradeConfig => _critDamageUpgradeConfig;
        public UpgradeItemConfig TowerLevelUpgradeConfig => _towerLevelUpgradeConfig;

        public event Action CloseButtonClicked;
        public event Action<string> OfferPurchaseClicked;

        public override void Awake()
        {
            base.Awake();
            _closeButton = _root.Q<Button>("CloseButton")
                           ?? throw new InvalidOperationException(
                               "ShopUIView: element 'CloseButton' was not found.");
            _contentView = new ShopPanelUIView(_root, this, _shopItemTemplate);
            _contentView.OfferPurchaseClicked += OnOfferPurchaseClicked;
            _closeButton.clicked += OnCloseButtonClicked;
        }

        public void SetTitle(string title) => _contentView.SetTitle(title);

        public void ClearItems() => _contentView.ClearItems();

        public void AddItem(
            IUpgradeItem item,
            Action onBuy,
            ILocalizationService localizationService) =>
            _contentView.AddItem(item, onBuy, localizationService);

        public void SetOffers(
            IReadOnlyList<ShopOfferUIData> offers,
            EShopOfferPlacementLayout layout) =>
            _contentView.SetOffers(offers, layout);

        public void ShowEmptyState(string text) => _contentView.ShowEmptyState(text);

        private void OnDestroy()
        {
            _closeButton.clicked -= OnCloseButtonClicked;
            _contentView.OfferPurchaseClicked -= OnOfferPurchaseClicked;
            _contentView.Dispose();
        }

        private void OnCloseButtonClicked() => CloseButtonClicked?.Invoke();
        private void OnOfferPurchaseClicked(string offerId) => OfferPurchaseClicked?.Invoke(offerId);
    }
}
