using System;

namespace Project.Scripts.Gameplay.Quests
{
    public interface IQuestRuntime : IDisposable
    {
        string Id { get; }
        string Title { get; }
        string Description { get; }
        int RewardGold { get; }
        int CurrentValue { get; }
        int TargetValue { get; }
        bool IsCompleted { get; }
        bool IsRewardClaimed { get; }

        event Action ProgressChanged;

        bool TryClaimReward();
    }
}