using Project.Scripts.Systems.UI;

using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.Systems.UI.Dtos;

namespace Project.Scripts.UI.MainMenuUI
{
    public class MainMenuUIPresenter : LayoutPresenterBase<IMainMenuUIView>, IMainMenuUIPresenter
    {
        private readonly MainMenuUIUseCase _menuUIUseCase;
        private readonly IGameManagerService _gameManagerService;
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;

        public MainMenuUIPresenter(
            MainMenuUIUseCase menuUIUseCase,
            IGameManagerService gameManagerService,
            IPublisher<HidePopupDto> hidePopupPublisher)
        {
            _menuUIUseCase = menuUIUseCase;
            _gameManagerService = gameManagerService;
            _hidePopupPublisher = hidePopupPublisher;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            _menuUIUseCase.GoldChanged += OnGoldChanged;
            _menuUIUseCase.GemsChanged += OnGemsChanged;
            _menuUIUseCase.RunChanged += OnRunChanged;
            _menuUIUseCase.ActiveSectionChanged += OnActiveSectionChanged;
            _layoutView.PreviousRunClicked += OnPreviousRunClicked;
            _layoutView.NextRunClicked += OnNextRunClicked;
            _layoutView.PlayClicked += OnPlayClicked;
            _layoutView.SectionClicked += OnSectionClicked;

            _layoutView.SetGoldCount(_menuUIUseCase.Gold);
            _layoutView.SetDiamondCount(_menuUIUseCase.Gems);
            _layoutView.SetActiveSection(_menuUIUseCase.ActiveSection);
            RefreshRun();
        }
        
        private void OnGoldChanged(int value)
        {
            _layoutView.SetGoldCount(value);
        }

        private void OnGemsChanged(int value)
        {
            _layoutView.SetDiamondCount(value);
        }

        private void OnRunChanged(RunConfig _) => RefreshRun();
        private void OnActiveSectionChanged(MainMenuSection section) => _layoutView.SetActiveSection(section);
        private void OnPreviousRunClicked() => _menuUIUseCase.SelectPreviousRun();
        private void OnNextRunClicked() => _menuUIUseCase.SelectNextRun();
        private void OnSectionClicked(MainMenuSection section) => _menuUIUseCase.SelectSection(section);

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
            var run = _menuUIUseCase.SelectedRun;
            _layoutView.SetRun(run.DisplayName, run.Icon, _menuUIUseCase.RunCount > 1);
        }

        public override void Dispose()
        {
            _menuUIUseCase.GoldChanged -= OnGoldChanged;
            _menuUIUseCase.GemsChanged -= OnGemsChanged;
            _menuUIUseCase.RunChanged -= OnRunChanged;
            _menuUIUseCase.ActiveSectionChanged -= OnActiveSectionChanged;
            _layoutView.PreviousRunClicked -= OnPreviousRunClicked;
            _layoutView.NextRunClicked -= OnNextRunClicked;
            _layoutView.PlayClicked -= OnPlayClicked;
            _layoutView.SectionClicked -= OnSectionClicked;

            base.Dispose();
        }
    }
}
