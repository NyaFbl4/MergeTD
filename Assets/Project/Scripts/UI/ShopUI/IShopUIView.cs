using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay.UpgradeItem;
using Project.Scripts.System.Localization;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.ShopUI
{
    public interface IUpgradeConfigProvider
    {
        UpgradeItemConfig DamageUpgradeConfig { get; }
        UpgradeItemConfig AttackSpeedUpgradeConfig { get; }
        UpgradeItemConfig HealthUpgradeConfig { get; }
        UpgradeItemConfig CritChanceUpgradeConfig { get; }
        UpgradeItemConfig CritDamageUpgradeConfig { get; }
        UpgradeItemConfig TowerLevelUpgradeConfig { get; }
    }

    public interface IShopContentView : IUpgradeConfigProvider
    {
        event Action<string> OfferPurchaseClicked;

        void SetTitle(string title);
        void ClearItems();
        void AddItem(IUpgradeItem item, Action onBuy, ILocalizationService localizationService);
        void SetOffers(
            IReadOnlyList<ShopOfferUIData> offers,
            EShopOfferPlacementLayout layout);
        void ShowEmptyState(string text);
    }

    public interface IShopUIView : ILayoutView, IShopContentView
    {
        event Action CloseButtonClicked;

        void InitializeShop(UIElements uiElements);
    }

    public interface IShopPanelUIView : IShopContentView
    {
        void SetVisible(bool visible);
        void Dispose();
    }
}
