using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(
        fileName = "Spell Catalog",
        menuName = "Project/Configs/Spells/Spell Catalog")]
    public sealed class SpellCatalog : ScriptableObject
    {
        [SerializeField] private List<BaseSpellConfig> _spells = new();
        [SerializeField] private SpellUpgradeConfig _upgradeConfig;

        public IReadOnlyList<BaseSpellConfig> Spells => _spells;
        public SpellUpgradeConfig UpgradeConfig => _upgradeConfig;

        public T Get<T>() where T : BaseSpellConfig
        {
            T result = null;

            foreach (var spell in _spells)
            {
                if (!(spell is T typedSpell))
                    continue;

                if (result != null)
                    throw new InvalidOperationException(
                        $"SpellCatalog contains more than one config of type {typeof(T).Name}.");

                result = typedSpell;
            }

            if (result == null)
                throw new InvalidOperationException(
                    $"SpellCatalog does not contain config of type {typeof(T).Name}.");

            return result;
        }

        public BaseSpellConfig Get(string spellId)
        {
            BaseSpellConfig result = null;

            foreach (var spell in _spells)
            {
                if (!string.Equals(spell.SpellId, spellId, StringComparison.Ordinal))
                    continue;

                if (result != null)
                    throw new InvalidOperationException(
                        $"SpellCatalog contains duplicate spell id '{spellId}'.");

                result = spell;
            }

            if (result == null)
                throw new InvalidOperationException(
                    $"SpellCatalog does not contain spell id '{spellId}'.");

            return result;
        }
    }
}
