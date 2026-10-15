using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Wave;
using UnityEngine;

namespace Project.Scripts.Gameplay.Run.Configs
{
    public enum EWaveRewardType
    {
        Gold,
        Gems
    }

    [Serializable]
    public sealed class WaveRewardConfig
    {
        [SerializeField] private EWaveRewardType _type;
        [SerializeField, Min(1)] private int _amount = 1;

        public EWaveRewardType Type => _type;
        public int Amount => _amount;
    }

    [Serializable]
    public class RunWaveConfig
    {
        [SerializeField] private string _displayName;
        [SerializeField] private EnemyWaveConfig _waveConfig;
        [SerializeField] private List<WaveRewardConfig> _rewards = new();
        [SerializeField] private bool _isBossWave;
        [SerializeField] private ERunPhase _phaseAfterComplete;

        public string DisplayName => _displayName;
        public EnemyWaveConfig WaveConfig => _waveConfig;
        public IReadOnlyList<WaveRewardConfig> Rewards => _rewards;
        public bool IsBossWave => _isBossWave;
        public ERunPhase PhaseAfterComplete => _phaseAfterComplete;

        public int GetRewardAmount(EWaveRewardType type)
        {
            var total = 0L;
            for (var i = 0; i < _rewards.Count; i++)
            {
                var reward = _rewards[i];
                if (reward.Type == type)
                    total += reward.Amount;
            }

            return (int)Math.Min(int.MaxValue, total);
        }
    }
}
