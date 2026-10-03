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

        public int SelectedCount => _world.ActiveSpellIds.Count;
        public int MaximumSelectedCount => MaximumActiveSpellCount;
        public event Action SelectionChanged
        {
            add => _world.SpellsChanged += value;
            remove => _world.SpellsChanged -= value;
        }
        public event Action<int> GemsChanged
        {
            add => _world.GemsChanged += value;
            remove => _world.GemsChanged -= value;
        }

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

            var selectedSpellIds = new HashSet<string>(_world.ActiveSpellIds, StringComparer.Ordinal);

            for (var i = 0; i < _spellCatalog.Spells.Count; i++)
            {
                var config = _spellCatalog.Spells[i];
                if (config == null || string.IsNullOrWhiteSpace(config.SpellId))
                    continue;

                var level = Math.Max(1, _world.GetSpellLevel(config.SpellId));
                var maximumLevel = Math.Max(
                    level,
                    Math.Min(
                        config.MaximumLevel,
                        _spellCatalog.UpgradeConfig.MaximumLevel));
                var targetLevel = level + 1;
                var nextUpgrade = config.GetUpgrade(targetLevel);
                var hasPrice = _spellCatalog.UpgradeConfig.TryGetPrice(targetLevel, out var upgradePrice);
                var hasNextUpgrade = level < maximumLevel && nextUpgrade != null && hasPrice;

                result.Add(new SpellUIItemData(
                    config.SpellId,
                    config.DisplayName,
                    config.Description,
                    config.Icon,
                    config.Background,
                    level,
                    config.Cooldown,
                    config.ManaCost,
                    selectedSpellIds.Contains(config.SpellId),
                    CanSelect(config.SpellId),
                    maximumLevel,
                    hasNextUpgrade ? upgradePrice : 0,
                    hasNextUpgrade ? nextUpgrade.Title : string.Empty,
                    hasNextUpgrade ? nextUpgrade.Description : string.Empty,
                    hasNextUpgrade,
                    hasNextUpgrade && _world.Gems >= upgradePrice));
            }

            return result;
        }

        public bool ToggleSelection(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId) || !ContainsSpell(spellId))
                return false;

            var isSelected = IsSelected(spellId);
            if (!isSelected && SelectedCount >= MaximumActiveSpellCount)
                return false;

            return _world.SetSpellSelected(spellId, !isSelected);
        }

        public bool TryUpgrade(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId) || !ContainsSpell(spellId))
                return false;

            var config = _spellCatalog.Get(spellId);
            var level = Math.Max(1, _world.GetSpellLevel(spellId));
            var targetLevel = level + 1;
            if (targetLevel > config.MaximumLevel
                || !_spellCatalog.UpgradeConfig.TryGetPrice(targetLevel, out var price)
                || config.GetUpgrade(targetLevel) == null)
                return false;

            return _world.TryUpgradeSpell(spellId, level, price);
        }

        public bool IsSelected(string spellId)
        {
            return _world.IsSpellSelected(spellId);
        }

        private bool CanSelect(string spellId)
        {
            return IsSelected(spellId)
                   || SelectedCount < MaximumActiveSpellCount;
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
