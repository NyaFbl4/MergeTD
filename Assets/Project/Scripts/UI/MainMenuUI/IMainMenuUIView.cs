using Project.Scripts.Systems.UI;

using System;
using UnityEngine;

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
        event Action PreviousRunClicked;
        event Action NextRunClicked;
        event Action PlayClicked;
        event Action<MainMenuSection> SectionClicked;

        void SetGoldCount(int goldCount);
        void SetDiamondCount(int diamondCount);
        void SetRun(string displayName, Sprite icon, bool canNavigate);
        void SetActiveSection(MainMenuSection section);
    }
}
