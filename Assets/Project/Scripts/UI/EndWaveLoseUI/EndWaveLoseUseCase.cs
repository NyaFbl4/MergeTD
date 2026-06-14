using System;
using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
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

        private bool _isShown;

        public EndWaveLoseUseCase(
            BaseHealth baseHealth,
            IEndWaveLoseUIPresenter endWaveLoseUIPresenter,
            IPlayerStatsUseCase playerStatsUseCase,
            IGameManagerService gameManagerService,
            IPublisher<ShowPopupDto> showPopupPublisher,
            IPublisher<HidePopupDto> hidePopupPublisher)
        {
            _baseHealth = baseHealth;
            _endWaveLoseUIPresenter = endWaveLoseUIPresenter;
            _playerStatsUseCase = playerStatsUseCase;
            _gameManagerService = gameManagerService;
            _showPopupPublisher = showPopupPublisher;
            _hidePopupPublisher = hidePopupPublisher;
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

            _isShown = true;
            _endWaveLoseUIPresenter.SetData(_playerStatsUseCase.Wave);
            _showPopupPublisher.Publish(new ShowPopupDto
            {
                TargetPopUpType = typeof(IEndWaveLoseUIPresenter)
            });
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