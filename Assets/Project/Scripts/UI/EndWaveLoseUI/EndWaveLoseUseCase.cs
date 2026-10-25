using Project.Scripts.System.Analytics;
using System;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Systems.UI.Dtos;
using Project.Scripts.UI.LevelUI;
using Project.Scripts.UI.MainMenuUI;
using UnityEngine;
using VContainer.Unity;
using YG;

namespace Project.Scripts.UI.EndWaveLoseUI
{
    public class EndWaveLoseUseCase : IInitializable, IDisposable
    {
        private const string ReviveCurrentWaveAdId = "end_wave_lose_reward";

        private readonly BaseHealth _baseHealth;
        private readonly IEndWaveLoseUIPresenter _endWaveLoseUIPresenter;
        private readonly RunBattleRuntime _runBattleRuntime;
        private readonly IPublisher<ShowPopupDto> _showPopupPublisher;
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly RunState _runState;

        private bool _isShown;
        private bool _isWaitingAdReward;
        private bool _isReviveGranted;
        private bool _isRewardedAdClosing;

        public EndWaveLoseUseCase(
            BaseHealth baseHealth,
            IEndWaveLoseUIPresenter endWaveLoseUIPresenter,
            RunBattleRuntime runBattleRuntime,
            IPublisher<ShowPopupDto> showPopupPublisher,
            IPublisher<HidePopupDto> hidePopupPublisher,
            RunState runState)
        {
            _baseHealth = baseHealth;
            _endWaveLoseUIPresenter = endWaveLoseUIPresenter;
            _runBattleRuntime = runBattleRuntime;
            _showPopupPublisher = showPopupPublisher;
            _hidePopupPublisher = hidePopupPublisher;
            _runState = runState;
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

            _isWaitingAdReward = false;
            _isReviveGranted = false;
            _isRewardedAdClosing = false;

            _isShown = true;
            _endWaveLoseUIPresenter.SetData(_runState.CurrentWave);
            _showPopupPublisher.Publish(new ShowPopupDto
            {
                TargetPopUpType = typeof(IEndWaveLoseUIPresenter)
            });
        }

        private void OnCloseRequested()
        {
            if (_isWaitingAdReward)
                return;

            ClosePopupAndOpenMenu();
        }

        private void OnAdRequested()
        {
            if (_isWaitingAdReward || _isReviveGranted)
                return;

            ShowRewardedAdForRevive();
        }

        private void ShowRewardedAdForRevive()
        {
            _isWaitingAdReward = true;
            SubscribeRewardedAdEvents();
            GameAnalytics.ShowRewarded(ReviveCurrentWaveAdId);
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
            if (!_isWaitingAdReward || rewardId != ReviveCurrentWaveAdId)
                return;

            _isReviveGranted = true;
        }

        private void OnRewardedAdClosed()
        {
            if (!_isWaitingAdReward)
                return;

            if (_isReviveGranted)
            {
                ClosePopupAndReviveAfterAdPauseAsync().Forget();
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

        private async UniTaskVoid ClosePopupAndReviveAfterAdPauseAsync()
        {
            if (_isRewardedAdClosing)
                return;

            _isRewardedAdClosing = true;
            UnsubscribeRewardedAdEvents();

            await UniTask.NextFrame();

            _isWaitingAdReward = false;
            _isRewardedAdClosing = false;
            ClosePopupAndRevive();
        }

        private void ClosePopupAndRevive()
        {
            ClosePopup();

            if (_runBattleRuntime.TryReviveCurrentWave())
                return;

            Debug.LogWarning("EndWaveLoseUseCase: current wave revive failed.");
            OpenMainMenu();
        }

        private void ClosePopupAndOpenMenu()
        {
            ClosePopup();
            OpenMainMenu();
        }

        private void OpenMainMenu()
        {
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(ILevelUIPresenter)
            });
            _showPopupPublisher.Publish(new ShowPopupDto
            {
                TargetPopUpType = typeof(IMainMenuUIPresenter)
            });
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
