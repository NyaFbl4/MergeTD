using Project.Scripts.Systems.UI;
using Project.Scripts.Configs;

using System;
using Project.Scripts.UI.InBattleUI;
using Project.Scripts.UI.ArmyUI;
using Project.Scripts.UI.PurchaseWindowUI;
using Project.Scripts.UI.SpellsUI;
using Project.Scripts.Gameplay.UpgradeItem;
using Project.Scripts.System.Localization;

namespace Project.Scripts.UI.MainMenuUI
{
    public enum MainMenuSection
    {
        Shop,
        Army,
        Battle,
        Spells,
        Base
    }

    public interface IMainMenuUIView : ILayoutView
    {
        event Action<MainMenuSection> SectionClicked;
        event Action SettingsButtonClicked;

        IInBattleUIView InBattleView { get; }
        IArmyUIView ArmyView { get; }
        IPurchaseWindowView PurchaseWindowView { get; }
        ISpellsPanelUIView SpellsPanelView { get; }

        void InitializeArmy(UIElements uiElements);
        void InitializePurchaseWindow(UIElements uiElements);
        void InitializeSpells(
            UIElements uiElements,
            ILocalizationService localizationService);
        void SetBaseUpgradesTitle(string title);
        void ClearBaseUpgrades();
        void AddBaseUpgrade(
            IBaseUpgradeItem item,
            bool canPurchase,
            Action onBuy,
            ILocalizationService localizationService);
        void SetGoldCount(int goldCount);
        void SetDiamondCount(int diamondCount);
        void SetActiveSection(MainMenuSection section);
    }
}
