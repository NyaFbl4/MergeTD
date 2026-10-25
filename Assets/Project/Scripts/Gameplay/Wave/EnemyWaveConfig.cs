using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Gameplay.Wave
{
    [Serializable]
    [CreateAssetMenu(menuName = "Project/Configs/Enemy wave config", fileName = "Enemy wave config")]
    public class EnemyWaveConfig : ScriptableObject
    {
        [SerializeField] private List<EnemySpawnSequenceConfig> _sequence;
        [SerializeField, Min(1)] private int _maxSpawnsPerTick = 1;
        [SerializeField, Min(0f)] private float _spawnWarningSeconds;

        public IReadOnlyList<EnemySpawnSequenceConfig> Sequence => _sequence;
        public int MaxSpawnsPerTick => Mathf.Max(1, _maxSpawnsPerTick);
        public float SpawnWarningSeconds => Mathf.Max(0f, _spawnWarningSeconds);
    }
}
