using System;
using Project.Scripts.Gameplay.Wave;
using UnityEngine;

namespace Project.Scripts.Gameplay.Run.Configs
{
    [Serializable]
    public class RunWaveConfig
    {
        [SerializeField] private EnemyWaveConfig _waveConfig;
        [SerializeField] private int _completeRewardGold;
        [SerializeField] private bool _isBossWave;
        [SerializeField] private ERunPhase _phaseAfterComplete;
        
        public EnemyWaveConfig WaveConfig => _waveConfig;
        public int CompleteRewardGold => _completeRewardGold;
        public bool IsBossWave => _isBossWave;
        public ERunPhase PhaseAfterComplete => _phaseAfterComplete;
    }
}