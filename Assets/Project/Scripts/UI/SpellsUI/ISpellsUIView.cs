using System;
using System.Collections.Generic;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.SpellsUI
{
    public interface ISpellsUIView : ILayoutView
    {
        event Action CloseButtonClicked;
        event Action<string> SpellSelectionClicked;

        void SetSelectedCount(int count, int maximumCount);
        void SetSpells(IReadOnlyList<SpellUIItemData> spells);
    }
}
