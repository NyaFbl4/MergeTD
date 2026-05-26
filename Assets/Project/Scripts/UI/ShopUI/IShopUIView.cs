using System;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.ShopUI
{
    public interface IShopUIView : ILayoutView
    {
        event Action CloseButtonClicked;

        void SetTitle(string title);
        void AddItem();
    }
}