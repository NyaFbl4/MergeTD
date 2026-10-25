using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Scripts.Gameplay.Field
{
    public sealed class TowerSlotUnlockClickTarget : MonoBehaviour, IPointerClickHandler
    {
        private TowerSlot _slot;

        public void Initialize(TowerSlot slot) => _slot = slot;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !eventData.dragging)
                _slot.RequestUnlock();
        }
    }
}
