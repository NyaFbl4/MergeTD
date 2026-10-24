using System.Linq;
using NUnit.Framework;
using Project.Scripts.Configs;
using Project.Scripts.UI.ShopUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.System.Shop.Editor
{
    public sealed class ShopGemOfferTests
    {
        private const string IconRoot = "Assets/Project/Resources/UI/new/Icons/Shop/";
        private ShopOfferCatalog _catalog;

        [SetUp]
        public void SetUp() => _catalog = ShopOfferCatalog.LoadFromResources();

        [Test]
        public void GemSection_ContainsExactlyThreeEnabledPacksInAgreedOrder()
        {
            var section = _catalog.GetPlacement("gems");
            Assert.That(section.Layout, Is.EqualTo(EShopOfferPlacementLayout.GemGrid));
            Assert.That(section.Offers.Where(o => o.Enabled).Select(o => o.Id),
                Is.EqualTo(new[] { "gems_80", "gems_500", "gems_1200" }));
            Assert.That(section.Offers.Where(o => o.Enabled).Select(o => o.Rewards.Single().Amount),
                Is.EqualTo(new[] { 80, 500, 1200 }));
        }

        [TestCase("gems_80", 80, "79 RUB", "ShopItem_Gem_Purple_1")]
        [TestCase("gems_500", 500, "379 RUB", "ShopItem_Gem_Purple_2")]
        [TestCase("gems_1200", 1200, "749 RUB", "ShopItem_Gem_Purple_4")]
        public void EnabledPack_PreservesPriceProductAndUsesOneCorrectCompositeIcon(
            string id, int amount, string price, string iconName)
        {
            var offer = _catalog.GetOffer(id);
            Assert.That(offer.Enabled, Is.True);
            Assert.That(offer.Price.Type, Is.EqualTo(EShopOfferPriceType.RealMoney));
            Assert.That(offer.Price.ProductId, Is.EqualTo(id));
            Assert.That(offer.Price.PreviewPrice, Is.EqualTo(price));
            Assert.That(offer.Rewards.Single().Type, Is.EqualTo(EShopOfferRewardType.Gems));
            Assert.That(offer.Rewards.Single().Amount, Is.EqualTo(amount));
            Assert.That(offer.VisualIconCount, Is.EqualTo(1));
            Assert.That(offer.MainIcon, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(offer.MainIcon), Is.EqualTo(IconRoot + iconName + ".png"));
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(offer.MainIcon));
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.alphaIsTransparency, Is.True);
        }

        [TestCase("gems_2700", 2700)]
        [TestCase("gems_7000", 7000)]
        [TestCase("gems_16000", 16000)]
        public void RetiredPack_IsDisabledButKeepsItsMappingForExistingPaymentRecovery(string id, int amount)
        {
            var offer = _catalog.GetOffer(id);
            Assert.That(offer.Enabled, Is.False);
            Assert.That(offer.Price.ProductId, Is.EqualTo(id));
            Assert.That(offer.Rewards.Single().Amount, Is.EqualTo(amount));
            Assert.That(offer.Rewards.Single().Type, Is.EqualTo(EShopOfferRewardType.Gems));
        }

        [TestCase("gems_80")]
        [TestCase("gems_500")]
        [TestCase("gems_1200")]
        public void CardTemplate_BindsOneCompositeIconAndTheCorrectAmountAndPrice(string id)
        {
            var offer = _catalog.GetOffer(id);
            var template = Resources.Load<VisualTreeAsset>("UIToolkit/New/Shop/ShopGemOfferCard");
            Assert.That(template, Is.Not.Null);
            var uiElements = AssetDatabase.LoadAssetAtPath<UIElements>("Assets/Project/Configs/UI/UIElements.asset");
            Assert.That(uiElements.ShopGemOfferCard, Is.SameAs(template));
            var root = template.Instantiate();
            var view = new ShopGemOfferItemView(root, _ => Assert.Fail("Binding must not start a purchase."));
            view.Bind(new ShopOfferUIData(offer.Id, offer.Title, offer.Description, offer.MainIcon,
                new[] { new ShopOfferRewardUIData(offer.MainIcon, offer.Rewards.Single().Amount.ToString()) },
                null, offer.Price.PreviewPrice, offer.Badge, offer.VisualIconCount,
                string.Empty, string.Empty, string.Empty, true));

            var primary = root.Q<VisualElement>("GemIconPrimary");
            Assert.That(primary.style.backgroundImage.value.sprite, Is.SameAs(offer.MainIcon));
            Assert.That(primary.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<VisualElement>("GemIconSecondary").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(root.Q<VisualElement>("GemIconTertiary").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(root.Q<Label>("AmountLabel").text, Is.EqualTo("×" + offer.Rewards.Single().Amount));
            Assert.That(root.Q<Label>("PriceLabel").text, Is.EqualTo(offer.Price.PreviewPrice));
        }
    }
}
