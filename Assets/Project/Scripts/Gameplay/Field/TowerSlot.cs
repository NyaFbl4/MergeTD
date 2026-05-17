using Project.Scripts.Gameplay.Towers;
using UnityEngine;

namespace Project.Scripts.Gameplay.Field
{
    public class TowerSlot : MonoBehaviour
    {
        [SerializeField] private Transform _towerAnchor;
        [SerializeField] private ETowerSlotType _slotType = ETowerSlotType.SpawnOnly;

        private TowerUnit _currentTower;
        private IUnitsCatalog _unitsCatalog;

        public bool IsOccupied => _currentTower != null;
        public Transform TowerAnchor => _towerAnchor != null ? _towerAnchor : transform;
        public TowerUnit CurrentTower => _currentTower;
        public bool IsSpawnOnly => _slotType == ETowerSlotType.SpawnOnly;
        public bool IsActiveOnly => _slotType == ETowerSlotType.ActiveOnly;
        public ETowerSlotType SlotType => _slotType;
        public void SetSlotType(ETowerSlotType slotType) => _slotType = slotType;
        
        public void Construct(IUnitsCatalog unitsCatalog)
        {
            _unitsCatalog = unitsCatalog;
        }
        
        public bool TryPlaceTower(TowerUnit towerPrefab)
        {
            if (IsOccupied || towerPrefab == null)
                return false;

            _currentTower = Instantiate(towerPrefab, TowerAnchor.position, TowerAnchor.rotation, TowerAnchor);
            _currentTower.CreateTower();
            BindDragHandler(_currentTower);
            ApplyFireState(_currentTower);

            return true;
        }

        public TowerUnit DetachTower()
        {
            if (_currentTower == null)
                return null;

            var tower = _currentTower;
            _currentTower = null;
            tower.transform.SetParent(null);
            return tower;
        }

        public bool TryAttachExistingTower(TowerUnit tower)
        {
            if (tower == null)
                return false;

            if (IsOccupied)
                return TryMergeTower(tower);

            _currentTower = tower;
            _currentTower.transform.SetParent(TowerAnchor);
            _currentTower.transform.SetPositionAndRotation(TowerAnchor.position, TowerAnchor.rotation);

            BindDragHandler(_currentTower);
            ApplyFireState(_currentTower);

            return true;
        }
        
        private bool TryMergeTower(TowerUnit incomingTower)
        {
            if (_currentTower == null)
                return false;

            if (!_currentTower.CanMergeWith(incomingTower))
                return false;

            var nextLevel = _currentTower.CurrentLevel + 1;
            var nextPrefab = _unitsCatalog.GetTowerPrefabByLevel(nextLevel);

            if (nextPrefab == null)
                return false;

            
            Destroy(_currentTower.gameObject);
            Destroy(incomingTower.gameObject);

            _currentTower = Instantiate(
                nextPrefab,
                TowerAnchor.position,
                TowerAnchor.rotation,
                TowerAnchor
            );

            _currentTower.CreateTower();
            BindDragHandler(_currentTower);
            ApplyFireState(_currentTower);

            return true;
        }
        
        private void BindDragHandler(TowerUnit towerObject)
        {
            var drag = towerObject.GetComponent<Project.Scripts.Gameplay.Towers.TowerDragHandler>();
            if (drag != null)
                drag.Init(this);
        }

        public void SetTower(TowerUnit towerInstance)
        {
            _currentTower = towerInstance;
            if (_currentTower != null)
                ApplyFireState(_currentTower);
        }

        public void ClearTower()
        {
            if (_currentTower != null)
                Destroy(_currentTower);

            _currentTower = null;
        }

        private void ApplyFireState(TowerUnit towerObject)
        {
            var towerUnit = towerObject.GetComponent<TowerUnit>();
            if (towerUnit != null)
                towerUnit.SetCanFire(IsActiveOnly);
        }
    }
}
