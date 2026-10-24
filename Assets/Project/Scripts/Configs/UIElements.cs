using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu (menuName = "Project/Configs/UIElements", fileName = "UIElements")]
    public class UIElements: ScriptableObject
    {
        [SerializeField] private VisualTreeAsset _towerSlotPanel;
        [SerializeField] private VisualTreeAsset _spellPanel;
        [SerializeField] private VisualTreeAsset _spellButton;
        [SerializeField] private VisualTreeAsset _spellInfo;
        [SerializeField] private VisualTreeAsset _shopOfferCard;
        [SerializeField] private VisualTreeAsset _shopGemOfferCard;
        [SerializeField] private VisualTreeAsset _shopEnergyOfferCard;
        [SerializeField] private VisualTreeAsset _shopGoldOfferCard;
        [SerializeField] private List<Sprite> _towerIcons;
        [SerializeField] private List<Sprite> _generatorIcons;
        [SerializeField] private VisualTreeAsset _towerPanel;
        [SerializeField] private Sprite _goldIcon;
        [SerializeField] private Sprite _gemsIcon;
        [SerializeField] private Sprite _goldPriceBackground;
        [SerializeField] private Sprite _gemsPriceBackground;

        public VisualTreeAsset TowerPanel => _towerPanel;
        public Sprite GoldIcon => _goldIcon;
        public Sprite GemsIcon => _gemsIcon;
        public Sprite GoldPriceBackground => _goldPriceBackground;
        public Sprite GemsPriceBackground => _gemsPriceBackground;
        public VisualTreeAsset TowerSlotPanel => _towerSlotPanel;
        public VisualTreeAsset SpellPanel => _spellPanel;
        public VisualTreeAsset SpellButton => _spellButton;
        public VisualTreeAsset SpellInfo => _spellInfo;
        public VisualTreeAsset ShopOfferCard => _shopOfferCard;
        public VisualTreeAsset ShopGemOfferCard => _shopGemOfferCard;
        public VisualTreeAsset ShopEnergyOfferCard => _shopEnergyOfferCard;
        public VisualTreeAsset ShopGoldOfferCard => _shopGoldOfferCard;
        public List<Sprite> TowerIcons => _towerIcons;
        public List<Sprite> GeneratorIcons => _generatorIcons;
    }
}
