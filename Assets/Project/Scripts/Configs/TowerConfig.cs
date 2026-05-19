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
        
        [Header("Start parametrs")]
        [SerializeField] private int _startPrice;
        [SerializeField] private int _startDamage;
        [SerializeField] private float _startAttackSpeed;
        [SerializeField] private float _animationSpeed;
        
        [Header("LevelUp parametrs")]
        [SerializeField] private int _updatePrice;
        [SerializeField] private int _updateDamage;
        [SerializeField] private float _updateAttackSpeed;
        
        public int StartTowerPrice => _startPrice;
        public int StartTowerDamage => _startDamage;
        public float StartAttackSpeed => _startAttackSpeed;
        public float AnimationSpeed => _animationSpeed;
        public Sprite Icon => _icon;
        public int TowerLevel => _towerLevel;
    }
}