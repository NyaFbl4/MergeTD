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
        private Button _questsButton;
        private Label _payTowerLabel;
        private Label _moneyLabel;
        private Label _currentBaseHealthLabel;
        private Label _maxBaseHealthLabel;
        private VisualElement _payButtonTowerIcon;
        private VisualElement _adButtonTowerIcon;
        
        public event Action BuyTowerButtonClicked;
        public event Action ShopButtonClicked;
        public event Action ADButtonClicked;
        public event Action QuestsButtonClicked;

        public override void Awake()
        {
            base.Awake();
            
            _payTowerButton = _root.Q<Button>("PayTowerButton");
            _payButtonTowerIcon = _payTowerButton.Q<VisualElement>("TowerIcon");
            _shopButton = _root.Q<Button>("ShopButton");
            _adButton = _root.Q<Button>("ADButton");
            _questsButton =  _root.Q<Button>("QuestsButton");
            _adButtonTowerIcon = _adButton.Q<VisualElement>("TowerIcon");
            _payTowerLabel = _root.Q<Label>("PayTowerLabel");
            _moneyLabel = _root.Q<Label>("MoneyLabel");
            _currentBaseHealthLabel =  _root.Q<Label>("CurrentBaseHealthLabel");
            _maxBaseHealthLabel = _root.Q<Label>("MaxBaseHealthLabel");
            
            if (_payTowerButton != null)
                _payTowerButton.clicked += OnBuyTowerButtonClicked;
            if (_shopButton != null)
                _shopButton.clicked += OnShopButtonClicked;
            if( _adButton != null)
                _adButton.clicked += OnADButtonClicked;
            if (_questsButton != null)
                _questsButton.clicked += OnQuestsButtonClicked;
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

        public void SetTowerLevel(int towerLevel)
        {
            var towerLabel1 = _payButtonTowerIcon.Q<Label>("TowerLeveLabel");
            towerLabel1.text = towerLevel.ToString();
            var towerLabel2 = _adButtonTowerIcon.Q<Label>("TowerLeveLabel");
            towerLabel2.text = towerLevel.ToString();
        }

        public void SetCurrentBaseHealth(int baseHealth)
        {
            _currentBaseHealthLabel.text = baseHealth.ToString();
        }

        public void SetMaxBaseHealth(int baseMaxHealth)
        {
            _maxBaseHealthLabel.text = baseMaxHealth.ToString();
        }

        private void OnBuyTowerButtonClicked() => BuyTowerButtonClicked?.Invoke();
        private void OnShopButtonClicked() => ShopButtonClicked?.Invoke();
        private void OnADButtonClicked() => ADButtonClicked?.Invoke();
        private void OnQuestsButtonClicked() => QuestsButtonClicked?.Invoke();

        private void OnDestroy()
        {
            if (_payTowerButton != null)
                _payTowerButton.clicked -= OnBuyTowerButtonClicked;
            if (_shopButton != null)
                _shopButton.clicked -= OnShopButtonClicked;
            if (_adButton != null)
                _adButton.clicked -= OnADButtonClicked;
            if (_questsButton != null)
                _questsButton.clicked -= OnQuestsButtonClicked;
        }
    }
}
