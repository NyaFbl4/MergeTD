using System;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Systems.UI;
using Project.Scripts.UI.PurchaseWindowUI;
using Project.Scripts.UI.TowerSlotUnlockUI;
using Project.Scripts.UI.CardChoiceUI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.LevelUI
{
    public interface ILevelUIView : ILayoutView
    {
        event Action BuyTowerButtonClicked;
        event Action BuyGeneratorButtonClicked;
        event Action ShopButtonClicked;
        event Action ADButtonClicked;
        event Action QuestsButtonClicked;
        event Action SettingsButtonClicked;
        event Action NextWaveButtonClicked;
        event Action SetSpellsButtonClicked;
        event Action SetTowersButtonClicked;
        event Action WeaponBarrageButtonClicked;
        event Action BaseRepairButtonClicked;
        event Action BarrageProtocolButtonClicked;
        event Action CryoDischargeButtonClicked;
        event Action EmpPulseButtonClicked;
        event Action OrbitalRailgunButtonClicked;
        event Action GravityTrapButtonClicked;
        
        ITowerSlotUnlockView TowerSlotUnlockView { get; }
        void InitializeTowerSlotUnlock(UIElements uiElements);
        IPurchaseWindowView PurchaseWindowView { get; }
        ICardChoiceView CardChoiceView { get; }

        void InitializePurchaseWindow(UIElements uiElements);
        void InitializeCardChoice();
        void SetPriceTower(int price);
        void SetGeneratorPrice(int price);
        void SetGeneratorPurchaseEnabled(bool isEnabled);
        void SetEnergy(double current, int maximum);
        void SetMoney(int money);
        void SetGems(int gems);
        void SetTowerIcon(Sprite towerIcon);
        void SetCurrentBaseHealth(int baseHealth);
        void SetMaxBaseHealth(int baseMaxHealth);
        void SetTowerLevel(int towerLevel);
        void SetCurrentWaveText(string text);
        void SetRunPhase(ERunPhase phase);
        void SetSpawnWarning(bool visible, string text);
        void SetNextWaveButtonEnabled(bool isEnabled);
        void SetTowerActionsEnabled(bool isEnabled);
        void SetSpellVisible(string spellId, bool isVisible);
        void SetSpellIcon(string spellId, Sprite icon);
        void SetSpellBackground(string spellId, Sprite background);
        void SetWeaponBarrageState(int cooldownSeconds, bool isTargeting, bool isEnabled);
        void SetBaseRepairState(string displayName, int healAmount, int cooldownSeconds, bool isBaseFull, bool isEnabled);
        void SetBarrageProtocolState(
            int attackSpeedBonusPercent,
            int activeSeconds,
            int cooldownSeconds,
            bool hasCombatTower,
            bool isEnabled);
        void SetCryoDischargeState(int cooldownSeconds, bool isTargeting, bool isEnabled);
        void SetEmpPulseState(int cooldownSeconds, bool isTargeting, bool isEnabled);
        void SetOrbitalRailgunState(
            int cooldownSeconds,
            bool isTargeting,
            bool isCasting,
            bool isEnabled);
        void SetGravityTrapState(
            int activeSeconds,
            int cooldownSeconds,
            bool isTargeting,
            bool isEnabled);
        void SetSpellManaCost(string spellId, int manaCost);
    }

}
