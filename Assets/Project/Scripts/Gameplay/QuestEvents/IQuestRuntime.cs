using System;
using UnityEngine;

namespace Project.Scripts.Gameplay.Quests
{
    public interface IQuestRuntime : IDisposable
    {
        string Id { get; }
        Sprite Icon { get; }
        string Description { get; }
        int RewardGold { get; }
        int CurrentValue { get; }
        int TargetValue { get; }
        bool IsCompleted { get; }
        bool IsRewardClaimed { get; }
        event Action ProgressChanged;
        void RestoreState(int currentValue, bool isRewardClaimed);
        bool TryClaimReward();
    }
}
