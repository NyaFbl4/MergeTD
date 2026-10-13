using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Towers;
using UnityEngine;

namespace Project.Scripts.Gameplay.Run.Cards
{
    public sealed class RunCardRuntimeState
    {
        private readonly RunCardCatalog _catalog;
        private readonly List<string> _selectedCardIds = new();
        private readonly Dictionary<ETowerType, float> _towerTypeDamageBonuses = new();

        private float _towerDamageBonus;
        private float _towerAttackSpeedBonus;
        private float _towerRangeBonus;
        private float _generatorEnergyBonus;
        private int _maxBaseHealthBonus;

        public event Action ModifiersChanged;

        public IReadOnlyList<string> SelectedCardIds => _selectedCardIds;
        public float TowerAttackSpeedBonus => _towerAttackSpeedBonus;
        public float TowerRangeBonus => _towerRangeBonus;
        public float GeneratorEnergyMultiplier => Mathf.Max(0f, 1f + _generatorEnergyBonus);
        public int MaxBaseHealthBonus => _maxBaseHealthBonus;

        public RunCardRuntimeState(RunCardCatalog catalog)
        {
            _catalog = catalog;
        }

        public bool HasSelected(string cardId) => _selectedCardIds.Contains(cardId);

        public float GetTowerDamageBonus(ETowerType towerType)
        {
            _towerTypeDamageBonuses.TryGetValue(towerType, out var typeBonus);
            return _towerDamageBonus + typeBonus;
        }

        public bool TryAdd(RunCardConfig card)
        {
            if (card == null || string.IsNullOrWhiteSpace(card.Id))
                return false;
            if (!_catalog.AllowDuplicateSelections && HasSelected(card.Id))
                return false;

            _selectedCardIds.Add(card.Id);
            ApplyPersistentEffects(card);
            ModifiersChanged?.Invoke();
            return true;
        }

        public void Restore(IEnumerable<string> selectedCardIds)
        {
            ResetInternal();

            if (selectedCardIds != null)
            {
                foreach (var cardId in selectedCardIds)
                {
                    if (!_catalog.TryGetById(cardId, out var card))
                    {
                        Debug.LogWarning($"RunCardRuntimeState: card '{cardId}' was not found in catalog.");
                        continue;
                    }

                    if (!_catalog.AllowDuplicateSelections && HasSelected(card.Id))
                        continue;

                    _selectedCardIds.Add(card.Id);
                    ApplyPersistentEffects(card);
                }
            }

            ModifiersChanged?.Invoke();
        }

        public void Reset()
        {
            ResetInternal();
            ModifiersChanged?.Invoke();
        }

        private void ApplyPersistentEffects(RunCardConfig card)
        {
            for (var i = 0; i < card.Effects.Count; i++)
            {
                var effect = card.Effects[i];
                switch (effect.EffectType)
                {
                    case ERunCardEffectType.TowerDamagePercent:
                        _towerDamageBonus += effect.Value;
                        break;
                    case ERunCardEffectType.TowerAttackSpeedPercent:
                        _towerAttackSpeedBonus += effect.Value;
                        break;
                    case ERunCardEffectType.TowerRangePercent:
                        _towerRangeBonus += effect.Value;
                        break;
                    case ERunCardEffectType.TowerTypeDamagePercent:
                        _towerTypeDamageBonuses.TryGetValue(effect.TowerType, out var currentBonus);
                        _towerTypeDamageBonuses[effect.TowerType] = currentBonus + effect.Value;
                        break;
                    case ERunCardEffectType.GeneratorEnergyPercent:
                        _generatorEnergyBonus += effect.Value;
                        break;
                    case ERunCardEffectType.MaxBaseHealth:
                        _maxBaseHealthBonus += effect.IntValue;
                        break;
                }
            }
        }

        private void ResetInternal()
        {
            _selectedCardIds.Clear();
            _towerTypeDamageBonuses.Clear();
            _towerDamageBonus = 0f;
            _towerAttackSpeedBonus = 0f;
            _towerRangeBonus = 0f;
            _generatorEnergyBonus = 0f;
            _maxBaseHealthBonus = 0;
        }
    }
}
