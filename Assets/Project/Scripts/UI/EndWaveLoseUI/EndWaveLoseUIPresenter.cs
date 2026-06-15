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

            _layoutView.SetCloseButtonText(_localizationService.Get(LocalizationKeys.EndWaveLoseCloseButton));
        }

        public void SetData(int waveNumber, int rewardCount)
        {
            _layoutView.SetTitle(_localizationService.Get(LocalizationKeys.EndWaveLoseTitle));
            _layoutView.SetDescription(_localizationService.Format(LocalizationKeys.EndWaveLoseDescriptionFormat, waveNumber));
            _layoutView.SetAdButtonText(_localizationService.Format(LocalizationKeys.EndWaveLoseAdButton, rewardCount));
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

            base.Dispose();
        }
    }
}