using System;
using System.Collections.Generic;

namespace Project.Scripts.UI.SpellsUI
{
    public interface ISpellsPanelUIView : IDisposable
    {
        event Action<string> SpellSelectionClicked;
        event Action<string> SpellInfoClicked;
        event Action<string> SpellUpgradeClicked;
        event Action SpellInfoClosed;

        void SetSpells(IReadOnlyList<SpellUIItemData> spells);
        void ShowSpellInfo(SpellUIItemData spell);
        void HideSpellInfo();
        void SetVisible(bool visible);
    }
}
