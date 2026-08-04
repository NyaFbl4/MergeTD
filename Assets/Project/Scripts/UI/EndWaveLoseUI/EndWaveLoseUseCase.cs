using System;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI.Dtos;
using UnityEngine;
using VContainer.Unity;
using YG;

namespace Project.Scripts.UI.EndWaveLoseUI
{
    public class EndWaveLoseUseCase : IInitializable, IDisposable
    {
        private const string RetryWaveRewardAdId = "end_wave_lose_reward";

        private readonly BaseHealth _baseHealth;
        private readonly IEndWaveLoseUIPresenter _endWaveLoseUIPresenter;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;
        private readonly IGameManagerService _gameManagerService;
        private readonly IPublisher<ShowPopupDto> _showPopupPublisher;
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly LevelConfig _levelConfig;
        private readonly ProgressCheckpointUseCase _progressCheckpointUseCase;

        private bool _isShown;
        private bool _isWaitingAdReward;
        private bool _isAdRewardClaimed;
        private bool _isRewardedAdClosing;
        private int _currentWaveNumber;
        private int _currentRewardCount;

        public EndWaveLoseUseCase(
            BaseHealth baseHealth,
            IEndWaveLoseUIPresenter endWaveLoseUIPresenter,
            IPlayerStatsUseCase playerStatsUseCase,
            IGameManagerService gameManagerService,
            IPublisher<ShowPopupDto> showPopupPublisher,
            IPublisher<HidePopupDto> hidePopupPublisher,
            LevelConfig levelConfig,
            ProgressCheckpointUseCase progressCheckpointUseCase)
        {
            _baseHealth = baseHealth;
            _endWaveLoseUIPresenter = endWaveLoseUIPresenter;
            _playerStatsUseCase = playerStatsUseCase;
            _gameManagerService = gameManagerService;
            _showPopupPublisher = showPopupPublisher;
            _hidePopupPublisher = hidePopupPublisher;
            _levelConfig = levelConfig;
            _progressCheckpointUseCase = progressCheckpointUseCase;
        }

        public void Initialize()
        {
            _baseHealth.Destroyed += OnBaseDestroyed;
            _endWaveLoseUIPresenter.CloseRequested += OnCloseRequested;
            _endWaveLoseUIPresenter.AdRequested += OnAdRequested;
        }

        private void OnBaseDestroyed()
        {
            if (_isShown)
                return;

            var waveNumber = _playerStatsUseCase.Wave;
            var rewardCount = GetWaveReward(waveNumber);

            _isWaitingAdReward = false;
            _isAdRewardClaimed = false;
            _isRewardedAdClosing = false;
            _currentWaveNumber = waveNumber;
            _currentRewardCount = rewardCount;

            _progressCheckpointUseCase.SaveRetryCheckpoint(waveNumber);

            _isShown = true;
            _endWaveLoseUIPresenter.SetData(waveNumber, rewardCount);
            _showPopupPublisher.Publish(new ShowPopupDto
            {
                TargetPopUpType = typeof(IEndWaveLoseUIPresenter)
            });
        }

        private int GetWaveReward(int waveNumber)
        {
            var waves = _levelConfig.Waves;
            if (waves == null || waves.Count == 0)
                return 0;

            var index = Math.Max(0, Math.Min(waveNumber - 1, waves.Count - 1));
            var wave = waves[index];
            return wave != null ? wave.CountGoldReward : 0;
        }

        private void OnCloseRequested()
        {
            if (_isWaitingAdReward)
                return;

            ClosePopupAndRestart();
        }

        private void OnAdRequested()
        {
            if (_isWaitingAdReward || _isAdRewardClaimed)
                return;

            if (_currentRewardCount <= 0)
                return;

            ShowRewardedAdForRetryReward();
        }

        private void ShowRewardedAdForRetryReward()
        {
            _isWaitingAdReward = true;
            SubscribeRewardedAdEvents();
            YG2.RewardedAdvShow(RetryWaveRewardAdId);
        }

        private void GrantAdReward()
        {
            if (_isAdRewardClaimed)
                return;

            _isAdRewardClaimed = true;

            _playerStatsUseCase.AddGold(_currentRewardCount);
            _progressCheckpointUseCase.SaveRetryCheckpoint(_currentWaveNumber);
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
            if (!_isWaitingAdReward || rewardId != RetryWaveRewardAdId)
                return;

            GrantAdReward();
        }

        private void OnRewardedAdClosed()
        {
            if (!_isWaitingAdReward)
                return;

            if (_isAdRewardClaimed)
            {
                ClosePopupAndRestartAfterAdPauseAsync().Forget();
                return;
            }

            CancelRewardedAdWaiting();
            Debug.Log("EndWaveLoseUseCase: rewarded ad was closed without reward.");
        }

        private void OnRewardedAdError()
        {
            if (!_isWaitingAdReward)
                return;

            CancelRewardedAdWaiting();
            Debug.LogWarning("EndWaveLoseUseCase: rewarded ad failed.");
        }

        private void CancelRewardedAdWaiting()
        {
            _isWaitingAdReward = false;
            _isRewardedAdClosing = false;
            UnsubscribeRewardedAdEvents();
        }

        private async UniTaskVoid ClosePopupAndRestartAfterAdPauseAsync()
        {
            if (_isRewardedAdClosing)
                return;

            _isRewardedAdClosing = true;
            UnsubscribeRewardedAdEvents();

            await UniTask.NextFrame();

            _isWaitingAdReward = false;
            _isRewardedAdClosing = false;
            ClosePopupAndRestart();
        }

        private void ClosePopupAndRestart()
        {
            ClosePopup();
            _gameManagerService.StartGame();
        }

        private void ClosePopup()
        {
            if (!_isShown)
                return;

            _isShown = false;
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(IEndWaveLoseUIPresenter)
            });
        }

        public void Dispose()
        {
            _baseHealth.Destroyed -= OnBaseDestroyed;
            _endWaveLoseUIPresenter.CloseRequested -= OnCloseRequested;
            _endWaveLoseUIPresenter.AdRequested -= OnAdRequested;

            if (_isWaitingAdReward)
                UnsubscribeRewardedAdEvents();
        }
    }
}
