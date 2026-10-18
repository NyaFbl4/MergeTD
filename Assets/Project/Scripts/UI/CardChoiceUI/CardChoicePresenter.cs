using System;
using Project.Scripts.Gameplay.Run.Cards;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;

namespace Project.Scripts.UI.CardChoiceUI
{
    public sealed class CardChoicePresenter : IDisposable
    {
        private const int MaxCards = 3;

        private readonly ICardChoiceView _view;
        private readonly RunCardChoiceUseCase _useCase;
        private readonly IAudioManager _audioManager;
        private readonly ILocalizationService _localizationService;

        public CardChoicePresenter(
            ICardChoiceView view,
            RunCardChoiceUseCase useCase,
            IAudioManager audioManager,
            ILocalizationService localizationService)
        {
            _view = view;
            _useCase = useCase;
            _audioManager = audioManager;
            _localizationService = localizationService;
        }

        public void Initialize()
        {
            _view.ChoiceRequested += OnChoiceRequested;
            _view.RerollRequested += OnRerollRequested;
            _useCase.StateChanged += Refresh;
            _localizationService.OnChangeLanguage += OnLanguageChanged;
            Refresh();
        }

        public void Dispose()
        {
            _view.ChoiceRequested -= OnChoiceRequested;
            _view.RerollRequested -= OnRerollRequested;
            _useCase.StateChanged -= Refresh;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;
        }

        private void OnChoiceRequested(int index)
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _useCase.TrySelect(index);
        }

        private void OnRerollRequested()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _useCase.TryReroll();
        }

        private void Refresh()
        {
            _view.SetHeader(_localizationService.Get(_useCase.Title));
            for (var i = 0; i < MaxCards; i++)
            {
                var visible = i < _useCase.Choices.Count;
                _view.SetCard(
                    i,
                    visible ? _useCase.Choices[i] : null,
                    visible,
                    _localizationService);
            }

            _view.SetReroll(
                _localizationService.Get(_useCase.RerollButtonText),
                _useCase.CanReroll);
            _view.SetVisible(_useCase.IsActive);
        }

        private void OnLanguageChanged(string _) => Refresh();
    }
}
