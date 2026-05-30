using System;
using Project.Scripts.System.UseCases;
using UnityEngine;

namespace Project.Scripts.Gameplay.Quests
{
    public abstract class QuestRuntimeBase<TConfig> : IQuestRuntime where TConfig : BaseQuestConfig
    {
        protected readonly TConfig _config;
        protected readonly IPlayerStatsUseCase _playerStats;

        private int _currentValue;
        private bool _isRewardClaimed;

        public string Id => _config.Id;
        public Sprite Icon => _config.Icon;
        public string Description => _config.Description;
        public int RewardGold => _config.RewardGold;
        public int CurrentValue => _currentValue;
        public int TargetValue => _config.TargetValue;
        public bool IsCompleted => _currentValue >= _config.TargetValue;
        public bool IsRewardClaimed => _isRewardClaimed;

        public event Action ProgressChanged;

        protected QuestRuntimeBase(TConfig config, IPlayerStatsUseCase playerStats)
        {
            _config = config;
            _playerStats = playerStats;
        }

        protected void AddProgress(int value)
        {
            if (IsCompleted || value <= 0)
                return;

            var nextValue = _currentValue + value;
            _currentValue = nextValue > _config.TargetValue ? _config.TargetValue : nextValue;
            ProgressChanged?.Invoke();
        }

        public bool TryClaimReward()
        {
            if (!IsCompleted || _isRewardClaimed)
                return false;

            _isRewardClaimed = true;
            _playerStats.AddGold(_config.RewardGold);
            ProgressChanged?.Invoke();
            return true;
        }

        public abstract void Dispose();
    }
}