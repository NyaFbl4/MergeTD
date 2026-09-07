using System.Collections.Generic;
using Project.Scripts.Gameplay.Towers;
using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Tower Config", fileName = "Tower Config")]
    public class TowerConfig : ScriptableObject
    {
        [SerializeField] private Sprite _icon;
        [SerializeField] private int _towerLevel;
        [SerializeField] private ETowerType _towerType;

        [Header("Generator")]
        [SerializeField, Min(0.1f)] private float _generationInterval = 3f;
        [SerializeField, Min(1)] private int _energyPerPulse = 5;
        
        [Header("Start parametrs")]
        [SerializeField] private int _startPrice;
        [SerializeField] private int _startDamage;
        [SerializeField] private float _startAttackSpeed;
        [SerializeField] private float _animationSpeed;
        
        [Header("LevelUp parametrs")]
        [SerializeField] private int _updatePrice;
        [SerializeField] private int _updateDamage;
        [SerializeField] private float _updateAttackSpeed;
        
        public ETowerType TowerType => _towerType;
        public float GenerationInterval => _generationInterval;
        public int EnergyPerPulse => _energyPerPulse;

        public int StartTowerPrice => _startPrice;
        public int StartTowerDamage => _startDamage;
        public float StartAttackSpeed => _startAttackSpeed;
        public float AnimationSpeed => _animationSpeed;
        public Sprite Icon => _icon;
        public int TowerLevel => _towerLevel;
    }
}