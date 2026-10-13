using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Towers;
using UnityEngine;

namespace Project.Scripts.Gameplay.Run.Cards
{
    public enum ERunCardRarity
    {
        Common,
        Rare,
        Epic
    }

    public enum ERunCardEffectType
    {
        TowerDamagePercent,
        TowerAttackSpeedPercent,
        TowerRangePercent,
        TowerTypeDamagePercent,
        GeneratorEnergyPercent,
        MaxBaseHealth,
        Gold,
        RepairBase,
        FreeTower
    }

    [Serializable]
    public sealed class RunCardEffectConfig
    {
        [SerializeField] private ERunCardEffectType _effectType;
        [SerializeField] private float _value;
        [SerializeField] private ETowerType _towerType = ETowerType.Combat;
        [SerializeField, Min(1)] private int _towerLevel = 1;

        public ERunCardEffectType EffectType => _effectType;
        public float Value => _value;
        public int IntValue => Mathf.RoundToInt(_value);
        public ETowerType TowerType => _towerType;
        public int TowerLevel => Mathf.Max(1, _towerLevel);
    }

    [CreateAssetMenu(fileName = "Run Card", menuName = "Configs/Run/Card")]
    public sealed class RunCardConfig : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 4)] private string _description;
        [SerializeField] private string _effectText;
        [SerializeField] private ERunCardRarity _rarity;
        [SerializeField] private string _rarityLabel = "Обычная";
        [SerializeField, Min(0f)] private float _selectionWeight = 1f;
        [SerializeField] private Color _tint = Color.white;
        [SerializeField] private Sprite _icon;
        [SerializeField] private List<RunCardEffectConfig> _effects = new();

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public string EffectText => _effectText;
        public ERunCardRarity Rarity => _rarity;
        public string RarityLabel => _rarityLabel;
        public float SelectionWeight => Mathf.Max(0f, _selectionWeight);
        public Color Tint => _tint;
        public Sprite Icon => _icon;
        public IReadOnlyList<RunCardEffectConfig> Effects => _effects;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _id = _id?.Trim();
            _selectionWeight = Mathf.Max(0f, _selectionWeight);
        }
#endif
    }
}
