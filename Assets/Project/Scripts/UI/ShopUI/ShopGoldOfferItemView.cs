using System;
using Project.Scripts.Systems.UI;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.ShopUI
{
    public sealed class ShopGoldOfferItemView : IShopOfferItemView
    {
        private readonly VisualElement _root;
        private readonly VisualElement _icon;
        private readonly VisualElement _badge;
        private readonly Label _badgeLabel;
        private readonly Label _amountLabel;
        private readonly VisualElement _priceIcon;
        private readonly Label _priceLabel;
        private readonly Button _buyButton;
        private string _offerId;

        public ShopGoldOfferItemView(VisualElement root, Action<string> onBuy)
        {
            _root = root.Q<VisualElement>("GoldOfferCard")
                    ?? throw new InvalidOperationException("Shop gold offer template requires 'GoldOfferCard'.");
            _icon = Require<VisualElement>("GoldIcon");
            _badge = Require<VisualElement>("BonusBadge");
            _badgeLabel = Require<Label>("BonusLabel");
            _amountLabel = Require<Label>("AmountLabel");
            _priceIcon = Require<VisualElement>("PriceIcon");
            _priceLabel = Require<Label>("PriceLabel");
            _buyButton = Require<Button>("BuyButton");
            _buyButton.clicked += () => onBuy(_offerId);
            UIButtonAnimationUtility.EnableDefault(_buyButton);
        }

        public void Bind(ShopOfferUIData offer)
        {
            if (offer.Rewards.Count != 1)
                throw new InvalidOperationException($"Gold grid offer '{offer.Id}' requires exactly one reward.");

            _offerId = offer.Id;
            _root.tooltip = offer.Status;
            _icon.style.backgroundImage = new StyleBackground(offer.MainIcon);
            _amountLabel.text = $"×{offer.Rewards[0].Text}";
            _priceLabel.text = offer.Price;
            _priceIcon.style.backgroundImage = new StyleBackground(offer.PriceIcon);
            _badgeLabel.text = offer.Badge;
            _badge.style.display = string.IsNullOrWhiteSpace(offer.Badge) ? DisplayStyle.None : DisplayStyle.Flex;
            _buyButton.SetEnabled(offer.CanPurchase);
            _root.EnableInClassList("shop-gem-card--disabled", !offer.CanPurchase);
        }

        private T Require<T>(string name) where T : VisualElement =>
            _root.Q<T>(name)
            ?? throw new InvalidOperationException($"Shop gold offer template requires '{name}'.");
    }
}
