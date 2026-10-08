using System;
using Project.Scripts.System.UseCases;
using Project.Scripts.System.Save;
using UnityEngine;

namespace Project.Scripts.Gameplay.Quests
{
    public abstract class QuestRuntimeBase<TConfig> : IQuestRuntime where TConfig : BaseQuestConfig
    {
        protected readonly TConfig _config;
        protected readonly IPlayerStatsUseCase _playerStats;
        private readonly IWorldService _world;

        private readonly int _targetValue;
        private readonly int _rewardGold;
        private readonly int _rewardGems;
        private int _currentValue;
        private bool _isRewardClaimed;

        public string Id => _config.Id;
        public Sprite Icon => _config.Icon;
        public string Description => _config.Description;
        public EQuestCategory Category { get; }
        public int RewardGold => _rewardGold;
        public int RewardGems => _rewardGems;
        public int CurrentValue => _currentValue;
        public int TargetValue => _targetValue;
        public bool IsCompleted => _currentValue >= _targetValue;
        public bool IsRewardClaimed => _isRewardClaimed;

        public event Action ProgressChanged;

        protected QuestRuntimeBase(
            TConfig config,
            IPlayerStatsUseCase playerStats,
            IWorldService world,
            EQuestCategory category,
            int targetValue,
            int rewardGold,
            int rewardGems)
        {
            _config = config;
            _playerStats = playerStats;
            _world = world;
            Category = category;
            _targetValue = Math.Max(1, targetValue);
            _rewardGold = Math.Max(0, rewardGold);
            _rewardGems = Math.Max(0, rewardGems);
        }

        protected void AddProgress(int value)
        {
            if (IsCompleted || value <= 0)
                return;

            var nextValue = _currentValue + value;
            _currentValue = nextValue > _targetValue ? _targetValue : nextValue;
            ProgressChanged?.Invoke();
        }

        public bool TryClaimReward()
        {
            if (!IsCompleted || _isRewardClaimed)
                return false;

            _isRewardClaimed = true;
            if (_rewardGold > 0)
                _playerStats.AddGold(_rewardGold);

            if (_rewardGems > 0)
                _world.AddGems(_rewardGems);

            ProgressChanged?.Invoke();
            return true;
        }

        public abstract void Dispose();
        
        
        public void RestoreState(int currentValue, bool isRewardClaimed)
        {
            _currentValue = Math.Clamp(currentValue, 0, _targetValue);
            _isRewardClaimed = isRewardClaimed;
            ProgressChanged?.Invoke();
        }
    }
}
