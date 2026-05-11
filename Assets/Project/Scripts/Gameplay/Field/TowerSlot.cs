using Project.Scripts.Gameplay.Towers;
using UnityEngine;

namespace Project.Scripts.Gameplay.Field
{
    public class TowerSlot : MonoBehaviour
    {
        [SerializeField] private Transform _towerAnchor;
        [SerializeField] private ETowerSlotType _slotType = ETowerSlotType.SpawnOnly;

        private GameObject _currentTower;

        public bool IsOccupied => _currentTower != null;
        public Transform TowerAnchor => _towerAnchor != null ? _towerAnchor : transform;
        public GameObject CurrentTower => _currentTower;
        public bool IsSpawnOnly => _slotType == ETowerSlotType.SpawnOnly;
        public bool IsActiveOnly => _slotType == ETowerSlotType.ActiveOnly;
        public ETowerSlotType SlotType => _slotType;
        public void SetSlotType(ETowerSlotType slotType) => _slotType = slotType;

        
        public bool TryPlaceTower(GameObject towerPrefab)
        {
            if (IsOccupied || towerPrefab == null)
                return false;

            _currentTower = Instantiate(towerPrefab, TowerAnchor.position, TowerAnchor.rotation, TowerAnchor);
            ApplyFireState(_currentTower);

            return true;
        }

        public GameObject DetachTower()
        {
            if (_currentTower == null)
                return null;

            var tower = _currentTower;
            _currentTower = null;
            tower.transform.SetParent(null);
            return tower;
        }

        public bool TryAttachExistingTower(GameObject tower)
        {
            if (tower == null || IsOccupied)
                return false;

            _currentTower = tower;
            _currentTower.transform.SetParent(TowerAnchor);
            _currentTower.transform.SetPositionAndRotation(TowerAnchor.position, TowerAnchor.rotation);
            ApplyFireState(_currentTower);
            return true;
        }

        public void SetTower(GameObject towerInstance)
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

        private void ApplyFireState(GameObject towerObject)
        {
            var towerUnit = towerObject.GetComponent<TowerUnit>();
            if (towerUnit != null)
                towerUnit.SetCanFire(IsActiveOnly);
        }
    }
}
