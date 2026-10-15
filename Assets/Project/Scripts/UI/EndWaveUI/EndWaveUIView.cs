using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.EndWaveUI
{
    public class EndWaveUIView : LayoutViewBase, IEndWaveUIView
    {
        private const string GoldIconPath = "UI/new/Icons/Icon_Gold";
        private const string GemsIconPath = "UI/new/Icons/Icon_Gem03_Diamond_Purple 1";

        [SerializeField] private VisualTreeAsset _rewardPanelTemplate;

        private Button _adButton;
        private Button _goNextButton;
        private Label _headerLabel;
        private VisualElement _rewardContainer;
        private StyleBackground _goldIcon;
        private StyleBackground _gemsIcon;

        public event Action CloseButtonClicked;
        public event Action ADButtonClicked;

        public override void Awake()
        {
            base.Awake();

            _adButton = Require<Button>("ADButton");
            _goNextButton = Require<Button>("GoNextButton");
            _headerLabel = Require<Label>("HeaderLabel");
            _rewardContainer = Require<VisualElement>("RewardPanel");
            _goldIcon = LoadSingleSprite(GoldIconPath);
            _gemsIcon = LoadSingleSprite(GemsIconPath);

            _rewardContainer.Clear();
            _adButton.clicked += OnADButtonClicked;
            _goNextButton.clicked += OnCloseButtonClicked;
            UIButtonAnimationUtility.EnableDefault(_adButton);
            UIButtonAnimationUtility.EnableDefault(_goNextButton);
        }

        public void SetTitle(string title)
        {
            _headerLabel.text = title;
        }

        public void SetRewards(IReadOnlyList<WaveRewardConfig> rewards)
        {
            _rewardContainer.Clear();

            for (var i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                var rewardPanel = _rewardPanelTemplate.Instantiate();
                var rewardIcon = RequireFrom<VisualElement>(rewardPanel, "RewardIcon");
                var rewardCountLabel = RequireFrom<Label>(rewardPanel, "RewardCountLabel");

                rewardIcon.style.backgroundImage = GetRewardIcon(reward.Type);
                rewardCountLabel.text = reward.Amount.ToString();
                _rewardContainer.Add(rewardPanel);
            }
        }

        public void SetAdButtonText(string text)
        {
            _adButton.text = text;
        }

        public void SetCloseButtonText(string text)
        {
            _goNextButton.text = text;
        }

        private StyleBackground GetRewardIcon(EWaveRewardType rewardType)
        {
            return rewardType switch
            {
                EWaveRewardType.Gold => _goldIcon,
                EWaveRewardType.Gems => _gemsIcon,
                _ => throw new ArgumentOutOfRangeException(nameof(rewardType), rewardType, null)
            };
        }

        private static StyleBackground LoadSingleSprite(string resourcePath)
        {
            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites.Length != 1)
                throw new InvalidOperationException(
                    $"Expected one sprite at Resources/{resourcePath}, found {sprites.Length}.");

            return new StyleBackground(sprites[0]);
        }

        private T Require<T>(string elementName) where T : VisualElement
        {
            return _root.Q<T>(elementName)
                   ?? throw new InvalidOperationException(
                       $"{nameof(EndWaveUIView)}: element '{elementName}' was not found.");
        }

        private static T RequireFrom<T>(VisualElement root, string elementName) where T : VisualElement
        {
            return root.Q<T>(elementName)
                   ?? throw new InvalidOperationException(
                       $"{nameof(EndWaveUIView)}: reward element '{elementName}' was not found.");
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
