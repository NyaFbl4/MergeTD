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
        private Button _closeButton;
        private ScrollView _scrollView;
        
        public event Action CloseButtonClicked;

        public override void Awake()
        {
            base.Awake();
            
            _closeButton = _root.Q<Button>("CloseButton");
            _scrollView = _root.Q<ScrollView>("ScrollView");
            _titleLabel = _root.Q<Label>("TitleLabel");

            if (_closeButton != null)
                _closeButton.clicked += OnCloseButtonClicked;
        }

        public void SetTitle(string title)
        {
            _titleLabel.text = title;
        }

        public void ClearItems()
        {
            _scrollView.Clear();
        }

        public void AddQuest(IQuestRuntime quest, Action onClaimReward, ILocalizationService localizationService)
        {
            var itemRoot = _questItemTemplate.Instantiate();
            var itemView = new QuestItemView(itemRoot);
            itemView.Bind(quest, onClaimReward, localizationService);
            _scrollView.Add(itemRoot);
        }
        
        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseButtonClicked;
        }

        private void OnCloseButtonClicked() => CloseButtonClicked?.Invoke();
    }
}