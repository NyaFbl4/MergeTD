using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(
        fileName = "Shop Offer Placement",
        menuName = "Project/Configs/Shop/Offer Placement")]
    public sealed class ShopOfferPlacementConfig : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _title;
        [SerializeField] private int _order;
        [SerializeField, Tooltip("List: OfferContainer. GemGrid: GemContainer. EnergyGrid: EnergyContainer. GoldGrid: GoldContainer.")]
        private EShopOfferPlacementLayout _layout;
        [SerializeField, Tooltip("Порядок элементов задаёт порядок карточек в разделе.")]
        private List<ShopOfferConfig> _offers = new();

        public string Id => _id;
        public string Title => _title;
        public int Order => _order;
        public EShopOfferPlacementLayout Layout => _layout;
        public IReadOnlyList<ShopOfferConfig> Offers => _offers;

        public void Validate()
        {
            if (_layout is not EShopOfferPlacementLayout.List and not EShopOfferPlacementLayout.GemGrid
                and not EShopOfferPlacementLayout.EnergyGrid and not EShopOfferPlacementLayout.GoldGrid)
                throw new InvalidOperationException($"Shop section '{name}' has an unsupported layout.");

            if (_offers == null)
                throw new InvalidOperationException($"Shop section '{name}' requires an offers list.");

            if (string.IsNullOrWhiteSpace(_id))
                throw new InvalidOperationException($"Shop offer placement '{name}' requires an id.");

            if (string.IsNullOrWhiteSpace(_title))
            {
                throw new InvalidOperationException(
                    $"Shop offer placement '{_id}' requires a localization key.");
            }
        }
    }
}
