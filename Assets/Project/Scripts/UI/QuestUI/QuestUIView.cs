using System;
using Project.Scripts.Gameplay.Quests;
using Project.Scripts.System.Localization;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.QuestUI
{
    public class QuestUIView : LayoutViewBase, IQuestUIView
    {
        [SerializeField] private VisualTreeAsset _questItemTemplate;

        private Label _titleLabel;
        private Label _dailyHeaderLabel;
        private Label _weeklyHeaderLabel;
        private Label _achievementHeaderLabel;
        private Button _closeButton;
        private VisualElement _dailyQuestContainer;
        private VisualElement _weeklyQuestContainer;
        private VisualElement _achievementQuestContainer;
        
        public event Action CloseButtonClicked;

        public override void Awake()
        {
            base.Awake();
            
            _closeButton = Require<Button>("CloseButton");
            _titleLabel = Require<Label>("TitleLabel");
            _dailyHeaderLabel = Require<Label>("DailyHeaderLabel");
            _weeklyHeaderLabel = Require<Label>("WeeklyHeaderLabel");
            _achievementHeaderLabel = Require<Label>("AchievementHeaderLabel");
            _dailyQuestContainer = Require<VisualElement>("DailyQuestContainer");
            _weeklyQuestContainer = Require<VisualElement>("WeeklyQuestContainer");
            _achievementQuestContainer = Require<VisualElement>("AchievementQuestContainer");

            _closeButton.clicked += OnCloseButtonClicked;
            UIButtonAnimationUtility.EnableDefault(_closeButton);
        }

        public void SetTexts(
            string title,
            string dailyHeader,
            string weeklyHeader,
            string achievementHeader,
            string closeButton)
        {
            _titleLabel.text = title;
            _dailyHeaderLabel.text = dailyHeader;
            _weeklyHeaderLabel.text = weeklyHeader;
            _achievementHeaderLabel.text = achievementHeader;
            _closeButton.text = closeButton;
        }

        public void ClearItems()
        {
            _dailyQuestContainer.Clear();
            _weeklyQuestContainer.Clear();
            _achievementQuestContainer.Clear();
        }

        public void AddQuest(
            EQuestCategory category,
            IQuestRuntime quest,
            Action onClaimReward,
            ILocalizationService localizationService)
        {
            var itemRoot = _questItemTemplate.Instantiate();
            var itemView = new QuestItemView(itemRoot);
            itemView.Bind(quest, onClaimReward, localizationService);

            var container = category switch
            {
                EQuestCategory.Daily => _dailyQuestContainer,
                EQuestCategory.Weekly => _weeklyQuestContainer,
                EQuestCategory.Achievement => _achievementQuestContainer,
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
            };
            container.Add(itemRoot);
        }
        
        private void OnDestroy()
        {
            _closeButton.clicked -= OnCloseButtonClicked;
        }

        private T Require<T>(string elementName) where T : VisualElement
        {
            return _root.Q<T>(elementName)
                   ?? throw new InvalidOperationException(
                       $"{nameof(QuestUIView)}: element '{elementName}' was not found.");
        }

        private void OnCloseButtonClicked() => CloseButtonClicked?.Invoke();
    }
}
