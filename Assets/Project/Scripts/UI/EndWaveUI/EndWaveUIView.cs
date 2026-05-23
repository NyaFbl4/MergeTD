using System;
using Project.Scripts.Systems.UI;
using Project.Scripts.UI.LevelUI;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.EndWaveUI
{
    public class EndWaveUIView : LayoutViewBase, IEndWaveUIView
    {
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
            _rewardLabel = _root.Q<Label>(" WaveRewardLabel");
            _rewardCountLabel = _root.Q<Label>(" CountRewardLabel");
            _adButtonLabel = _root.Q<Label>(" ADButtonLabel");
            _closeButtonLabel = _root.Q<Label>(" CloseButtonLabel");
            
            if (_adButton != null)
                _adButton.clicked += OnCloseButtonClicked;
            if (_closeButton != null)
                _closeButton.clicked += OnADButtonClicked;
        }

        public void SetTitle(string title)
        {
            throw new NotImplementedException();
        }

        public void SetRewardCount(int rewardCount)
        {
            throw new NotImplementedException();
        }

        private void OnDestroy()
        {
            if (_adButton != null)
                _adButton.clicked -= OnCloseButtonClicked;
            if (_closeButton != null)
                _closeButton.clicked -= OnADButtonClicked;
        }
        
        private void OnCloseButtonClicked() => CloseButtonClicked?.Invoke();
        private void OnADButtonClicked() => ADButtonClicked?.Invoke();
    }
}