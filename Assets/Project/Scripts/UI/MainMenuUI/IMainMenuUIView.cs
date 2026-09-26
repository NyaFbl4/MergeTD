using Project.Scripts.Systems.UI;

using System;
using Project.Scripts.UI.InBattleUI;

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

        IInBattleUIView InBattleView { get; }

        void SetGoldCount(int goldCount);
        void SetDiamondCount(int diamondCount);
        void SetActiveSection(MainMenuSection section);
    }
}
