using System;
using MessagePipe;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI.Dtos;
using VContainer.Unity;

namespace Project.Scripts.UI.EndWaveLoseUI
{
    public class EndWaveLoseUseCase : IInitializable, IDisposable
    {
        private readonly BaseHealth _baseHealth;
        private readonly IEndWaveLoseUIPresenter _endWaveLoseUIPresenter;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;
        private readonly IGameManagerService _gameManagerService;
        private readonly IPublisher<ShowPopupDto> _showPopupPublisher;
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly LevelConfig _levelConfig;
        private readonly ProgressCheckpointUseCase _progressCheckpointUseCase;

        private bool _isShown;

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
            ClosePopup();
            _gameManagerService.StartGame();
        }

        private void OnAdRequested()
        {
            ClosePopup();
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
        }
    }
}