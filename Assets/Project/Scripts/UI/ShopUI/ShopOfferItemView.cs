using System;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.ShopUI
{
    public sealed class ShopOfferItemView
    {
        private readonly VisualElement _root;
        private readonly VisualElement _mainIcon;
        private readonly Label _titleLabel;
        private readonly Label _descriptionLabel;
        private readonly VisualElement[] _rewardPanels;
        private readonly VisualElement[] _rewardIcons;
        private readonly Label[] _rewardLabels;
        private readonly VisualElement _priceIcon;
        private readonly Label _priceLabel;
        private readonly Label _limitLabel;
        private readonly Label _timerLabel;
        private readonly Label _statusLabel;
        private readonly Button _buyButton;

        private string _offerId;

        public ShopOfferItemView(VisualElement root, Action<string> onBuy)
        {
            _root = root.Q<VisualElement>("OfferCard")
                    ?? throw new InvalidOperationException("Shop offer template requires 'OfferCard'.");
            _mainIcon = Require<VisualElement>("MainIcon");
            _titleLabel = Require<Label>("TitleLabel");
            _descriptionLabel = Require<Label>("DescriptionLabel");
            _priceIcon = Require<VisualElement>("PriceIcon");
            _priceLabel = Require<Label>("PriceLabel");
            _limitLabel = Require<Label>("LimitLabel");
            _timerLabel = Require<Label>("TimerLabel");
            _statusLabel = Require<Label>("StatusLabel");
            _buyButton = Require<Button>("BuyButton");
            _rewardPanels = new[]
            {
                Require<VisualElement>("PrimaryReward"),
                Require<VisualElement>("SecondaryReward")
            };
            _rewardIcons = new[]
            {
                Require<VisualElement>("PrimaryRewardIcon"),
                Require<VisualElement>("SecondaryRewardIcon")
            };
            _rewardLabels = new[]
            {
                Require<Label>("PrimaryRewardLabel"),
                Require<Label>("SecondaryRewardLabel")
            };

            _buyButton.clicked += () => onBuy(_offerId);
            UIButtonAnimationUtility.EnableDefault(_buyButton);
        }

        public void Bind(ShopOfferUIData offer)
        {
            _offerId = offer.Id;
            _titleLabel.text = offer.Title;
            _descriptionLabel.text = offer.Description;
            _priceLabel.text = offer.Price;
            _limitLabel.text = offer.Limit;
            _timerLabel.text = offer.Timer;
            _statusLabel.text = offer.Status;
            SetImage(_mainIcon, offer.MainIcon);
            SetImage(_priceIcon, offer.PriceIcon);
            _priceIcon.style.display = offer.PriceIcon != null ? DisplayStyle.Flex : DisplayStyle.None;

            for (var i = 0; i < _rewardPanels.Length; i++)
            {
                var visible = i < offer.Rewards.Count;
                _rewardPanels[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible)
                    continue;

                SetImage(_rewardIcons[i], offer.Rewards[i].Icon);
                _rewardLabels[i].text = offer.Rewards[i].Text;
            }

            _limitLabel.style.display = string.IsNullOrWhiteSpace(offer.Limit)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            _timerLabel.style.display = string.IsNullOrWhiteSpace(offer.Timer)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            _statusLabel.style.display = string.IsNullOrWhiteSpace(offer.Status)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            _buyButton.SetEnabled(offer.CanPurchase);
            _root.EnableInClassList("shop-offer-card--disabled", !offer.CanPurchase);
        }

        private T Require<T>(string name) where T : VisualElement
        {
            return _root.Q<T>(name)
                   ?? throw new InvalidOperationException(
                       $"Shop offer template requires '{name}'.");
        }

        private static void SetImage(VisualElement element, Sprite sprite)
        {
            if (sprite != null)
                element.style.backgroundImage = new StyleBackground(sprite);
            else
                element.style.backgroundImage = StyleKeyword.None;
        }
    }
}
