using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Tower Config", fileName = "Tower Config")]
    public class TowerConfig : ScriptableObject
    {
        [Header("Start parametrs")]
        [SerializeField] private int _startPrice;
        [SerializeField] private int _startDamage;
        [SerializeField] private float _startAttackSpeed;
        
        [Header("LevelUp parametrs")]
        [SerializeField] private int _updatePrice;
        [SerializeField] private int _updateDamage;
        [SerializeField] private float _updateAttackSpeed;
        
        public int StartTowerPrice => _startPrice;
        public int StartTowerDamage => _startDamage;
        public float StartAttackSpeed => _startAttackSpeed;
    }
}