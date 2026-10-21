using System;
using Project.Scripts.Systems.UI;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.ShopUI
{
    public sealed class ShopGemOfferItemView : IShopOfferItemView
    {
        private readonly VisualElement _root;
        private readonly VisualElement _iconsRoot;
        private readonly VisualElement[] _icons;
        private readonly VisualElement _badge;
        private readonly Label _badgeLabel;
        private readonly Label _amountLabel;
        private readonly Label _priceLabel;
        private readonly Button _buyButton;

        private string _offerId;

        public ShopGemOfferItemView(VisualElement root, Action<string> onBuy)
        {
            _root = root.Q<VisualElement>("GemOfferCard")
                    ?? throw new InvalidOperationException(
                        "Shop gem offer template requires 'GemOfferCard'.");
            _iconsRoot = Require<VisualElement>("GemIcons");
            _icons = new[]
            {
                Require<VisualElement>("GemIconPrimary"),
                Require<VisualElement>("GemIconSecondary"),
                Require<VisualElement>("GemIconTertiary")
            };
            _badge = Require<VisualElement>("BonusBadge");
            _badgeLabel = Require<Label>("BonusLabel");
            _amountLabel = Require<Label>("AmountLabel");
            _priceLabel = Require<Label>("PriceLabel");
            _buyButton = Require<Button>("BuyButton");

            _buyButton.clicked += () => onBuy(_offerId);
            UIButtonAnimationUtility.EnableDefault(_buyButton);
        }

        public void Bind(ShopOfferUIData offer)
        {
            if (offer.Rewards.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Gem grid offer '{offer.Id}' requires exactly one reward.");
            }

            _offerId = offer.Id;
            _root.tooltip = offer.Status;
            _amountLabel.text = $"×{offer.Rewards[0].Text}";
            _priceLabel.text = string.IsNullOrWhiteSpace(offer.Price) ? "—" : offer.Price;
            _badgeLabel.text = offer.Badge;
            _badge.style.display = string.IsNullOrWhiteSpace(offer.Badge)
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            _iconsRoot.EnableInClassList("shop-gem-icons--one", offer.VisualIconCount == 1);
            _iconsRoot.EnableInClassList("shop-gem-icons--two", offer.VisualIconCount == 2);
            _iconsRoot.EnableInClassList("shop-gem-icons--three", offer.VisualIconCount == 3);
            for (var i = 0; i < _icons.Length; i++)
            {
                _icons[i].style.backgroundImage = new StyleBackground(offer.MainIcon);
                _icons[i].style.display = i < offer.VisualIconCount
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            _buyButton.SetEnabled(offer.CanPurchase);
            _root.EnableInClassList("shop-gem-card--disabled", !offer.CanPurchase);
        }

        private T Require<T>(string name) where T : VisualElement
        {
            return _root.Q<T>(name)
                   ?? throw new InvalidOperationException(
                       $"Shop gem offer template requires '{name}'.");
        }
    }
}
