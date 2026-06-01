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
        }
        
        public override async UniTask ActivateAsync()
        {
            _gameManagerService.PauseGame();
            RefreshAudioButtons();
            await base.ActivateAsync();
        }

        public override async UniTask DeactivateAsync()
        {
            await base.DeactivateAsync();
            _gameManagerService.ResumeGame();
        }
        
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
            _localizationService.SetLanguage("ru");           
        }
        
        private void OnEnLanguageButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _localizationService.SetLanguage("en");
        }
        
        private void RefreshAudioButtons()
        {
            _layoutView.SetMusicEnabled(_audioManager.IsMusicEnabled);
            _layoutView.SetSoundEnabled(_audioManager.IsSoundEnabled);
        }
        
        public override void Dispose()
        {
            _layoutView.CloseButtonClicked -= OnCloseButtonClicked;
            _layoutView.SoundButtonClicked -= OnSoundButtonClicked;
            _layoutView.MusicButtonClicked -= OnMusicButtonClicked;
            _layoutView.RuButtonClicked -= OnRuLanguageButtonClicked;
            _layoutView.EnButtonClicked -= OnEnLanguageButtonClicked;
            
            base.Dispose();
        }
    }
}