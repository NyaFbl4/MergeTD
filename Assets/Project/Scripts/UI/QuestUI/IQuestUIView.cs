using System;
using Project.Scripts.Gameplay.Quests;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.QuestUI
{
    public interface IQuestUIView : ILayoutView
    {
        event Action CloseButtonClicked;
        
        void SetTitle(string title);
        void ClearItems();
        void AddQuest(IQuestRuntime quest, Action onClaimReward);
    }
}