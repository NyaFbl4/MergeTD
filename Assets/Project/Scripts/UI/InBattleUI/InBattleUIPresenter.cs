using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.Gameplay.Quests;
using Project.Scripts.System.Localization;
using Project.Scripts.System.Reward;
using Project.Scripts.Systems.UI.Dtos;
using Project.Scripts.UI.MainMenuUI;
using Project.Scripts.UI.QuestUI;
using UnityEngine;
using YG;

namespace Project.Scripts.UI.InBattleUI
{
    public sealed class InBattleUIPresenter : IInBattleUIPresenter
    {
        private const string DailyRewardAdId = "daily_reward_double";

        private readonly IInBattleUIView _view;
        private readonly IRunSelectionService _runSelection;
        private readonly IGameManagerService _gameManagerService;
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly IPublisher<ShowPopupDto> _showPopupPublisher;
        private readonly DailyRewardService _dailyRewardService;
        private readonly QuestService _questService;
        private readonly ILocalizationService _localizationService;
        private CancellationTokenSource _refreshCancellation;
        private DailyRewardState _dailyRewardState;
        private bool _hasDailyRewardState;
        private bool _isWaitingDailyRewardAd;

        public InBattleUIPresenter(
            IInBattleUIView view,
            IRunSelectionService runSelection,
            IGameManagerService gameManagerService,
            IPublisher<HidePopupDto> hidePopupPublisher,
            IPublisher<ShowPopupDto> showPopupPublisher,
            DailyRewardService dailyRewardService,
            QuestService questService,
            ILocalizationService localizationService)
        {
            _view = view;
            _runSelection = runSelection;
            _gameManagerService = gameManagerService;
            _hidePopupPublisher = hidePopupPublisher;
            _showPopupPublisher = showPopupPublisher;
            _dailyRewardService = dailyRewardService;
            _questService = questService;
            _localizationService = localizationService;
        }

        public void Initialize()
        {
            _runSelection.SelectionChanged += OnRunChanged;
            _view.PreviousRunClicked += OnPreviousRunClicked;
            _view.NextRunClicked += OnNextRunClicked;
            _view.PlayClicked += OnPlayClicked;
            _view.DailyRewardClicked += OnDailyRewardClicked;
            _view.DailyRewardNormalClaimClicked += OnDailyRewardNormalClaimClicked;
            _view.DailyRewardDoubleClaimClicked += OnDailyRewardDoubleClaimClicked;
            _view.DailyQuestsButtonClicked += OnDailyQuestsButtonClicked;
            _view.DailyQuestClaimClicked += OnDailyQuestClaimClicked;
            _questService.QuestsChanged += OnDailyQuestsChanged;
            _localizationService.OnChangeLanguage += OnLanguageChanged;

            RefreshRun();
            RefreshDailyRewards();
            _questService.EnsureActiveQuests();
            RefreshDailyQuests();
            _refreshCancellation = new CancellationTokenSource();
            RefreshDailyRewardsLoopAsync(_refreshCancellation.Token).Forget();
        }

        public void Dispose()
        {
            _refreshCancellation.Cancel();
            _refreshCancellation.Dispose();
            UnsubscribeRewardedAdEvents();
            _runSelection.SelectionChanged -= OnRunChanged;
            _view.PreviousRunClicked -= OnPreviousRunClicked;
            _view.NextRunClicked -= OnNextRunClicked;
            _view.PlayClicked -= OnPlayClicked;
            _view.DailyRewardClicked -= OnDailyRewardClicked;
            _view.DailyRewardNormalClaimClicked -= OnDailyRewardNormalClaimClicked;
            _view.DailyRewardDoubleClaimClicked -= OnDailyRewardDoubleClaimClicked;
            _view.DailyQuestsButtonClicked -= OnDailyQuestsButtonClicked;
            _view.DailyQuestClaimClicked -= OnDailyQuestClaimClicked;
            _questService.QuestsChanged -= OnDailyQuestsChanged;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;
        }

        private async UniTaskVoid RefreshDailyRewardsLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var canceled = await UniTask.Delay(
                    TimeSpan.FromSeconds(1),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken).SuppressCancellationThrow();
                if (canceled)
                    return;

                var state = _dailyRewardService.GetState();
                if (!_hasDailyRewardState || !HasSameDailyRewardDisplayState(state, _dailyRewardState))
                    RefreshDailyRewards(state);
                else
                    RefreshDailyRewardCountdown(state);

                _questService.EnsureActiveQuests();
            }
        }

        private void OnRunChanged(RunConfig _) => RefreshRun();
        private void OnPreviousRunClicked() => _runSelection.SelectPrevious();
        private void OnNextRunClicked() => _runSelection.SelectNext();

        private void OnPlayClicked()
        {
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(IMainMenuUIPresenter)
            });
            _gameManagerService.StartGame();
        }

        private void OnDailyRewardClicked()
        {
            var state = _dailyRewardService.GetState();
            if (!state.CanClaim)
            {
                RefreshDailyRewards();
                return;
            }

            _view.ShowDailyRewardOffer(
                _dailyRewardService.Rewards[state.AvailableRewardIndex]);
        }

        private void OnDailyRewardNormalClaimClicked()
        {
            _dailyRewardService.TryClaim(1);
            _view.HideDailyRewardOffer();
            RefreshDailyRewards();
        }

        private void OnDailyRewardDoubleClaimClicked()
        {
            if (_isWaitingDailyRewardAd || YG2.nowAdsShow)
                return;

            _isWaitingDailyRewardAd = true;
            _view.SetDailyRewardOfferButtonsEnabled(false);
            SubscribeRewardedAdEvents();
            YG2.RewardedAdvShow(DailyRewardAdId);
        }

        private void OnDailyQuestClaimClicked(IQuestRuntime quest)
        {
            _questService.TryClaimReward(quest);
        }

        private void OnDailyQuestsButtonClicked()
        {
            _showPopupPublisher.Publish(new ShowPopupDto
            {
                TargetPopUpType = typeof(IQuestUIPresenter)
            });
        }

        private void OnDailyQuestsChanged() => RefreshDailyQuests();
        private void OnLanguageChanged(string _) => RefreshDailyQuests();

        private void RefreshDailyQuests()
        {
            _view.SetDailyQuests(
                _questService.Quests,
                _localizationService);
        }

        private void SubscribeRewardedAdEvents()
        {
            YG2.onRewardAdv += OnRewardedAdReward;
            YG2.onCloseRewardedAdv += OnRewardedAdClosed;
            YG2.onErrorRewardedAdv += OnRewardedAdError;
        }

        private void UnsubscribeRewardedAdEvents()
        {
            YG2.onRewardAdv -= OnRewardedAdReward;
            YG2.onCloseRewardedAdv -= OnRewardedAdClosed;
            YG2.onErrorRewardedAdv -= OnRewardedAdError;
        }

        private void OnRewardedAdReward(string rewardId)
        {
            if (!_isWaitingDailyRewardAd || rewardId != DailyRewardAdId)
                return;

            FinishRewardedAdRequest();
            _dailyRewardService.TryClaim(2);
            _view.HideDailyRewardOffer();
            RefreshDailyRewards();
        }

        private void OnRewardedAdClosed()
        {
            if (!_isWaitingDailyRewardAd)
                return;

            FinishRewardedAdRequest();
            _view.SetDailyRewardOfferButtonsEnabled(true);
        }

        private void OnRewardedAdError()
        {
            if (!_isWaitingDailyRewardAd)
                return;

            FinishRewardedAdRequest();
            _view.SetDailyRewardOfferButtonsEnabled(true);
            Debug.LogWarning("InBattleUIPresenter: daily reward ad failed.");
        }

        private void FinishRewardedAdRequest()
        {
            _isWaitingDailyRewardAd = false;
            UnsubscribeRewardedAdEvents();
        }

        private void RefreshRun()
        {
            var run = _runSelection.SelectedRun;
            _view.SetRun(run.DisplayName, run.Icon, _runSelection.Count > 1);
        }

        private void RefreshDailyRewards()
        {
            RefreshDailyRewards(_dailyRewardService.GetState());
        }

        private void RefreshDailyRewards(DailyRewardState state)
        {
            _dailyRewardState = state;
            _hasDailyRewardState = true;
            _view.SetDailyRewards(
                _dailyRewardService.Rewards,
                state);
            RefreshDailyRewardCountdown(state);
        }

        private void RefreshDailyRewardCountdown(DailyRewardState state)
        {
            if (!state.IsTimeAvailable || !state.CanClaim && !state.ClaimedToday)
            {
                _view.SetDailyRewardCountdown(true, "Время\nнедоступно");
                return;
            }

            if (!state.ClaimedToday)
            {
                _view.SetDailyRewardCountdown(false, string.Empty);
                return;
            }

            if (!_dailyRewardService.TryGetTimeUntilNextMoscowDay(out var remaining))
            {
                _view.SetDailyRewardCountdown(true, "Время\nнедоступно");
                return;
            }

            var totalSeconds = (long)Math.Ceiling(Math.Max(0d, remaining.TotalSeconds));
            var hours = totalSeconds / 3600;
            var minutes = totalSeconds % 3600 / 60;
            var seconds = totalSeconds % 60;
            _view.SetDailyRewardCountdown(
                true,
                $"Следующая награда\n{hours:00}:{minutes:00}:{seconds:00}");
        }

        private static bool HasSameDailyRewardDisplayState(
            DailyRewardState left,
            DailyRewardState right)
        {
            return left.IsTimeAvailable == right.IsTimeAvailable
                   && left.CanClaim == right.CanClaim
                   && left.ClaimedToday == right.ClaimedToday
                   && left.CurrentDay == right.CurrentDay
                   && left.AvailableRewardIndex == right.AvailableRewardIndex
                   && left.ClaimedThroughIndex == right.ClaimedThroughIndex;
        }
    }
}
