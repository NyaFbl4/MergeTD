using System;
using System.Linq;
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
    public sealed class ShopGoldOfferTests
    {
        private const string SaveKey = "merge_td_world";
        private bool _hadLocalSave;
        private string _localSave;
        private WorldSaveData _originalWorld;
        private string _originalWorldJson;
        private WorldService _world;
        private ShopOfferCatalog _catalog;
        private ShopOfferService _service;
        private Clock _clock;

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
            _catalog = ShopOfferCatalog.LoadFromResources();
            _clock = new Clock();
            _service = new ShopOfferService(_catalog, _world, _clock, new PlayerEnergyService(_world, _clock));
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
        public void GoldCatalog_ContainsExactlyThreeAgreedPacks()
        {
            var section = _catalog.GetPlacement("gold");
            Assert.That(section.Layout, Is.EqualTo(EShopOfferPlacementLayout.GoldGrid));
            Assert.That(section.Title, Is.EqualTo("shop.gold_tab"));
            Assert.That(section.Offers.Select(o => o.Id), Is.EqualTo(new[] { "gold_1000", "gold_5000", "gold_15000" }));
            Assert.That(section.Offers.Select(o => o.Rewards.Single().Amount), Is.EqualTo(new[] { 1000, 5000, 15000 }));
            Assert.That(section.Offers.Select(o => o.Price.GemsAmount), Is.EqualTo(new[] { 50, 200, 500 }));
            Assert.That(section.Offers.All(o => o.Enabled && !o.IsTimed && o.DailyPurchaseLimit == 0), Is.True);
            Assert.That(section.Offers.All(o => o.Price.Type == EShopOfferPriceType.Gems), Is.True);
            Assert.That(section.Offers.All(o => o.Rewards.Single().Type == EShopOfferRewardType.Gold), Is.True);
        }

        [TestCase("gold_1000", 1000, 50)]
        [TestCase("gold_5000", 5000, 200)]
        [TestCase("gold_15000", 15000, 500)]
        public void Purchase_AtomicallyDebitsGemsGrantsGoldAndPersists(string id, int gold, int gems)
        {
            _world.AddGems(gems);
            var initialGold = _world.Gold;
            var energy = _world.PlayerEnergy;
            _world.GemsChanged += balance =>
            {
                Assert.That(balance, Is.Zero);
                Assert.That(_world.Gold, Is.EqualTo(initialGold + gold));
                var saved = JsonUtility.FromJson<WorldSaveData>(PlayerPrefs.GetString(SaveKey));
                Assert.That(saved.gold, Is.EqualTo(initialGold + gold));
                Assert.That(saved.gems, Is.Zero);
            };

            Assert.That(_service.TryPurchase(id), Is.True);
            Assert.That(_world.Gold, Is.EqualTo(initialGold + gold));
            Assert.That(_world.Gems, Is.Zero);
            Assert.That(_world.PlayerEnergy, Is.EqualTo(energy));
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            var reloaded = new WorldService(new WorldSaveService());
            Assert.That(reloaded.Gold, Is.EqualTo(initialGold + gold));
            Assert.That(reloaded.Gems, Is.Zero);
        }

        [TestCase("gold_1000", 50)]
        [TestCase("gold_5000", 200)]
        [TestCase("gold_15000", 500)]
        public void InsufficientGems_LeavesBothCurrenciesAndSaveUnchanged(string id, int price)
        {
            _world.AddGems(price - 1);
            var before = PlayerPrefs.GetString(SaveKey);
            var gold = _world.Gold;
            var offer = _service.GetVisibleOffers("gold").Single(o => o.Config.Id == id);
            Assert.That(offer.CanPurchase, Is.False);
            Assert.That(offer.BlockReason, Is.EqualTo(EShopOfferBlockReason.InsufficientGems));
            Assert.That(_service.TryPurchase(id), Is.False);
            Assert.That(_world.Gold, Is.EqualTo(gold));
            Assert.That(_world.Gems, Is.EqualTo(price - 1));
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(before));
        }

        [TestCase("gold_1000", 1000, 50)]
        [TestCase("gold_5000", 5000, 200)]
        [TestCase("gold_15000", 15000, 500)]
        public void RepeatPurchase_ChargesEveryTimeAndStopsWhenGemsRunOut(string id, int gold, int price)
        {
            _world.AddGems(price * 2);
            var initialGold = _world.Gold;
            Assert.That(_service.TryPurchase(id), Is.True);
            Assert.That(_service.TryPurchase(id), Is.True);
            Assert.That(_service.TryPurchase(id), Is.False);
            Assert.That(_world.Gold, Is.EqualTo(initialGold + gold * 2));
            Assert.That(_world.Gems, Is.Zero);
        }

        [Test]
        public void GoldExchange_DoesNotRequireServerTimeOrPaymentProvider()
        {
            _clock.Available = false;
            _world.AddGems(50);
            Assert.That(_service.GetVisibleOffers("gold").Count, Is.EqualTo(3));
            Assert.That(_service.TryPurchase("gold_1000"), Is.True);
            Assert.That(_world.Gems, Is.Zero);
        }

        [TestCase("gold_1000", "ShopItem_Coin_2")]
        [TestCase("gold_5000", "ShopItem_Coin_4")]
        [TestCase("gold_15000", "ShopItem_Coin_5")]
        public void CardTemplate_BindsCorrectGoldIconGemPriceAndAvailability(string id, string iconName)
        {
            var offer = _catalog.GetOffer(id);
            Assert.That(AssetDatabase.GetAssetPath(offer.MainIcon),
                Is.EqualTo("Assets/Project/Resources/UI/new/Icons/Shop/" + iconName + ".png"));
            var ui = AssetDatabase.LoadAssetAtPath<UIElements>("Assets/Project/Configs/UI/UIElements.asset");
            Assert.That(ui.ShopGoldOfferCard, Is.Not.Null);
            var root = ui.ShopGoldOfferCard.Instantiate();
            var view = new ShopGoldOfferItemView(root, _ => Assert.Fail("Binding cannot trigger a purchase."));
            foreach (var enabled in new[] { false, true })
            {
                view.Bind(new ShopOfferUIData(id, offer.Title, string.Empty, offer.MainIcon,
                    new[] { new ShopOfferRewardUIData(ui.GoldIcon, offer.Rewards.Single().Amount.ToString()) },
                    ui.GemsIcon, offer.Price.GemsAmount.ToString(), offer.Badge, 1,
                    string.Empty, string.Empty, string.Empty, enabled));
                Assert.That(root.Q<VisualElement>("GoldIcon").style.backgroundImage.value.sprite, Is.SameAs(offer.MainIcon));
                Assert.That(root.Q<VisualElement>("PriceIcon").style.backgroundImage.value.sprite, Is.SameAs(ui.GemsIcon));
                Assert.That(root.Q<Label>("AmountLabel").text, Is.EqualTo("×" + offer.Rewards.Single().Amount));
                Assert.That(root.Q<Label>("PriceLabel").text, Is.EqualTo(offer.Price.GemsAmount.ToString()));
                Assert.That(root.Q<Button>("BuyButton").enabledSelf, Is.EqualTo(enabled));
            }
        }

        [Test]
        public void BothShopSurfaces_ContainGoldContainersAndExistingPacksStayAvailable()
        {
            foreach (var path in new[] { "UIToolkit/New/ShopPanel", "UIToolkit/ShopUI" })
            {
                var root = Resources.Load<VisualTreeAsset>(path).Instantiate();
                Assert.That(root.Q<VisualElement>("GoldContainer"), Is.Not.Null, path);
                Assert.That(root.Q<Label>("GoldSectionLabel"), Is.Not.Null, path);
            }
            Assert.That(_service.GetVisibleOffers("gems").Select(o => o.Config.Id),
                Is.EqualTo(new[] { "gems_80", "gems_500", "gems_1200" }));
            Assert.That(_catalog.GetPlacement("energy").Offers.Count, Is.EqualTo(3));
            Assert.That(_catalog.GetOffer("offer_gold_3000").Price.Type, Is.EqualTo(EShopOfferPriceType.RealMoney));
        }

        [TestCase(2, 1)]
        [TestCase(3, 0)]
        public void Validation_StillRejectsGemRewardsForGemsAndGoldForAds(int priceType, int rewardType)
        {
            var config = ScriptableObject.CreateInstance<ShopOfferConfig>();
            try
            {
                JsonUtility.FromJsonOverwrite(
                    "{\"_id\":\"invalid_exchange\",\"_title\":\"test\",\"_dailyPurchaseLimit\":1,"
                    + "\"_price\":{\"_type\":" + priceType + ",\"_gemsAmount\":50},"
                    + "\"_rewards\":[{\"_type\":" + rewardType + ",\"_amount\":100}]}", config);
                Assert.Throws<InvalidOperationException>(() => config.Validate());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private sealed class Clock : IDailyRewardTimeProvider
        {
            public bool Available = true;
            public bool TryGetCurrentUnixMilliseconds(out long milliseconds)
            {
                milliseconds = 1792832400000;
                return Available;
            }
        }
    }
}
