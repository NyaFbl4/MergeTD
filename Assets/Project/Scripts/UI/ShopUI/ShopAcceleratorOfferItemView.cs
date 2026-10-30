using System;
using Project.Scripts.Systems.UI;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.ShopUI
{
    public sealed class ShopAcceleratorOfferItemView : IShopOfferItemView
    {
        private readonly VisualElement _root;
        private readonly VisualElement[] _icons;
        private readonly VisualElement _badge;
        private readonly Label _badgeLabel;
        private readonly Label _titleLabel;
        private readonly Label _amountLabel;
        private readonly VisualElement _priceIcon;
        private readonly Label _priceLabel;
        private readonly Label _limitLabel;
        private readonly Label _timerLabel;
        private readonly Label _statusLabel;
        private readonly Button _buyButton;
        private string _offerId;

        public ShopAcceleratorOfferItemView(VisualElement root, Action<string> onBuy)
        {
            _root = root.Q<VisualElement>("AcceleratorOfferCard")
                    ?? throw new InvalidOperationException("Accelerator offer template requires 'AcceleratorOfferCard'.");
            _icons = new[]
            {
                Require<VisualElement>("AcceleratorIconPrimary"),
                Require<VisualElement>("AcceleratorIconSecondary"),
                Require<VisualElement>("AcceleratorIconTertiary")
            };
            _badge = Require<VisualElement>("BonusBadge");
            _badgeLabel = Require<Label>("BonusLabel");
            _titleLabel = Require<Label>("TitleLabel");
            _amountLabel = Require<Label>("AmountLabel");
            _priceIcon = Require<VisualElement>("PriceIcon");
            _priceLabel = Require<Label>("PriceLabel");
            _limitLabel = Require<Label>("LimitLabel");
            _timerLabel = Require<Label>("TimerLabel");
            _statusLabel = Require<Label>("StatusLabel");
            _buyButton = Require<Button>("BuyButton");
            _buyButton.clicked += () => onBuy(_offerId);
            UIButtonAnimationUtility.EnableDefault(_buyButton);
        }

        public void Bind(ShopOfferUIData offer)
        {
            if (offer.Rewards.Count != 1)
                throw new InvalidOperationException($"Accelerator offer '{offer.Id}' requires exactly one reward.");

            _offerId = offer.Id;
            _root.tooltip = offer.Status;
            _titleLabel.text = offer.Title;
            _amountLabel.text = $"+{offer.Rewards[0].Text}";
            _priceLabel.text = string.IsNullOrWhiteSpace(offer.Price) ? "—" : offer.Price;
            _badgeLabel.text = offer.Badge;
            _badge.style.display = string.IsNullOrWhiteSpace(offer.Badge) ? DisplayStyle.None : DisplayStyle.Flex;
            _limitLabel.text = offer.Limit;
            _timerLabel.text = offer.Timer;
            _statusLabel.text = offer.Status;
            _priceIcon.style.display = offer.PriceIcon == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (offer.PriceIcon != null)
                _priceIcon.style.backgroundImage = new StyleBackground(offer.PriceIcon);

            for (var i = 0; i < _icons.Length; i++)
            {
                _icons[i].style.backgroundImage = new StyleBackground(offer.MainIcon);
                _icons[i].style.display = i < offer.VisualIconCount ? DisplayStyle.Flex : DisplayStyle.None;
            }

            _buyButton.SetEnabled(offer.CanPurchase);
        }

        private T Require<T>(string name) where T : VisualElement =>
            _root.Q<T>(name) ?? throw new InvalidOperationException($"Accelerator offer template requires '{name}'.");
    }
}
