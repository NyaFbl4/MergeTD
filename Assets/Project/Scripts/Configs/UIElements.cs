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
        [SerializeField] private List<Sprite> _towerIcons;
        [SerializeField] private List<Sprite> _generatorIcons;
        
        public VisualTreeAsset TowerSlotPanel => _towerSlotPanel;
        public VisualTreeAsset SpellPanel => _spellPanel;
        public VisualTreeAsset SpellButton => _spellButton;
        public VisualTreeAsset SpellInfo => _spellInfo;
        public List<Sprite> TowerIcons => _towerIcons;
        public List<Sprite> GeneratorIcons => _generatorIcons;
    }
}
