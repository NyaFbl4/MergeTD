using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.System.Shop;
using Project.Scripts.System.UseCases;
using UnityEngine;

namespace Project.Scripts.UI.ShopUI
{
    public sealed class ShopContentPresenter : IDisposable
    {
        private const float TimerRefreshIntervalSeconds = 1f;

        private readonly IShopContentView _view;
        private readonly ILocalizationService _localizationService;
        private readonly IPlayerStatsUseCase _playerStats;
        private readonly IAudioManager _audioManager;
        private readonly ShopOfferService _offerService;
        private readonly UIElements _uiElements;
        private readonly Func<bool> _isActive;
        private readonly Sprite _energyIcon;
        private readonly Sprite _acceleratorIcon;
        private readonly Sprite _rewardedAdIcon;

        private float _nextTimerRefreshAt;

        public ShopContentPresenter(
            IShopContentView view,
            ILocalizationService localizationService,
            IPlayerStatsUseCase playerStats,
            BaseHealth baseHealth,
            IAudioManager audioManager,
            ShopOfferService offerService,
            UIElements uiElements,
            Func<bool> isActive)
        {
            _view = view;
            _localizationService = localizationService;
            _playerStats = playerStats;
            _audioManager = audioManager;
            _offerService = offerService;
            _uiElements = uiElements;
            _isActive = isActive;
            _acceleratorIcon = LoadSingleSprite("UI/new/Icons/Icon_Stopwatch");
            _energyIcon = LoadSingleSprite("UI/new/Icons/Icon_Energy");
            _rewardedAdIcon = LoadSingleSprite("UI/new/Icons/Icon_ImageIcon_Ad_00_l");
        }

        public void Initialize()
        {
            _view.OfferPurchaseClicked += OnOfferPurchaseClicked;
            _playerStats.OnGoldChanged += OnGoldChanged;
            _playerStats.UpgradesChanged += OnUpgradesChanged;
            _localizationService.OnChangeLanguage += OnLanguageChanged;
            _offerService.StateChanged += OnOfferStateChanged;
            _offerService.PurchaseSucceeded += OnOfferPurchaseSucceeded;

            Refresh();
        }

        public void Refresh()
        {
            _nextTimerRefreshAt = Time.unscaledTime + TimerRefreshIntervalSeconds;
            _view.SetTitle(_localizationService.Get(LocalizationKeys.ShopTitle));

            var offers = new List<ShopOfferUIData>();
            var gemOffers = new List<ShopOfferUIData>();
            var energyOffers = new List<ShopOfferUIData>();
            var acceleratorOffers = new List<ShopOfferUIData>();
            var goldOffers = new List<ShopOfferUIData>();
            var placements = _offerService.Placements;
            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                var target = placement.Layout switch
                {
                    EShopOfferPlacementLayout.GemGrid => gemOffers,
                    EShopOfferPlacementLayout.EnergyGrid => energyOffers,
                    EShopOfferPlacementLayout.AcceleratorGrid => acceleratorOffers,
                    EShopOfferPlacementLayout.GoldGrid => goldOffers,
                    _ => offers
                };
                var states = _offerService.GetVisibleOffers(placement.Id);
                for (var j = 0; j < states.Count; j++)
                    target.Add(CreateUIData(states[j]));
            }

            _view.SetOffers(offers, EShopOfferPlacementLayout.List);
            _view.SetOffers(gemOffers, EShopOfferPlacementLayout.GemGrid);
            _view.SetOffers(energyOffers, EShopOfferPlacementLayout.EnergyGrid);
            _view.SetOffers(acceleratorOffers, EShopOfferPlacementLayout.AcceleratorGrid);
            _view.SetOffers(goldOffers, EShopOfferPlacementLayout.GoldGrid);
            if (offers.Count == 0 && gemOffers.Count == 0 && energyOffers.Count == 0 && goldOffers.Count == 0 && acceleratorOffers.Count == 0)
                _view.ShowEmptyState(_localizationService.Get(LocalizationKeys.ShopNoOffers));
        }

        public void Tick()
        {
            if (!_isActive()
                || Time.unscaledTime < _nextTimerRefreshAt)
                return;

            Refresh();
        }

        public void Dispose()
        {
            _view.OfferPurchaseClicked -= OnOfferPurchaseClicked;
            _playerStats.OnGoldChanged -= OnGoldChanged;
            _playerStats.UpgradesChanged -= OnUpgradesChanged;
            _localizationService.OnChangeLanguage -= OnLanguageChanged;
            _offerService.StateChanged -= OnOfferStateChanged;
            _offerService.PurchaseSucceeded -= OnOfferPurchaseSucceeded;
        }

        public ShopOfferUIData CreateUIData(ShopOfferState state)
        {
            var config = state.Config;
            var rewards = CreateRewards(config);
            var mainIcon = config.MainIcon ?? ResolveMainIcon(config, rewards);
            var priceIcon = config.Price.Type switch
            {
                EShopOfferPriceType.Gold => _uiElements.GoldIcon,
                EShopOfferPriceType.Gems => _uiElements.GemsIcon,
                EShopOfferPriceType.RewardedAd => _rewardedAdIcon,
                _ => null
            };
            var price = config.Price.Type switch
            {
                EShopOfferPriceType.Gold => config.Price.GoldAmount.ToString(),
                EShopOfferPriceType.Gems => config.Price.GemsAmount.ToString(),
                EShopOfferPriceType.RewardedAd => _localizationService.Get(LocalizationKeys.ShopWatchAd),
                _ => string.IsNullOrWhiteSpace(state.PaymentPrice)
                    ? config.Price.PreviewPrice
                    : state.PaymentPrice
            };
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
                string.IsNullOrWhiteSpace(config.Badge) ? string.Empty : _localizationService.Get(config.Badge),
                config.VisualIconCount,
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
                    case EShopOfferRewardType.Accelerators:
                        result.Add(new ShopOfferRewardUIData(_acceleratorIcon, reward.Amount.ToString()));
                        break;
                    case EShopOfferRewardType.Energy:
                        result.Add(new ShopOfferRewardUIData(_energyIcon, reward.Amount.ToString()));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return result;
        }

        private static Sprite ResolveMainIcon(
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
                EShopOfferBlockReason.InsufficientGems =>
                    _localizationService.Get(LocalizationKeys.ShopOfferInsufficientGems),
                EShopOfferBlockReason.AdUnavailable =>
                    _localizationService.Get(LocalizationKeys.ShopOfferAdUnavailable),
                EShopOfferBlockReason.AcceleratorStorageFull =>
                    _localizationService.Get("shop.accelerator_storage_full"),
                EShopOfferBlockReason.EnergyStorageFull =>
                    _localizationService.Get(LocalizationKeys.ShopOfferEnergyStorageFull),
                _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
            };
        }

        private static string FormatDuration(TimeSpan duration)
        {
            var totalHours = Math.Max(0, (int)Math.Floor(duration.TotalHours));
            return $"{totalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
        }

        private static Sprite LoadSingleSprite(string path)
        {
            var sprites = Resources.LoadAll<Sprite>(path);
            if (sprites.Length != 1)
                throw new MissingReferenceException($"Expected one sprite at Resources/{path}.");

            return sprites[0];
        }

        private void OnLanguageChanged(string _) => Refresh();

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
            if (_isActive())
                Refresh();
        }

        private void OnGoldChanged(int _) => Refresh();
        private void OnUpgradesChanged() => Refresh();
    }
}
