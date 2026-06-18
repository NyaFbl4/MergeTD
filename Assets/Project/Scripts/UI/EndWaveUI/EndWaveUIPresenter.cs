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
        public event Action ReviewRequested;

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
            _layoutView.ReviewButtonClicked += OnReviewButtonClicked;

            _layoutView.SetRewardLabel(_localizationService.Get(LocalizationKeys.EndWaveRewardLabel));
            _layoutView.SetAdButtonText(_localizationService.Get(LocalizationKeys.EndWaveAdButton));
            _layoutView.SetCloseButtonText(_localizationService.Get(LocalizationKeys.EndWaveCloseButton));
            _layoutView.ShowAdButtonOnly();
        }

        public void SetData(string title, int rewardCount)
        {
            _layoutView.SetTitle(title);
            _layoutView.SetRewardCount(rewardCount);
            _audioManager.PlaySound(ESoundId.EndWave);
        }
        
        public void SetAdButtonAdMode()
        {
            _layoutView.SetAdButtonText(_localizationService.Get(LocalizationKeys.EndWaveAdButton));
            _layoutView.ShowAdButtonOnly();
        }

        public void SetAdButtonReviewMode(int rewardCount)
        {
            _layoutView.SetReviewButtonText(
                _localizationService.Format(LocalizationKeys.EndWaveReviewButtonFormat, rewardCount));
            _layoutView.ShowReviewButtonOnly();
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

        private void OnReviewButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            ReviewRequested?.Invoke();
        }

        public override void Dispose()
        {
            _layoutView.CloseButtonClicked -= OnCloseButtonClicked;
            _layoutView.ADButtonClicked -= OnAdButtonClicked;
            _layoutView.ReviewButtonClicked -= OnReviewButtonClicked;

            base.Dispose();
        }
    }
}