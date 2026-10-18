using System;
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

        void SetTitle(string title);
        void ClearItems();
        void AddItem(IUpgradeItem item, Action onBuy, ILocalizationService localizationService);
    }
}
