using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.UpgradeItem;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.System.Shop;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI;
using Project.Scripts.Systems.UI.Dtos;
using UnityEngine;
using VContainer.Unity;

namespace Project.Scripts.UI.ShopUI
{
    public class ShopUIPresenter : LayoutPresenterBase<IShopUIView>, IShopUIPresenter, ITickable
    {
        private const string UpgradeTabId = "upgrades";
        private const float TimerRefreshIntervalSeconds = 1f;

        private readonly IPublisher<HidePopupDto> _hidePopupPublisher;
        private readonly IGameManagerService _gameManagerService;
        private readonly ILocalizationService _localizationService;
        private readonly IPlayerStatsUseCase _playerStats;
        private readonly BaseHealth _baseHealth;
        private readonly IAudioManager _audioManager;
        private readonly ShopOfferService _offerService;
        private readonly UIElements _uiElements;

        private List<IUpgradeItem> _upgradeItems;
        private string _selectedTabId = UpgradeTabId;
        private float _nextTimerRefreshAt;

        public ShopUIPresenter(
            IPublisher<HidePopupDto> hidePopupPublisher,
            IGameManagerService gameManagerService,
            ILocalizationService localizationService,
            IPlayerStatsUseCase playerStats,
            BaseHealth baseHealth,
            IAudioManager audioManager,
            ShopOfferService offerService,
            UIElements uiElements)
        {
            _hidePopupPublisher = hidePopupPublisher;
            _gameManagerService = gameManagerService;
            _localizationService = localizationService;
            _playerStats = playerStats;
            _baseHealth = baseHealth;
            _audioManager = audioManager;
            _offerService = offerService;
            _uiElements = uiElements;
        }

        public override void Initialize()
        {
            base.Initialize();

            _upgradeItems = new List<IUpgradeItem>
            {
                new TowerLevelUpgradeItem(_layoutView.TowerLevelUpgradeConfig, _playerStats),
                new TowerDamageUpgradeItem(_layoutView.DamageUpgradeConfig, _playerStats),
                new TowerAttackSpeedUpgradeItem(_layoutView.AttackSpeedUpgradeConfig, _playerStats),
                new BaseHealthUpgradeItem(_layoutView.HealthUpgradeConfig, _playerStats, _baseHealth),
                new TowerCritChanceUpgradeItem(_layoutView.CritChanceUpgradeConfig, _playerStats),
                new TowerCritDamageUpgradeItem(_layoutView.CritDamageUpgradeConfig, _playerStats)
            };

            _layoutView.CloseButtonClicked += OnCloseButtonClicked;
            _layoutView.TabClicked += OnTabClicked;
            _layoutView.OfferPurchaseClicked += OnOfferPurchaseClicked;
            _playerStats.OnGoldChanged += OnGoldChanged;
            _playerStats.UpgradesChanged += OnUpgradesChanged;
            _localizationService.OnChangeLanguage += OnLanguageChanged;
            _offerService.StateChanged += OnOfferStateChanged;
            _offerService.PurchaseSucceeded += OnOfferPurchaseSucceeded;

            Refresh();
        }

        public override async UniTask ActivateAsync()
        {
            _gameManagerService.PauseGame();
            _nextTimerRefreshAt = Time.unscaledTime + TimerRefreshIntervalSeconds;
            Refresh();
            await base.ActivateAsync();
        }

        public override async UniTask DeactivateAsync()
        {
            await base.DeactivateAsync();
            _gameManagerService.ResumeGame();
        }

        public void Tick()
        {
            if (!IsActive
                || string.Equals(_selectedTabId, UpgradeTabId, StringComparison.Ordinal)
                || Time.unscaledTime < _nextTimerRefreshAt)
                return;

            _nextTimerRefreshAt = Time.unscaledTime + TimerRefreshIntervalSeconds;
            Refresh();
        }

        private void Refresh()
        {
            _layoutView.SetTitle(_localizationService.Get(LocalizationKeys.ShopTitle));

            var placements = _offerService.GetVisiblePlacements();
            var tabs = new List<ShopTabUIData>(placements.Count + 1)
            {
                new(UpgradeTabId, _localizationService.Get(LocalizationKeys.ShopUpgradesTab))
            };

            var selectedTabExists = string.Equals(
                _selectedTabId,
                UpgradeTabId,
                StringComparison.Ordinal);
            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                tabs.Add(new ShopTabUIData(
                    placement.Id,
                    _localizationService.Get(placement.Title)));

                if (string.Equals(placement.Id, _selectedTabId, StringComparison.Ordinal))
                    selectedTabExists = true;
            }

            if (!selectedTabExists)
                _selectedTabId = UpgradeTabId;

            _layoutView.SetTabs(tabs, _selectedTabId);
            if (string.Equals(_selectedTabId, UpgradeTabId, StringComparison.Ordinal))
                RefreshUpgrades();
            else
                RefreshOffers();
        }

        private void RefreshUpgrades()
        {
            _layoutView.ClearItems();
            for (var i = 0; i < _upgradeItems.Count; i++)
            {
                var item = _upgradeItems[i];
                _layoutView.AddItem(item, () =>
                {
                    if (!item.TryUpgrade())
                        return;

                    _audioManager.PlaySound(ESoundId.AddUpgrade);
                    Refresh();
                }, _localizationService);
            }
        }

        private void RefreshOffers()
        {
            var states = _offerService.GetVisibleOffers(_selectedTabId);
            if (states.Count == 0)
            {
                _layoutView.ShowEmptyState(
                    _localizationService.Get(LocalizationKeys.ShopNoOffers));
                return;
            }

            var offers = new List<ShopOfferUIData>(states.Count);
            for (var i = 0; i < states.Count; i++)
                offers.Add(CreateUIData(states[i]));

            _layoutView.SetOffers(offers);
        }

        private ShopOfferUIData CreateUIData(ShopOfferState state)
        {
            var config = state.Config;
            var rewards = CreateRewards(config);
            var mainIcon = config.MainIcon ?? ResolveMainIcon(config, rewards);
            var priceIcon = config.Price.Type == EShopOfferPriceType.Gold
                ? _uiElements.GoldIcon
                : null;
            var price = config.Price.Type == EShopOfferPriceType.Gold
                ? config.Price.GoldAmount.ToString()
                : string.IsNullOrWhiteSpace(state.PaymentPrice)
                    ? "—"
                    : state.PaymentPrice;
            var limit = config.DailyPurchaseLimit > 0
                ? _localizationService.Format(
                    LocalizationKeys.ShopOfferLimitFormat,
                    state.RemainingPurchases,
                    config.DailyPurchaseLimit)
                : string.Empty;
            var timer = CreateTimerText(state);
            var status = GetBlockReasonText(state.BlockReason);

            return new ShopOfferUIData(
                config.Id,
                _localizationService.Get(config.Title),
                string.IsNullOrWhiteSpace(config.Description)
                    ? string.Empty
                    : _localizationService.Get(config.Description),
                mainIcon,
                rewards,
                priceIcon,
                price,
                limit,
                timer,
                status,
                state.CanPurchase);
        }

        private List<ShopOfferRewardUIData> CreateRewards(ShopOfferConfig config)
        {
            var result = new List<ShopOfferRewardUIData>(config.Rewards.Count);
            for (var i = 0; i < config.Rewards.Count; i++)
            {
                var reward = config.Rewards[i];
                switch (reward.Type)
                {
                    case EShopOfferRewardType.Gold:
                        result.Add(new ShopOfferRewardUIData(
                            _uiElements.GoldIcon,
                            reward.Amount.ToString()));
                        break;
                    case EShopOfferRewardType.Gems:
                        result.Add(new ShopOfferRewardUIData(
                            _uiElements.GemsIcon,
                            reward.Amount.ToString()));
                        break;
                    case EShopOfferRewardType.Spell:
                        result.Add(new ShopOfferRewardUIData(
                            reward.Spell.Icon,
                            _localizationService.Get(reward.Spell.DisplayName)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return result;
        }

        private Sprite ResolveMainIcon(
            ShopOfferConfig config,
            IReadOnlyList<ShopOfferRewardUIData> rewards)
        {
            for (var i = 0; i < config.Rewards.Count; i++)
            {
                if (config.Rewards[i].Type == EShopOfferRewardType.Spell)
                    return config.Rewards[i].Spell.Icon;
            }

            return rewards.Count > 0 ? rewards[0].Icon : null;
        }

        private string CreateTimerText(ShopOfferState state)
        {
            var parts = new List<string>(2);
            if (state.Config.IsTimed)
            {
                parts.Add(_localizationService.Format(
                    LocalizationKeys.ShopOfferEndsFormat,
                    FormatDuration(state.TimeUntilEnd)));
            }

            if (state.Config.DailyPurchaseLimit > 0
                && state.BlockReason != EShopOfferBlockReason.TimeUnavailable)
            {
                parts.Add(_localizationService.Format(
                    LocalizationKeys.ShopOfferResetFormat,
                    FormatDuration(state.TimeUntilReset)));
            }

            return string.Join(" • ", parts);
        }

        private string GetBlockReasonText(EShopOfferBlockReason reason)
        {
            return reason switch
            {
                EShopOfferBlockReason.None => string.Empty,
                EShopOfferBlockReason.TimeUnavailable =>
                    _localizationService.Get(LocalizationKeys.ShopOfferTimeUnavailable),
                EShopOfferBlockReason.DailyLimitReached =>
                    _localizationService.Get(LocalizationKeys.ShopOfferLimitReached),
                EShopOfferBlockReason.InsufficientGold =>
                    _localizationService.Get(LocalizationKeys.ShopOfferInsufficientGold),
                EShopOfferBlockReason.ProductUnavailable =>
                    _localizationService.Get(LocalizationKeys.ShopOfferProductUnavailable),
                EShopOfferBlockReason.PurchaseInProgress =>
                    _localizationService.Get(LocalizationKeys.ShopOfferPurchaseInProgress),
                _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
            };
        }

        private static string FormatDuration(TimeSpan duration)
        {
            var totalHours = Math.Max(0, (int)Math.Floor(duration.TotalHours));
            return $"{totalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
        }

        private void OnLanguageChanged(string _) => Refresh();

        private void OnTabClicked(string tabId)
        {
            _selectedTabId = tabId;
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            Refresh();
        }

        private void OnOfferPurchaseClicked(string offerId)
        {
            if (!_offerService.TryPurchase(offerId))
                return;

            _audioManager.PlaySound(ESoundId.UiButtonClick);
        }

        private void OnOfferPurchaseSucceeded(string _)
        {
            _audioManager.PlaySound(ESoundId.AddUpgrade);
            Refresh();
        }

        private void OnOfferStateChanged()
        {
            if (IsActive)
                Refresh();
        }

        private void OnCloseButtonClicked()
        {
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            _hidePopupPublisher.Publish(new HidePopupDto
            {
                TargetPopUpType = typeof(IShopUIPresenter)
            });
        }

        private void OnGoldChanged(int _) => Refresh();
        private void OnUpgradesChanged() => Refresh();

        public override void Dispose()
        {
            _layoutView.CloseButtonClicked -= OnCloseButtonClicked;
            _layoutView.TabClicked -= OnTabClicked;
            _layoutView.OfferPurchaseClicked -= OnOfferPurchaseClicked;
            _playerStats.OnGoldChanged -= OnGoldChanged;
            _playerStats.UpgradesChanged -= OnUpgradesChanged;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;
            _offerService.StateChanged -= OnOfferStateChanged;
            _offerService.PurchaseSucceeded -= OnOfferPurchaseSucceeded;

            base.Dispose();
        }
    }
}
