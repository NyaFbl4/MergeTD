using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Project.Scripts.System.Reward;
using Project.Scripts.System.Reward.RewardConfigs;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.InBattleUI
{
    public sealed class InBattleUIView : IInBattleUIView
    {
        private const int RewardCellCount = DailyRewards.CycleLength;
        private const float PulseSpeed = 4f;
        private const float PulseScale = 1.08f;
        private const string GoldIconPath = "UI/new/Icons/Icon_Gold";
        private const string GemsIconPath = "UI/new/Icons/Icon_Gem03_Diamond_Purple 1";

        private static readonly Color NeutralRewardColor = Color.white;
        private static readonly Color AvailableRewardColor = new Color32(255, 218, 48, 255);
        private static readonly Color ClaimedRewardColor = new Color32(78, 218, 88, 255);

        private readonly VisualElement _root;
        private readonly Button _previousRunButton;
        private readonly Button _nextRunButton;
        private readonly Button _playButton;
        private readonly Button _dailyRewardButton;
        private readonly Button _normalRewardButton;
        private readonly Button _doubleRewardButton;
        private readonly Label _levelNameLabel;
        private readonly Label _dailyRewardCountdownLabel;
        private readonly Label _offerAmountLabel;
        private readonly VisualElement _runIcon;
        private readonly VisualElement _offerOverlay;
        private readonly VisualElement _offerIcon;
        private readonly VisualElement[] _rewardCells = new VisualElement[RewardCellCount];
        private readonly VisualElement[] _rewardIcons = new VisualElement[RewardCellCount];
        private readonly Label[] _rewardAmountLabels = new Label[RewardCellCount];
        private readonly StyleBackground _goldIcon;
        private readonly StyleBackground _gemsIcon;
        private int _pulseVersion;

        public event Action PreviousRunClicked;
        public event Action NextRunClicked;
        public event Action PlayClicked;
        public event Action DailyRewardClicked;
        public event Action DailyRewardNormalClaimClicked;
        public event Action DailyRewardDoubleClaimClicked;

        public InBattleUIView(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _previousRunButton = Require<Button>("PrevLevelButton");
            _nextRunButton = Require<Button>("NextLevelButton");
            _playButton = Require<Button>("PlayButton");
            _dailyRewardButton = Require<Button>("DailyRewardClaimButton");
            _normalRewardButton = Require<Button>("DailyRewardNormalButton");
            _doubleRewardButton = Require<Button>("DailyRewardDoubleButton");
            _levelNameLabel = Require<Label>("LevelNameLabel");
            _dailyRewardCountdownLabel = Require<Label>("DailyRewardCountdownLabel");
            _offerAmountLabel = Require<Label>("DailyRewardOfferAmountLabel");
            _runIcon = Require<VisualElement>("RunIcon");
            _offerOverlay = Require<VisualElement>("DailyRewardOfferOverlay");
            _offerIcon = Require<VisualElement>("DailyRewardOfferIcon");
            _goldIcon = LoadSingleSprite(GoldIconPath);
            _gemsIcon = LoadSingleSprite(GemsIconPath);

            var rewardContainer = Require<VisualElement>("DailyRewardContainer");
            if (rewardContainer.childCount != RewardCellCount)
                throw new InvalidOperationException(
                    $"Expected {RewardCellCount} daily reward cells, found {rewardContainer.childCount}.");

            for (var i = 0; i < RewardCellCount; i++)
            {
                var rewardTemplate = rewardContainer.ElementAt(i);
                _rewardCells[i] = RequireFrom<VisualElement>(rewardTemplate, "DailyRewardElement");
                _rewardIcons[i] = RequireFrom<VisualElement>(rewardTemplate, "RewardIcon");
                _rewardAmountLabels[i] = RequireFrom<Label>(rewardTemplate, "RevardCountLabel");
            }

            _previousRunButton.clicked += OnPreviousRunClicked;
            _nextRunButton.clicked += OnNextRunClicked;
            _playButton.clicked += OnPlayClicked;
            _dailyRewardButton.clicked += OnDailyRewardClicked;
            _normalRewardButton.clicked += OnDailyRewardNormalClaimClicked;
            _doubleRewardButton.clicked += OnDailyRewardDoubleClaimClicked;

            UIButtonAnimationUtility.EnableDefault(_previousRunButton);
            UIButtonAnimationUtility.EnableDefault(_nextRunButton, flipX: true);
            UIButtonAnimationUtility.EnableDefault(_playButton);
            UIButtonAnimationUtility.EnableDefault(_dailyRewardButton);
            UIButtonAnimationUtility.EnableDefault(_normalRewardButton);
            UIButtonAnimationUtility.EnableDefault(_doubleRewardButton);
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

        public void SetDailyRewards(IReadOnlyList<DailyRewardEntry> rewards, DailyRewardState state)
        {
            if (rewards.Count != RewardCellCount)
                throw new InvalidOperationException($"Expected {RewardCellCount} daily rewards.");

            var pulseVersion = ++_pulseVersion;
            for (var i = 0; i < RewardCellCount; i++)
            {
                var reward = rewards[i];
                _rewardIcons[i].style.backgroundImage = GetRewardIcon(reward.Currency);
                _rewardAmountLabels[i].text = reward.Amount.ToString();
                _rewardCells[i].style.scale = new Scale(Vector3.one);

                if (i <= state.ClaimedThroughIndex)
                {
                    _rewardCells[i].style.unityBackgroundImageTintColor = ClaimedRewardColor;
                }
                else if (state.CanClaim && i == state.AvailableRewardIndex)
                {
                    _rewardCells[i].style.unityBackgroundImageTintColor = AvailableRewardColor;
                    PulseRewardAsync(_rewardCells[i], pulseVersion).Forget();
                }
                else
                {
                    _rewardCells[i].style.unityBackgroundImageTintColor = NeutralRewardColor;
                }
            }

            var canClaim = state.CanClaim && state.IsTimeAvailable;
            _dailyRewardButton.text = "ЗАБРАТЬ";
            _dailyRewardButton.style.display = canClaim
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _dailyRewardButton.SetEnabled(canClaim);
            _dailyRewardButton.pickingMode = canClaim
                ? PickingMode.Position
                : PickingMode.Ignore;
            _dailyRewardButton.focusable = canClaim;
        }

        public void SetDailyRewardCountdown(bool visible, string text)
        {
            _dailyRewardCountdownLabel.text = text;
            _dailyRewardCountdownLabel.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void ShowDailyRewardOffer(DailyRewardEntry reward)
        {
            var doubledAmount = (int)Math.Min(int.MaxValue, (long)reward.Amount * 2);
            _offerIcon.style.backgroundImage = GetRewardIcon(reward.Currency);
            _offerAmountLabel.text = doubledAmount.ToString();
            SetDailyRewardOfferButtonsEnabled(true);
            _offerOverlay.style.display = DisplayStyle.Flex;
        }

        public void HideDailyRewardOffer()
        {
            _offerOverlay.style.display = DisplayStyle.None;
        }

        public void SetDailyRewardOfferButtonsEnabled(bool enabled)
        {
            _normalRewardButton.SetEnabled(enabled);
            _doubleRewardButton.SetEnabled(enabled);
        }

        public void SetVisible(bool visible)
        {
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Dispose()
        {
            _pulseVersion++;
            _previousRunButton.clicked -= OnPreviousRunClicked;
            _nextRunButton.clicked -= OnNextRunClicked;
            _playButton.clicked -= OnPlayClicked;
            _dailyRewardButton.clicked -= OnDailyRewardClicked;
            _normalRewardButton.clicked -= OnDailyRewardNormalClaimClicked;
            _doubleRewardButton.clicked -= OnDailyRewardDoubleClaimClicked;
        }

        private async UniTaskVoid PulseRewardAsync(VisualElement rewardCell, int version)
        {
            while (version == _pulseVersion)
            {
                var pulse = (Mathf.Sin(Time.unscaledTime * PulseSpeed) + 1f) * 0.5f;
                var scale = Mathf.Lerp(1f, PulseScale, pulse);
                rewardCell.style.scale = new Scale(new Vector3(scale, scale, 1f));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }

        private StyleBackground GetRewardIcon(EDailyRewardCurrency currency)
        {
            switch (currency)
            {
                case EDailyRewardCurrency.Gold:
                    return _goldIcon;
                case EDailyRewardCurrency.Gems:
                    return _gemsIcon;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currency), currency, null);
            }
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
                       $"{nameof(InBattleUIView)}: element '{elementName}' was not found.");
        }

        private static T RequireFrom<T>(VisualElement root, string elementName) where T : VisualElement
        {
            return root.Q<T>(elementName)
                   ?? throw new InvalidOperationException(
                       $"Daily reward element '{elementName}' was not found.");
        }

        private void OnPreviousRunClicked() => PreviousRunClicked?.Invoke();
        private void OnNextRunClicked() => NextRunClicked?.Invoke();
        private void OnPlayClicked() => PlayClicked?.Invoke();
        private void OnDailyRewardClicked() => DailyRewardClicked?.Invoke();
        private void OnDailyRewardNormalClaimClicked() => DailyRewardNormalClaimClicked?.Invoke();
        private void OnDailyRewardDoubleClaimClicked() => DailyRewardDoubleClaimClicked?.Invoke();
    }
}
