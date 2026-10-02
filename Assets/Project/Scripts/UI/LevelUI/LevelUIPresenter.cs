using YG;
using MessagePipe;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Systems;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;
using Project.Scripts.UI.EndWaveUI;
using Project.Scripts.UI.QuestUI;
using Project.Scripts.UI.SettingsUI;
using Project.Scripts.UI.ShopUI;
using Project.Scripts.UI.SpellsUI;
using UnityEngine;

namespace Project.Scripts.UI.LevelUI
{
    public class LevelUIPresenter : LayoutPresenterBase<ILevelUIView>, ILevelUIPresenter
    {
        private const string UpgradeLowestTowerRewardId = "level_ui_upgrade_lowest_tower";

        private readonly ILevelUIUseCase _levelUIUseCase;
        private readonly IBuyTowerUseCase _buyTowerUseCase;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;
        private readonly RunBattleRuntime _runBattleRuntime;
        private readonly RunState _runState;
        private readonly RunEnergyService _energy;
        private readonly BaseHealth _baseHealth;
        private readonly ILocalizationService _localizationService;
        private readonly IPublisher<ShowPopupDto> _showPopupPublisher;
        private readonly IGameManagerService _gameManagerService;
        private readonly IAudioManager _audioManager;
        private readonly ISpellsUIUseCase _spellsUIUseCase;
        private readonly WeaponBarrageUseCase _weaponBarrageUseCase;
        private readonly BaseRepairUseCase _baseRepairUseCase;
        private readonly BarrageProtocolUseCase _barrageProtocolUseCase;
        private readonly CryoDischargeUseCase _cryoDischargeUseCase;
        private readonly EmpPulseUseCase _empPulseUseCase;
        private readonly OrbitalRailgunUseCase _orbitalRailgunUseCase;
        private readonly GravityTrapUseCase _gravityTrapUseCase;

        private bool _isWaitingAdReward;

        public LevelUIPresenter(
            IBuyTowerUseCase buyTowerUseCase,
            IPlayerStatsUseCase playerStatsUseCase,
            RunBattleRuntime runBattleRuntime,
            RunState runState,
            RunEnergyService energy,
            ILevelUIUseCase levelUIUseCase,
            BaseHealth baseHealth,
            ILocalizationService localizationService,
            IPublisher<ShowPopupDto> showPopupPublisher,
            IGameManagerService gameManagerService,
            IAudioManager audioManager,
            ISpellsUIUseCase spellsUIUseCase,
            WeaponBarrageUseCase weaponBarrageUseCase,
            BaseRepairUseCase baseRepairUseCase,
            BarrageProtocolUseCase barrageProtocolUseCase,
            CryoDischargeUseCase cryoDischargeUseCase,
            EmpPulseUseCase empPulseUseCase,
            OrbitalRailgunUseCase orbitalRailgunUseCase,
            GravityTrapUseCase gravityTrapUseCase)
        {
            _buyTowerUseCase = buyTowerUseCase;
            _playerStatsUseCase = playerStatsUseCase;
            _runBattleRuntime = runBattleRuntime;
            _runState = runState;
            _energy = energy;
            _levelUIUseCase = levelUIUseCase;
            _baseHealth = baseHealth;
            _localizationService = localizationService;
            _showPopupPublisher = showPopupPublisher;
            _gameManagerService = gameManagerService;
            _audioManager = audioManager;
            _spellsUIUseCase = spellsUIUseCase;
            _weaponBarrageUseCase = weaponBarrageUseCase;
            _baseRepairUseCase = baseRepairUseCase;
            _barrageProtocolUseCase = barrageProtocolUseCase;
            _cryoDischargeUseCase = cryoDischargeUseCase;
            _empPulseUseCase = empPulseUseCase;
            _orbitalRailgunUseCase = orbitalRailgunUseCase;
            _gravityTrapUseCase = gravityTrapUseCase;
        }

        public override void Initialize()
        {
            base.Initialize();

            _buyTowerUseCase.TowerCostChanged += OnTowerCostChanged;
            _playerStatsUseCase.SelectedTowerLevelChanged += OnSelectedTowerLevelChanged;
            _playerStatsUseCase.WaveChanged += OnCurrentWaveChanged;
            _layoutView.BuyTowerButtonClicked += OnPayTowerButtonClicked;
            _layoutView.BuyGeneratorButtonClicked += OnPayGeneratorButtonClicked;
            _energy.Changed += OnEnergyChanged;
            _layoutView.ShopButtonClicked += OnShopButtonClicked;
            _layoutView.ADButtonClicked += OnADButtonClicked;
            _layoutView.QuestsButtonClicked += OnQuestsButtonClicked;
            _layoutView.SettingsButtonClicked += OnSettingsButtonClicked;
            _layoutView.NextWaveButtonClicked += OnNextWaveButtonClicked;
            _layoutView.SetSpellsButtonClicked += OnSetSpellsButtonClicked;
            _playerStatsUseCase.OnGoldChanged += OnGoldChanged;
            _baseHealth.OnMaxHealthChanged += OnMaxHealthChanged;
            _baseHealth.OnCurrentHealthChanged += OnCurrentHealthChanged;
            _localizationService.OnChangeLanguage += OnLanguageChanged;
            _runState.PhaseChanged += OnRunPhaseChanged;
            _layoutView.WeaponBarrageButtonClicked += OnWeaponBarrageButtonClicked;
            _weaponBarrageUseCase.StateChanged += OnWeaponBarrageStateChanged;
            _layoutView.BaseRepairButtonClicked += OnBaseRepairButtonClicked;
            _baseRepairUseCase.StateChanged += OnBaseRepairStateChanged;
            _layoutView.BarrageProtocolButtonClicked += OnBarrageProtocolButtonClicked;
            _barrageProtocolUseCase.StateChanged += OnBarrageProtocolStateChanged;
            _layoutView.CryoDischargeButtonClicked += OnCryoDischargeButtonClicked;
            _cryoDischargeUseCase.StateChanged += OnCryoDischargeStateChanged;
            _layoutView.EmpPulseButtonClicked += OnEmpPulseButtonClicked;
            _empPulseUseCase.StateChanged += OnEmpPulseStateChanged;
            _layoutView.OrbitalRailgunButtonClicked += OnOrbitalRailgunButtonClicked;
            _orbitalRailgunUseCase.StateChanged += OnOrbitalRailgunStateChanged;
            _layoutView.GravityTrapButtonClicked += OnGravityTrapButtonClicked;
            _gravityTrapUseCase.StateChanged += OnGravityTrapStateChanged;
            _spellsUIUseCase.SelectionChanged += OnSpellSelectionChanged;

            _layoutView.SetPriceTower(_buyTowerUseCase.TowerCost);
            _layoutView.SetGeneratorPrice(_buyTowerUseCase.GeneratorCost);
            RefreshEnergy();
            _layoutView.SetMoney(_playerStatsUseCase.Gold);

            RefreshWaveText();
            RefreshRunPhaseControls();
            UpdateTowerIcon();
            RefreshWeaponBarrage();
            RefreshBaseRepair();
            RefreshBarrageProtocol();
            RefreshCryoDischarge();
            RefreshEmpPulse();
            RefreshOrbitalRailgun();
            RefreshGravityTrap();
            RefreshActiveSpells();
        }

        private void OnTowerCostChanged(int price)
        {
            _layoutView.SetPriceTower(price);
        }
        private void RefreshWaveText()
        {
            var waveText = _localizationService.Format(LocalizationKeys.LevelWaveFormat, _playerStatsUseCase.Wave);
            var displayName = _runState.CurrentWaveConfig?.DisplayName;
            if (!string.IsNullOrWhiteSpace(displayName))
                waveText = $"{waveText}: {displayName}";

            _layoutView.SetCurrentWaveText(waveText);
        }

        private void OnPayTowerButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            var result = _buyTowerUseCase.TryBuyTower();
            Debug.Log($"BuyTower result: {result}");
        }

        private void OnPayGeneratorButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            var result = _buyTowerUseCase.TryBuyGenerator();
            Debug.Log($"BuyGenerator result: {result}");
        }

        private void OnEnergyChanged(int energy)
        {
            RefreshEnergy();
            RefreshWeaponBarrage();
            RefreshBaseRepair();
            RefreshBarrageProtocol();
            RefreshCryoDischarge();
            RefreshEmpPulse();
            RefreshOrbitalRailgun();
            RefreshGravityTrap();
        }

        private void RefreshEnergy() => _layoutView.SetEnergy(_energy.Current, _energy.Max);

        private void RefreshGeneratorPurchase() => _layoutView.SetGeneratorPurchaseEnabled(
            _runState.CanEditDefense && _playerStatsUseCase.CanSpend(_buyTowerUseCase.GeneratorCost));

        private void OnNextWaveButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _runBattleRuntime.AdvanceFromLevelButton();
            RefreshRunPhaseControls();
        }

        private void OnWeaponBarrageButtonClicked()
        {
            if (!_spellsUIUseCase.IsSelected(SpellIds.WeaponBarrage))
                return;

            _audioManager.PlaySound(ESoundId.UiButtonClick);

            if (_cryoDischargeUseCase.IsTargeting)
                _cryoDischargeUseCase.ToggleTargeting();

            if (_empPulseUseCase.IsTargeting)
                _empPulseUseCase.ToggleTargeting();

            if (_orbitalRailgunUseCase.IsTargeting)
                _orbitalRailgunUseCase.ToggleTargeting();

            if (_gravityTrapUseCase.IsTargeting)
                _gravityTrapUseCase.ToggleTargeting();

            _weaponBarrageUseCase.ToggleTargeting();
            RefreshWeaponBarrage();
        }

        private void OnWeaponBarrageStateChanged()
        {
            RefreshWeaponBarrage();
        }

        private void RefreshWeaponBarrage()
        {
            _layoutView.SetWeaponBarrageState(
                Mathf.CeilToInt(_weaponBarrageUseCase.CooldownRemaining),
                _weaponBarrageUseCase.IsTargeting,
                _weaponBarrageUseCase.CanInteract);
        }

        private void OnBaseRepairButtonClicked()
        {
            if (!_spellsUIUseCase.IsSelected(SpellIds.BaseRepair))
                return;

            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _baseRepairUseCase.TryUse();
            RefreshBaseRepair();
        }

        private void OnBaseRepairStateChanged()
        {
            RefreshBaseRepair();
        }

        private void RefreshBaseRepair()
        {
            _layoutView.SetBaseRepairState(
                _baseRepairUseCase.DisplayName,
                _baseRepairUseCase.HealAmount,
                Mathf.CeilToInt(_baseRepairUseCase.CooldownRemaining),
                _baseRepairUseCase.IsBaseFull,
                _baseRepairUseCase.CanUse);
        }

        private void OnBarrageProtocolButtonClicked()
        {
            if (!_spellsUIUseCase.IsSelected(SpellIds.BarrageProtocol))
                return;

            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _barrageProtocolUseCase.TryUse();
            RefreshBarrageProtocol();
        }

        private void OnBarrageProtocolStateChanged()
        {
            RefreshBarrageProtocol();
        }

        private void RefreshBarrageProtocol()
        {
            _layoutView.SetBarrageProtocolState(
                Mathf.RoundToInt((_barrageProtocolUseCase.AttackSpeedMultiplier - 1f) * 100f),
                Mathf.CeilToInt(_barrageProtocolUseCase.ActiveDurationRemaining),
                Mathf.CeilToInt(_barrageProtocolUseCase.CooldownRemaining),
                _barrageProtocolUseCase.HasCombatTower,
                _barrageProtocolUseCase.CanUse);
        }

        private void OnCryoDischargeButtonClicked()
        {
            if (!_spellsUIUseCase.IsSelected(SpellIds.CryoDischarge))
                return;

            _audioManager.PlaySound(ESoundId.UiButtonClick);

            if (_weaponBarrageUseCase.IsTargeting)
                _weaponBarrageUseCase.ToggleTargeting();

            if (_empPulseUseCase.IsTargeting)
                _empPulseUseCase.ToggleTargeting();

            if (_orbitalRailgunUseCase.IsTargeting)
                _orbitalRailgunUseCase.ToggleTargeting();

            if (_gravityTrapUseCase.IsTargeting)
                _gravityTrapUseCase.ToggleTargeting();

            _cryoDischargeUseCase.ToggleTargeting();
            RefreshCryoDischarge();
        }

        private void OnCryoDischargeStateChanged()
        {
            RefreshCryoDischarge();
        }

        private void RefreshCryoDischarge()
        {
            _layoutView.SetCryoDischargeState(
                Mathf.CeilToInt(_cryoDischargeUseCase.CooldownRemaining),
                _cryoDischargeUseCase.IsTargeting,
                _cryoDischargeUseCase.CanInteract);
        }

        private void OnEmpPulseButtonClicked()
        {
            if (!_spellsUIUseCase.IsSelected(SpellIds.EmpPulse))
                return;

            _audioManager.PlaySound(ESoundId.UiButtonClick);

            if (_weaponBarrageUseCase.IsTargeting)
                _weaponBarrageUseCase.ToggleTargeting();

            if (_cryoDischargeUseCase.IsTargeting)
                _cryoDischargeUseCase.ToggleTargeting();

            if (_orbitalRailgunUseCase.IsTargeting)
                _orbitalRailgunUseCase.ToggleTargeting();

            if (_gravityTrapUseCase.IsTargeting)
                _gravityTrapUseCase.ToggleTargeting();

            _empPulseUseCase.ToggleTargeting();
            RefreshEmpPulse();
        }

        private void OnEmpPulseStateChanged()
        {
            RefreshEmpPulse();
        }

        private void RefreshEmpPulse()
        {
            _layoutView.SetEmpPulseState(
                Mathf.CeilToInt(_empPulseUseCase.CooldownRemaining),
                _empPulseUseCase.IsTargeting,
                _empPulseUseCase.CanInteract);
        }

        private void OnOrbitalRailgunButtonClicked()
        {
            if (!_spellsUIUseCase.IsSelected(SpellIds.OrbitalRailgun))
                return;

            _audioManager.PlaySound(ESoundId.UiButtonClick);

            if (_weaponBarrageUseCase.IsTargeting)
                _weaponBarrageUseCase.ToggleTargeting();

            if (_cryoDischargeUseCase.IsTargeting)
                _cryoDischargeUseCase.ToggleTargeting();

            if (_empPulseUseCase.IsTargeting)
                _empPulseUseCase.ToggleTargeting();

            if (_gravityTrapUseCase.IsTargeting)
                _gravityTrapUseCase.ToggleTargeting();

            _orbitalRailgunUseCase.ToggleTargeting();
            RefreshOrbitalRailgun();
        }

        private void OnOrbitalRailgunStateChanged()
        {
            RefreshOrbitalRailgun();
        }

        private void RefreshOrbitalRailgun()
        {
            _layoutView.SetOrbitalRailgunState(
                Mathf.CeilToInt(_orbitalRailgunUseCase.CooldownRemaining),
                _orbitalRailgunUseCase.IsTargeting,
                _orbitalRailgunUseCase.IsCasting,
                _orbitalRailgunUseCase.CanInteract);
        }

        private void OnGravityTrapButtonClicked()
        {
            if (!_spellsUIUseCase.IsSelected(SpellIds.GravityTrap))
                return;

            _audioManager.PlaySound(ESoundId.UiButtonClick);

            if (_weaponBarrageUseCase.IsTargeting)
                _weaponBarrageUseCase.ToggleTargeting();

            if (_cryoDischargeUseCase.IsTargeting)
                _cryoDischargeUseCase.ToggleTargeting();

            if (_empPulseUseCase.IsTargeting)
                _empPulseUseCase.ToggleTargeting();

            if (_orbitalRailgunUseCase.IsTargeting)
                _orbitalRailgunUseCase.ToggleTargeting();

            _gravityTrapUseCase.ToggleTargeting();
            RefreshGravityTrap();
        }

        private void OnGravityTrapStateChanged()
        {
            RefreshGravityTrap();
        }

        private void RefreshGravityTrap()
        {
            _layoutView.SetGravityTrapState(
                Mathf.CeilToInt(_gravityTrapUseCase.ActiveDurationRemaining),
                Mathf.CeilToInt(_gravityTrapUseCase.CooldownRemaining),
                _gravityTrapUseCase.IsTargeting,
                _gravityTrapUseCase.CanInteract);
        }

        private void OnSpellSelectionChanged()
        {
            RefreshActiveSpells();
        }

        private void RefreshActiveSpells()
        {
            var spells = _spellsUIUseCase.GetSpells();
            for (var i = 0; i < spells.Count; i++)
            {
                _layoutView.SetSpellIcon(spells[i].SpellId, spells[i].Icon);
                _layoutView.SetSpellBackground(spells[i].SpellId, spells[i].Background);
                _layoutView.SetSpellVisible(spells[i].SpellId, spells[i].IsSelected);
            }
        }

        private void OnShopButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            Debug.Log("OnShopButtonClicked");
            _showPopupPublisher.Publish(new ShowPopupDto
            {
                TargetPopUpType = typeof(IShopUIPresenter)
            });
            _gameManagerService.PauseGame();
        }

        private void OnSetSpellsButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _showPopupPublisher.Publish(new ShowPopupDto
            {
                TargetPopUpType = typeof(ISpellsUIPresenter)
            });
        }

        private void OnADButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);

            if (!_levelUIUseCase.HasUpgradeableTower())
            {
                Debug.LogWarning("LevelUIPresenter: no upgradeable towers for rewarded ad.");
                return;
            }

            if (_isWaitingAdReward)
                return;

            Debug.Log("OnADButtonClicked");
            
            _isWaitingAdReward = true;
            SubscribeRewardedAdEvents();
            YG2.RewardedAdvShow(UpgradeLowestTowerRewardId);
        }

        private void OnQuestsButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            Debug.Log("OnQuestsButtonClicked");
            _showPopupPublisher.Publish(new ShowPopupDto
            {
                TargetPopUpType = typeof(IQuestUIPresenter)
            });
        }

        private void OnSettingsButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            Debug.Log("OnSettingsButtonClicked");
            _showPopupPublisher.Publish(new ShowPopupDto
            {
                TargetPopUpType = typeof(ISettingsUIPresenter)
            });
        }

        private void OnGoldChanged(int gold)
        {
            _layoutView.SetMoney(gold);
            RefreshGeneratorPurchase();
        }

        private void OnSelectedTowerLevelChanged(int level)
        {
            UpdateTowerIcon();
        }

        private void OnMaxHealthChanged(int health)
        {
            _layoutView.SetMaxBaseHealth(health);
        }

        private void OnCurrentHealthChanged(int health)
        {
            _layoutView.SetCurrentBaseHealth(health);
            RefreshBaseRepair();
        }

        private void OnCurrentWaveChanged(int wave)
        {
            RefreshWaveText();
            RefreshRunPhaseControls();
        }

        private void UpdateTowerIcon()
        {
            var towerConfig = _levelUIUseCase.GetSelectedTowerConfig();
            if (towerConfig == null)
            {
                Debug.LogWarning($"LevelUIPresenter: Tower config not found for level {_playerStatsUseCase.SelectedTowerLevel}.");
                return;
            }

            _layoutView.SetTowerLevel(towerConfig.TowerLevel);
            _layoutView.SetTowerIcon(towerConfig.Icon);
        }
        
        private void OnLanguageChanged(string _)
        {
            RefreshWaveText();
            RefreshEnergy();
        }

        private void OnRunPhaseChanged(ERunPhase phase)
        {
            RefreshRunPhaseControls();
        }

        private void RefreshRunPhaseControls()
        {
            _layoutView.SetRunPhase(_runState.Phase);
            _layoutView.SetNextWaveButtonEnabled(_runBattleRuntime.CanUseNextWaveButton);
            _layoutView.SetTowerActionsEnabled(_runState.CanEditDefense);
            RefreshGeneratorPurchase();
            RefreshWeaponBarrage();
            RefreshBaseRepair();
            RefreshBarrageProtocol();
            RefreshCryoDischarge();
            RefreshEmpPulse();
            RefreshOrbitalRailgun();
            RefreshGravityTrap();
        }

        private void TryGrantAdTowerUpgrade()
        {
            var upgraded = _levelUIUseCase.TryUpgradeRandomLowestLevelTower();
            Debug.Log(upgraded
                ? "LevelUIPresenter: rewarded ad upgraded a tower."
                : "LevelUIPresenter: rewarded ad reward could not upgrade a tower.");
        }
        
        private void SubscribeRewardedAdEvents()
        {
            YG2.onRewardAdv += OnRewardedAdReward;
            YG2.onCloseRewardedAdv += OnRewardedAdClosed;
            YG2.onErrorRewardedAdv += OnRewardedAdError;
        }

        private void UnsubscribeRewardedAdEvents()
        {
            YG2.onRewardAdv -= OnRewardedAdReward;
            YG2.onCloseRewardedAdv -= OnRewardedAdClosed;
            YG2.onErrorRewardedAdv -= OnRewardedAdError;
        }

        private void OnRewardedAdReward(string rewardId)
        {
            if (!_isWaitingAdReward || rewardId != UpgradeLowestTowerRewardId)
                return;

            _isWaitingAdReward = false;
            UnsubscribeRewardedAdEvents();
            TryGrantAdTowerUpgrade();
        }

        private void OnRewardedAdClosed()
        {
            if (!_isWaitingAdReward)
                return;

            _isWaitingAdReward = false;
            UnsubscribeRewardedAdEvents();
            Debug.Log("LevelUIPresenter: rewarded ad was closed without reward.");
        }

        private void OnRewardedAdError()
        {
            if (!_isWaitingAdReward)
                return;

            _isWaitingAdReward = false;
            UnsubscribeRewardedAdEvents();
            Debug.LogWarning("LevelUIPresenter: rewarded ad failed.");
        }

        public override void Dispose()
        {
            _layoutView.BuyTowerButtonClicked -= OnPayTowerButtonClicked;
            _layoutView.BuyGeneratorButtonClicked -= OnPayGeneratorButtonClicked;
            _energy.Changed -= OnEnergyChanged;
            _playerStatsUseCase.SelectedTowerLevelChanged -= OnSelectedTowerLevelChanged;
            _playerStatsUseCase.WaveChanged -= OnCurrentWaveChanged;
            _layoutView.ShopButtonClicked -= OnShopButtonClicked;
            _layoutView.ADButtonClicked -= OnADButtonClicked;
            _layoutView.QuestsButtonClicked -= OnQuestsButtonClicked;
            _layoutView.SettingsButtonClicked -= OnSettingsButtonClicked;
            _layoutView.NextWaveButtonClicked -= OnNextWaveButtonClicked;
            _layoutView.SetSpellsButtonClicked -= OnSetSpellsButtonClicked;
            _playerStatsUseCase.OnGoldChanged -= OnGoldChanged;
            _buyTowerUseCase.TowerCostChanged -= OnTowerCostChanged;
            _baseHealth.OnMaxHealthChanged -= OnMaxHealthChanged;
            _baseHealth.OnCurrentHealthChanged -= OnCurrentHealthChanged;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;
            _runState.PhaseChanged -= OnRunPhaseChanged;
            _layoutView.WeaponBarrageButtonClicked -= OnWeaponBarrageButtonClicked;
            _weaponBarrageUseCase.StateChanged -= OnWeaponBarrageStateChanged;
            _layoutView.BaseRepairButtonClicked -= OnBaseRepairButtonClicked;
            _baseRepairUseCase.StateChanged -= OnBaseRepairStateChanged;
            _layoutView.BarrageProtocolButtonClicked -= OnBarrageProtocolButtonClicked;
            _barrageProtocolUseCase.StateChanged -= OnBarrageProtocolStateChanged;
            _layoutView.CryoDischargeButtonClicked -= OnCryoDischargeButtonClicked;
            _cryoDischargeUseCase.StateChanged -= OnCryoDischargeStateChanged;
            _layoutView.EmpPulseButtonClicked -= OnEmpPulseButtonClicked;
            _empPulseUseCase.StateChanged -= OnEmpPulseStateChanged;
            _layoutView.OrbitalRailgunButtonClicked -= OnOrbitalRailgunButtonClicked;
            _orbitalRailgunUseCase.StateChanged -= OnOrbitalRailgunStateChanged;
            _layoutView.GravityTrapButtonClicked -= OnGravityTrapButtonClicked;
            _gravityTrapUseCase.StateChanged -= OnGravityTrapStateChanged;
            _spellsUIUseCase.SelectionChanged -= OnSpellSelectionChanged;
            
            if (_isWaitingAdReward)
                UnsubscribeRewardedAdEvents();

            base.Dispose();
        }
    }
}
