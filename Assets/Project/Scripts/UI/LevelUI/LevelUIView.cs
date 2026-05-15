using System;
using Project.Scripts.Systems.UI;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.LevelUI
{
    public class LevelUIView : LayoutViewBase, ILevelUIView
    {
        private Button _payTowerButton;
        private Button _shopButton;
        private Label _payTowerLabel;
        private Label _moneyLabel;
        
        public event Action BuyTowerButtonClicked;
        public event Action ShopButtonClicked;

        public override void Awake()
        {
            base.Awake();
            
            _payTowerButton = _root.Q<Button>("PayTowerButton");
            _shopButton = _root.Q<Button>("ShopButton");
            _payTowerLabel = _root.Q<Label>("PayTowerLabel");
            _moneyLabel =  _root.Q<Label>("MoneyLabel");
            
            if (_payTowerButton != null)
                _payTowerButton.clicked += OnBuyTowerButtonClicked;
            if (_shopButton != null)
                _shopButton.clicked += OnShopButtonClicked;
        }
        
        public void SetPriceTower(int price)
        {
            _payTowerLabel.text = price.ToString();
        }

        public void SetMoney(int money)
        {
            _moneyLabel.text = money.ToString();
        }
        
        private void OnBuyTowerButtonClicked() => BuyTowerButtonClicked?.Invoke();
        private void OnShopButtonClicked() => ShopButtonClicked?.Invoke();

        private void OnDestroy()
        {
            if (_payTowerButton != null)
                _payTowerButton.clicked -= OnBuyTowerButtonClicked;
            if (_shopButton != null)
                _shopButton.clicked -= OnShopButtonClicked;
        }
    }
}
