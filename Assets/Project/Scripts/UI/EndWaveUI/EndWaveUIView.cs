using System;
using Project.Scripts.Systems.UI;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.EndWaveUI
{
    public class EndWaveUIView : LayoutViewBase, IEndWaveUIView
    {
        private Button _adButton;
        private Button _reviewButton;
        private Button _closeButton;
        
        private Label _titleLabel;
        private Label _rewardLabel;
        private Label _rewardCountLabel;
        private Label _adButtonLabel;
        private Label _reviewButtonLabel;
        private Label _closeButtonLabel;
        
        public event Action CloseButtonClicked;
        public event Action ADButtonClicked;
        public event Action ReviewButtonClicked;

        public override void Awake()
        {
            base.Awake();
            
            _adButton = _root.Q<Button>("ADButton");
            _reviewButton = _root.Q<Button>("ADButton1");
            _closeButton = _root.Q<Button>("CloseButton");
            _titleLabel = _root.Q<Label>("EndWavePanelTitleLabel");
            _rewardLabel = _root.Q<Label>("WaveRewardLabel");
            _rewardCountLabel = _root.Q<Label>("CountRewardLabel");
            _adButtonLabel = _adButton?.Q<Label>("ADButtonLabel");
            _reviewButtonLabel = _reviewButton?.Q<Label>("ADButtonLabel");
            _closeButtonLabel = _root.Q<Label>("CloseButtonLabel");
            
            if (_adButton != null)
                _adButton.clicked += OnADButtonClicked;
            if (_reviewButton != null)
                _reviewButton.clicked += OnReviewButtonClicked;
            if (_closeButton != null)
                _closeButton.clicked += OnCloseButtonClicked;

            ShowAdButtonOnly();
        }

        public void SetTitle(string title)
        {
            if (_titleLabel != null)
                _titleLabel.text = title;
        }

        public void SetRewardCount(int rewardCount)
        {
            if (_rewardCountLabel != null)
                _rewardCountLabel.text = rewardCount.ToString();
        }

        public void SetRewardLabel(string text)
        {
            if (_rewardLabel != null)
                _rewardLabel.text = text;
        }

        public void SetAdButtonText(string text)
        {
            if (_adButtonLabel != null)
                _adButtonLabel.text = text;
        }

        public void SetReviewButtonText(string text)
        {
            if (_reviewButtonLabel != null)
                _reviewButtonLabel.text = text;
        }

        public void SetCloseButtonText(string text)
        {
            if (_closeButtonLabel != null)
                _closeButtonLabel.text = text;
        }

        public void ShowAdButtonOnly()
        {
            SetDisplay(_adButton, true);
            SetDisplay(_reviewButton, false);
        }

        public void ShowReviewButtonOnly()
        {
            SetDisplay(_adButton, false);
            SetDisplay(_reviewButton, true);
        }

        private static void SetDisplay(VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnDestroy()
        {
            if (_adButton != null)
                _adButton.clicked -= OnADButtonClicked;
            if (_reviewButton != null)
                _reviewButton.clicked -= OnReviewButtonClicked;
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseButtonClicked;
        }
        
        private void OnCloseButtonClicked() => CloseButtonClicked?.Invoke();
        private void OnADButtonClicked() => ADButtonClicked?.Invoke();
        private void OnReviewButtonClicked() => ReviewButtonClicked?.Invoke();
    }
}