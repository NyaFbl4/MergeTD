using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.System.Reward;
using Project.Scripts.System.Reward.RewardConfigs;
using Project.Scripts.System.Save;
using Project.Scripts.System.Shop;
using Project.Scripts.UI.InBattleUI;
using Project.Scripts.UI.ShopUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using YG;

namespace Project.Scripts.Gameplay.Run.Editor
{
    public sealed class RunAcceleratorTests
    {
        private const string SaveKey = "merge_td_world";
        private bool _hadSave;
        private string _save;
        private WorldSaveData _ygWorld;
        private string _ygJson;
        private float _timeScale;
        private WorldService _world;
        private ShopOfferCatalog _catalog;
        private Clock _clock;
        private ShopOfferService _shop;

        [SetUp]
        public void SetUp()
        {
            _hadSave = PlayerPrefs.HasKey(SaveKey);
            _save = PlayerPrefs.GetString(SaveKey);
            _ygWorld = YG2.saves.world;
            _ygJson = YG2.saves.worldJson;
            _timeScale = Time.timeScale;
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            PlayerPrefs.DeleteKey(SaveKey);
            _world = new WorldService(new WorldSaveService());
            _clock = new Clock();
            _catalog = ShopOfferCatalog.LoadFromResources();
            _shop = new ShopOfferService(_catalog, _world, _clock, new PlayerEnergyService(_world, _clock));
        }

        [TearDown]
        public void TearDown()
        {
            YG2.saves.world = _ygWorld;
            YG2.saves.worldJson = _ygJson;
            if (_hadSave) PlayerPrefs.SetString(SaveKey, _save);
            else PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            Time.timeScale = _timeScale;
        }

        [Test]
        public void Catalog_ContainsAgreedRewardsPricesAndDailyLimit()
        {
            var offers = _catalog.GetPlacement("accelerators").Offers;
            Assert.That(offers.Select(o => o.Rewards.Single().Amount), Is.EqualTo(new[] { 1, 1, 5 }));
            Assert.That(offers.All(o => o.Rewards.Single().Type == EShopOfferRewardType.Accelerators), Is.True);
            Assert.That(offers.All(o => o.MainIcon != null), Is.True);
            Assert.That(offers[0].DailyPurchaseLimit, Is.EqualTo(1));
            Assert.That(offers[0].Price.Type, Is.EqualTo(EShopOfferPriceType.RewardedAd));
            Assert.That(offers[1].Price.GemsAmount, Is.EqualTo(20));
            Assert.That(offers[1].DailyPurchaseLimit, Is.Zero);
            Assert.That(offers[2].Price.ProductId, Is.EqualTo("offer_accelerators_5"));
            Assert.That(offers[2].Price.PreviewPrice, Is.EqualTo("200 RUB"));
        }

        [Test]
        public void GemPurchase_GrantsOneAndSavesBothBalancesTogether()
        {
            _world.AddGems(20);
            Assert.That(_shop.TryPurchase("accelerators_gems"), Is.True);
            Assert.That(_world.Gems, Is.Zero);
            Assert.That(_world.Accelerators, Is.EqualTo(1));
            var stored = JsonUtility.FromJson<WorldSaveData>(PlayerPrefs.GetString(SaveKey));
            Assert.That(stored.accelerators, Is.EqualTo(1));
            Assert.That(stored.gems, Is.Zero);
            Assert.That(new WorldService(new WorldSaveService()).Accelerators, Is.EqualTo(1));
        }

        [Test]
        public void GemPurchase_CanRepeatWithoutDailyLimit()
        {
            _world.AddGems(40);
            Assert.That(_shop.TryPurchase("accelerators_gems"), Is.True);
            Assert.That(_shop.TryPurchase("accelerators_gems"), Is.True);
            Assert.That(_world.Accelerators, Is.EqualTo(2));
            Assert.That(_world.Gems, Is.Zero);
        }

        [Test]
        public void InsufficientGems_DoesNotGrantOrSpend()
        {
            _world.AddGems(19);
            Assert.That(_shop.TryPurchase("accelerators_gems"), Is.False);
            Assert.That(_world.Gems, Is.EqualTo(19));
            Assert.That(_world.Accelerators, Is.Zero);
        }

        [Test]
        public void ConfirmedPayment_GrantsFive()
        {
            Callback("OnPaymentSucceeded", "offer_accelerators_5");
            Assert.That(_world.Accelerators, Is.EqualTo(5));
            Assert.That(new WorldService(new WorldSaveService()).Accelerators, Is.EqualTo(5));
        }

        [Test]
        public void FailedPayment_DoesNotGrantAccelerators()
        {
            Pending("_pendingPaymentOffer", _catalog.GetOffer("accelerators_cash"));
            Callback("OnPaymentFailed", "offer_accelerators_5");
            Assert.That(_world.Accelerators, Is.Zero);
        }

        [Test]
        public void CompletedAd_GrantsOneOnlyOnceAndPersistsLimit()
        {
            PendingAd();
            Callback("OnRewardedAdReward", "unrelated");
            Assert.That(_world.Accelerators, Is.Zero);
            Callback("OnRewardedAdReward", "shop_offer_accelerators_ads");
            Callback("OnRewardedAdReward", "shop_offer_accelerators_ads");
            Assert.That(_world.Accelerators, Is.EqualTo(1));
            Assert.That(State().BlockReason, Is.EqualTo(EShopOfferBlockReason.DailyLimitReached));
            var reloaded = new WorldService(new WorldSaveService());
            var shop = new ShopOfferService(_catalog, reloaded, _clock, new PlayerEnergyService(reloaded, _clock));
            Assert.That(shop.GetVisibleOffers("accelerators")[0].CanPurchase, Is.False);
        }

        [TestCase("OnRewardedAdClosed")]
        [TestCase("OnRewardedAdError")]
        public void UnfinishedAd_DoesNotConsumeDailyClaim(string callback)
        {
            PendingAd();
            Callback(callback);
            Callback("OnRewardedAdReward", "shop_offer_accelerators_ads");
            Assert.That(_world.Accelerators, Is.Zero);
            Assert.That(State().CanPurchase, Is.True);
        }

        [Test]
        public void MoscowMidnight_ResetsAdLimit()
        {
            _clock.Now = new DateTimeOffset(2026, 10, 30, 20, 59, 59, TimeSpan.Zero).ToUnixTimeMilliseconds();
            PendingAd();
            Callback("OnRewardedAdReward", "shop_offer_accelerators_ads");
            Assert.That(State().CanPurchase, Is.False);
            _clock.Now += 1000;
            Assert.That(State().CanPurchase, Is.True);
            PendingAd();
            Callback("OnRewardedAdReward", "shop_offer_accelerators_ads");
            Assert.That(_world.Accelerators, Is.EqualTo(2));
        }

        [Test]
        public void MissingServerTime_BlocksDailyAdWithoutBlockingGemPurchase()
        {
            _clock.Available = false;
            Assert.That(State().BlockReason, Is.EqualTo(EShopOfferBlockReason.TimeUnavailable));
            _world.AddGems(20);
            Assert.That(_shop.TryPurchase("accelerators_gems"), Is.True);
        }

        [TestCase(1, 10)]
        [TestCase(2, 20)]
        public void DayFour_GrantsOneBonusWithExistingGemReward(int multiplier, int gems)
        {
            var day = DailyRewardDayCalculator.FromUnixMilliseconds(_clock.Now);
            for (var index = 0; index < 3; index++)
                Assert.That(_world.TryClaimDailyReward(day - 3 + index, index, 1, 0), Is.True);
            var config = Resources.Load<DailyRewards>("Daily Rewards");
            var service = new DailyRewardService(_world, config, _clock);
            Assert.That(service.GetState().AvailableRewardIndex, Is.EqualTo(3));
            Assert.That(service.TryClaim(multiplier), Is.True);
            Assert.That(_world.Accelerators, Is.EqualTo(1));
            Assert.That(_world.Gems, Is.EqualTo(gems));
            Assert.That(service.TryClaim(multiplier), Is.False);
            Assert.That(_world.Accelerators, Is.EqualTo(1));
            Assert.That(config.DailyRewardsConfig.Count(r => r.BonusAccelerators > 0), Is.EqualTo(1));
        }

        [Test]
        public void OldSave_DefaultsAcceleratorsToZeroAndPreservesCurrency()
        {
            YG2.saves.world = JsonUtility.FromJson<WorldSaveData>("{\"version\":2,\"gold\":777,\"gems\":13}");
            var world = new WorldService(new WorldSaveService());
            Assert.That(world.Accelerators, Is.Zero);
            Assert.That(world.Gold, Is.EqualTo(777));
            Assert.That(world.Gems, Is.EqualTo(13));
        }

        [Test]
        public void FullStorage_DoesNotSpendGems()
        {
            YG2.saves.world = JsonUtility.FromJson<WorldSaveData>("{\"version\":2,\"gems\":20,\"accelerators\":2147483647}");
            var world = new WorldService(new WorldSaveService());
            Assert.That(world.TryApplyShopOfferPurchase("test", 0, 0, 0, "", -1, 0,
                gemsPrice: 20, rewardAccelerators: 1), Is.False);
            Assert.That(world.Gems, Is.EqualTo(20));
            Assert.That(world.Accelerators, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void RunEntry_SpendsOnce_PauseKeepsSpeed_ReentrySpendsAgain_EmptyRunsNormally()
        {
            Grant(2);
            var selection = new Selection();
            using var container = BuildManager(selection);
            var manager = container.Resolve<GameManagerService>();
            manager.StartGame();
            Assert.That(_world.Accelerators, Is.EqualTo(1));
            Assert.That(Time.timeScale, Is.EqualTo(1.5f));
            manager.StartGame();
            Assert.That(_world.Accelerators, Is.EqualTo(1));
            manager.PauseGame();
            Assert.That(Time.timeScale, Is.Zero);
            manager.StartGame();
            manager.ResumeGame();
            Assert.That(_world.Accelerators, Is.EqualTo(1));
            Assert.That(Time.timeScale, Is.EqualTo(1.5f));
            manager.FinishGame();
            Assert.That(manager.SpeedMultiplier, Is.EqualTo(1f));
            manager.StartGame();
            Assert.That(_world.Accelerators, Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(1.5f));
            manager.FinishGame();
            manager.StartGame();
            Assert.That(_world.Accelerators, Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void MenuSettingsBeforeRun_DoesNotBlockOrSpendUntilRunEntry()
        {
            Grant(1);
            using var container = BuildManager(new Selection());
            var manager = container.Resolve<GameManagerService>();
            manager.PauseGame();
            manager.ResumeGame();
            Assert.That(_world.Accelerators, Is.EqualTo(1));
            manager.StartGame();
            Assert.That(_world.Accelerators, Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(1.5f));
        }

        [Test]
        public void LockedRun_DoesNotSpendOrChangeSpeed()
        {
            Grant(1);
            using var container = BuildManager(new Selection { Unlocked = false });
            var manager = container.Resolve<GameManagerService>();
            Time.timeScale = 0;
            manager.StartGame();
            Assert.That(_world.Accelerators, Is.EqualTo(1));
            Assert.That(Time.timeScale, Is.Zero);
        }

        [Test]
        public void Reset_ClearsAccelerators()
        {
            Grant(5);
            _world.Reset();
            Assert.That(_world.Accelerators, Is.Zero);
            Assert.That(new WorldService(new WorldSaveService()).Accelerators, Is.Zero);
        }

        [Test]
        public void UI_ImportsBonusInventoryAndReusableShopCard()
        {
            var tree = Resources.Load<VisualTreeAsset>("UIToolkit/New/InBattlePanel");
            var root = tree.Instantiate();
            Assert.That(root.Q<Label>("AcceleratorsLabel"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("AcceleratorRewardBonus"), Is.Not.Null);
            using var view = new InBattleUIView(root);
            var rewards = Resources.Load<DailyRewards>("Daily Rewards").DailyRewardsConfig;
            view.ShowDailyRewardOffer(rewards[3]);
            Assert.That(root.Q<VisualElement>("DailyRewardAcceleratorBonus").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<Label>("DailyRewardAcceleratorBonusAmount").text, Is.EqualTo("1"));
            var ui = AssetDatabase.LoadAssetAtPath<UIElements>("Assets/Project/Configs/UI/UIElements.asset");
            Assert.That(_catalog.GetPlacement("accelerators").Layout, Is.EqualTo(EShopOfferPlacementLayout.AcceleratorGrid));
            var card = ui.ShopAcceleratorOfferCard.Instantiate();
            var offer = _catalog.GetOffer("accelerators_gems");
            new ShopAcceleratorOfferItemView(card, _ => Assert.Fail("Binding must not purchase.")).Bind(
                new ShopOfferUIData(offer.Id, "1 ускоритель", "×1.5", offer.MainIcon,
                    new[] { new ShopOfferRewardUIData(offer.MainIcon, "1") }, ui.GemsIcon, "20", "", 1, "", "", "", true));
            Assert.That(card.Q<Label>("PriceLabel").text, Is.EqualTo("20"));
            Assert.That(card.Q<Label>("AmountLabel").text, Is.EqualTo("+1"));
            Assert.That(card.Q<VisualElement>("AcceleratorIconPrimary").style.backgroundImage.value.sprite,
                Is.SameAs(offer.MainIcon));
            Assert.That(AssetDatabase.GetAssetPath(offer.MainIcon),
                Is.EqualTo("Assets/Project/Resources/UI/new/Icons/Icon_Stopwatch.png"));
            foreach (var path in new[] { "UIToolkit/New/ShopPanel", "UIToolkit/ShopUI" })
            {
                var shopRoot = Resources.Load<VisualTreeAsset>(path).Instantiate();
                var sections = shopRoot.Q<VisualElement>("EnergyOffersPanel").parent;
                var acceleratorPanel = shopRoot.Q<VisualElement>("AcceleratorOffersPanel");
                Assert.That(sections.IndexOf(acceleratorPanel),
                    Is.EqualTo(sections.IndexOf(shopRoot.Q<VisualElement>("EnergyOffersPanel")) + 1));
                var shopView = new ShopPanelUIView(shopRoot, null, ui);
                try
                {
                    var data = new ShopOfferUIData(offer.Id, "1 ускоритель", "", offer.MainIcon,
                        new[] { new ShopOfferRewardUIData(offer.MainIcon, "1") }, ui.GemsIcon, "20", "", 1, "", "", "", true);
                    shopView.SetOffers(new[] { data }, EShopOfferPlacementLayout.AcceleratorGrid);
                    Assert.That(shopRoot.Q<VisualElement>("AcceleratorContainer").childCount, Is.EqualTo(1));
                    Assert.That(shopRoot.Q<VisualElement>("OfferContainer").childCount, Is.Zero);
                    Assert.That(shopRoot.Q<VisualElement>("EnergyContainer").childCount, Is.Zero);
                    shopView.SetOffers(Array.Empty<ShopOfferUIData>(), EShopOfferPlacementLayout.AcceleratorGrid);
                    Assert.That(acceleratorPanel.style.display.value, Is.EqualTo(DisplayStyle.None));
                }
                finally
                {
                    shopView.Dispose();
                }
            }
        }

        private void Grant(int amount) => Assert.That(_world.TryApplyShopOfferPurchase("test", 0, 0, 0, "", -1, 0,
            rewardAccelerators: amount), Is.True);
        private IObjectResolver BuildManager(Selection selection)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance<IRunSelectionService>(selection);
            builder.RegisterInstance<IWorldService>(_world);
            builder.Register<GameManagerService>(Lifetime.Singleton);
            return builder.Build();
        }
        private ShopOfferState State() => _shop.GetVisibleOffers("accelerators")[0];
        private void PendingAd()
        {
            Pending("_pendingRewardedAdOffer", _catalog.GetOffer("accelerators_ads"));
            Pending("_pendingRewardedAdDay", ShopOfferTime.GetPurchaseDay(EShopOfferPriceType.RewardedAd, _clock.Now));
        }
        private void Pending(string field, object value) => typeof(ShopOfferService)
            .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_shop, value);
        private void Callback(string method, params object[] args) => typeof(ShopOfferService)
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_shop, args);
        private sealed class Clock : IDailyRewardTimeProvider
        {
            public long Now = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
            public bool Available = true;
            public bool TryGetCurrentUnixMilliseconds(out long value) { value = Now; return Available; }
        }
        private sealed class Selection : IRunSelectionService
        {
            public bool Unlocked = true;
            public int Count => 1;
            public int SelectedIndex => 0;
            public RunConfig SelectedRun => null;
            public RunConfig PreviousRun => null;
            public bool IsSelectedRunUnlocked => Unlocked;
            public event Action<RunConfig> SelectionChanged { add { } remove { } }
            public void SelectNext() { }
            public void SelectPrevious() { }
        }
    }
}
