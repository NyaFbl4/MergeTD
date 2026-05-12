using Project.Scripts.Gameplay.Field;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Scripts.Gameplay.Towers
{
    public class TowerDragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private float _z = 0f;
        
        private TowerSlot _sourceSlot;
        private TowerUnit _towerUnit;
        private Camera _camera;
        private Vector3 _startPos;
        private Collider2D _towerCollider;
        private void Awake()
        {
            _towerUnit = GetComponent<TowerUnit>();
            _camera = Camera.main;
            _towerCollider = GetComponent<Collider2D>();
            
            if (_sourceSlot == null)
                _sourceSlot = GetComponentInParent<TowerSlot>();
        }

        public void Init(TowerSlot sourceSlot)
        {
            _sourceSlot = sourceSlot;
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_sourceSlot == null)
                _sourceSlot = GetComponentInParent<TowerSlot>();
            if (_sourceSlot == null)
                return;
            
            _startPos = transform.position;
            _sourceSlot.DetachTower();
            _towerUnit?.SetCanFire(false);
            if (_towerCollider != null)
                _towerCollider.enabled = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var world = _camera.ScreenToWorldPoint(eventData.position);
            world.z = _z;
            transform.position = world;
        } 
        
        public void OnEndDrag(PointerEventData eventData)
        {
             var world = _camera.ScreenToWorldPoint(eventData.position);
             world.z = _z;

             var targetSlot = FindSlotUnderPointer(world);
             if (targetSlot != null && targetSlot.TryAttachExistingTower(_towerUnit))
             {
                 if (_towerCollider != null)
                     _towerCollider.enabled = true;
                 return;
             }

             // rollback
             if (_sourceSlot != null && _sourceSlot.TryAttachExistingTower(_towerUnit))
             {
                 if (_towerCollider != null)
                     _towerCollider.enabled = true;
                 return;
             }

             transform.position = _startPos;
             if (_towerCollider != null)
                 _towerCollider.enabled = true;
        }

        private TowerSlot FindSlotUnderPointer(Vector3 worldPosition)
        {
            var hits = Physics2D.OverlapPointAll(worldPosition);
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null)
                    continue;

                var slot = hit.GetComponent<TowerSlot>();
                if (slot == null)
                    slot = hit.GetComponentInParent<TowerSlot>();

                if (slot != null)
                    return slot;
            }

            return null;
        }
    }
}
