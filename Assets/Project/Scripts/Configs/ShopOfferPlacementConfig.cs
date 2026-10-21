using System;
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
        [SerializeField] private EShopOfferPlacementLayout _layout;

        public string Id => _id;
        public string Title => _title;
        public int Order => _order;
        public EShopOfferPlacementLayout Layout => _layout;

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
}
