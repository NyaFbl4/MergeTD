using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.System.Save;

namespace Project.Scripts.Gameplay.Run
{
    public sealed class SpellProgressionService
    {
        private readonly SpellCatalog _catalog;
        private readonly IWorldService _world;

        public SpellProgressionService(SpellCatalog catalog, IWorldService world)
        {
            _catalog = catalog;
            _world = world;
        }

        public bool CanUnlock(string spellId)
        {
            if (_world.IsSpellUnlocked(spellId))
                return false;
            var config = _catalog.Get(spellId);
            return config.UnlockType switch
            {
                ESpellUnlockType.Gold => config.UnlockGoldPrice > 0 && _world.Gold >= config.UnlockGoldPrice,
                ESpellUnlockType.Gems => config.UnlockGemPrice > 0 && _world.Gems >= config.UnlockGemPrice,
                _ => false
            };
        }

        public bool TryUnlock(string spellId)
        {
            if (!CanUnlock(spellId))
                return false;
            var config = _catalog.Get(spellId);
            return _world.TryPurchaseSpell(spellId,
                config.UnlockType == ESpellUnlockType.Gold ? config.UnlockGoldPrice : 0,
                config.UnlockType == ESpellUnlockType.Gems ? config.UnlockGemPrice : 0);
        }

        public bool RecordVictory(string runId)
        {
            var rewards = new List<string>();
            foreach (var spell in _catalog.Spells)
            {
                if (spell.UnlockType == ESpellUnlockType.RunCompletion
                    && string.Equals(spell.UnlockRunId, runId, StringComparison.Ordinal))
                    rewards.Add(spell.SpellId);
            }
            return _world.TryCompleteRun(runId, rewards);
        }
    }
}
