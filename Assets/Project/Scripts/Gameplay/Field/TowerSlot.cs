using Project.Scripts.Gameplay.Towers;
using UnityEngine;

namespace Project.Scripts.Gameplay.Field
{
    public class TowerSlot : MonoBehaviour
    {
        [SerializeField] private Transform _towerAnchor;
        [SerializeField] private ETowerSlotType _slotType;

        private GameObject _currentTower;

        public bool IsOccupied => _currentTower != null;
        public Transform TowerAnchor => _towerAnchor != null ? _towerAnchor : transform;
        public GameObject CurrentTower => _currentTower;
        public bool IsSpawnOnly => _slotType == ETowerSlotType.SpawnOnly;
        public bool IsActiveOnly => _slotType == ETowerSlotType.ActiveOnly;
        public ETowerSlotType SlotType => _slotType;

        
        public bool TryPlaceTower(GameObject towerPrefab)
        {
            if (IsOccupied || towerPrefab == null)
                return false;

            _currentTower = Instantiate(towerPrefab, TowerAnchor.position, TowerAnchor.rotation, TowerAnchor);
                
            var towerUnit = _currentTower.GetComponent<TowerUnit>();
            if (towerUnit != null)
                towerUnit.SetCanFire(IsActiveOnly); // ключевая строка

            return true;
        }

        public void SetTower(GameObject towerInstance)
        {
            _currentTower = towerInstance;
        }

        public void ClearTower()
        {
            if (_currentTower != null)
                Destroy(_currentTower);

            _currentTower = null;
        }
    }
}
