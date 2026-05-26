using System;
using UnityEngine;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    [Serializable]
    public class UpgradeLevelData
    {
        [SerializeField] private int _price;
        [SerializeField] private float _value;

        public int Price => _price;
        public float Value => _value;
    }
}