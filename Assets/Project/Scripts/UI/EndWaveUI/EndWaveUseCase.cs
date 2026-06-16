using YG;
using System;
using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Systems;
using Project.Scripts.System.Localization;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI.Dtos;
using UnityEngine;
using VContainer.Unity;

namespace Project.Scripts.UI.EndWaveUI
{
    public class EndWaveUseCase : IInitializable, IDisposable
    {
        private const string DoubleWaveRewardAdId = "end_wave_double_reward";

        private readonly BattlefieldRuntime _battlefieldRuntime;
        private readonly IEndWaveUIPresenter _endWaveUIPresenter;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;
        private readonly IPublisher<ShowPopupDto> _showPopupPublisher;
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly IGameManagerService _gameManagerService;
        private readonly ILocalizationService _localizationService;
        private readonly ProgressCheckpointUseCase _progressCheckpointUseCase;

        private bool _isLastWave;
        private bool _isWaitingAdReward;
        private bool _isAdRewardClaimed;
        private int _currentWaveNumber;
        private int _currentRewardCount;

        public EndWaveUseCase(
            BattlefieldRuntime battlefieldRuntime,
            IEndWaveUIPresenter endWaveUIPresenter,
            IPlayerStatsUseCase playerStatsUseCase,
            IPublisher<ShowPopupDto> showPopupPublisher,
            IPublisher<HidePopupDto> hidePopupPublisher,
            IGameManagerService gameManagerService,
            ILocalizationService localizationService,
            ProgressCheckpointUseCase progressCheckpointUseCase)
        {
            _battlefieldRuntime = battlefieldRuntime;
            _endWaveUIPresenter = endWaveUIPresenter;
            _playerStatsUseCase = playerStatsUseCase;
            _showPopupPublisher = showPopupPublisher;
            _hidePopupPublisher = hidePopupPublisher;
            _gameManagerService = gameManagerService;
            _localizationService = localizationService;
            _progressCheckpointUseCase = progressCheckpointUseCase;
        }

        public void Initialize()
        {
            _battlefieldRuntime.WaveCompleted += OnWaveCompleted;
            _endWaveUIPresenter.CloseRequested += OnCloseRequested;
            _endWaveUIPresenter.AdRequested += OnAdRequested;
        }

        private void OnWaveCompleted(int waveNumber, int rewardCount, bool isLastWave)
        {
            _isLastWave = isLastWave;
            _isWaitingAdReward = false;
            _isAdRewardClaimed = false;
            _currentWaveNumber = waveNumber;
            _currentRewardCount = rewardCount;

            _playerStatsUseCase.AddGold(rewardCount);
            _progressCheckpointUseCase.SaveCheckpoint(waveNumber + 1);
            _endWaveUIPresenter.SetData(_localizationService.Format(LocalizationKeys.EndWaveTitleFormat, 
                                        waveNumber), rewardCount);

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
            if (_isWaitingAdReward || _isAdRewardClaimed)
                return;

            if (_currentRewardCount <= 0)
            {
                Debug.LogWarning("EndWaveUseCase: reward count is empty, rewarded ad reward was skipped.");
                return;
            }
            
            _isWaitingAdReward = true;
            SubscribeRewardedAdEvents();
            YG2.RewardedAdvShow(DoubleWaveRewardAdId);
            GrantAdRewardAndContinue();
        }

        private void GrantAdRewardAndContinue()
        {
            if (_isAdRewardClaimed)
                return;

            _isAdRewardClaimed = true;
            _isWaitingAdReward = false;

            _playerStatsUseCase.AddGold(_currentRewardCount);
            _progressCheckpointUseCase.SaveCheckpoint(_currentWaveNumber + 1);
            ClosePopupAndContinue();
        }

#if RewardedAdv_yg
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
#endif

        private void ClosePopupAndContinue()
        {
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(IEndWaveUIPresenter)
            });

            if (_isLastWave)
            {
                _gameManagerService.FinishGame();
                return;
            }

            _battlefieldRuntime.ContinueAfterEndWavePopup();
        }

        public void Dispose()
        {
            _battlefieldRuntime.WaveCompleted -= OnWaveCompleted;
            _endWaveUIPresenter.CloseRequested -= OnCloseRequested;
            _endWaveUIPresenter.AdRequested -= OnAdRequested;

#if RewardedAdv_yg
            if (_isWaitingAdReward)
                UnsubscribeRewardedAdEvents();
#endif
        }        
    }
}