using System;
using Project.Scripts.Gameplay.Enemies;
using UnityEngine;

namespace Project.Scripts.Configs
{
    [Serializable]
    public class EnemyTypeStats
    {
        [SerializeField] private EEnemyType _enemyType;
        [SerializeField, Min(0.01f)] private float _healthMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float _moveSpeedMultiplier = 1f;

        public EEnemyType EnemyType => _enemyType;
        public float HealthMultiplier => _healthMultiplier;
        public float MoveSpeedMultiplier => _moveSpeedMultiplier;
    }
}