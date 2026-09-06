using System;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.LevelUI
{
    public interface ILevelUIView : ILayoutView
    {
        event Action BuyTowerButtonClicked;
        event Action ShopButtonClicked;
        event Action ADButtonClicked;
        event Action QuestsButtonClicked;
        event Action SettingsButtonClicked;
        event Action NextWaveButtonClicked;
        
        void SetPriceTower(int price);
        void SetMoney(int money);
        void SetTowerIcon(Sprite towerIcon);
        void SetCurrentBaseHealth(int baseHealth);
        void SetMaxBaseHealth(int baseMaxHealth);
        void SetTowerLevel(int towerLevel);
        void SetCurrentWaveText(string text);
        void SetNextWaveButtonEnabled(bool isEnabled);
        void SetTowerActionsEnabled(bool isEnabled);
    }
}
