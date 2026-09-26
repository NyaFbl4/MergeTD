using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.Systems.UI.Dtos;
using Project.Scripts.UI.MainMenuUI;

namespace Project.Scripts.UI.InBattleUI
{
    public sealed class InBattleUIPresenter : IInBattleUIPresenter
    {
        private readonly IInBattleUIView _view;
        private readonly IRunSelectionService _runSelection;
        private readonly IGameManagerService _gameManagerService;
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;

        public InBattleUIPresenter(
            IInBattleUIView view,
            IRunSelectionService runSelection,
            IGameManagerService gameManagerService,
            IPublisher<HidePopupDto> hidePopupPublisher)
        {
            _view = view;
            _runSelection = runSelection;
            _gameManagerService = gameManagerService;
            _hidePopupPublisher = hidePopupPublisher;
        }

        public void Initialize()
        {
            _runSelection.SelectionChanged += OnRunChanged;
            _view.PreviousRunClicked += OnPreviousRunClicked;
            _view.NextRunClicked += OnNextRunClicked;
            _view.PlayClicked += OnPlayClicked;

            RefreshRun();
        }

        public void Dispose()
        {
            _runSelection.SelectionChanged -= OnRunChanged;
            _view.PreviousRunClicked -= OnPreviousRunClicked;
            _view.NextRunClicked -= OnNextRunClicked;
            _view.PlayClicked -= OnPlayClicked;
        }

        private void OnRunChanged(RunConfig _) => RefreshRun();
        private void OnPreviousRunClicked() => _runSelection.SelectPrevious();
        private void OnNextRunClicked() => _runSelection.SelectNext();

        private void OnPlayClicked()
        {
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(IMainMenuUIPresenter)
            });
            _gameManagerService.StartGame();
        }

        private void RefreshRun()
        {
            var run = _runSelection.SelectedRun;
            _view.SetRun(run.DisplayName, run.Icon, _runSelection.Count > 1);
        }
    }
}
