using Project.Scripts.Systems.UI;
using Project.Scripts.Configs;

using System;
using Project.Scripts.UI.InBattleUI;
using Project.Scripts.UI.ArmyUI;
using Project.Scripts.UI.SpellsUI;

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
        ISpellsPanelUIView SpellsPanelView { get; }

        void InitializeArmy(UIElements uiElements);
        void InitializeSpells(UIElements uiElements);
        void SetGoldCount(int goldCount);
        void SetDiamondCount(int diamondCount);
        void SetActiveSection(MainMenuSection section);
    }
}
