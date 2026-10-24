using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.System.Reward;
using Project.Scripts.System.Save;
using VContainer.Unity;
using YG;

namespace Project.Scripts.System.Shop
{
    public enum EShopOfferBlockReason
    {
        None = 0,
        TimeUnavailable = 1,
        DailyLimitReached = 2,
        InsufficientGold = 3,
        ProductUnavailable = 4,
        PurchaseInProgress = 5,
        InsufficientGems = 6,
        AdUnavailable = 7,
        EnergyStorageFull = 8
    }

    public readonly struct ShopOfferState
    {
        public ShopOfferConfig Config { get; }
        public bool CanPurchase { get; }
        public EShopOfferBlockReason BlockReason { get; }
        public string PaymentPrice { get; }
        public int PurchasesToday { get; }
        public int RemainingPurchases { get; }
        public TimeSpan TimeUntilEnd { get; }
        public TimeSpan TimeUntilReset { get; }

        public ShopOfferState(
            ShopOfferConfig config,
            bool canPurchase,
            EShopOfferBlockReason blockReason,
            string paymentPrice,
            int purchasesToday,
            int remainingPurchases,
            TimeSpan timeUntilEnd,
            TimeSpan timeUntilReset)
        {
            Config = config;
            CanPurchase = canPurchase;
            BlockReason = blockReason;
            PaymentPrice = paymentPrice;
            PurchasesToday = purchasesToday;
            RemainingPurchases = remainingPurchases;
            TimeUntilEnd = timeUntilEnd;
            TimeUntilReset = timeUntilReset;
        }
    }

    public static class ShopOfferTime
    {
        public const long MillisecondsPerDay = 86_400_000;

        public static long GetUtcDay(long unixMilliseconds)
        {
            if (unixMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(unixMilliseconds));

            return unixMilliseconds / MillisecondsPerDay;
        }

        public static TimeSpan TimeUntilNextUtcDay(long unixMilliseconds)
        {
            var nextDay = (GetUtcDay(unixMilliseconds) + 1) * MillisecondsPerDay;
            return TimeSpan.FromMilliseconds(Math.Max(0, nextDay - unixMilliseconds));
        }

        public static bool IsActive(long unixMilliseconds, long start, long end) =>
            unixMilliseconds >= start && unixMilliseconds < end;

        public static long GetPurchaseDay(EShopOfferPriceType priceType, long unixMilliseconds) =>
            priceType == EShopOfferPriceType.RewardedAd
                ? DailyRewardDayCalculator.FromUnixMilliseconds(unixMilliseconds)
                : GetUtcDay(unixMilliseconds);

        public static TimeSpan TimeUntilPurchaseLimitReset(EShopOfferPriceType priceType, long unixMilliseconds) =>
            priceType == EShopOfferPriceType.RewardedAd
                ? DailyRewardDayCalculator.TimeUntilNextMoscowDay(unixMilliseconds)
                : TimeUntilNextUtcDay(unixMilliseconds);
    }

    public sealed class ShopOfferService : IInitializable, IDisposable
    {
        private readonly ShopOfferCatalog _catalog;
        private readonly IWorldService _world;
        private readonly IDailyRewardTimeProvider _timeProvider;
        private readonly PlayerEnergyService _playerEnergy;

        private ShopOfferConfig _pendingPaymentOffer;
        private long _pendingPaymentUtcDay = -1;
        private const string RewardedAdPrefix = "shop_offer_";
        private ShopOfferConfig _pendingRewardedAdOffer;
        private long _pendingRewardedAdDay = -1;

        public IReadOnlyList<ShopOfferPlacementConfig> Placements => _catalog.Placements;

        public event Action StateChanged;
        public event Action<string> PurchaseSucceeded;
        public event Action<string> PurchaseFailed;

        public ShopOfferService(
            ShopOfferCatalog catalog,
            IWorldService world,
            IDailyRewardTimeProvider timeProvider,
            PlayerEnergyService playerEnergy)
        {
            _catalog = catalog;
            _world = world;
            _timeProvider = timeProvider;
            _playerEnergy = playerEnergy;
        }

        public void Initialize()
        {
            YG2.onGetPayments += OnPaymentsLoaded;
            YG2.onPurchaseSuccess += OnPaymentSucceeded;
            YG2.onPurchaseFailed += OnPaymentFailed;
            YG2.onRewardAdv += OnRewardedAdReward;
            YG2.onCloseRewardedAdv += OnRewardedAdClosed;
            YG2.onErrorRewardedAdv += OnRewardedAdError;
            _world.GoldChanged += OnGoldChanged;
            _world.GemsChanged += OnGoldChanged;
            _world.SpellsChanged += OnWorldStateChanged;
            _world.ShopOfferProgressChanged += OnWorldStateChanged;
            RecoverUnconsumedPayments();
        }

        public void Dispose()
        {
            YG2.onGetPayments -= OnPaymentsLoaded;
            YG2.onPurchaseSuccess -= OnPaymentSucceeded;
            YG2.onPurchaseFailed -= OnPaymentFailed;
            YG2.onRewardAdv -= OnRewardedAdReward;
            YG2.onCloseRewardedAdv -= OnRewardedAdClosed;
            YG2.onErrorRewardedAdv -= OnRewardedAdError;
            _world.GoldChanged -= OnGoldChanged;
            _world.GemsChanged -= OnGoldChanged;
            _world.SpellsChanged -= OnWorldStateChanged;
            _world.ShopOfferProgressChanged -= OnWorldStateChanged;
        }

        public List<ShopOfferPlacementConfig> GetVisiblePlacements()
        {
            var result = new List<ShopOfferPlacementConfig>();
            for (var i = 0; i < _catalog.Placements.Count; i++)
            {
                var placement = _catalog.Placements[i];
                if (HasVisibleOffer(placement.Id))
                    result.Add(placement);
            }

            return result;
        }

        public List<ShopOfferState> GetVisibleOffers(string placementId)
        {
            var result = new List<ShopOfferState>();
            var placement = _catalog.GetPlacement(placementId);
            for (var i = 0; i < placement.Offers.Count; i++)
            {
                var config = placement.Offers[i];
                if (TryGetState(config, out var state))
                    result.Add(state);
            }

            return result;
        }

        public ShopOfferPlacementConfig GetPlacement(string placementId) =>
            _catalog.GetPlacement(placementId);

        public bool TryPurchase(string offerId)
        {
            var config = _catalog.GetOffer(offerId);
            if (!TryGetState(config, out var state) || !state.CanPurchase)
                return false;

            if (config.Price.Type == EShopOfferPriceType.RealMoney)
            {
                _pendingPaymentOffer = config;
                _pendingPaymentUtcDay = ResolvePurchaseDay(config);
                StateChanged?.Invoke();
                YG2.BuyPayments(config.Price.ProductId);
                return true;
            }

            if (config.Price.Type == EShopOfferPriceType.RewardedAd)
            {
                _pendingRewardedAdOffer = config;
                _pendingRewardedAdDay = ResolvePurchaseDay(config);
                StateChanged?.Invoke();
                YG2.RewardedAdvShow(RewardedAdPrefix + config.Id);
                return true;
            }

            var currentDay = ResolvePurchaseDay(config);
            var goldPrice = config.Price.Type == EShopOfferPriceType.Gold ? config.Price.GoldAmount : 0;
            var gemsPrice = config.Price.Type == EShopOfferPriceType.Gems ? config.Price.GemsAmount : 0;
            if (!TryApplyPurchase(config, goldPrice, currentDay, gemsPrice))
                return false;

            PurchaseSucceeded?.Invoke(config.Id);
            StateChanged?.Invoke();
            return true;
        }

        private bool HasVisibleOffer(string placementId)
        {
            var placement = _catalog.GetPlacement(placementId);
            for (var i = 0; i < placement.Offers.Count; i++)
            {
                if (TryGetState(placement.Offers[i], out _))
                    return true;
            }

            return false;
        }

        private bool TryGetState(ShopOfferConfig config, out ShopOfferState state)
        {
            state = default;
            if (!config.Enabled || ContainsOwnedSpell(config))
                return false;

            var hasTime = _timeProvider.TryGetCurrentUnixMilliseconds(out var unixMilliseconds)
                          && unixMilliseconds > 0;
            if (config.IsTimed)
            {
                if (!hasTime
                    || !ShopOfferTime.IsActive(
                        unixMilliseconds,
                        config.StartUnixMilliseconds,
                        config.EndUnixMilliseconds))
                    return false;
            }

            var blockReason = EShopOfferBlockReason.None;
            var purchasesToday = 0;
            var remainingPurchases = 0;
            var timeUntilReset = TimeSpan.Zero;
            var timeUntilEnd = config.IsTimed
                ? TimeSpan.FromMilliseconds(
                    Math.Max(0, config.EndUnixMilliseconds - unixMilliseconds))
                : TimeSpan.Zero;

            if (config.DailyPurchaseLimit > 0)
            {
                if (!hasTime)
                {
                    blockReason = EShopOfferBlockReason.TimeUnavailable;
                }
                else
                {
                    var utcDay = ShopOfferTime.GetPurchaseDay(config.Price.Type, unixMilliseconds);
                    purchasesToday = _world.GetShopOfferPurchasesToday(config.Id, utcDay);
                    remainingPurchases = Math.Max(0, config.DailyPurchaseLimit - purchasesToday);
                    timeUntilReset = ShopOfferTime.TimeUntilPurchaseLimitReset(config.Price.Type, unixMilliseconds);
                    if (remainingPurchases == 0)
                        blockReason = EShopOfferBlockReason.DailyLimitReached;
                }
            }

            var paymentPrice = string.Empty;
            if (config.Price.Type == EShopOfferPriceType.RealMoney)
            {
                var product = YG2.PurchaseByID(config.Price.ProductId);
                if (product == null)
                {
                    if (blockReason == EShopOfferBlockReason.None)
                        blockReason = EShopOfferBlockReason.ProductUnavailable;
                }
                else
                {
                    paymentPrice = product.price;
                }

                if (_pendingPaymentOffer != null && blockReason == EShopOfferBlockReason.None)
                    blockReason = EShopOfferBlockReason.PurchaseInProgress;
            }
            else if (config.Price.Type == EShopOfferPriceType.Gold
                     && !_world.CanSpendGold(config.Price.GoldAmount)
                     && blockReason == EShopOfferBlockReason.None)
            {
                blockReason = EShopOfferBlockReason.InsufficientGold;
            }

            else if (config.Price.Type == EShopOfferPriceType.Gems
                     && _world.Gems < config.Price.GemsAmount
                     && blockReason == EShopOfferBlockReason.None)
            {
                blockReason = EShopOfferBlockReason.InsufficientGems;
            }
            else if (config.Price.Type == EShopOfferPriceType.RewardedAd
                     && YG2.nowAdsShow
                     && blockReason == EShopOfferBlockReason.None)
            {
                blockReason = EShopOfferBlockReason.AdUnavailable;
            }

            for (var i = 0; i < config.Rewards.Count; i++)
            {
                if (config.Rewards[i].Type == EShopOfferRewardType.Energy
                    && config.Rewards[i].Amount > int.MaxValue - _world.PlayerEnergy
                    && blockReason == EShopOfferBlockReason.None)
                    blockReason = EShopOfferBlockReason.EnergyStorageFull;
            }

            if ((_pendingPaymentOffer != null || _pendingRewardedAdOffer != null)
                && blockReason == EShopOfferBlockReason.None)
                blockReason = EShopOfferBlockReason.PurchaseInProgress;

            state = new ShopOfferState(
                config,
                blockReason == EShopOfferBlockReason.None,
                blockReason,
                paymentPrice,
                purchasesToday,
                remainingPurchases,
                timeUntilEnd,
                timeUntilReset);
            return true;
        }

        private long ResolvePurchaseDay(ShopOfferConfig config)
        {
            if (config.DailyPurchaseLimit <= 0)
                return -1;

            if (!_timeProvider.TryGetCurrentUnixMilliseconds(out var unixMilliseconds) || unixMilliseconds <= 0)
                return -1;

            return ShopOfferTime.GetPurchaseDay(config.Price.Type, unixMilliseconds);
        }

        private bool TryApplyPurchase(ShopOfferConfig config, int goldPrice, long utcDay, int gemsPrice = 0)
        {
            var rewardGold = 0;
            var rewardGems = 0;
            var rewardEnergy = 0;
            var spellId = string.Empty;

            for (var i = 0; i < config.Rewards.Count; i++)
            {
                var reward = config.Rewards[i];
                switch (reward.Type)
                {
                    case EShopOfferRewardType.Gold:
                        rewardGold = AddClamped(rewardGold, reward.Amount);
                        break;
                    case EShopOfferRewardType.Gems:
                        rewardGems = AddClamped(rewardGems, reward.Amount);
                        break;
                    case EShopOfferRewardType.Spell:
                        spellId = reward.Spell.SpellId;
                        break;
                    case EShopOfferRewardType.Energy:
                        rewardEnergy = AddClamped(rewardEnergy, reward.Amount);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (rewardEnergy > 0)
                _playerEnergy.GetState();

            return _world.TryApplyShopOfferPurchase(
                config.Id,
                goldPrice,
                rewardGold,
                rewardGems,
                spellId,
                utcDay,
                config.DailyPurchaseLimit,
                gemsPrice,
                rewardEnergy,
                config.Price.Type == EShopOfferPriceType.RealMoney && !string.IsNullOrEmpty(spellId));
        }

        private void OnRewardedAdReward(string rewardId)
        {
            if (_pendingRewardedAdOffer == null
                || rewardId != RewardedAdPrefix + _pendingRewardedAdOffer.Id)
                return;

            var config = _pendingRewardedAdOffer;
            var currentDay = ResolvePurchaseDay(config);
            if (currentDay < 0)
                currentDay = _pendingRewardedAdDay;

            _pendingRewardedAdOffer = null;
            _pendingRewardedAdDay = -1;
            if (TryApplyPurchase(config, 0, currentDay))
                PurchaseSucceeded?.Invoke(config.Id);
            else
                PurchaseFailed?.Invoke(config.Id);

            StateChanged?.Invoke();
        }

        private void OnRewardedAdClosed() => CancelRewardedAd();
        private void OnRewardedAdError() => CancelRewardedAd();

        private void CancelRewardedAd()
        {
            if (_pendingRewardedAdOffer == null)
                return;

            var offerId = _pendingRewardedAdOffer.Id;
            _pendingRewardedAdOffer = null;
            _pendingRewardedAdDay = -1;
            PurchaseFailed?.Invoke(offerId);
            StateChanged?.Invoke();
        }

        private bool ContainsOwnedSpell(ShopOfferConfig config)
        {
            for (var i = 0; i < config.Rewards.Count; i++)
            {
                var reward = config.Rewards[i];
                if (reward.Type == EShopOfferRewardType.Spell
                    && _world.IsSpellUnlocked(reward.Spell.SpellId))
                    return true;
            }

            return false;
        }

        private void OnPaymentSucceeded(string productId)
        {
            ShopOfferConfig config;
            long currentDay;
            if (_pendingPaymentOffer != null
                && string.Equals(
                    _pendingPaymentOffer.Price.ProductId,
                    productId,
                    StringComparison.Ordinal))
            {
                config = _pendingPaymentOffer;
                _pendingPaymentOffer = null;
                currentDay = _pendingPaymentUtcDay;
                _pendingPaymentUtcDay = -1;
            }
            else if (!TryFindPaymentOffer(productId, out config))
            {
                return;
            }
            else
            {
                currentDay = -1;
            }

            if (TryApplyPurchase(config, 0, currentDay))
                PurchaseSucceeded?.Invoke(config.Id);
            else
                PurchaseFailed?.Invoke(config.Id);

            StateChanged?.Invoke();
        }

        private void OnPaymentFailed(string productId)
        {
            if (_pendingPaymentOffer == null
                || !string.Equals(
                    _pendingPaymentOffer.Price.ProductId,
                    productId,
                    StringComparison.Ordinal))
                return;

            var offerId = _pendingPaymentOffer.Id;
            _pendingPaymentOffer = null;
            _pendingPaymentUtcDay = -1;
            PurchaseFailed?.Invoke(offerId);
            StateChanged?.Invoke();
        }

        private void OnPaymentsLoaded()
        {
            RecoverUnconsumedPayments();
            StateChanged?.Invoke();
        }

        private bool TryFindPaymentOffer(string productId, out ShopOfferConfig result)
        {
            for (var i = 0; i < _catalog.Offers.Count; i++)
            {
                var offer = _catalog.Offers[i];
                if (offer.Price.Type == EShopOfferPriceType.RealMoney
                    && string.Equals(offer.Price.ProductId, productId, StringComparison.Ordinal))
                {
                    result = offer;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private void RecoverUnconsumedPayments()
        {
#if !UNITY_EDITOR
            for (var i = 0; i < _catalog.Offers.Count; i++)
            {
                var offer = _catalog.Offers[i];
                if (offer.Price.Type != EShopOfferPriceType.RealMoney)
                    continue;

                var product = YG2.PurchaseByID(offer.Price.ProductId);
                if (product != null && !product.consumed)
                    YG2.ConsumePurchaseByID(product.id);
            }
#endif
        }

        private void OnGoldChanged(int _) => StateChanged?.Invoke();
        private void OnWorldStateChanged() => StateChanged?.Invoke();

        private static int AddClamped(int current, int amount) =>
            current > int.MaxValue - amount ? int.MaxValue : current + amount;
    }
}
