using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.EndWaveUI
{
    public class EndWaveUIPresenter : LayoutPresenterBase<IEndWaveUIView>, IEndWaveUIPresenter
    {
        private readonly ILocalizationService _localizationService;
        private readonly IAudioManager _audioManager;
        private int _waveNumber;
        private bool _hasData;

        public event Action CloseRequested;
        public event Action AdRequested;

        public EndWaveUIPresenter(ILocalizationService localizationService, IAudioManager audioManager)
        {
            _localizationService = localizationService;
            _audioManager = audioManager;
        }

        public override void Initialize()
        {
            base.Initialize();

            _layoutView.CloseButtonClicked += OnCloseButtonClicked;
            _layoutView.ADButtonClicked += OnAdButtonClicked;
            _localizationService.OnChangeLanguage += OnLanguageChanged;
            RefreshLocalization();
        }

        public void SetData(int waveNumber, IReadOnlyList<WaveRewardConfig> rewards)
        {
            _waveNumber = waveNumber;
            _hasData = true;
            RefreshLocalization();
            _layoutView.SetRewards(rewards);
            _audioManager.PlaySound(ESoundId.EndWave);
        }

        private void OnLanguageChanged(string _) => RefreshLocalization();

        private void RefreshLocalization()
        {
            _layoutView.SetAdButtonText(_localizationService.Get(LocalizationKeys.EndWaveAdButton));
            _layoutView.SetCloseButtonText(_localizationService.Get(LocalizationKeys.EndWaveCloseButton));
            if (_hasData)
            {
                _layoutView.SetTitle(
                    _localizationService
                        .Format(LocalizationKeys.EndWaveTitleFormat, _waveNumber)
                        .ToUpperInvariant());
            }
        }

        private void OnCloseButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            CloseRequested?.Invoke();
        }

        private void OnAdButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            AdRequested?.Invoke();
        }

        public override void Dispose()
        {
            _layoutView.CloseButtonClicked -= OnCloseButtonClicked;
            _layoutView.ADButtonClicked -= OnAdButtonClicked;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;

            base.Dispose();
        }
    }
}
