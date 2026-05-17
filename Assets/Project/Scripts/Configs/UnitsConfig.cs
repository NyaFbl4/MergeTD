using System.Collections.Generic;
using Project.Scripts.Gameplay.Enemies;
using Project.Scripts.Gameplay.Towers;
using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Enemies Config", fileName = "Units Config")]
    public class UnitsConfig : ScriptableObject
    {
        [SerializeField] private List<EnemyUnit> _enemies = new();
        [SerializeField] private List<TowerUnit> _towers;

        public List<EnemyUnit> Enemies => _enemies;
        public List<TowerUnit> Towers => _towers;
    }
}
