using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Configs
{
    public static class SpellIds
    {
        public const string WeaponBarrage = "weapon_barrage";
        public const string BaseRepair = "base_repair";
        public const string BarrageProtocol = "barrage_protocol";
        public const string CryoDischarge = "cryo_discharge";
        public const string EmpPulse = "emp_pulse";
        public const string OrbitalRailgun = "orbital_railgun";
        public const string GravityTrap = "gravity_trap";
    }

    [Serializable]
    public sealed class SpellUpgradeDescription
    {
        [SerializeField, Min(2)] private int _level = 2;
        [SerializeField] private string _title;
        [SerializeField, TextArea(2, 4)] private string _description;

        public int Level => _level;
        public string Title => _title;
        public string Description => _description;
    }

    public abstract class BaseSpellConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _spellId;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 5)] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Sprite _background;

        [Header("Common gameplay")]
        [SerializeField, Min(0.1f)] private float _cooldown = 10f;
        [SerializeField, Min(1)] private int _manaCost = 1;

        [Header("Visual effect")]
        [SerializeField] private GameObject _vfxPrefab;
        [SerializeField, Min(0.01f)] private float _vfxScale = 1f;
        [SerializeField] private Vector3 _vfxEulerAngles;
        [SerializeField, Min(0f)] private float _vfxLifetime;

        [Header("Future upgrades")]
        [SerializeField] private List<SpellUpgradeDescription> _upgrades = new();

        public string SpellId => _spellId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Sprite Background => _background;
        public float Cooldown => _cooldown;
        public int ManaCost => _manaCost;
        public GameObject VfxPrefab => _vfxPrefab;
        public float VfxScale => _vfxScale;
        public Vector3 VfxEulerAngles => _vfxEulerAngles;
        public float VfxLifetime => _vfxLifetime;
        public IReadOnlyList<SpellUpgradeDescription> Upgrades => _upgrades;
    }
}
