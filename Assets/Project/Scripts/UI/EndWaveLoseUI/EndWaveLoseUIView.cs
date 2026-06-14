using System;
using Project.Scripts.Systems.UI;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.EndWaveLoseUI
{
    public class EndWaveLoseUIView : LayoutViewBase, IEndWaveLoseUIView
    {
        private Button _adButton;
        private Button _closeButton;
        private Label _titleLabel;
        private Label _descriptionLabel;
        private Label _adButtonLabel;
        private Label _closeButtonLabel;

        public event Action CloseButtonClicked;
        public event Action ADButtonClicked;

        public override void Awake()
        {
            base.Awake();

            _adButton = _root.Q<Button>("ADButton");
            _closeButton = _root.Q<Button>("CloseButton");
            _titleLabel = _root.Q<Label>("EndWaveTitleLabel");
            _descriptionLabel = _root.Q<Label>("EndWaveDescription");
            _adButtonLabel = _root.Q<Label>("ADButtonLabel");
            _closeButtonLabel = _root.Q<Label>("CloseButtonLabel");

            if (_adButton != null)
                _adButton.clicked += OnADButtonClicked;
            if (_closeButton != null)
                _closeButton.clicked += OnCloseButtonClicked;
        }

        public void SetTitle(string title)
        {
            if (_titleLabel != null)
                _titleLabel.text = title;
        }

        public void SetDescription(string description)
        {
            if (_descriptionLabel != null)
                _descriptionLabel.text = description;
        }

        public void SetAdButtonText(string text)
        {
            if (_adButtonLabel != null)
                _adButtonLabel.text = text;
        }

        public void SetCloseButtonText(string text)
        {
            if (_closeButtonLabel != null)
                _closeButtonLabel.text = text;
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