using Project.Scripts.Systems.UI;
using Project.Scripts.Configs;
using Project.Scripts.System.Save;
using Project.Scripts.UI.ArmyUI;

using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.UI.InBattleUI;
using Project.Scripts.Systems.UI.Dtos;

namespace Project.Scripts.UI.MainMenuUI
{
    public class MainMenuUIPresenter : LayoutPresenterBase<IMainMenuUIView>, IMainMenuUIPresenter
    {
        private readonly MainMenuUIUseCase _menuUIUseCase;
        private readonly UIElements _uiElements;
        private readonly IWorldService _world;
        private readonly IRunSelectionService _runSelection;
        private readonly IGameManagerService _gameManagerService;
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private IInBattleUIPresenter _inBattlePresenter;
        private IArmyUIPresenter _armyPresenter;

        public MainMenuUIPresenter(
            MainMenuUIUseCase menuUIUseCase,
            UIElements uiElements,
            IWorldService world,
            IRunSelectionService runSelection,
            IGameManagerService gameManagerService,
            IPublisher<HidePopupDto> hidePopupPublisher)
        {
            _menuUIUseCase = menuUIUseCase;
            _uiElements = uiElements;
            _world = world;
            _runSelection = runSelection;
            _gameManagerService = gameManagerService;
            _hidePopupPublisher = hidePopupPublisher;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            _layoutView.InitializeArmy(_uiElements);
            _armyPresenter = new ArmyUIPresenter(_layoutView.ArmyView, _world);
            _armyPresenter.Initialize();

            _inBattlePresenter = new InBattleUIPresenter(
                _layoutView.InBattleView,
                _runSelection,
                _gameManagerService,
                _hidePopupPublisher);
            _inBattlePresenter.Initialize();
            
            _menuUIUseCase.GoldChanged += OnGoldChanged;
            _menuUIUseCase.GemsChanged += OnGemsChanged;
            _menuUIUseCase.ActiveSectionChanged += OnActiveSectionChanged;
            _layoutView.SectionClicked += OnSectionClicked;

            _layoutView.SetGoldCount(_menuUIUseCase.Gold);
            _layoutView.SetDiamondCount(_menuUIUseCase.Gems);
            _layoutView.SetActiveSection(_menuUIUseCase.ActiveSection);
        }
        
        private void OnGoldChanged(int value)
        {
            _layoutView.SetGoldCount(value);
        }

        private void OnGemsChanged(int value)
        {
            _layoutView.SetDiamondCount(value);
        }

        private void OnActiveSectionChanged(MainMenuSection section) => _layoutView.SetActiveSection(section);
        private void OnSectionClicked(MainMenuSection section) => _menuUIUseCase.SelectSection(section);

        public override void Dispose()
        {
            _menuUIUseCase.GoldChanged -= OnGoldChanged;
            _menuUIUseCase.GemsChanged -= OnGemsChanged;
            _menuUIUseCase.ActiveSectionChanged -= OnActiveSectionChanged;
            _layoutView.SectionClicked -= OnSectionClicked;
            _inBattlePresenter.Dispose();
            _armyPresenter.Dispose();

            base.Dispose();
        }
    }
}
