using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Gameplay.Wave
{
    [Serializable]
    [CreateAssetMenu(menuName = "Project/Configs/Enemy wave config", fileName = "Enemy wave config")]
    public class EnemyWaveConfig : ScriptableObject
    {
        [SerializeField] private int _countGoldReward;
        [SerializeField] private List<EnemySpawnSequenceConfig> _sequence;
        [SerializeField] private float _delayAfterWave;
        [SerializeField] private int _killRewardGold;
        
        public int CountGoldReward => _countGoldReward;
        public int KillRewardGold => _killRewardGold;
        public IReadOnlyList<EnemySpawnSequenceConfig> Sequence => _sequence;
        public float DelayAfterWave => _delayAfterWave;
    }
}