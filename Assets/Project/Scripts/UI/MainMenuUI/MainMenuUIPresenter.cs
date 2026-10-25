using Project.Scripts.System.Analytics;
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
using Project.Scripts.UI.TowerSlotUnlockUI;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Systems.UI.Dtos;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Reward;
using Project.Scripts.Gameplay.Quests;
using Project.Scripts.System.Localization;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.UpgradeItem;
using Project.Scripts.System.UseCases;
using Project.Scripts.UI.ShopUI;
using Project.Scripts.UI.SettingsUI;
using Project.Scripts.System.Shop;
using VContainer.Unity;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

namespace Project.Scripts.UI.MainMenuUI
{
    public class MainMenuUIPresenter : LayoutPresenterBase<IMainMenuUIView>, IMainMenuUIPresenter, ITickable
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
        private readonly ShopOfferService _shopOfferService;
        private readonly PlayerEnergyService _playerEnergy;
        private float _nextEnergyRefreshAt;
        private IInBattleUIPresenter _inBattlePresenter;
        private IArmyUIPresenter _armyPresenter;
        private PurchaseWindowPresenter _purchaseWindowPresenter;
        private TowerSlotUnlockPresenter _slotUnlockPresenter;
        private TowerSlotUnlockConfig _slotUnlockConfig;
        private SpellsPanelUIPresenter _spellsPanelPresenter;
        private ShopContentPresenter _shopPresenter;
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
            BaseHealth baseHealth,
            ShopOfferService shopOfferService,
            PlayerEnergyService playerEnergy)
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
            _shopOfferService = shopOfferService;
            _playerEnergy = playerEnergy;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            _layoutView.InitializeShop(_upgradeConfigs, _uiElements);
            _shopPresenter = new ShopContentPresenter(
                _layoutView.ShopView,
                _localizationService,
                _playerStats,
                _baseHealth,
                _audioManager,
                _shopOfferService,
                _uiElements,
                () => _menuUIUseCase.ActiveSection == MainMenuSection.Shop);
            _shopPresenter.Initialize();

            _layoutView.InitializeArmy(_uiElements);
            _armyPresenter = new ArmyUIPresenter(
                _layoutView.ArmyView,
                _world,
                _armyUIUseCase,
                _audioManager);
            _armyPresenter.Initialize();

            _layoutView.InitializeTowerSlotUnlock(_uiElements);
            _slotUnlockConfig = TowerSlotUnlockConfig.LoadFromResources();
            _slotUnlockPresenter = new TowerSlotUnlockPresenter(
                _layoutView.TowerSlotUnlockView, _world, _localizationService, _audioManager,
                () => IsActive && _menuUIUseCase.ActiveSection == MainMenuSection.Army);
            _layoutView.ArmyView.LockedSlotClicked += OnLockedSlotClicked;
            _world.TowerSlotsChanged += RefreshSlotUnlockHints;
            RefreshSlotUnlockHints();

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
                _localizationService,
                () => _menuUIUseCase.SelectSection(MainMenuSection.Shop));
            _spellsPanelPresenter.Initialize();

            _baseUpgradeItems = new List<IBaseUpgradeItem>
            {
                new BaseHealthUpgradeItem(_upgradeConfigs.HealthUpgradeConfig, _playerStats, _baseHealth),
                new BaseRepairUpgradeItem(_baseUpgradeConfigs.Repair, _playerStats),
                new ManaRestorationUpgradeItem(_baseUpgradeConfigs.ManaRestoration, _playerStats),
                new MaxEnergyUpgradeItem(_baseUpgradeConfigs.MaxEnergy, _playerStats, _world),
                new StartEnergyUpgradeItem(_baseUpgradeConfigs.StartEnergy, _playerStats)
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
                _audioManager,
                _playerEnergy);
            _inBattlePresenter.Initialize();
            
            _menuUIUseCase.GoldChanged += OnGoldChanged;
            _menuUIUseCase.GemsChanged += OnGemsChanged;
            _menuUIUseCase.ActiveSectionChanged += OnActiveSectionChanged;
            _layoutView.SectionClicked += OnSectionClicked;
            _layoutView.SettingsButtonClicked += OnSettingsButtonClicked;
            _playerStats.OnGoldChanged += OnPlayerGoldChanged;
            _playerStats.UpgradesChanged += OnUpgradesChanged;
            _localizationService.OnChangeLanguage += OnLanguageChanged;

            _layoutView.SetGoldCount(_menuUIUseCase.Gold);
            _layoutView.SetDiamondCount(_menuUIUseCase.Gems);
            RefreshPlayerEnergy();
            _layoutView.SetActiveSection(_menuUIUseCase.ActiveSection);
            RefreshBaseUpgrades();
        }
        
        private void OnLockedSlotClicked(int index)
        {
            _purchaseWindowPresenter.Hide();
            _slotUnlockPresenter.Show(TowerSlotGrid.GetSlotId(index));
        }

        private void RefreshSlotUnlockHints()
        {
            for (var i = 10; i < TowerSlotGrid.SlotCount; i++)
            {
                var id = TowerSlotGrid.GetSlotId(i);
                var unlocked = _world.IsTowerSlotUnlocked(id);
                var next = TowerSlotUnlockConfig.IsNextLockedSlot(_world, id);
                var hint = unlocked ? string.Empty : next
                    ? _localizationService.Format("slots.unlock", _slotUnlockConfig.GetPrice(id))
                    : _localizationService.Get("slots.previous_required");
                _layoutView.ArmyView.SetSlotUnlockInfo(i, next, hint);
            }
        }

        public override UniTask DeactivateAsync()
        {
            _slotUnlockPresenter.Hide();
            return base.DeactivateAsync();
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
            _slotUnlockPresenter.Hide();
            _layoutView.SetActiveSection(section);
            _purchaseWindowPresenter.Hide();
            if (section == MainMenuSection.Shop)
                _shopPresenter.Refresh();
        }

        public override async UniTask ActivateAsync()
        {
            _inBattlePresenter.OnMenuShown();
            RefreshPlayerEnergy();
            await base.ActivateAsync();
            GameAnalytics.EnterMenu();
        }

        public void Tick()
        {
            _shopPresenter.Tick();
            if (Time.unscaledTime < _nextEnergyRefreshAt)
                return;

            _nextEnergyRefreshAt = Time.unscaledTime + 1f;
            RefreshPlayerEnergy();
        }

        private void RefreshPlayerEnergy()
        {
            var state = _playerEnergy.GetState();
            var countdown = string.Empty;
            if (state.Current < state.Max)
            {
                if (state.IsTimeAvailable)
                {
                    var seconds = (long)Math.Ceiling(state.TimeUntilNextRestore.TotalSeconds);
                    countdown = $"{seconds / 3600:00}:{seconds % 3600 / 60:00}:{seconds % 60:00}";
                }
                else
                {
                    countdown = "--:--:--";
                }
            }

            _layoutView.SetPlayerEnergy(state.Current, state.Max, countdown);
            _layoutView.InBattleView.SetPlayButtonEnabled(state.Current > 0);
        }

        private void OnSectionClicked(MainMenuSection section)
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _menuUIUseCase.SelectSection(section);
            _purchaseWindowPresenter.Hide();
        }

        private void OnSetTowersButtonClicked()
        {
            _slotUnlockPresenter.Hide();
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _purchaseWindowPresenter.Show();
        }

        private void OnSetSpellsButtonClicked() => OnSectionClicked(MainMenuSection.Spells);

        private void OnSettingsButtonClicked()
        {
            _slotUnlockPresenter.Hide();
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _showPopupPublisher.Publish(new ShowPopupDto
            {
                TargetPopUpType = typeof(ISettingsUIPresenter)
            });
        }

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
            RefreshSlotUnlockHints();
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
            _layoutView.SettingsButtonClicked -= OnSettingsButtonClicked;
            _layoutView.ArmyView.SetTowersButtonClicked -= OnSetTowersButtonClicked;
            _layoutView.ArmyView.SetSpellsButtonClicked -= OnSetSpellsButtonClicked;
            _playerStats.OnGoldChanged -= OnPlayerGoldChanged;
            _playerStats.UpgradesChanged -= OnUpgradesChanged;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;
            _inBattlePresenter.Dispose();
            _layoutView.ArmyView.LockedSlotClicked -= OnLockedSlotClicked;
            _world.TowerSlotsChanged -= RefreshSlotUnlockHints;
            _slotUnlockPresenter.Dispose();
            _purchaseWindowPresenter.Dispose();
            _armyPresenter.Dispose();
            _spellsPanelPresenter.Dispose();
            _shopPresenter.Dispose();

            base.Dispose();
        }
    }
}
