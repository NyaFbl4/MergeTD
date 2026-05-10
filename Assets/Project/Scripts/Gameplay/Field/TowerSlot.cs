using UnityEngine;

namespace Project.Scripts.Gameplay.Field
{
    public class TowerSlot : MonoBehaviour
    {
        [SerializeField] private Transform _towerAnchor;

        private GameObject _currentTower;

        public bool IsOccupied => _currentTower != null;
        public Transform TowerAnchor => _towerAnchor != null ? _towerAnchor : transform;
        public GameObject CurrentTower => _currentTower;

        public bool TryPlaceTower(GameObject towerPrefab)
        {
            if (IsOccupied || towerPrefab == null)
                return false;

            _currentTower = Instantiate(towerPrefab, TowerAnchor.position, TowerAnchor.rotation, TowerAnchor);
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
