using System;
using Project.Scripts.Gameplay.Quests;
using Project.Scripts.System.Localization;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.QuestUI
{
    public class QuestItemView
    {
        private readonly VisualElement _icon;
        private readonly Label _descriptionLabel;
        private readonly Label _rewardLabel;
        private readonly VisualElement _rewardIcon;
        private readonly VisualElement _progressFill;
        private readonly Button _takeRewardButton;
        private readonly StyleBackground _goldIcon;
        private readonly StyleBackground _gemsIcon;

        public QuestItemView(VisualElement root)
        {
            _icon = root.Q<VisualElement>("QuestIcon");
            _descriptionLabel = root.Q<Label>("DescriptionQuestLabel");
            _rewardLabel = root.Q<Label>("RewardLabel");
            _rewardIcon = root.Q<VisualElement>("RewardIcon");
            _progressFill = root.Q<VisualElement>("ProgressFill");
            _takeRewardButton = root.Q<Button>("TakeRewardButton");
            _goldIcon = LoadSingleSprite("UI/new/Icons/Icon_Gold");
            _gemsIcon = LoadSingleSprite("UI/new/Icons/Icon_Gem03_Diamond_Purple 1");
            UIButtonAnimationUtility.EnableDefault(_takeRewardButton);
        }

        public void Bind(IQuestRuntime quest, Action onClaimReward, ILocalizationService localizationService)
        {
            _descriptionLabel.text = localizationService.Format(quest.Description, quest.TargetValue);
            var rewardGems = quest.RewardGems;
            _rewardLabel.text = (rewardGems > 0 ? rewardGems : quest.RewardGold).ToString();
            _rewardIcon.style.backgroundImage = rewardGems > 0 ? _gemsIcon : _goldIcon;
            _icon.style.backgroundImage = new StyleBackground(quest.Icon);
            _progressFill.style.width = Length.Percent(
                100f * quest.CurrentValue / Math.Max(1, quest.TargetValue));

            var canClaim = quest.IsCompleted && !quest.IsRewardClaimed;
            _takeRewardButton.text = quest.IsRewardClaimed
                ? localizationService.Get(LocalizationKeys.QuestDone)
                : canClaim
                    ? localizationService.Get(LocalizationKeys.QuestClaim)
                    : $"{quest.CurrentValue}/{quest.TargetValue}";
            _takeRewardButton.SetEnabled(canClaim);
            _takeRewardButton.style.opacity = 1f;

            _takeRewardButton.clicked += onClaimReward;
        }

        private static StyleBackground LoadSingleSprite(string resourcePath)
        {
            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites.Length != 1)
                throw new InvalidOperationException(
                    $"Expected one sprite at Resources/{resourcePath}, found {sprites.Length}.");

            return new StyleBackground(sprites[0]);
        }
    }
}
