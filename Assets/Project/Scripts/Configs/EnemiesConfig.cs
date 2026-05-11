using System.Collections.Generic;
using Project.Scripts.Gameplay.Enemies;
using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Enemies Config", fileName = "EnemiesConfig")]
    public class EnemiesConfig : ScriptableObject
    {
        [SerializeField] private List<EnemyUnit> _enemies = new();

        public List<EnemyUnit> Enemies => _enemies;
    }
}
