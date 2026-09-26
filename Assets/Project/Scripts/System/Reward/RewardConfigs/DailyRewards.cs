using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.System.Reward.RewardConfigs
{
    [Serializable]
    [CreateAssetMenu(menuName = "Project/Configs/Reward config", fileName = "Reward config")]
    public class DailyRewards : ScriptableObject
    {
        private List<RewardConfig> _dailyRewardsConfig;
        
        public List<RewardConfig> DailyRewardsConfig => _dailyRewardsConfig;
    }
}