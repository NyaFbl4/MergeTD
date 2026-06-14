using System;
using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Systems;
using Project.Scripts.System.Localization;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI.Dtos;
using VContainer.Unity;

namespace Project.Scripts.UI.EndWaveUI
{
    public class EndWaveUseCase : IInitializable, IDisposable
    {
        private readonly BattlefieldRuntime _battlefieldRuntime;
        private readonly IEndWaveUIPresenter _endWaveUIPresenter;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;
        private readonly IPublisher<ShowPopupDto> _showPopupPublisher;
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly IGameManagerService _gameManagerService;
        private readonly ILocalizationService _localizationService;
        private readonly ProgressCheckpointUseCase _progressCheckpointUseCase;

        private bool _isLastWave;

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
            ClosePopupAndContinue();
        }

        private void OnAdRequested()
        {
            // Пока можно сделать так же, как обычное закрытие.
            // Потом сюда добавите ad flow и x2 reward.
            ClosePopupAndContinue();
        }

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
        }        
    }
}