using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Project.Scripts.Configs;
using Project.Scripts.System.Reward;
using Project.Scripts.System.Save;
using Project.Scripts.System.Shop;
using Project.Scripts.UI.SpellsUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Project.Scripts.System.Localization;

using YG;

namespace Project.Scripts.Gameplay.Run.Editor
{
    public sealed class SpellProgressionTests
    {
        private const string SaveKey = "merge_td_world";
        private bool _hadLocalSave;
        private string _localSave;
        private WorldSaveData _originalWorld;
        private string _originalWorldJson;
        private SpellCatalog _catalog;
        private WorldService _world;
        private SpellProgressionService _progression;
        private SpellsUIUseCase _ui;

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
            _catalog = AssetDatabase.LoadAssetAtPath<SpellCatalog>("Assets/Project/Configs/SpellConfigs/Spell Catalog.asset");
            Reload();
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
        public void NewPlayer_HasOnlyWeaponBarrageUnlockedAndSelected()
        {
            Assert.That(_world.Spells.Where(s => s.isUnlocked).Select(s => s.spellId),
                Is.EqualTo(new[] { SpellIds.WeaponBarrage }));
            Assert.That(_world.ActiveSpellIds, Is.EqualTo(new[] { SpellIds.WeaponBarrage }));
            Assert.That(_ui.GetSpells().Count, Is.EqualTo(7));
            Assert.That(_ui.GetSpells().Count(s => s.IsUnlocked), Is.EqualTo(1));
        }

        [TestCase(SpellIds.BaseRepair, 1500)]
        [TestCase(SpellIds.BarrageProtocol, 10000)]
        public void GoldUnlock_IsOneAtomicPurchase(string id, int price)
        {
            _world.AddGold(price - _world.Gold);
            _world.AddGems(11);
            Assert.That(_ui.TryUnlock(id), Is.True);
            Assert.That(_world.Gold, Is.Zero);
            Assert.That(_world.Gems, Is.EqualTo(11));
            Assert.That(_world.GetSpellLevel(id), Is.EqualTo(1));
            Assert.That(_ui.TryUnlock(id), Is.False);
            var saved = JsonUtility.FromJson<WorldSaveData>(PlayerPrefs.GetString(SaveKey));
            Assert.That(saved.gold, Is.Zero);
            Assert.That(saved.spells.Single(s => s.spellId == id).isUnlocked, Is.True);
        }

        [Test]
        public void InsufficientGold_CannotUnlockOrChangeBalances()
        {
            var gold = _world.Gold;
            Assert.That(_ui.TryUnlock(SpellIds.BaseRepair), Is.False);
            Assert.That(_world.Gold, Is.EqualTo(gold));
            Assert.That(_world.IsSpellUnlocked(SpellIds.BaseRepair), Is.False);
            Assert.That(_ui.GetSpells().Single(s => s.SpellId == SpellIds.BaseRepair).CanUnlock, Is.False);
        }

        [Test]
        public void EmpUnlock_CostsExactly150Gems()
        {
            _world.AddGems(149);
            Assert.That(_ui.TryUnlock(SpellIds.EmpPulse), Is.False);
            _world.AddGems(1);
            Assert.That(_ui.TryUnlock(SpellIds.EmpPulse), Is.True);
            Assert.That(_world.Gems, Is.Zero);
            Assert.That(_world.Gold, Is.EqualTo(500));
        }

        [TestCase(SpellIds.CryoDischarge)]
        [TestCase(SpellIds.OrbitalRailgun)]
        [TestCase(SpellIds.GravityTrap)]
        public void RunAndOfferSpells_CannotBeBoughtForCurrency(string id)
        {
            _world.AddGems(10000);
            _world.AddGold(100000);
            Assert.That(_ui.TryUnlock(id), Is.False);
            Assert.That(_world.IsSpellUnlocked(id), Is.False);
        }

        [Test]
        public void LockedSpells_CannotBeSelectedOrUpgradedEvenThroughWorldApi()
        {
            _world.AddGems(1000);
            Assert.That(_world.SetSpellSelected(SpellIds.EmpPulse, true), Is.False);
            Assert.That(_ui.ToggleSelection(SpellIds.EmpPulse), Is.False);
            Assert.That(_ui.TryUpgrade(SpellIds.EmpPulse), Is.False);
            Assert.That(_world.TryUpgradeSpell(SpellIds.EmpPulse, 1, 200), Is.False);
            Assert.That(_world.Gems, Is.EqualTo(1000));
            var locked = _ui.GetSpells().Single(s => s.SpellId == SpellIds.EmpPulse);
            Assert.That(locked.IsUnlocked || locked.CanSelect || locked.HasNextUpgrade, Is.False);
            Assert.That(locked.Level, Is.Zero);
        }

        [Test]
        public void FirstAndThirdVictories_UnlockOnlyMatchingSpells()
        {
            Assert.That(_progression.RecordVictory("default-run-2"), Is.True);
            Assert.That(_world.IsSpellUnlocked(SpellIds.CryoDischarge), Is.False);
            Assert.That(_world.IsSpellUnlocked(SpellIds.OrbitalRailgun), Is.False);
            _progression.RecordVictory("default-run-1");
            Assert.That(_world.IsSpellUnlocked(SpellIds.CryoDischarge), Is.True);
            Assert.That(_world.IsSpellUnlocked(SpellIds.OrbitalRailgun), Is.False);
            _progression.RecordVictory("default-run-3");
            Assert.That(_world.IsSpellUnlocked(SpellIds.OrbitalRailgun), Is.True);
            Assert.That(_world.IsSpellUnlocked(SpellIds.GravityTrap), Is.False);
        }

        [Test]
        public void RepeatVictory_DoesNotRegrantOrDowngradeSpell()
        {
            _progression.RecordVictory("default-run-1");
            _world.SetSpellLevel(SpellIds.CryoDischarge, 3);
            Assert.That(_progression.RecordVictory("default-run-1"), Is.False);
            Assert.That(_world.GetSpellLevel(SpellIds.CryoDischarge), Is.EqualTo(3));
            Assert.That(_world.CompletedRunIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void SaveReload_RetainsCompletionAndPurchasedSpell()
        {
            _progression.RecordVictory("default-run-1");
            _world.AddGems(150);
            _ui.TryUnlock(SpellIds.EmpPulse);
            ReloadFromLocal();
            Assert.That(_world.CompletedRunIds, Is.EqualTo(new[] { "default-run-1" }));
            Assert.That(_world.IsSpellUnlocked(SpellIds.CryoDischarge), Is.True);
            Assert.That(_world.IsSpellUnlocked(SpellIds.EmpPulse), Is.True);
            Assert.That(_progression.RecordVictory("default-run-1"), Is.False);
        }

        [Test]
        public void Reset_LocksEarnedSpellsAndAllowsEarningRunRewardAgain()
        {
            _progression.RecordVictory("default-run-1");
            _world.AddGems(150);
            _ui.TryUnlock(SpellIds.EmpPulse);
            _world.Reset();
            Assert.That(_world.CompletedRunIds, Is.Empty);
            Assert.That(_world.IsSpellUnlocked(SpellIds.EmpPulse), Is.False);
            Assert.That(_world.IsSpellUnlocked(SpellIds.CryoDischarge), Is.False);
            Assert.That(_world.IsSpellSelected(SpellIds.WeaponBarrage), Is.True);
            Assert.That(_progression.RecordVictory("default-run-1"), Is.True);
        }

        [Test]
        public void PaidBundle_Grants100GemsOnlyOnceAndSurvivesResetAtLevelOne()
        {
            var shop = Shop();
            PaymentCallback(shop);
            Assert.That(_world.Gems, Is.EqualTo(100));
            Assert.That(_world.IsSpellUnlocked(SpellIds.GravityTrap), Is.True);
            PaymentCallback(shop);
            Assert.That(_world.Gems, Is.EqualTo(100));
            _world.SetSpellLevel(SpellIds.GravityTrap, 4);
            _world.Reset();
            Assert.That(_world.Gems, Is.Zero);
            Assert.That(_world.GetSpellLevel(SpellIds.GravityTrap), Is.EqualTo(1));
            Assert.That(shop.GetVisibleOffers("offers").Any(s => s.Config.Id == "offer_gravity_trap"), Is.False);
            PaymentCallback(shop);
            Assert.That(_world.Gems, Is.Zero);
            ReloadFromLocal();
            _world.Reset();
            Assert.That(_world.GetSpellLevel(SpellIds.GravityTrap), Is.EqualTo(1));
            Assert.That(_world.Gems, Is.Zero);
        }

        [Test]
        public void BundleCatalog_HasAgreedPriceRewardsAndNoTimeLimit()
        {
            var bundle = ShopOfferCatalog.LoadFromResources().GetOffer("offer_gravity_trap");
            Assert.That(bundle.Price.ProductId, Is.EqualTo("offer_gravity_trap"));
            Assert.That(bundle.Price.PreviewPrice, Is.EqualTo("199 RUB"));
            Assert.That(bundle.IsTimed || bundle.DailyPurchaseLimit > 0, Is.False);
            Assert.That(bundle.Rewards.Single(r => r.Type == EShopOfferRewardType.Gems).Amount, Is.EqualTo(100));
            Assert.That(bundle.Rewards.Single(r => r.Type == EShopOfferRewardType.Spell).Spell.SpellId,
                Is.EqualTo(SpellIds.GravityTrap));
            Assert.That(bundle.MainIcon, Is.Not.Null);
        }

        [Test]
        public void LegacySelection_DoesNotTurnUnownedSpellsIntoOwnership()
        {
            YG2.saves.world = new WorldSaveData
            {
                version = 2,
                activeSpellIds = new() { SpellIds.CryoDischarge, SpellIds.WeaponBarrage },
                spells = new() { new SpellProgressSaveData(SpellIds.EmpPulse, true, 3) }
            };
            Reload();
            Assert.That(_world.ActiveSpellIds, Is.EqualTo(new[] { SpellIds.WeaponBarrage }));
            Assert.That(_world.GetSpellLevel(SpellIds.EmpPulse), Is.EqualTo(3));
            Assert.That(_world.IsSpellUnlocked(SpellIds.CryoDischarge), Is.False);
        }

        [Test]
        public void LegacyExplicitGravityOwnership_IsPreservedOnReset()
        {
            YG2.saves.world = new WorldSaveData
            {
                version = 2,
                spells = new() { new SpellProgressSaveData(SpellIds.GravityTrap, true, 3) }
            };
            Reload();
            _world.Reset();
            Assert.That(_world.GetSpellLevel(SpellIds.GravityTrap), Is.EqualTo(1));
            Assert.That(_world.Gems, Is.Zero);
        }

        [Test]
        public void UnlockedSpell_CanBeSelectedAndUpgradedNormally()
        {
            _world.AddGems(350);
            _ui.TryUnlock(SpellIds.EmpPulse);
            Assert.That(_ui.ToggleSelection(SpellIds.EmpPulse), Is.True);
            Assert.That(_ui.TryUpgrade(SpellIds.EmpPulse), Is.True);
            Assert.That(_world.GetSpellLevel(SpellIds.EmpPulse), Is.EqualTo(2));
            Assert.That(_world.Gems, Is.Zero);
        }

        [Test]
        public void SpellInfo_ShowsUnlockConditionInsteadOfUpgradeAndTracksAffordability()
        {
            var elements = AssetDatabase.LoadAssetAtPath<UIElements>("Assets/Project/Configs/UI/UIElements.asset");
            var root = elements.SpellInfo.Instantiate();
            using var view = new SpellInfoView(root);
            var localization = new LocalizationStub();
            view.Bind(_ui.GetSpells().Single(s => s.SpellId == SpellIds.CryoDischarge), localization);
            Assert.That(root.Q<Label>("UpgradeDescriptionLabel").text, Is.EqualTo("spells.unlock_run_format:1"));
            Assert.That(root.Q<Label>("UpgradeButtonLabel").text, Is.EqualTo("spells.completion_reward"));
            Assert.That(root.Q<Button>("UpgradeButton").enabledSelf, Is.False);
            Assert.That(root.Q<Label>("UpgradeSectionLabel").style.display.value, Is.EqualTo(DisplayStyle.None));
            _world.AddGold(1250);
            view.Bind(_ui.GetSpells().Single(s => s.SpellId == SpellIds.BaseRepair), localization);
            Assert.That(root.Q<Label>("UpgradePriceLabel").text, Is.EqualTo("1500"));
            Assert.That(root.Q<Button>("UpgradeButton").enabledSelf, Is.True);
            Assert.That(root.Q<Button>("SetSpellButton").enabledSelf, Is.False);
        }

        private sealed class LocalizationStub : ILocalizationService
        {
            public event Action<string> OnChangeLanguage { add { } remove { } }
            public string CurrentLanguageCode => "ru";
            public bool SetLanguage(string code) => true;
            public string Get(string key) => key;
            public string Format(string key, params object[] args) => key + ":" + string.Join(",", args);
        }

        private void Reload()
        {
            _world = new WorldService(new WorldSaveService());
            _progression = new SpellProgressionService(_catalog, _world);
            _ui = new SpellsUIUseCase(_catalog, _world, _progression);
        }

        private void ReloadFromLocal()
        {
            YG2.saves.world = null;
            YG2.saves.worldJson = string.Empty;
            Reload();
        }

        private ShopOfferService Shop()
        {
            var clock = new Clock();
            return new ShopOfferService(ShopOfferCatalog.LoadFromResources(), _world, clock,
                new PlayerEnergyService(_world, clock));
        }

        private static void PaymentCallback(ShopOfferService shop) => typeof(ShopOfferService)
            .GetMethod("OnPaymentSucceeded", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(shop, new object[] { "offer_gravity_trap" });

        private sealed class Clock : IDailyRewardTimeProvider
        {
            public bool TryGetCurrentUnixMilliseconds(out long now)
            {
                now = 1_792_800_000_000;
                return true;
            }
        }
    }
}
