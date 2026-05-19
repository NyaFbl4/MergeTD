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
        
        void SetPriceTower(int price);
        void SetMoney(int money);
        void SetTowerIcon(Sprite towerIcon);
        void SetCurrentBaseHealth(int baseHealth);
        void SetMaxBaseHealth(int baseMaxHealth);
        void SetTowerLevel(int towerLevel);
    }
}