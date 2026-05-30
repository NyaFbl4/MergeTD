using System;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Enemies;
using UnityEngine;

namespace Project.Scripts.Gameplay.Wave
{
    [Serializable]
    public class EnemySpawnSequenceConfig
    {
        [SerializeField] private EnemyUnit _enemyPrefab;
        [SerializeField] private EnemyConfig _enemyConfig;
        [SerializeField] private float _healthMultiplier = 1f;
        [SerializeField] private int _count;
        [SerializeField] private float _spawnInterval = 1f;
        [SerializeField] private float _startDelay;
        
        public float StartDelay => _startDelay;
        public EnemyUnit EnemyPrefab => _enemyPrefab;
        public EnemyConfig EnemyConfig => _enemyConfig;
        public float HealthMultiplier => _healthMultiplier;
        public int Count => _count;
        public float SpawnInterval => _spawnInterval;
    }
}