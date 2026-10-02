using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.System.Save;

namespace Project.Scripts.UI.SpellsUI
{
    public sealed class SpellsUIUseCase : ISpellsUIUseCase
    {
        private const int MaximumActiveSpellCount = 3;

        private readonly SpellCatalog _spellCatalog;
        private readonly IWorldService _world;
        private readonly HashSet<string> _selectedSpellIds = new(StringComparer.Ordinal);

        public int SelectedCount => _selectedSpellIds.Count;
        public int MaximumSelectedCount => MaximumActiveSpellCount;
        public event Action SelectionChanged;

        public SpellsUIUseCase(SpellCatalog spellCatalog, IWorldService world)
        {
            _spellCatalog = spellCatalog;
            _world = world;
        }

        public IReadOnlyList<SpellUIItemData> GetSpells()
        {
            var result = new List<SpellUIItemData>();
            if (_spellCatalog == null || _spellCatalog.Spells == null)
                return result;

            for (var i = 0; i < _spellCatalog.Spells.Count; i++)
            {
                var config = _spellCatalog.Spells[i];
                if (config == null || string.IsNullOrWhiteSpace(config.SpellId))
                    continue;

                result.Add(new SpellUIItemData(
                    config.SpellId,
                    config.DisplayName,
                    config.Description,
                    config.Icon,
                    config.Background,
                    Math.Max(1, _world.GetSpellLevel(config.SpellId)),
                    config.Cooldown,
                    config.ManaCost,
                    _selectedSpellIds.Contains(config.SpellId),
                    CanSelect(config.SpellId)));
            }

            return result;
        }

        public bool ToggleSelection(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId) || !ContainsSpell(spellId))
                return false;

            if (_selectedSpellIds.Remove(spellId))
            {
                SelectionChanged?.Invoke();
                return true;
            }

            if (_selectedSpellIds.Count >= MaximumActiveSpellCount)
                return false;

            _selectedSpellIds.Add(spellId);
            SelectionChanged?.Invoke();

            return true;
        }

        public bool IsSelected(string spellId)
        {
            return !string.IsNullOrWhiteSpace(spellId)
                   && _selectedSpellIds.Contains(spellId);
        }

        private bool CanSelect(string spellId)
        {
            return IsSelected(spellId)
                   || _selectedSpellIds.Count < MaximumActiveSpellCount;
        }

        private bool ContainsSpell(string spellId)
        {
            if (_spellCatalog == null || _spellCatalog.Spells == null)
                return false;

            for (var i = 0; i < _spellCatalog.Spells.Count; i++)
            {
                var config = _spellCatalog.Spells[i];
                if (config != null && string.Equals(config.SpellId, spellId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
