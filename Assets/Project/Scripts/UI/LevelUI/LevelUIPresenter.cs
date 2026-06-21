using YG;
using MessagePipe;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;
using Project.Scripts.UI.EndWaveUI;
using Project.Scripts.UI.QuestUI;
using Project.Scripts.UI.SettingsUI;
using Project.Scripts.UI.ShopUI;
using UnityEngine;

namespace Project.Scripts.UI.LevelUI
{
    public class LevelUIPresenter : LayoutPresenterBase<ILevelUIView>, ILevelUIPresenter
    {
        private const string UpgradeLowestTowerRewardId = "level_ui_upgrade_lowest_tower";

        private readonly ILevelUIUseCase _levelUIUseCase;
        private readonly IBuyTowerUseCase _buyTowerUseCase;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;
        private readonly BaseHealth _baseHealth;
        private readonly ILocalizationService _localizationService;
        private readonly IPublisher<ShowPopupDto> _showPopupPublisher;
        private readonly IGameManagerService _gameManagerService;
        private readonly IAudioManager _audioManager;

        private bool _isWaitingAdReward;

        public LevelUIPresenter(
            IBuyTowerUseCase buyTowerUseCase,
            IPlayerStatsUseCase playerStatsUseCase,
            ILevelUIUseCase levelUIUseCase,
            BaseHealth baseHealth,
            ILocalizationService localizationService,
            IPublisher<ShowPopupDto> showPopupPublisher,
            IGameManagerService gameManagerService,
            IAudioManager audioManager)
        {
            _buyTowerUseCase = buyTowerUseCase;
            _playerStatsUseCase = playerStatsUseCase;
            _levelUIUseCase = levelUIUseCase;
            _baseHealth = baseHealth;
            _localizationService = localizationService;
            _showPopupPublisher = showPopupPublisher;
            _gameManagerService = gameManagerService;
            _audioManager = audioManager;
        }

        public override void Initialize()
        {
            base.Initialize();

            _buyTowerUseCase.TowerCostChanged += OnTowerCostChanged;
            _playerStatsUseCase.SelectedTowerLevelChanged += OnSelectedTowerLevelChanged;
            _playerStatsUseCase.WaveChanged += OnCurrentWaveChanged;
            _layoutView.BuyTowerButtonClicked += OnPayTowerButtonClicked;
            _layoutView.ShopButtonClicked += OnShopButtonClicked;
            _layoutView.ADButtonClicked += OnADButtonClicked;
            _layoutView.QuestsButtonClicked += OnQuestsButtonClicked;
            _layoutView.SettingsButtonClicked += OnSettingsButtonClicked;
            _playerStatsUseCase.OnGoldChanged += OnGoldChanged;
            _baseHealth.OnMaxHealthChanged += OnMaxHealthChanged;
            _baseHealth.OnCurrentHealthChanged += OnCurrentHealthChanged;
            _playerStatsUseCase.WaveChanged += OnCurrentWaveChanged;

            _layoutView.SetPriceTower(_buyTowerUseCase.TowerCost);
            _layoutView.SetMoney(_playerStatsUseCase.Gold);
            _layoutView.SetCurrentWaveText(_localizationService.Format(LocalizationKeys.LevelWaveFormat, _playerStatsUseCase.Wave));

            UpdateTowerIcon();
        }

        private void OnTowerCostChanged(int price)
        {
            _layoutView.SetPriceTower(price);
        }

        private void OnPayTowerButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            var result = _buyTowerUseCase.TryBuyTower();
            Debug.Log($"BuyTower result: {result}");
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
        }

        private void OnCurrentWaveChanged(int wave)
        {
            _layoutView.SetCurrentWaveText(_localizationService.Format(LocalizationKeys.LevelWaveFormat, wave));
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
            _layoutView.SetCurrentWaveText(
                _localizationService.Format(LocalizationKeys.LevelWaveFormat, _playerStatsUseCase.Wave));
        }

        private void TryGrantAdTowerUpgrade()
        {
            var upgraded = _levelUIUseCase.TryUpgradeRandomLowestLevelTower();
            Debug.Log(upgraded
                ? "LevelUIPresenter: rewarded ad upgraded a tower."
                : "LevelUIPresenter: rewarded ad reward could not upgrade a tower.");
        }

#if RewardedAdv_yg
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
#endif

        public override void Dispose()
        {
            _layoutView.BuyTowerButtonClicked -= OnPayTowerButtonClicked;
            _playerStatsUseCase.SelectedTowerLevelChanged -= OnSelectedTowerLevelChanged;
            _playerStatsUseCase.WaveChanged -= OnCurrentWaveChanged;
            _layoutView.ShopButtonClicked -= OnShopButtonClicked;
            _layoutView.ADButtonClicked -= OnADButtonClicked;
            _layoutView.QuestsButtonClicked -= OnQuestsButtonClicked;
            _layoutView.SettingsButtonClicked -= OnSettingsButtonClicked;
            _playerStatsUseCase.OnGoldChanged -= OnGoldChanged;
            _buyTowerUseCase.TowerCostChanged -= OnTowerCostChanged;
            _baseHealth.OnMaxHealthChanged -= OnMaxHealthChanged;
            _baseHealth.OnCurrentHealthChanged -= OnCurrentHealthChanged;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;

#if RewardedAdv_yg
            if (_isWaitingAdReward)
                UnsubscribeRewardedAdEvents();
#endif

            base.Dispose();
        }
    }
}
