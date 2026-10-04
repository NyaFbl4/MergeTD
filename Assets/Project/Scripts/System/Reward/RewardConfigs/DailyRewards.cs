using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.System.Reward.RewardConfigs
{
    public enum EDailyRewardCurrency
    {
        Gold,
        Gems
    }

    [Serializable]
    public sealed class DailyRewardEntry
    {
        [SerializeField] private EDailyRewardCurrency _currency;
        [SerializeField, Min(1)] private int _amount = 1;

        public EDailyRewardCurrency Currency => _currency;
        public int Amount => _amount;
    }

    [CreateAssetMenu(menuName = "Project/Configs/Daily rewards", fileName = "Daily Rewards")]
    public sealed class DailyRewards : ScriptableObject
    {
        public const int CycleLength = 7;

        [SerializeField] private List<DailyRewardEntry> _dailyRewardsConfig = new(CycleLength);

        public IReadOnlyList<DailyRewardEntry> DailyRewardsConfig => _dailyRewardsConfig;

        public void Validate()
        {
            if (_dailyRewardsConfig.Count != CycleLength)
                throw new InvalidOperationException(
                    $"Daily rewards config must contain exactly {CycleLength} entries.");

            for (var i = 0; i < _dailyRewardsConfig.Count; i++)
            {
                var reward = _dailyRewardsConfig[i]
                             ?? throw new InvalidOperationException(
                                 $"Daily reward at index {i} is missing.");

                if (reward.Amount <= 0)
                    throw new InvalidOperationException(
                        $"Daily reward at index {i} must have a positive amount.");
            }
        }
    }
}
