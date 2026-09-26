using System;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.InBattleUI
{
    public sealed class InBattleUIView : IInBattleUIView
    {
        private readonly VisualElement _root;
        private readonly Button _previousRunButton;
        private readonly Button _nextRunButton;
        private readonly Button _playButton;
        private readonly Label _levelNameLabel;
        private readonly VisualElement _runIcon;

        public event Action PreviousRunClicked;
        public event Action NextRunClicked;
        public event Action PlayClicked;

        public InBattleUIView(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _previousRunButton = Require<Button>("PrevLevelButton");
            _nextRunButton = Require<Button>("NextLevelButton");
            _playButton = Require<Button>("PlayButton");
            _levelNameLabel = Require<Label>("LevelNameLabel");
            _runIcon = Require<VisualElement>("RunIcon");

            _previousRunButton.clicked += OnPreviousRunClicked;
            _nextRunButton.clicked += OnNextRunClicked;
            _playButton.clicked += OnPlayClicked;

            UIButtonAnimationUtility.EnableDefault(_previousRunButton);
            UIButtonAnimationUtility.EnableDefault(_nextRunButton, flipX: true);
            UIButtonAnimationUtility.EnableDefault(_playButton);
        }

        public void SetRun(string displayName, Sprite icon, bool canNavigate)
        {
            _levelNameLabel.text = displayName;
            _runIcon.style.backgroundImage = icon == null
                ? new StyleBackground(StyleKeyword.None)
                : new StyleBackground(icon);
            _previousRunButton.SetEnabled(canNavigate);
            _nextRunButton.SetEnabled(canNavigate);
        }

        public void SetVisible(bool visible)
        {
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Dispose()
        {
            _previousRunButton.clicked -= OnPreviousRunClicked;
            _nextRunButton.clicked -= OnNextRunClicked;
            _playButton.clicked -= OnPlayClicked;
        }

        private T Require<T>(string elementName) where T : VisualElement
        {
            return _root.Q<T>(elementName)
                   ?? throw new InvalidOperationException(
                       $"{nameof(InBattleUIView)}: element '{elementName}' was not found.");
        }

        private void OnPreviousRunClicked() => PreviousRunClicked?.Invoke();
        private void OnNextRunClicked() => NextRunClicked?.Invoke();
        private void OnPlayClicked() => PlayClicked?.Invoke();
    }
}
