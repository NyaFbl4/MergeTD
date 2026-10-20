using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Configs
{
    public enum EShopOfferPriceType
    {
        RealMoney = 0,
        Gold = 1
    }

    public enum EShopOfferRewardType
    {
        Gold = 0,
        Gems = 1,
        Spell = 2
    }

    [Serializable]
    public sealed class ShopOfferPrice
    {
        [SerializeField] private EShopOfferPriceType _type;
        [SerializeField] private string _productId;
        [SerializeField, Min(1)] private int _goldAmount = 1;

        public EShopOfferPriceType Type => _type;
        public string ProductId => _productId;
        public int GoldAmount => _goldAmount;

        public void Validate(string offerId)
        {
            switch (_type)
            {
                case EShopOfferPriceType.RealMoney:
                    if (string.IsNullOrWhiteSpace(_productId))
                    {
                        throw new InvalidOperationException(
                            $"Shop offer '{offerId}' requires a Payments_yg product id.");
                    }
                    break;
                case EShopOfferPriceType.Gold:
                    if (_goldAmount <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Shop offer '{offerId}' requires a positive gold price.");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_type), _type, null);
            }
        }
    }

    [Serializable]
    public sealed class ShopOfferReward
    {
        [SerializeField] private EShopOfferRewardType _type;
        [SerializeField, Min(1)] private int _amount = 1;
        [SerializeField] private BaseSpellConfig _spell;

        public EShopOfferRewardType Type => _type;
        public int Amount => _amount;
        public BaseSpellConfig Spell => _spell;

        public void Validate(string offerId)
        {
            switch (_type)
            {
                case EShopOfferRewardType.Gold:
                case EShopOfferRewardType.Gems:
                    if (_amount <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Shop offer '{offerId}' contains a non-positive currency reward.");
                    }
                    break;
                case EShopOfferRewardType.Spell:
                    if (_spell == null || string.IsNullOrWhiteSpace(_spell.SpellId))
                    {
                        throw new InvalidOperationException(
                            $"Shop offer '{offerId}' requires a spell config with a stable id.");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_type), _type, null);
            }
        }
    }

    [CreateAssetMenu(
        fileName = "Shop Offer Placement",
        menuName = "Project/Configs/Shop/Offer Placement")]
    public sealed class ShopOfferPlacementConfig : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _title;
        [SerializeField] private int _order;

        public string Id => _id;
        public string Title => _title;
        public int Order => _order;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
                throw new InvalidOperationException($"Shop offer placement '{name}' requires an id.");

            if (string.IsNullOrWhiteSpace(_title))
            {
                throw new InvalidOperationException(
                    $"Shop offer placement '{_id}' requires a localization key.");
            }
        }
    }

    [CreateAssetMenu(
        fileName = "Shop Offer",
        menuName = "Project/Configs/Shop/Offer")]
    public sealed class ShopOfferConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private ShopOfferPlacementConfig _placement;
        [SerializeField] private int _order;
        [SerializeField] private bool _enabled = true;

        [Header("Presentation")]
        [SerializeField] private string _title;
        [SerializeField, TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite _mainIcon;

        [Header("Purchase")]
        [SerializeField] private ShopOfferPrice _price = new();
        [SerializeField] private List<ShopOfferReward> _rewards = new();

        [Header("Availability")]
        [SerializeField] private bool _isTimed;
        [SerializeField] private long _startUnixMilliseconds;
        [SerializeField] private long _endUnixMilliseconds;
        [SerializeField, Min(0)] private int _dailyPurchaseLimit;

        public string Id => _id;
        public ShopOfferPlacementConfig Placement => _placement;
        public int Order => _order;
        public bool Enabled => _enabled;
        public string Title => _title;
        public string Description => _description;
        public Sprite MainIcon => _mainIcon;
        public ShopOfferPrice Price => _price;
        public IReadOnlyList<ShopOfferReward> Rewards => _rewards;
        public bool IsTimed => _isTimed;
        public long StartUnixMilliseconds => _startUnixMilliseconds;
        public long EndUnixMilliseconds => _endUnixMilliseconds;
        public int DailyPurchaseLimit => _dailyPurchaseLimit;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
                throw new InvalidOperationException($"Shop offer asset '{name}' requires an id.");

            if (_placement == null)
                throw new InvalidOperationException($"Shop offer '{_id}' requires a placement.");

            _placement.Validate();

            if (string.IsNullOrWhiteSpace(_title))
                throw new InvalidOperationException($"Shop offer '{_id}' requires a title key.");

            if (_price == null)
                throw new InvalidOperationException($"Shop offer '{_id}' requires a price.");

            _price.Validate(_id);

            if (_rewards == null || _rewards.Count == 0)
                throw new InvalidOperationException($"Shop offer '{_id}' requires at least one reward.");

            if (_rewards.Count > 2)
                throw new InvalidOperationException($"Shop offer '{_id}' supports at most two rewards.");

            var spellRewards = 0;
            var rewardTypes = new HashSet<EShopOfferRewardType>();
            for (var i = 0; i < _rewards.Count; i++)
            {
                var reward = _rewards[i]
                             ?? throw new InvalidOperationException(
                                 $"Shop offer '{_id}' contains an empty reward.");
                reward.Validate(_id);
                if (!rewardTypes.Add(reward.Type))
                {
                    throw new InvalidOperationException(
                        $"Shop offer '{_id}' contains duplicate reward types.");
                }

                if (reward.Type == EShopOfferRewardType.Spell)
                    spellRewards++;
            }

            if (spellRewards > 1)
                throw new InvalidOperationException($"Shop offer '{_id}' can contain only one spell reward.");

            ValidateRewardComposition(rewardTypes);

            if (_isTimed
                && (_startUnixMilliseconds <= 0
                    || _endUnixMilliseconds <= _startUnixMilliseconds))
            {
                throw new InvalidOperationException(
                    $"Shop offer '{_id}' requires a valid UTC time interval.");
            }
        }

        private void ValidateRewardComposition(HashSet<EShopOfferRewardType> rewardTypes)
        {
            if (_price.Type == EShopOfferPriceType.Gold)
            {
                if (_rewards.Count != 1 || !rewardTypes.Contains(EShopOfferRewardType.Gems))
                {
                    throw new InvalidOperationException(
                        $"Gold offer '{_id}' must reward only gems.");
                }

                if (_dailyPurchaseLimit <= 0)
                {
                    throw new InvalidOperationException(
                        $"Gold offer '{_id}' requires a daily purchase limit.");
                }

                return;
            }

            var validSingleReward = _rewards.Count == 1;
            var validDoubleReward = _rewards.Count == 2
                                    && rewardTypes.Contains(EShopOfferRewardType.Gems)
                                    && (rewardTypes.Contains(EShopOfferRewardType.Gold)
                                        || rewardTypes.Contains(EShopOfferRewardType.Spell));
            if (!validSingleReward && !validDoubleReward)
            {
                throw new InvalidOperationException(
                    $"Real-money offer '{_id}' has an unsupported reward combination.");
            }

            if (_dailyPurchaseLimit > 0)
            {
                throw new InvalidOperationException(
                    $"Real-money offer '{_id}' cannot use a daily purchase limit.");
            }
        }
    }

    public sealed class ShopOfferCatalog
    {
        private const string ResourceRoot = "Configs/Offers";

        private readonly ShopOfferConfig[] _offers;
        private readonly ShopOfferPlacementConfig[] _placements;

        public IReadOnlyList<ShopOfferConfig> Offers => _offers;
        public IReadOnlyList<ShopOfferPlacementConfig> Placements => _placements;

        private ShopOfferCatalog(
            ShopOfferConfig[] offers,
            ShopOfferPlacementConfig[] placements)
        {
            _offers = offers;
            _placements = placements;
        }

        public static ShopOfferCatalog LoadFromResources()
        {
            var offers = Resources.LoadAll<ShopOfferConfig>(ResourceRoot);
            var offerIds = new HashSet<string>(StringComparer.Ordinal);
            var productIds = new HashSet<string>(StringComparer.Ordinal);
            var placementsById = new Dictionary<string, ShopOfferPlacementConfig>(StringComparer.Ordinal);

            for (var i = 0; i < offers.Length; i++)
            {
                var offer = offers[i];
                offer.Validate();

                if (!offerIds.Add(offer.Id))
                    throw new InvalidOperationException($"Duplicate shop offer id '{offer.Id}'.");

                if (offer.Price.Type == EShopOfferPriceType.RealMoney
                    && !productIds.Add(offer.Price.ProductId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate shop payment product id '{offer.Price.ProductId}'.");
                }

                var placement = offer.Placement;
                if (placementsById.TryGetValue(placement.Id, out var existing)
                    && existing != placement)
                {
                    throw new InvalidOperationException(
                        $"Duplicate shop offer placement id '{placement.Id}'.");
                }

                placementsById[placement.Id] = placement;
            }

            Array.Sort(offers, CompareOffers);

            var placements = new ShopOfferPlacementConfig[placementsById.Count];
            placementsById.Values.CopyTo(placements, 0);
            Array.Sort(placements, ComparePlacements);
            return new ShopOfferCatalog(offers, placements);
        }

        public ShopOfferConfig GetOffer(string offerId)
        {
            for (var i = 0; i < _offers.Length; i++)
            {
                if (string.Equals(_offers[i].Id, offerId, StringComparison.Ordinal))
                    return _offers[i];
            }

            throw new InvalidOperationException($"Unknown shop offer id '{offerId}'.");
        }

        private static int CompareOffers(ShopOfferConfig left, ShopOfferConfig right)
        {
            var placementOrder = ComparePlacements(left.Placement, right.Placement);
            if (placementOrder != 0)
                return placementOrder;

            var order = left.Order.CompareTo(right.Order);
            return order != 0 ? order : string.CompareOrdinal(left.Id, right.Id);
        }

        private static int ComparePlacements(
            ShopOfferPlacementConfig left,
            ShopOfferPlacementConfig right)
        {
            var order = left.Order.CompareTo(right.Order);
            return order != 0 ? order : string.CompareOrdinal(left.Id, right.Id);
        }
    }
}
