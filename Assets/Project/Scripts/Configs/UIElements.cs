using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu (menuName = "Configs/UIElements", fileName = "UIElements")]
    public class UIElements: ScriptableObject
    {
        [SerializeField] private VisualTreeAsset _slotPanel;
        
        public VisualTreeAsset SlotPanel => _slotPanel;
    }
}