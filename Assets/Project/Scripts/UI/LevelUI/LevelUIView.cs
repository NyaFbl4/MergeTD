using System;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.LevelUI
{
    public class LevelUIView : LayoutViewBase, ILevelUIView
    {
        private Button _payTowerButton;
        private Button _shopButton;
        private Button _adButton;
        private Label _payTowerLabel;
        private Label _moneyLabel;
        private VisualElement _payButtonTowerIcon;
        private VisualElement _adButtonTowerIcon;
        
        public event Action BuyTowerButtonClicked;
        public event Action ShopButtonClicked;
        public event Action ADButtonClicked;

        public override void Awake()
        {
            base.Awake();
            
            _payTowerButton = _root.Q<Button>("PayTowerButton");
            _payButtonTowerIcon = _payTowerButton.Q<VisualElement>("TowerIcon");
            _shopButton = _root.Q<Button>("ShopButton");
            _adButton = _root.Q<Button>("ADButton");
            _adButtonTowerIcon = _adButton.Q<VisualElement>("TowerIcon");
            _payTowerLabel = _root.Q<Label>("PayTowerLabel");
            _moneyLabel =  _root.Q<Label>("MoneyLabel");
            
            if (_payTowerButton != null)
                _payTowerButton.clicked += OnBuyTowerButtonClicked;
            if (_shopButton != null)
                _shopButton.clicked += OnShopButtonClicked;
            if( _adButton != null)
                _adButton.clicked += OnADButtonClicked;
        }
        
        public void SetPriceTower(int price)
        {
            _payTowerLabel.text = price.ToString();
        }

        public void SetMoney(int money)
        {
            _moneyLabel.text = money.ToString();
        }

        public void SetTowerIcon(Sprite towerIcon)
        {
            _payButtonTowerIcon.style.backgroundImage = new StyleBackground(towerIcon);
            _adButtonTowerIcon.style.backgroundImage = new StyleBackground(towerIcon);
        }
        
        private void OnBuyTowerButtonClicked() => BuyTowerButtonClicked?.Invoke();
        private void OnShopButtonClicked() => ShopButtonClicked?.Invoke();
        private void OnADButtonClicked() => ADButtonClicked?.Invoke();

        private void OnDestroy()
        {
            if (_payTowerButton != null)
                _payTowerButton.clicked -= OnBuyTowerButtonClicked;
            if (_shopButton != null)
                _shopButton.clicked -= OnShopButtonClicked;
            if (_adButton != null)
                _adButton.clicked -= OnADButtonClicked;
        }
    }
}
