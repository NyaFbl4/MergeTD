using System;
using Project.Scripts.Gameplay.Quests;
using Project.Scripts.System.Localization;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.QuestUI
{
    public interface IQuestUIView : ILayoutView
    {
        event Action CloseButtonClicked;
        
        void SetTexts(
            string title,
            string dailyHeader,
            string weeklyHeader,
            string achievementHeader,
            string closeButton);
        void ClearItems();
        void AddQuest(
            EQuestCategory category,
            IQuestRuntime quest,
            Action onClaimReward,
            ILocalizationService localizationService);
    }
}
