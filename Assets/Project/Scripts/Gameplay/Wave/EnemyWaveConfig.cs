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
        public IReadOnlyList<EnemySpawnSequenceConfig> Sequence => _sequence;
    }
}
