using System;
using System.Collections.Generic;
using Project.Scripts.System.Reward;
using Project.Scripts.System.Reward.RewardConfigs;
using UnityEngine;

namespace Project.Scripts.UI.InBattleUI
{
    public interface IInBattleUIView : IDisposable
    {
        event Action PreviousRunClicked;
        event Action NextRunClicked;
        event Action PlayClicked;
        event Action DailyRewardClicked;
        event Action DailyRewardNormalClaimClicked;
        event Action DailyRewardDoubleClaimClicked;

        void SetRun(string displayName, Sprite icon, bool canNavigate);
        void SetDailyRewards(IReadOnlyList<DailyRewardEntry> rewards, DailyRewardState state);
        void SetDailyRewardCountdown(bool visible, string text);
        void ShowDailyRewardOffer(DailyRewardEntry reward);
        void HideDailyRewardOffer();
        void SetDailyRewardOfferButtonsEnabled(bool enabled);
        void SetVisible(bool visible);
    }
}
