using System;
using System.Collections.Generic;
using System.Linq;
using Project.Scripts.Configs;
using Project.Scripts.System.Save;
using Project.Scripts.Gameplay.Run;

namespace Project.Scripts.UI.SpellsUI
{
    public sealed class SpellsUIUseCase : ISpellsUIUseCase
    {
        private const int MaximumActiveSpellCount = 3;

        private readonly SpellCatalog _spellCatalog;
        private readonly IWorldService _world;
        private readonly SpellProgressionService _progression;

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
        public event Action<int> GoldChanged
        {
            add => _world.GoldChanged += value;
            remove => _world.GoldChanged -= value;
        }

        public SpellsUIUseCase(SpellCatalog spellCatalog, IWorldService world, SpellProgressionService progression)
        {
            _spellCatalog = spellCatalog;
            _world = world;
            _progression = progression;
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

                var unlocked = _world.IsSpellUnlocked(config.SpellId);
                var level = _world.GetSpellLevel(config.SpellId);
                var maximumLevel = Math.Max(
                    level,
                    Math.Min(
                        config.MaximumLevel,
                        _spellCatalog.UpgradeConfig.MaximumLevel));
                var targetLevel = level + 1;
                var nextUpgrade = config.GetUpgrade(targetLevel);
                var hasPrice = _spellCatalog.UpgradeConfig.TryGetPrice(targetLevel, out var upgradePrice);
                var hasNextUpgrade = unlocked && level < maximumLevel && nextUpgrade != null && hasPrice;

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
                    hasNextUpgrade && _world.Gems >= upgradePrice,
                    unlocked,
                    config.UnlockType,
                    config.UnlockType == ESpellUnlockType.Gold ? config.UnlockGoldPrice : config.UnlockGemPrice,
                    config.UnlockLocationNumber,
                    !unlocked && _progression.CanUnlock(config.SpellId)));
            }

            return result.OrderByDescending(spell => spell.IsUnlocked).ToList();
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
            if (string.IsNullOrWhiteSpace(spellId) || !ContainsSpell(spellId) || !_world.IsSpellUnlocked(spellId))
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

        public bool TryUnlock(string spellId) =>
            !string.IsNullOrWhiteSpace(spellId) && ContainsSpell(spellId) && _progression.TryUnlock(spellId);

        private bool CanSelect(string spellId)
        {
            return _world.IsSpellUnlocked(spellId)
                   && (IsSelected(spellId) || SelectedCount < MaximumActiveSpellCount);
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
