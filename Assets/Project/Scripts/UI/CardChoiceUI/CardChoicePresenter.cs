using System;
using Project.Scripts.Gameplay.Run.Cards;

namespace Project.Scripts.UI.CardChoiceUI
{
    public sealed class CardChoicePresenter : IDisposable
    {
        private const int MaxCards = 3;

        private readonly ICardChoiceView _view;
        private readonly RunCardChoiceUseCase _useCase;

        public CardChoicePresenter(ICardChoiceView view, RunCardChoiceUseCase useCase)
        {
            _view = view;
            _useCase = useCase;
        }

        public void Initialize()
        {
            _view.ChoiceRequested += OnChoiceRequested;
            _view.RerollRequested += OnRerollRequested;
            _useCase.StateChanged += Refresh;
            Refresh();
        }

        public void Dispose()
        {
            _view.ChoiceRequested -= OnChoiceRequested;
            _view.RerollRequested -= OnRerollRequested;
            _useCase.StateChanged -= Refresh;
        }

        private void OnChoiceRequested(int index) => _useCase.TrySelect(index);
        private void OnRerollRequested() => _useCase.TryReroll();

        private void Refresh()
        {
            _view.SetHeader(_useCase.Title);
            for (var i = 0; i < MaxCards; i++)
            {
                var visible = i < _useCase.Choices.Count;
                _view.SetCard(i, visible ? _useCase.Choices[i] : null, visible);
            }

            _view.SetReroll(_useCase.RerollButtonText, _useCase.CanReroll);
            _view.SetVisible(_useCase.IsActive);
        }
    }
}
