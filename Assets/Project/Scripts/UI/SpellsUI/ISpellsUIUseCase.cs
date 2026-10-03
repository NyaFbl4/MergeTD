using System;
using System.Collections.Generic;

namespace Project.Scripts.UI.SpellsUI
{
    public interface ISpellsUIUseCase
    {
        int SelectedCount { get; }
        int MaximumSelectedCount { get; }
        event Action SelectionChanged;
        event Action<int> GemsChanged;

        IReadOnlyList<SpellUIItemData> GetSpells();
        bool IsSelected(string spellId);
        bool ToggleSelection(string spellId);
        bool TryUpgrade(string spellId);
    }
}
