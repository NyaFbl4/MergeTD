using Project.Scripts.Systems.UI;
using Project.Scripts.Configs;
using System.Collections.Generic;
using Project.Scripts.System.Save;
using Project.Scripts.UI.ArmyUI;
using Project.Scripts.UI.SpellsUI;

using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.UI.InBattleUI;
using Project.Scripts.UI.PurchaseWindowUI;
using Project.Scripts.Systems.UI.Dtos;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Reward;
using Project.Scripts.Gameplay.Quests;
using Project.Scripts.System.Localization;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.UpgradeItem;
using Project.Scripts.System.UseCases;
using Project.Scripts.UI.ShopUI;

namespace Project.Scripts.UI.MainMenuUI
{
    public class MainMenuUIPresenter : LayoutPresenterBase<IMainMenuUIView>, IMainMenuUIPresenter
    {
        private readonly MainMenuUIUseCase _menuUIUseCase;
        private readonly UIElements _uiElements;
        private readonly IWorldService _world;
        private readonly IArmyUIUseCase _armyUIUseCase;
        private readonly ISpellsUIUseCase _spellsUIUseCase;
        private readonly IAudioManager _audioManager;
        private readonly IRunSelectionService _runSelection;
        private readonly IGameManagerService _gameManagerService;
        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly IPublisher<ShowPopupDto> _showPopupPublisher;
        private readonly DailyRewardService _dailyRewardService;
        private readonly QuestService _questService;
        private readonly ILocalizationService _localizationService;
        private readonly IUpgradeConfigProvider _upgradeConfigs;
        private readonly BaseUpgradeConfigProvider _baseUpgradeConfigs;
        private readonly IPlayerStatsUseCase _playerStats;
        private readonly BaseHealth _baseHealth;
        private IInBattleUIPresenter _inBattlePresenter;
        private IArmyUIPresenter _armyPresenter;
        private PurchaseWindowPresenter _purchaseWindowPresenter;
        private SpellsPanelUIPresenter _spellsPanelPresenter;
        private List<IBaseUpgradeItem> _baseUpgradeItems;

        public MainMenuUIPresenter(
            MainMenuUIUseCase menuUIUseCase,
            UIElements uiElements,
            IWorldService world,
            IArmyUIUseCase armyUIUseCase,
            ISpellsUIUseCase spellsUIUseCase,
            IAudioManager audioManager,
            IRunSelectionService runSelection,
            IGameManagerService gameManagerService,
            IPublisher<HidePopupDto> hidePopupPublisher,
            IPublisher<ShowPopupDto> showPopupPublisher,
            DailyRewardService dailyRewardService,
            QuestService questService,
            ILocalizationService localizationService,
            IUpgradeConfigProvider upgradeConfigs,
            BaseUpgradeConfigProvider baseUpgradeConfigs,
            IPlayerStatsUseCase playerStats,
            BaseHealth baseHealth)
        {
            _menuUIUseCase = menuUIUseCase;
            _uiElements = uiElements;
            _world = world;
            _armyUIUseCase = armyUIUseCase;
            _spellsUIUseCase = spellsUIUseCase;
            _audioManager = audioManager;
            _runSelection = runSelection;
            _gameManagerService = gameManagerService;
            _hidePopupPublisher = hidePopupPublisher;
            _showPopupPublisher = showPopupPublisher;
            _dailyRewardService = dailyRewardService;
            _questService = questService;
            _localizationService = localizationService;
            _upgradeConfigs = upgradeConfigs;
            _baseUpgradeConfigs = baseUpgradeConfigs;
            _playerStats = playerStats;
            _baseHealth = baseHealth;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            _layoutView.InitializeArmy(_uiElements);
            _armyPresenter = new ArmyUIPresenter(
                _layoutView.ArmyView,
                _world,
                _armyUIUseCase,
                _audioManager);
            _armyPresenter.Initialize();

            _layoutView.InitializePurchaseWindow(_uiElements);
            _purchaseWindowPresenter = new PurchaseWindowPresenter(
                _layoutView.PurchaseWindowView,
                _world,
                _armyUIUseCase,
                _audioManager,
                _localizationService);
            _purchaseWindowPresenter.Initialize();
            _layoutView.ArmyView.SetTowersButtonClicked += OnSetTowersButtonClicked;
            _layoutView.ArmyView.SetSpellsButtonClicked += OnSetSpellsButtonClicked;

            _layoutView.InitializeSpells(_uiElements, _localizationService);
            _spellsPanelPresenter = new SpellsPanelUIPresenter(
                _layoutView.SpellsPanelView,
                _spellsUIUseCase,
                _audioManager,
                _localizationService);
            _spellsPanelPresenter.Initialize();

            _baseUpgradeItems = new List<IBaseUpgradeItem>
            {
                new BaseHealthUpgradeItem(_upgradeConfigs.HealthUpgradeConfig, _playerStats, _baseHealth),
                new BaseRepairUpgradeItem(_baseUpgradeConfigs.Repair, _playerStats),
                new ManaRestorationUpgradeItem(_baseUpgradeConfigs.ManaRestoration, _playerStats),
                new MaxEnergyUpgradeItem(_baseUpgradeConfigs.MaxEnergy, _playerStats, _world)
            };

            _inBattlePresenter = new InBattleUIPresenter(
                _layoutView.InBattleView,
                _runSelection,
                _gameManagerService,
                _hidePopupPublisher,
                _showPopupPublisher,
                _dailyRewardService,
                _questService,
                _localizationService,
                _audioManager);
            _inBattlePresenter.Initialize();
            
            _menuUIUseCase.GoldChanged += OnGoldChanged;
            _menuUIUseCase.GemsChanged += OnGemsChanged;
            _menuUIUseCase.ActiveSectionChanged += OnActiveSectionChanged;
            _layoutView.SectionClicked += OnSectionClicked;
            _playerStats.OnGoldChanged += OnPlayerGoldChanged;
            _playerStats.UpgradesChanged += OnUpgradesChanged;
            _localizationService.OnChangeLanguage += OnLanguageChanged;

            _layoutView.SetGoldCount(_menuUIUseCase.Gold);
            _layoutView.SetDiamondCount(_menuUIUseCase.Gems);
            _layoutView.SetActiveSection(_menuUIUseCase.ActiveSection);
            RefreshBaseUpgrades();
        }
        
        private void OnGoldChanged(int value)
        {
            _layoutView.SetGoldCount(value);
        }

        private void OnGemsChanged(int value)
        {
            _layoutView.SetDiamondCount(value);
        }

        private void OnActiveSectionChanged(MainMenuSection section)
        {
            _layoutView.SetActiveSection(section);
            _purchaseWindowPresenter.Hide();
        }

        private void OnSectionClicked(MainMenuSection section)
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _menuUIUseCase.SelectSection(section);
            _purchaseWindowPresenter.Hide();
        }

        private void OnSetTowersButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _purchaseWindowPresenter.Show();
        }

        private void OnSetSpellsButtonClicked() => OnSectionClicked(MainMenuSection.Spells);

        private void OnPlayerGoldChanged(int _)
        {
            RefreshBaseUpgrades();
        }

        private void OnUpgradesChanged()
        {
            RefreshBaseUpgrades();
        }

        private void OnLanguageChanged(string _)
        {
            RefreshBaseUpgrades();
        }

        private void RefreshBaseUpgrades()
        {
            _layoutView.SetBaseUpgradesTitle(
                _localizationService.Get(LocalizationKeys.BaseUpgradesTitle));
            _layoutView.ClearBaseUpgrades();

            foreach (var item in _baseUpgradeItems)
            {
                _layoutView.AddBaseUpgrade(
                    item,
                    !item.IsMaxLevel && _playerStats.CanSpend(item.NextPrice),
                    () =>
                    {
                        if (item.TryUpgrade())
                            _audioManager.PlaySound(ESoundId.AddUpgrade);
                    },
                    _localizationService);
            }
        }

        public override void Dispose()
        {
            _menuUIUseCase.GoldChanged -= OnGoldChanged;
            _menuUIUseCase.GemsChanged -= OnGemsChanged;
            _menuUIUseCase.ActiveSectionChanged -= OnActiveSectionChanged;
            _layoutView.SectionClicked -= OnSectionClicked;
            _layoutView.ArmyView.SetTowersButtonClicked -= OnSetTowersButtonClicked;
            _layoutView.ArmyView.SetSpellsButtonClicked -= OnSetSpellsButtonClicked;
            _playerStats.OnGoldChanged -= OnPlayerGoldChanged;
            _playerStats.UpgradesChanged -= OnUpgradesChanged;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;
            _inBattlePresenter.Dispose();
            _purchaseWindowPresenter.Dispose();
            _armyPresenter.Dispose();
            _spellsPanelPresenter.Dispose();

            base.Dispose();
        }
    }
}
