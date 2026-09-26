using UnityEngine;

namespace Project.Scripts.System.Reward
{
    public class RewardData
    {
        private int _rewardId;
        private string _rewardName;
        private string _rewardDescription;
        private Sprite _rewardIcon;
        
        public int RewardId => _rewardId;
        public string RewardName => _rewardName;
        public string RewardDescription => _rewardDescription;
        public Sprite RewardIcon => _rewardIcon;
    }
}