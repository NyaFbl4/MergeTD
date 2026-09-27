using Project.Scripts.Systems.UI;
using Project.Scripts.Configs;

using System;
using Project.Scripts.UI.InBattleUI;
using Project.Scripts.UI.ArmyUI;

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
        IArmyUIView ArmyView { get; }

        void InitializeArmy(UIElements uiElements);
        void SetGoldCount(int goldCount);
        void SetDiamondCount(int diamondCount);
        void SetActiveSection(MainMenuSection section);
    }
}
