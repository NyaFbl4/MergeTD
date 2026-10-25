using System;
using System.Linq;
using NUnit.Framework;
using Project.Scripts.Configs;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.UI.TowerSlotUnlockUI;
using UnityEngine;
using YG;

namespace Project.Scripts.System.Save.Editor
{
    public sealed class TowerSlotUnlockTests
    {
        private const string SaveKey = "merge_td_world";
        private bool _hadLocal;
        private string _local;
        private WorldSaveData _originalWorld;
        private string _originalWorldJson;

        [SetUp]
        public void SetUp()
        {
            _hadLocal = PlayerPrefs.HasKey(SaveKey);
            _local = PlayerPrefs.GetString(SaveKey);
            _originalWorld = YG2.saves.world;
            _originalWorldJson = YG2.saves.worldJson;
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [TearDown]
        public void TearDown()
        {
            YG2.saves.world = _originalWorld;
            YG2.saves.worldJson = _originalWorldJson;
            if (_hadLocal) PlayerPrefs.SetString(SaveKey, _local);
            else PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        private static WorldService World(int gold)
        {
            var world = new WorldService(new WorldSaveService());
            if (world.Gold > gold) world.TrySpendGold(world.Gold - gold);
            else world.AddGold(gold - world.Gold);
            return world;
        }

        [Test]
        public void SequentialPurchases_ChargeConfiguredPricesAndPersist()
        {
            var world = World(18250);
            var config = TowerSlotUnlockConfig.LoadFromResources();
            var expected = new[] { 750, 1500, 3000, 5000, 8000 };
            for (var i = 0; i < 5; i++)
            {
                var id = "3" + (i + 1);
                Assert.That(config.GetPrice(id), Is.EqualTo(expected[i]));
                Assert.That(world.TryPurchaseTowerSlot(id, expected[i]), Is.True);
                Assert.That(world.IsTowerSlotUnlocked(id), Is.True);
            }
            Assert.That(world.Gold, Is.Zero);
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            var reloaded = new WorldService(new WorldSaveService());
            Assert.That(reloaded.Gold, Is.Zero);
            Assert.That(reloaded.TowerSlots.All(s => s.unlockedSlot == WorldTowerSlotSaveData.Unlocked), Is.True);
            var disk = JsonUtility.FromJson<WorldSaveData>(PlayerPrefs.GetString(SaveKey));
            Assert.That(disk.gold, Is.Zero);
            Assert.That(disk.towerSlots.All(s => s.unlockedSlot == WorldTowerSlotSaveData.Unlocked), Is.True);
        }

        [TestCase(0)]
        [TestCase(500)]
        [TestCase(749)]
        public void InsufficientGold_DoesNotChangeWalletOrSlot(int gold)
        {
            var world = World(gold);
            Assert.That(world.TryPurchaseTowerSlot("31", 750), Is.False);
            Assert.That(world.Gold, Is.EqualTo(gold));
            Assert.That(world.IsTowerSlotUnlocked("31"), Is.False);
        }

        [Test]
        public void ExactPrice_OpensOnce_AndDuplicateDoesNotChargeAgain()
        {
            var world = World(1500);
            Assert.That(world.TryPurchaseTowerSlot("31", 750), Is.True);
            Assert.That(world.TryPurchaseTowerSlot("31", 750), Is.False);
            Assert.That(world.Gold, Is.EqualTo(750));
        }

        [TestCase("32", 1500)]
        [TestCase("33", 3000)]
        [TestCase("34", 5000)]
        [TestCase("35", 8000)]
        public void OutOfOrderPurchase_IsRejected(string id, int price)
        {
            var world = World(20000);
            Assert.That(world.TryPurchaseTowerSlot(id, price), Is.False);
            Assert.That(world.Gold, Is.EqualTo(20000));
            Assert.That(world.IsTowerSlotUnlocked(id), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("11")]
        [TestCase("36")]
        [TestCase("99")]
        public void InvalidOrNonExpansionSlot_IsRejectedWithoutCharge(string id)
        {
            var world = World(20000);
            Assert.That(world.TryPurchaseTowerSlot(id, 750), Is.False);
            Assert.That(world.Gold, Is.EqualTo(20000));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(749)]
        [TestCase(751)]
        public void StaleOrForgedPrice_IsRejected(int price)
        {
            var world = World(20000);
            Assert.That(world.TryPurchaseTowerSlot("31", price), Is.False);
            Assert.That(world.Gold, Is.EqualTo(20000));
            Assert.That(world.IsTowerSlotUnlocked("31"), Is.False);
        }

        [Test]
        public void NotificationsAndDisk_AlwaysSeeWalletAndUnlockTogether()
        {
            var world = World(750);
            var goldEvents = 0;
            var slotEvents = 0;
            Action verify = () =>
            {
                Assert.That(world.Gold, Is.Zero);
                Assert.That(world.IsTowerSlotUnlocked("31"), Is.True);
                var saved = JsonUtility.FromJson<WorldSaveData>(PlayerPrefs.GetString(SaveKey));
                Assert.That(saved.gold, Is.Zero);
                Assert.That(saved.towerSlots.Single(s => s.slotId == "31").unlockedSlot,
                    Is.EqualTo(WorldTowerSlotSaveData.Unlocked));
            };
            world.GoldChanged += _ => { goldEvents++; verify(); };
            world.TowerSlotsChanged += () => { slotEvents++; verify(); };
            Assert.That(world.TryPurchaseTowerSlot("31", 750), Is.True);
            Assert.That(goldEvents, Is.EqualTo(1));
            Assert.That(slotEvents, Is.EqualTo(1));
        }

        [Test]
        public void ExistingRightSlot_RemainsOpen_AndLeftSlotKeepsItsPrice()
        {
            var world = World(10000);
            world.UnlockTowerSlot("35");
            world = new WorldService(new WorldSaveService());
            Assert.That(world.TryPurchaseTowerSlot("31", 750), Is.True);
            Assert.That(world.Gold, Is.EqualTo(9250));
            Assert.That(world.IsTowerSlotUnlocked("35"), Is.True);
            Assert.That(world.TryPurchaseTowerSlot("35", 8000), Is.False);
        }

        [Test]
        public void FullReset_RestoresLocksAndStartingWallet()
        {
            var world = World(750);
            world.TryPurchaseTowerSlot("31", 750);
            world.Reset();
            Assert.That(world.Gold, Is.EqualTo(500));
            Assert.That(world.IsTowerSlotUnlocked("31"), Is.False);
        }

        [Test]
        public void Window_LoadsEditableUxmlWithoutExternalStyleSheets()
        {
            var tree = Resources.Load<UnityEngine.UIElements.VisualTreeAsset>("UIToolkit/New/TowerSlotUnlockWindow");
            Assert.That(tree, Is.Not.Null);
            Assert.That(UnityEditor.AssetDatabase.GetDependencies(UnityEditor.AssetDatabase.GetAssetPath(tree), false)
                .Any(path => path.EndsWith(".uss", StringComparison.OrdinalIgnoreCase)), Is.False);
            var root = tree.CloneTree();
            Assert.That(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root, "TowerSlotUnlockWindow"), Is.Not.Null);
            Assert.That(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(root, "SlotUnlockBlockedStatus"), Is.Not.Null);
            Assert.That(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root, "SlotUnlockPurchaseBlocked"), Is.Not.Null);
            Assert.That(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root, "SlotUnlockPurchase"), Is.Not.Null);
            Assert.That(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root, "SlotUnlockClose"), Is.Not.Null);
        }

        [Test]
        public void Presenter_RechecksPhaseBeforeCharging()
        {
            var world = World(750);
            var view = new FakeView();
            var allowed = true;
            using (var presenter = new TowerSlotUnlockPresenter(view, world, new FakeLocalization(), new FakeAudio(), () => allowed))
            {
                presenter.Show("31");
                Assert.That(view.CanPurchase, Is.True);
                allowed = false;
                view.ClickPurchase();
                Assert.That(world.Gold, Is.EqualTo(750));
                Assert.That(world.IsTowerSlotUnlocked("31"), Is.False);
                Assert.That(view.Visible, Is.False);
            }
        }

        [Test]
        public void Presenter_RefreshesAffordabilityAndRejectsDoubleClick()
        {
            var world = World(749);
            var view = new FakeView();
            using (var presenter = new TowerSlotUnlockPresenter(view, world, new FakeLocalization(), new FakeAudio(), () => true))
            {
                presenter.Show("31");
                Assert.That(view.CanPurchase, Is.False);
                Assert.That(view.Status, Does.StartWith("slots.missing_gold"));
                world.AddGold(1);
                Assert.That(view.CanPurchase, Is.True);
                view.ClickPurchase();
                view.ClickPurchase();
                Assert.That(world.Gold, Is.Zero);
                Assert.That(world.IsTowerSlotUnlocked("31"), Is.True);
                Assert.That(view.Visible, Is.False);
            }
        }

        [Test]
        public void Presenter_OutOfOrderAndClose_DoNotCharge()
        {
            var world = World(20000);
            var view = new FakeView();
            using (var presenter = new TowerSlotUnlockPresenter(view, world, new FakeLocalization(), new FakeAudio(), () => true))
            {
                presenter.Show("33");
                Assert.That(view.Status, Is.EqualTo("slots.previous_required"));
                Assert.That(view.CanPurchase, Is.False);
                view.ClickPurchase();
                Assert.That(world.Gold, Is.EqualTo(20000));
                view.ClickClose();
                Assert.That(view.Visible, Is.False);
            }
        }

        private sealed class FakeView : ITowerSlotUnlockView
        {
            public event Action PurchaseRequested;
            public event Action CloseRequested;
            public bool Visible { get; private set; }
            public bool CanPurchase { get; private set; }
            public string Status { get; private set; }
            public void SetVisible(bool visible) => Visible = visible;
            public void ClickPurchase() => PurchaseRequested?.Invoke();
            public void ClickClose() => CloseRequested?.Invoke();
            public void SetContent(string title, string ordinal, string description, string price,
                string status, string purchase, string close, bool canPurchase)
            { Status = status; CanPurchase = canPurchase; }
            public void Dispose() { }
        }

        private sealed class FakeLocalization : ILocalizationService
        {
            public event Action<string> OnChangeLanguage;
            public string CurrentLanguageCode => "ru";
            public bool SetLanguage(string languageCode) { OnChangeLanguage?.Invoke(languageCode); return true; }
            public string Get(string key) => key;
            public string Format(string key, params object[] args) => key + ":" + string.Join(",", args);
        }

        private sealed class FakeAudio : IAudioManager
        {
            public bool IsMusicEnabled => true;
            public bool IsSoundEnabled => true;
            public void PlaySound(ESoundId soundId) { }
            public void PlayMusic() { }
            public void StopMusic() { }
            public void SetMusicEnabled(bool enabled) { }
            public void SetSoundEnabled(bool enabled) { }
        }
    }
}
