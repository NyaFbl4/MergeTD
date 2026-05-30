using System;
using Project.Scripts.Gameplay.Quests;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.QuestUI
{
    public class QuestItemView
    {
        private readonly VisualElement _icon;
        private readonly Label _descriptionLabel;
        private readonly Label _currentProgressLabel;
        private readonly Label _maxProgressLabel;
        private readonly Label _rewardLabel;
        private readonly Button _takeRewardButton;

        public QuestItemView(VisualElement root)
        {
            _icon = root.Q<VisualElement>("QuestIcon");
            _descriptionLabel = root.Q<Label>("DescriptionQuestLabel");
            _currentProgressLabel = root.Q<Label>("CurrentProgress");
            _maxProgressLabel = root.Q<Label>("MaxProgress");
            _rewardLabel = root.Q<Label>("RewardLabel");
            _takeRewardButton = root.Q<Button>("TakeRewardButton");
        }

        public void Bind(IQuestRuntime quest, Action onClaimReward)
        {
            _descriptionLabel.text = quest.Description;
            _currentProgressLabel.text = quest.CurrentValue.ToString();
            _maxProgressLabel.text = quest.TargetValue.ToString();
            _rewardLabel.text = quest.RewardGold.ToString();

            if (_icon != null)
                _icon.style.backgroundImage = new StyleBackground(quest.Icon);

            _takeRewardButton.SetEnabled(quest.IsCompleted && !quest.IsRewardClaimed);

            if (quest.IsRewardClaimed)
                _takeRewardButton.text = "Done";

            _takeRewardButton.clicked += onClaimReward;
        }
    }
}