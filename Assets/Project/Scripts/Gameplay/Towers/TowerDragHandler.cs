using Project.Scripts.Gameplay.Field;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace Project.Scripts.Gameplay.Towers
{
    public class TowerDragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private float _z = 0f;
        
        private TowerSlot _sourceSlot;
        private TowerSellZone _sellZone;
        private TowerUnit _towerUnit;
        private Camera _camera;
        private Vector3 _startPos;
        private Collider2D _towerCollider;
        private SortingGroup _sortingGroup;
        private int _startSortingLayerId;
        private int _startSortingOrder;
        private bool _isDragging;
        
        private void Awake()
        {
            _towerUnit = GetComponent<TowerUnit>();
            _camera = Camera.main;
            _towerCollider = GetComponent<Collider2D>();
            _sortingGroup = GetComponent<SortingGroup>();
            
            if (_sourceSlot == null)
                _sourceSlot = GetComponentInParent<TowerSlot>();
        }

        public void Init(TowerSlot sourceSlot, TowerSellZone sellZone)
        {
            _sourceSlot = sourceSlot;
            _sellZone = sellZone;
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log($"Begin drag: {name}");

            if (_sourceSlot == null)
                _sourceSlot = GetComponentInParent<TowerSlot>();

            if (_sourceSlot == null)
            {
                Debug.LogWarning($"TowerDragHandler: source slot is null for {name}");
                return;
            }
            
            if (!_sourceSlot.CanEditTower)
                return;

            _startPos = transform.position;
            if (_sourceSlot.DetachTower(false) != _towerUnit)
                return;
            
            _isDragging = true;
            _startSortingLayerId = _sortingGroup.sortingLayerID;
            _startSortingOrder = _sortingGroup.sortingOrder;
            _sortingGroup.sortingLayerID = _sellZone.SortingLayerId;
            _sortingGroup.sortingOrder = _sellZone.SortingOrder + 1;
            _towerUnit?.SetCanFire(false);
            _sellZone.Show(_towerUnit);

            if (_towerCollider != null)
                _towerCollider.enabled = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;
            
            transform.position = GetPointerWorldPosition(eventData);
            _sellZone.SetHovered(_sellZone.ContainsScreenPoint(eventData.position, _camera));
        } 
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;
            
            var world = GetPointerWorldPosition(eventData);

             if (_sellZone.ContainsScreenPoint(eventData.position, _camera))
             {
                 _sourceSlot.SellDetachedTower(_towerUnit, _sellZone.CurrentRefund);
                 CompleteDrag();
                 return;
             }

             var targetSlot = FindSlotUnderPointer(world);
             if (targetSlot != null && targetSlot.TryAttachExistingTower(_towerUnit, false))
             {
                 targetSlot.CommitMoveFrom(_sourceSlot);
                 CompleteDrag();
                 return;
             }

             // rollback
             if (_sourceSlot != null && _sourceSlot.TryAttachExistingTower(_towerUnit, false))
             {
                 CompleteDrag();
                 return;
             }

             transform.position = _startPos;
             CompleteDrag();
        } 

        private void CompleteDrag()
        {
            _isDragging = false;
            _sortingGroup.sortingLayerID = _startSortingLayerId;
            _sortingGroup.sortingOrder = _startSortingOrder;
            _sellZone.Hide();

            if (_towerCollider != null)
                _towerCollider.enabled = true;
        }
        private Vector3 GetPointerWorldPosition(PointerEventData eventData)
        {
            if (_camera == null)
                _camera = Camera.main;

            var screen = new Vector3(
                eventData.position.x,
                eventData.position.y,
                Mathf.Abs(_camera.transform.position.z - transform.position.z));

            var world = _camera.ScreenToWorldPoint(screen);
            world.z = _z;
            return world;
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
