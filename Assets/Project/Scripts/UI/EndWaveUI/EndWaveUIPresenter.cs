using System;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.EndWaveUI
{
    public class EndWaveUIPresenter : LayoutPresenterBase<IEndWaveUIView>, IEndWaveUIPresenter
    {
        private readonly ILocalizationService _localizationService;
        private readonly IAudioManager _audioManager;

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

            _layoutView.SetRewardLabel(_localizationService.Get(LocalizationKeys.EndWaveRewardLabel));
            _layoutView.SetAdButtonText(_localizationService.Get(LocalizationKeys.EndWaveAdButton));
            _layoutView.SetCloseButtonText(_localizationService.Get(LocalizationKeys.EndWaveCloseButton));
        }

        public void SetData(string title, int rewardCount)
        {
            _layoutView.SetTitle(title);
            _layoutView.SetRewardCount(rewardCount);
            _audioManager.PlaySound(ESoundId.EndWave);
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
