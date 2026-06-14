using System;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.EndWaveLoseUI
{
    public interface IEndWaveLoseUIView : ILayoutView
    {
        event Action CloseButtonClicked;
        event Action ADButtonClicked;

        void SetTitle(string title);
        void SetDescription(string description);
        void SetAdButtonText(string text);
        void SetCloseButtonText(string text);
    }
}