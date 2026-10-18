using System;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.EndWaveLoseUI
{
    public class EndWaveLoseUIPresenter : LayoutPresenterBase<IEndWaveLoseUIView>, IEndWaveLoseUIPresenter
    {
        private readonly ILocalizationService _localizationService;
        private readonly IAudioManager _audioManager;
        private int _waveNumber;
        private bool _hasData;

        public event Action CloseRequested;
        public event Action AdRequested;

        public EndWaveLoseUIPresenter(ILocalizationService localizationService, IAudioManager audioManager)
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

        public void SetData(int waveNumber)
        {
            _waveNumber = waveNumber;
            _hasData = true;
            RefreshLocalization();
        }

        private void OnLanguageChanged(string _) => RefreshLocalization();

        private void RefreshLocalization()
        {
            _layoutView.SetCloseButtonText(
                _localizationService.Get(LocalizationKeys.EndWaveLoseCloseButton));
            _layoutView.SetAdButtonText(
                _localizationService.Get(LocalizationKeys.EndWaveLoseAdButton));
            if (_hasData)
            {
                _layoutView.SetTitle(
                    _localizationService
                        .Format(LocalizationKeys.EndWaveLoseDescriptionFormat, _waveNumber)
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
