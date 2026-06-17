using System;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.EndWaveUI
{
    public class EndWaveUIView : LayoutViewBase, IEndWaveUIView
    {
        [SerializeField] private Sprite _adButtonSprite;
        [SerializeField] private Sprite _reviewButtonSprite;
        
        private Button _adButton;
        private Button _closeButton;
        
        private Label _titleLabel;
        private Label _rewardLabel;
        private Label _rewardCountLabel;
        private Label _adButtonLabel;
        private Label _closeButtonLabel;
        
        public event Action CloseButtonClicked;
        public event Action ADButtonClicked;

        public override void Awake()
        {
            base.Awake();
            
            _adButton = _root.Q<Button>("ADButton");
            _closeButton = _root.Q<Button>("CloseButton");
            _titleLabel = _root.Q<Label>("EndWavePanelTitleLabel");
            _rewardLabel = _root.Q<Label>("WaveRewardLabel");
            _rewardCountLabel = _root.Q<Label>("CountRewardLabel");
            _adButtonLabel = _root.Q<Label>("ADButtonLabel");
            _closeButtonLabel = _root.Q<Label>("CloseButtonLabel");
            
            if (_adButton != null)
                _adButton.clicked += OnADButtonClicked;
            if (_closeButton != null)
                _closeButton.clicked += OnCloseButtonClicked;
        }

        public void SetTitle(string title)
        {
            _titleLabel.text = title;
        }

        public void SetRewardCount(int rewardCount)
        {
            _rewardCountLabel.text = rewardCount.ToString();
        }

        public void SetRewardLabel(string text)
        {
            _rewardLabel.text = text;
        }

        public void SetAdButtonText(string text)
        {
            _adButtonLabel.text = text;
        }

        public void SetCloseButtonText(string text)
        {
            _closeButtonLabel.text = text;
        }
        
        public void SetAdButtonAdMode()
        {
            _adButton.style.backgroundImage = new StyleBackground(_adButtonSprite);
        }

        public void SetAdButtonReviewMode()
        {
            _adButton.style.backgroundImage = new StyleBackground(_reviewButtonSprite);
        }

        private void OnDestroy()
        {
            if (_adButton != null)
                _adButton.clicked -= OnADButtonClicked;
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseButtonClicked;
        }
        
        private void OnCloseButtonClicked() => CloseButtonClicked?.Invoke();
        private void OnADButtonClicked() => ADButtonClicked?.Invoke();
    }
}