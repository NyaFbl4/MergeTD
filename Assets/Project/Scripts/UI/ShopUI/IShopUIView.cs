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

    public interface IShopUIView : ILayoutView, IUpgradeConfigProvider
    {
        event Action CloseButtonClicked;
        event Action<string> TabClicked;
        event Action<string> OfferPurchaseClicked;

        void SetTitle(string title);
        void SetTabs(IReadOnlyList<ShopTabUIData> tabs, string selectedTabId);
        void ClearItems();
        void AddItem(IUpgradeItem item, Action onBuy, ILocalizationService localizationService);
        void SetOffers(IReadOnlyList<ShopOfferUIData> offers);
        void ShowEmptyState(string text);
    }
}
