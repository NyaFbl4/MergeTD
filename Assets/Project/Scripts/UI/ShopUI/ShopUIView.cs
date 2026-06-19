using System;
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

        private Label _titleLabel;
        private Button _closeButton;
        private ScrollView _scrollView;

        public UpgradeItemConfig DamageUpgradeConfig => _damageUpgradeConfig;
        public UpgradeItemConfig AttackSpeedUpgradeConfig => _attackSpeedUpgradeConfig;
        public UpgradeItemConfig HealthUpgradeConfig => _healthUpgradeConfig;
        public UpgradeItemConfig CritChanceUpgradeConfig => _critChanceUpgradeConfig;
        public UpgradeItemConfig CritDamageUpgradeConfig => _critDamageUpgradeConfig;
        public UpgradeItemConfig TowerLevelUpgradeConfig => _towerLevelUpgradeConfig;

        public event Action CloseButtonClicked;

        public override void Awake()
        {
            base.Awake();

            _closeButton = _root.Q<Button>("CloseButton");
            _scrollView = _root.Q<ScrollView>("ScrollView");
            _titleLabel = _root.Q<Label>("TitleLabel");

            if (_closeButton != null)
                _closeButton.clicked += OnCloseButtonClicked;
        }

        public void SetTitle(string title)
        {
            _titleLabel.text = title;
        }

        public void ClearItems()
        {
            _scrollView.Clear();
        }

        public void AddItem(IUpgradeItem item, Action onBuy, ILocalizationService localizationService)
        {
            var itemRoot = _shopItemTemplate.Instantiate();
            var itemView = new UpgradeItemView(itemRoot);
            itemView.Bind(item, onBuy, localizationService);
            _scrollView.Add(itemRoot);
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseButtonClicked;
        }

        private void OnCloseButtonClicked() => CloseButtonClicked?.Invoke();
    }
}