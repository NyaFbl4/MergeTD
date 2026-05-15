using System;
using Project.Scripts.Systems.UI;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.LevelUI
{
    public interface ILevelUIView : ILayoutView
    {
        event Action BuyTowerButtonClicked;
        event Action ShopButtonClicked;
        
        void SetPriceTower(int price);
        void SetMoney(int money);
    }
}