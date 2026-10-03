using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Configs
{
    [Serializable]
    public sealed class SpellUpgradePrice
    {
        [SerializeField, Min(2)] private int _level = 2;
        [SerializeField, Min(1)] private int _price = 1;

        public int Level => _level;
        public int Price => _price;
    }

    [CreateAssetMenu(
        fileName = "Spell Upgrade Config",
        menuName = "Project/Configs/Spells/Spell Upgrade Config")]
    public sealed class SpellUpgradeConfig : ScriptableObject
    {
        [SerializeField] private List<SpellUpgradePrice> _levels = new();

        public IReadOnlyList<SpellUpgradePrice> Levels => _levels;

        public int MaximumLevel
        {
            get
            {
                var maximumLevel = 1;
                for (var i = 0; i < _levels.Count; i++)
                    maximumLevel = Math.Max(maximumLevel, _levels[i].Level);

                return maximumLevel;
            }
        }

        public bool TryGetPrice(int targetLevel, out int price)
        {
            var found = false;
            price = 0;

            for (var i = 0; i < _levels.Count; i++)
            {
                var level = _levels[i];
                if (level.Level != targetLevel)
                    continue;

                if (found)
                    throw new InvalidOperationException(
                        $"Spell upgrade config contains duplicate level {targetLevel}.");

                if (level.Price < 1)
                    throw new InvalidOperationException(
                        $"Spell upgrade price for level {targetLevel} must be positive.");

                found = true;
                price = level.Price;
            }

            return found;
        }
    }
}
