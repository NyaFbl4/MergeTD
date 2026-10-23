using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.UI.ShopUI
{
    public interface IShopOfferItemView
    {
        void Bind(ShopOfferUIData offer);
    }

    public sealed class ShopOfferRewardUIData
    {
        public Sprite Icon { get; }
        public string Text { get; }

        public ShopOfferRewardUIData(Sprite icon, string text)
        {
            Icon = icon;
            Text = text;
        }
    }

    public sealed class ShopOfferUIData
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public Sprite MainIcon { get; }
        public IReadOnlyList<ShopOfferRewardUIData> Rewards { get; }
        public Sprite PriceIcon { get; }
        public string Price { get; }
        public string Badge { get; }
        public int VisualIconCount { get; }
        public string Limit { get; }
        public string Timer { get; }
        public string Status { get; }
        public bool CanPurchase { get; }

        public ShopOfferUIData(
            string id,
            string title,
            string description,
            Sprite mainIcon,
            IReadOnlyList<ShopOfferRewardUIData> rewards,
            Sprite priceIcon,
            string price,
            string badge,
            int visualIconCount,
            string limit,
            string timer,
            string status,
            bool canPurchase)
        {
            Id = id;
            Title = title;
            Description = description;
            MainIcon = mainIcon;
            Rewards = rewards;
            PriceIcon = priceIcon;
            Price = price;
            Badge = badge;
            VisualIconCount = visualIconCount;
            Limit = limit;
            Timer = timer;
            Status = status;
            CanPurchase = canPurchase;
        }
    }
}
