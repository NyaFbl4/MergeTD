using System;
using UnityEngine;

namespace Project.Scripts.UI.InBattleUI
{
    public interface IInBattleUIView : IDisposable
    {
        event Action PreviousRunClicked;
        event Action NextRunClicked;
        event Action PlayClicked;

        void SetRun(string displayName, Sprite icon, bool canNavigate);
        void SetVisible(bool visible);
    }
}
