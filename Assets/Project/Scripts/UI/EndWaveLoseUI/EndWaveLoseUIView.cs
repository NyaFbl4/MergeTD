using System;
using Project.Scripts.Systems.UI;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.EndWaveLoseUI
{
    public class EndWaveLoseUIView : LayoutViewBase, IEndWaveLoseUIView
    {
        private Button _adButton;
        private Button _goNextButton;
        private Label _headerLabel;
        private VisualElement _rewardPanel;

        public event Action CloseButtonClicked;
        public event Action ADButtonClicked;

        public override void Awake()
        {
            base.Awake();

            _adButton = Require<Button>("ADButton");
            _goNextButton = Require<Button>("GoNextButton");
            _headerLabel = Require<Label>("HeaderLabel");
            _rewardPanel = Require<VisualElement>("RewardPanel");

            _rewardPanel.style.display = DisplayStyle.None;
            _adButton.clicked += OnADButtonClicked;
            _goNextButton.clicked += OnCloseButtonClicked;
            UIButtonAnimationUtility.EnableDefault(_adButton);
            UIButtonAnimationUtility.EnableDefault(_goNextButton);
        }

        public void SetTitle(string title)
        {
            _headerLabel.text = title;
        }

        public void SetAdButtonText(string text)
        {
            _adButton.text = text;
        }

        public void SetCloseButtonText(string text)
        {
            _goNextButton.text = text;
        }

        private T Require<T>(string elementName) where T : VisualElement
        {
            return _root.Q<T>(elementName)
                   ?? throw new InvalidOperationException(
                       $"{nameof(EndWaveLoseUIView)}: element '{elementName}' was not found.");
        }

        private void OnDestroy()
        {
            _adButton.clicked -= OnADButtonClicked;
            _goNextButton.clicked -= OnCloseButtonClicked;
        }

        private void OnCloseButtonClicked() => CloseButtonClicked?.Invoke();
        private void OnADButtonClicked() => ADButtonClicked?.Invoke();
    }
}
