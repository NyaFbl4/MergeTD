using System;
using UnityEngine;

namespace Project.Scripts.System.Reward
{
    [Serializable]
    [CreateAssetMenu(menuName = "Project/Configs/Reward config", fileName = "Reward config")]
    public class RewardConfig : ScriptableObject
    {
        private RewardData _rewardData;
        
        public RewardData RewardData => _rewardData;
    }
}