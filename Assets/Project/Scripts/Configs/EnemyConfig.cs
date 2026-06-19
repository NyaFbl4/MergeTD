using Project.Scripts.Gameplay.Enemies;
using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Enemy Config", fileName = "Enemy Config")]
    public class EnemyConfig: ScriptableObject
    {
        [SerializeField] private int _startHealth;
        [SerializeField] private int _startDamage;
        [SerializeField] private float _startMoveSpeed;
        [SerializeField] private EnemyTypeStats[] _typeStats;

        public int StartHealth => _startHealth;
        public int StartDamage => _startDamage;
        public float StartMoveSpeed => _startMoveSpeed;

        public float GetHealthMultiplier(EEnemyType enemyType)
        {
            var stats = GetTypeStats(enemyType);
            return stats == null ? 1f : Mathf.Max(0.01f, stats.HealthMultiplier);
        }

        public float GetMoveSpeedMultiplier(EEnemyType enemyType)
        {
            var stats = GetTypeStats(enemyType);
            return stats == null ? 1f : Mathf.Max(0.01f, stats.MoveSpeedMultiplier);
        }

        private EnemyTypeStats GetTypeStats(EEnemyType enemyType)
        {
            if (_typeStats == null)
                return null;

            for (var i = 0; i < _typeStats.Length; i++)
            {
                var stats = _typeStats[i];
                if (stats != null && stats.EnemyType == enemyType)
                    return stats;
            }

            return null;
        }
    }
}