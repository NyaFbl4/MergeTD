using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;
using Project.Scripts.System.Save;
using Cysharp.Threading.Tasks;
using Project.Scripts.UI.LevelUI;
using Project.Scripts.UI.MainMenuUI;

namespace Project.Scripts.UI.SettingsUI
{
    public class SettingsUIPresenter : LayoutPresenterBase<ISettingsUIView>, ISettingsUIPresenter
    {
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly IPublisher<ShowPopupDto> _showPopupPublisher;
        private readonly IAudioManager _audioManager;
        private readonly ILocalizationService _localizationService;
        private readonly IGameManagerService _gameManagerService;
        private readonly ProgressSaveService _progressSaveService;
        private readonly IWorldService _world;
        private bool _isReturningToMenu;

        public SettingsUIPresenter(
            IPublisher<HidePopupDto> hidePopupPublisher, 
            IAudioManager audioManager, 
            ILocalizationService localizationService,
            ProgressSaveService progressSaveService,
            IWorldService world,
            IGameManagerService gameManagerService,
            IPublisher<ShowPopupDto> showPopupPublisher)
        {
            _hidePopupPublisher = hidePopupPublisher;
            _showPopupPublisher = showPopupPublisher;
            _audioManager = audioManager;
            _localizationService = localizationService;
            _gameManagerService = gameManagerService;
            _progressSaveService = progressSaveService;
            _world = world;
            
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
            _layoutView.ResetProgressButtonClicked += OnResetProgressButtonClicked;
            _layoutView.GoToMenuButtonClicked += OnGoToMenuButtonClicked;
            
            RefreshAudioButtons();
            RefreshLocalizedTexts();
        }
        
        public override async UniTask ActivateAsync()
        {
            _isReturningToMenu = false;
            _gameManagerService.PauseGame();
            RefreshAudioButtons();
            RefreshLocalizedTexts();
            await base.ActivateAsync();
        }

        public override async UniTask DeactivateAsync()
        {
            var shouldResumeGame = !_isReturningToMenu;
            await base.DeactivateAsync();
            if (shouldResumeGame)
                _gameManagerService.ResumeGame();
        }
        
        private void OnResetProgressButtonClicked()
        {
            if (_isReturningToMenu)
                return;

            _isReturningToMenu = true;
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _gameManagerService.FinishGame();
            _progressSaveService.Clear();
            _world.Reset();
            ShowMainMenu();
        }

        private void OnGoToMenuButtonClicked()
        {
            if (_isReturningToMenu)
                return;

            _isReturningToMenu = true;
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _gameManagerService.FinishGame();
            ShowMainMenu();
        }

        private void ShowMainMenu()
        {
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(ISettingsUIPresenter),
            });
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(ILevelUIPresenter),
            });
            _showPopupPublisher.Publish(new ShowPopupDto
            {
                TargetPopUpType = typeof(IMainMenuUIPresenter),
            });
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
            _layoutView.SetResetButtonLabel(_localizationService.Get(LocalizationKeys.SettingsResetProgress));
            _layoutView.SetGoToMenuButtonLabel(_localizationService.Get(LocalizationKeys.SettingsGoToMenu));
        }
        
        public override void Dispose()
        {
            _layoutView.CloseButtonClicked -= OnCloseButtonClicked;
            _layoutView.SoundButtonClicked -= OnSoundButtonClicked;
            _layoutView.MusicButtonClicked -= OnMusicButtonClicked;
            _layoutView.RuButtonClicked -= OnRuLanguageButtonClicked;
            _layoutView.EnButtonClicked -= OnEnLanguageButtonClicked;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;
            _layoutView.ResetProgressButtonClicked -= OnResetProgressButtonClicked;
            _layoutView.GoToMenuButtonClicked -= OnGoToMenuButtonClicked;
            
            base.Dispose();
        }
    }
}
