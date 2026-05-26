using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.ShopUI
{
    public class UpgradeItem
    {
        private VisualElement _root;
        private Label _description;
        private Label _price;
        private Button _buyButton;
        private List<VisualElement> _levelStars = new();
        
        private int _currentLevel;
        private int _currentPrice;

        public UpgradeItem(VisualElement root)
        {
            _root = root;
            _description = root.Q<Label>("description");
            _price = root.Q<Label>("price");
            _buyButton = root.Q<Button>("buy-button");
        }

        public void Bind(UpgradeViewConfig data, Action onBuy)
        {
            _description.text = data.Description;
            _price.text = data.PriceOnLevel[0].ToString();
            _buyButton.clicked += onBuy;
        }
    }
}