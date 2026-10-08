using System;
using System.Collections.Generic;
using Project.Scripts.System.Reward;
using Project.Scripts.System.Reward.RewardConfigs;
using Project.Scripts.Gameplay.Quests;
using Project.Scripts.System.Localization;
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
        event Action DailyQuestsButtonClicked;
        event Action<IQuestRuntime> DailyQuestClaimClicked;

        void SetRun(string displayName, Sprite icon, bool canNavigate);
        void SetDailyRewards(IReadOnlyList<DailyRewardEntry> rewards, DailyRewardState state);
        void SetDailyRewardCountdown(bool visible, string text);
        void ShowDailyRewardOffer(DailyRewardEntry reward);
        void HideDailyRewardOffer();
        void SetDailyRewardOfferButtonsEnabled(bool enabled);
        void SetDailyQuests(
            IReadOnlyList<IQuestRuntime> quests,
            ILocalizationService localizationService);
        void SetVisible(bool visible);
    }
}
