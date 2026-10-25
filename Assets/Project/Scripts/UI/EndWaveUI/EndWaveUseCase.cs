using System;
using System.Collections.Generic;
using MessagePipe;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI.Dtos;
using Project.Scripts.UI.LevelUI;
using Project.Scripts.UI.MainMenuUI;
using UnityEngine;
using VContainer.Unity;
using YG;

namespace Project.Scripts.UI.EndWaveUI
{
    public class EndWaveUseCase : IInitializable, IDisposable
    {
        private const string DoubleWaveRewardAdId = "end_wave_double_reward";

        private readonly RunBattleRuntime _runBattleRuntime;
        private readonly IEndWaveUIPresenter _endWaveUIPresenter;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;
        private readonly IWorldService _worldService;
        private readonly IPublisher<ShowPopupDto> _showPopupPublisher;
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;

        private IReadOnlyList<WaveRewardConfig> _currentRewards = Array.Empty<WaveRewardConfig>();
        private bool _isWaitingAdReward;
        private bool _isAdRewardClaimed;
        private bool _isShown;
        private bool _isVictoryReward;

        public EndWaveUseCase(
            RunBattleRuntime runBattleRuntime,
            IEndWaveUIPresenter endWaveUIPresenter,
            IPlayerStatsUseCase playerStatsUseCase,
            IWorldService worldService,
            IPublisher<ShowPopupDto> showPopupPublisher,
            IPublisher<HidePopupDto> hidePopupPublisher)
        {
            _runBattleRuntime = runBattleRuntime;
            _endWaveUIPresenter = endWaveUIPresenter;
            _playerStatsUseCase = playerStatsUseCase;
            _worldService = worldService;
            _showPopupPublisher = showPopupPublisher;
            _hidePopupPublisher = hidePopupPublisher;
        }

        public void Initialize()
        {
            _runBattleRuntime.WaveCompleted += OnWaveCompleted;
            _endWaveUIPresenter.CloseRequested += OnCloseRequested;
            _endWaveUIPresenter.AdRequested += OnAdRequested;
        }

        private void OnWaveCompleted(
            int waveNumber,
            IReadOnlyList<WaveRewardConfig> rewards,
            ERunPhase phaseAfterComplete)
        {
            switch (phaseAfterComplete)
            {
                case ERunPhase.Reward:
                    GrantWaveRewards(rewards);
                    ShowRewardPopup(waveNumber, rewards);
                    break;
                case ERunPhase.CardChoice:
                    GrantWaveRewards(rewards);
                    break;
                case ERunPhase.Victory:
                    GrantWaveRewards(rewards);
                    ShowRewardPopup(waveNumber, rewards, true);
                    break;
                case ERunPhase.Defeat:
                    break;
            }
        }

        private void GrantWaveRewards(IReadOnlyList<WaveRewardConfig> rewards)
        {
            for (var i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                switch (reward.Type)
                {
                    case EWaveRewardType.Gold:
                        _playerStatsUseCase.AddGold(reward.Amount);
                        break;
                    case EWaveRewardType.Gems:
                        _worldService.AddGems(reward.Amount);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(reward.Type), reward.Type, null);
                }
            }
        }

        private void ShowRewardPopup(
            int waveNumber,
            IReadOnlyList<WaveRewardConfig> rewards,
            bool isVictoryReward = false)
        {
            _isShown = true;
            _isVictoryReward = isVictoryReward;
            _isWaitingAdReward = false;
            _isAdRewardClaimed = false;
            _currentRewards = rewards;

            _endWaveUIPresenter.SetData(waveNumber, rewards);

            _showPopupPublisher.Publish(new ShowPopupDto
            {
                TargetPopUpType = typeof(IEndWaveUIPresenter)
            });
        }

        private void OnCloseRequested()
        {
            if (_isWaitingAdReward)
                return;

            ClosePopupAndContinue();
        }

        private void OnAdRequested()
        {
            if (!_isShown || _isWaitingAdReward || _isAdRewardClaimed || !HasPositiveReward())
                return;

            _isWaitingAdReward = true;
            SubscribeRewardedAdEvents();
            YG2.RewardedAdvShow(DoubleWaveRewardAdId);
        }

        private bool HasPositiveReward()
        {
            for (var i = 0; i < _currentRewards.Count; i++)
            {
                if (_currentRewards[i].Amount > 0)
                    return true;
            }

            return false;
        }

        private void GrantAdRewardAndContinue()
        {
            if (_isAdRewardClaimed)
                return;

            _isAdRewardClaimed = true;
            _isWaitingAdReward = false;
            GrantWaveRewards(_currentRewards);
            ClosePopupAndContinue();
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
            if (!_isWaitingAdReward || rewardId != DoubleWaveRewardAdId)
                return;

            UnsubscribeRewardedAdEvents();
            GrantAdRewardAndContinue();
        }

        private void OnRewardedAdClosed()
        {
            if (!_isWaitingAdReward)
                return;

            _isWaitingAdReward = false;
            UnsubscribeRewardedAdEvents();
            Debug.Log("EndWaveUseCase: rewarded ad was closed without reward.");
        }

        private void OnRewardedAdError()
        {
            if (!_isWaitingAdReward)
                return;

            _isWaitingAdReward = false;
            UnsubscribeRewardedAdEvents();
            Debug.LogWarning("EndWaveUseCase: rewarded ad failed.");
        }

        private void ClosePopupAndContinue()
        {
            if (!_isShown)
                return;

            _isShown = false;
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(IEndWaveUIPresenter)
            });

            if (_isVictoryReward)
            {
                _runBattleRuntime.FinishVictory();
                _hidePopupPublisher.Publish(new HidePopupDto
                {
                    TargetPopUpType = typeof(ILevelUIPresenter)
                });
                _showPopupPublisher.Publish(new ShowPopupDto
                {
                    TargetPopUpType = typeof(IMainMenuUIPresenter)
                });
                TryShowInterstitialAd();
                return;
            }

            TryShowInterstitialAd();
            _runBattleRuntime.ContinueToNextPreparation();
        }

        private static void TryShowInterstitialAd()
        {
            if (YG2.nowAdsShow)
                return;

            YG2.InterstitialAdvShow();
        }

        public void Dispose()
        {
            _runBattleRuntime.WaveCompleted -= OnWaveCompleted;
            _endWaveUIPresenter.CloseRequested -= OnCloseRequested;
            _endWaveUIPresenter.AdRequested -= OnAdRequested;

            if (_isWaitingAdReward)
                UnsubscribeRewardedAdEvents();
        }
    }
}
