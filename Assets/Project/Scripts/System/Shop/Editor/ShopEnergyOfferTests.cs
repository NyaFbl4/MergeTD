using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Project.Scripts.Configs;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.System.Reward;
using Project.Scripts.System.Save;
using Project.Scripts.UI.ShopUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using YG;

namespace Project.Scripts.System.Shop.Editor
{
    public sealed class ShopEnergyOfferTests
    {
        private const string SaveKey = "merge_td_world";
        private bool _hadLocalSave;
        private string _localSave;
        private WorldSaveData _originalWorld;
        private string _originalWorldJson;
        private WorldService _world;
        private TestClock _clock;
        private ShopOfferCatalog _catalog;
        private ShopOfferService _service;
        private PlayerEnergyService _energy;

        [SetUp]
        public void SetUp()
        {
            _hadLocalSave = PlayerPrefs.HasKey(SaveKey);
            _localSave = PlayerPrefs.GetString(SaveKey);
            _originalWorld = YG2.saves.world;
            _originalWorldJson = YG2.saves.worldJson;
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            PlayerPrefs.DeleteKey(SaveKey);
            _world = new WorldService(new WorldSaveService());
            _clock = new TestClock
            {
                Now = new DateTimeOffset(2026, 10, 23, 10, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                Available = true
            };
            _catalog = ShopOfferCatalog.LoadFromResources();
            _energy = new PlayerEnergyService(_world, _clock);
            _service = new ShopOfferService(_catalog, _world, _clock, _energy);
        }

        [TearDown]
        public void TearDown()
        {
            YG2.saves.world = _originalWorld;
            YG2.saves.worldJson = _originalWorldJson;
            if (_hadLocalSave)
                PlayerPrefs.SetString(SaveKey, _localSave);
            else
                PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void EnergyCatalog_ContainsThreeAgreedOffersWithValidIcons()
        {
            var offers = _catalog.GetPlacement("energy").Offers;
            Assert.That(offers.Count, Is.EqualTo(3));
            Assert.That(offers.Select(o => o.Price.Type), Is.EqualTo(new[]
            {
                EShopOfferPriceType.RewardedAd, EShopOfferPriceType.Gems, EShopOfferPriceType.RealMoney
            }));
            Assert.That(offers.Select(o => o.Rewards[0].Amount), Is.EqualTo(new[] { 2, 5, 20 }));
            Assert.That(offers.All(o => o.MainIcon != null), Is.True);
            Assert.That(offers[0].DailyPurchaseLimit, Is.EqualTo(1));
            Assert.That(offers[1].Price.GemsAmount, Is.EqualTo(40));
            Assert.That(offers[2].Price.ProductId, Is.EqualTo("offer_energy_20"));
        }

        [Test]
        public void GemPurchase_DebitsFortyAndGrantsFiveAboveCapacity()
        {
            _world.AddGems(40);
            Assert.That(_service.TryPurchase("energy_gems_5"), Is.True);
            Assert.That(_world.Gems, Is.Zero);
            Assert.That(_world.PlayerEnergy, Is.EqualTo(15));
            var stored = JsonUtility.FromJson<WorldSaveData>(PlayerPrefs.GetString(SaveKey));
            Assert.That(stored.gems, Is.Zero);
            Assert.That(stored.playerEnergy.current, Is.EqualTo(15));
        }

        [Test]
        public void InsufficientGems_ChangesNeitherBalanceNorEnergy()
        {
            _world.AddGems(39);
            Assert.That(_service.TryPurchase("energy_gems_5"), Is.False);
            Assert.That(_world.Gems, Is.EqualTo(39));
            Assert.That(_world.PlayerEnergy, Is.EqualTo(10));
            Assert.That(State("energy_gems_5").BlockReason, Is.EqualTo(EShopOfferBlockReason.InsufficientGems));
        }

        [Test]
        public void RepeatedGemPurchases_KeepAllPurchasedEnergy()
        {
            _world.AddGems(80);
            Assert.That(_service.TryPurchase("energy_gems_5"), Is.True);
            Assert.That(_service.TryPurchase("energy_gems_5"), Is.True);
            Assert.That(_world.PlayerEnergy, Is.EqualTo(20));
            Assert.That(_world.Gems, Is.Zero);
        }

        [Test]
        public void RecoveryIsAppliedBeforeAddingPurchasedEnergy()
        {
            _world.SavePlayerEnergy(4, _clock.Now - 5 * PlayerEnergyService.RestoreIntervalMilliseconds);
            _world.AddGems(40);
            Assert.That(_service.TryPurchase("energy_gems_5"), Is.True);
            Assert.That(_world.PlayerEnergy, Is.EqualTo(15));
            Assert.That(_world.PlayerEnergyNextRestoreUnixMilliseconds, Is.Zero);
        }

        [Test]
        public void PurchaseBelowCapacity_PreservesPartialRecoveryHour()
        {
            var deadline = _clock.Now + 1_800_000;
            _world.SavePlayerEnergy(0, deadline);
            _world.AddGems(40);
            _service.TryPurchase("energy_gems_5");
            Assert.That(_world.PlayerEnergy, Is.EqualTo(5));
            Assert.That(_world.PlayerEnergyNextRestoreUnixMilliseconds, Is.EqualTo(deadline));
        }

        [Test]
        public void CompletedRewardedAd_GrantsTwoExactlyOnce()
        {
            PendingAd();
            Invoke("OnRewardedAdReward", "shop_offer_energy_ad_2");
            Invoke("OnRewardedAdReward", "shop_offer_energy_ad_2");
            Assert.That(_world.PlayerEnergy, Is.EqualTo(12));
            Assert.That(State("energy_ad_2").BlockReason, Is.EqualTo(EShopOfferBlockReason.DailyLimitReached));
        }

        [Test]
        public void UnrelatedAdReward_DoesNotGrantEnergyOrConsumeDailyClaim()
        {
            PendingAd();
            Invoke("OnRewardedAdReward", "daily_reward_double");
            Assert.That(_world.PlayerEnergy, Is.EqualTo(10));
            Assert.That(ClaimsToday(), Is.Zero);
            Invoke("OnRewardedAdClosed");
        }

        [TestCase("OnRewardedAdClosed")]
        [TestCase("OnRewardedAdError")]
        public void UnfinishedAd_DoesNotConsumeRewardOrLimit(string callback)
        {
            PendingAd();
            Invoke(callback);
            Invoke("OnRewardedAdReward", "shop_offer_energy_ad_2");
            Assert.That(_world.PlayerEnergy, Is.EqualTo(10));
            Assert.That(ClaimsToday(), Is.Zero);
        }

        [Test]
        public void DuplicateAdAttempt_CannotBypassPersistedDailyLimit()
        {
            PendingAd();
            Invoke("OnRewardedAdReward", "shop_offer_energy_ad_2");
            PendingAd();
            Invoke("OnRewardedAdReward", "shop_offer_energy_ad_2");
            Assert.That(_world.PlayerEnergy, Is.EqualTo(12));
            Assert.That(ClaimsToday(), Is.EqualTo(1));
        }

        [Test]
        public void DailyAdLimit_ResetsAtMoscowMidnight()
        {
            _clock.Now = new DateTimeOffset(2026, 10, 23, 20, 59, 59, TimeSpan.Zero).ToUnixTimeMilliseconds();
            PendingAd();
            Invoke("OnRewardedAdReward", "shop_offer_energy_ad_2");
            Assert.That(State("energy_ad_2").TimeUntilReset, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(State("energy_ad_2").CanPurchase, Is.False);
            _clock.Now += 1000;
            Assert.That(State("energy_ad_2").CanPurchase, Is.True);
            PendingAd();
            Invoke("OnRewardedAdReward", "shop_offer_energy_ad_2");
            Assert.That(_world.PlayerEnergy, Is.EqualTo(14));
        }

        [Test]
        public void RewardAfterMidnight_UsesTheClaimDay()
        {
            _clock.Now = new DateTimeOffset(2026, 10, 23, 20, 59, 59, TimeSpan.Zero).ToUnixTimeMilliseconds();
            PendingAd();
            _clock.Now += 1000;
            Invoke("OnRewardedAdReward", "shop_offer_energy_ad_2");
            Assert.That(ClaimsToday(), Is.EqualTo(1));
            Assert.That(State("energy_ad_2").CanPurchase, Is.False);
        }

        [Test]
        public void MissingServerTime_BlocksDailyAd()
        {
            _clock.Available = false;
            Assert.That(State("energy_ad_2").BlockReason, Is.EqualTo(EShopOfferBlockReason.TimeUnavailable));
            Assert.That(_service.TryPurchase("energy_ad_2"), Is.False);
        }

        [Test]
        public void TemporaryClockLossDuringAd_StillGrantsTheEarnedReward()
        {
            PendingAd();
            _clock.Available = false;
            Invoke("OnRewardedAdReward", "shop_offer_energy_ad_2");
            _clock.Available = true;
            Assert.That(_world.PlayerEnergy, Is.EqualTo(12));
            Assert.That(ClaimsToday(), Is.EqualTo(1));
        }

        [Test]
        public void DailyAdLimit_SurvivesSaveReload()
        {
            PendingAd();
            Invoke("OnRewardedAdReward", "shop_offer_energy_ad_2");
            var reloaded = new WorldService(new WorldSaveService());
            var restored = new ShopOfferService(_catalog, reloaded, _clock, new PlayerEnergyService(reloaded, _clock));
            Assert.That(restored.GetVisibleOffers("energy")[0].CanPurchase, Is.False);
            Assert.That(reloaded.PlayerEnergy, Is.EqualTo(12));
        }

        [Test]
        public void ConfirmedPayment_GrantsTwentyWithoutSpendingGems()
        {
            Invoke("OnPaymentSucceeded", "offer_energy_20");
            Assert.That(_world.PlayerEnergy, Is.EqualTo(30));
            Assert.That(_world.Gems, Is.Zero);
        }

        [Test]
        public void FailedPayment_DoesNotGrantEnergy()
        {
            SetPending("_pendingPaymentOffer", _catalog.GetOffer("energy_cash_20"));
            Invoke("OnPaymentFailed", "offer_energy_20");
            Assert.That(_world.PlayerEnergy, Is.EqualTo(10));
        }

        [Test]
        public void EnergyCard_BindsAmountPriceAndDisabledState()
        {
            var uiElements = AssetDatabase.LoadAssetAtPath<UIElements>("Assets/Project/Configs/UI/UIElements.asset");
            var root = uiElements.ShopEnergyOfferCard.Instantiate();
            var view = new ShopEnergyOfferItemView(root, _ => { });
            var config = _catalog.GetOffer("energy_gems_5");
            view.Bind(new ShopOfferUIData(config.Id, "Запас энергии", string.Empty, config.MainIcon,
                new[] { new ShopOfferRewardUIData(config.MainIcon, "5") }, null, "40", string.Empty,
                2, string.Empty, string.Empty, "Недостаточно алмазов", false));
            Assert.That(root.Q<Label>("AmountLabel").text, Is.EqualTo("+5"));
            Assert.That(root.Q<Label>("PriceLabel").text, Is.EqualTo("40"));
            Assert.That(root.Q<Button>("BuyButton").enabledSelf, Is.False);
        }

        private ShopOfferState State(string id) => _service.GetVisibleOffers("energy").Single(s => s.Config.Id == id);
        private int ClaimsToday() => _world.GetShopOfferPurchasesToday("energy_ad_2",
            ShopOfferTime.GetPurchaseDay(EShopOfferPriceType.RewardedAd, _clock.Now));

        private void PendingAd()
        {
            SetPending("_pendingRewardedAdOffer", _catalog.GetOffer("energy_ad_2"));
            SetPending("_pendingRewardedAdDay", ShopOfferTime.GetPurchaseDay(EShopOfferPriceType.RewardedAd, _clock.Now));
        }

        private void SetPending(string name, object value) =>
            typeof(ShopOfferService).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_service, value);

        private void Invoke(string name, params object[] args) =>
            typeof(ShopOfferService).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_service, args);

        private sealed class TestClock : IDailyRewardTimeProvider
        {
            public long Now;
            public bool Available;
            public bool TryGetCurrentUnixMilliseconds(out long unixMilliseconds)
            {
                unixMilliseconds = Available ? Now : 0;
                return Available;
            }
        }
    }
}
