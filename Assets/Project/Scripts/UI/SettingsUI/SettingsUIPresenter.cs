using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;
using Cysharp.Threading.Tasks;

namespace Project.Scripts.UI.SettingsUI
{
    public class SettingsUIPresenter : LayoutPresenterBase<ISettingsUIView>, ISettingsUIPresenter
    {
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly IAudioManager _audioManager;
        private readonly ILocalizationService _localizationService;
        private readonly IGameManagerService _gameManagerService;

        public SettingsUIPresenter(
            IPublisher<HidePopupDto> hidePopupPublisher, 
            IAudioManager audioManager, 
            ILocalizationService localizationService, 
            IGameManagerService gameManagerService)
        {
            _hidePopupPublisher = hidePopupPublisher;
            _audioManager = audioManager;
            _localizationService = localizationService;
            _gameManagerService = gameManagerService;
            
            _localizationService.OnChangeLanguage += OnLanguageChanged;
        }

        public override void Initialize()
        {
            base.Initialize();
            
            _layoutView.CloseButtonClicked += OnCloseButtonClicked;
            _layoutView.SoundButtonClicked += OnSoundButtonClicked;
            _layoutView.MusicButtonClicked += OnMusicButtonClicked;
            _layoutView.RuButtonClicked += OnRuLanguageButtonClicked;
            _layoutView.EnButtonClicked += OnEnLanguageButtonClicked;
            
            RefreshAudioButtons();
            RefreshLocalizedTexts();
        }
        
        public override async UniTask ActivateAsync()
        {
            _gameManagerService.PauseGame();
            RefreshAudioButtons();
            RefreshLocalizedTexts();
            await base.ActivateAsync();
        }

        public override async UniTask DeactivateAsync()
        {
            await base.DeactivateAsync();
            _gameManagerService.ResumeGame();
        }
        
        private void OnLanguageChanged(string _) => RefreshLocalizedTexts();
        
        private void OnCloseButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(ISettingsUIPresenter),
            });
        }

        private void OnMusicButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            var nextState = !_audioManager.IsMusicEnabled;
            _audioManager.SetMusicEnabled(nextState);
            _layoutView.SetMusicEnabled(nextState);
        }
        
        private void OnSoundButtonClicked()
        {
            var wasEnabled = _audioManager.IsSoundEnabled;

            if (wasEnabled)
                _audioManager.PlaySound(ESoundId.UiButtonClick);

            var nextState = !wasEnabled;
            _audioManager.SetSoundEnabled(nextState);
            _layoutView.SetSoundEnabled(nextState);

            if (nextState)
                _audioManager.PlaySound(ESoundId.UiButtonClick);
        }
        
        private void OnRuLanguageButtonClicked()
        {
             _audioManager.PlaySound(ESoundId.UiButtonClick);
             if (_localizationService.SetLanguage("ru"))     
                RefreshLocalizedTexts();
        }
        
        private void OnEnLanguageButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            if (_localizationService.SetLanguage("en"))
                RefreshLocalizedTexts();
        }
        
        private void RefreshAudioButtons()
        {
            _layoutView.SetMusicEnabled(_audioManager.IsMusicEnabled);
            _layoutView.SetSoundEnabled(_audioManager.IsSoundEnabled);
        }
        
        private void RefreshLocalizedTexts()
        {
            _layoutView.SetTitle(_localizationService.Get(LocalizationKeys.SettingsTitle));
            _layoutView.SetMusicLabel(_localizationService.Get(LocalizationKeys.SettingsMusic));
            _layoutView.SetSoundLabel(_localizationService.Get(LocalizationKeys.SettingsSound));
            _layoutView.SetLanguageLabel(_localizationService.Get(LocalizationKeys.SettingsLanguage));
        }
        
        public override void Dispose()
        {
            _layoutView.CloseButtonClicked -= OnCloseButtonClicked;
            _layoutView.SoundButtonClicked -= OnSoundButtonClicked;
            _layoutView.MusicButtonClicked -= OnMusicButtonClicked;
            _layoutView.RuButtonClicked -= OnRuLanguageButtonClicked;
            _layoutView.EnButtonClicked -= OnEnLanguageButtonClicked;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;
            
            base.Dispose();
        }
    }
}