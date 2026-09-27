using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu (menuName = "Project/Configs/UIElements", fileName = "UIElements")]
    public class UIElements: ScriptableObject
    {
        [SerializeField] private VisualTreeAsset _towerSlotPanel;
        [SerializeField] private List<Sprite> _towerIcons;
        [SerializeField] private List<Sprite> _generatorIcons;
        
        public VisualTreeAsset TowerSlotPanel => _towerSlotPanel;
        public List<Sprite> TowerIcons => _towerIcons;
        public List<Sprite> GeneratorIcons => _generatorIcons;
    }
}