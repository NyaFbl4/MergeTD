using Project.Scripts.Gameplay.Towers;
using Project.Scripts.System.Audio;
using Project.Scripts.System.UseCases;
using UnityEngine;

namespace Project.Scripts.Gameplay.Field
{
    public class TowerSlot : MonoBehaviour
    {
        [SerializeField] private Transform _towerAnchor;
        [SerializeField] private ETowerSlotType _slotType = ETowerSlotType.SpawnOnly;

        private IPlayerStatsUseCase _playerStats;
        private IAudioManager _audioManager;
        private TowerUnit _currentTower;
        private IUnitsCatalog _unitsCatalog;
        private Collider2D _dropCollider;

        public bool IsOccupied => _currentTower != null;
        public Transform TowerAnchor => _towerAnchor != null ? _towerAnchor : transform;
        public TowerUnit CurrentTower => _currentTower;
        public bool IsSpawnOnly => _slotType == ETowerSlotType.SpawnOnly;
        public bool IsActiveOnly => _slotType == ETowerSlotType.ActiveOnly;
        public ETowerSlotType SlotType => _slotType;
        public void SetSlotType(ETowerSlotType slotType) => _slotType = slotType;

        private void Awake()
        {
            _dropCollider = GetComponent<Collider2D>();
            RefreshDropCollider();
        }

        private void RefreshDropCollider()
        {
            if (_dropCollider == null)
                return;

            _dropCollider.enabled = _currentTower == null;
        }

        public void Construct(IUnitsCatalog unitsCatalog)
        {
            _unitsCatalog = unitsCatalog;
        }

        public bool TryPlaceTower(TowerUnit towerPrefab, IPlayerStatsUseCase playerStats, IAudioManager audioManager)
        {
            if (IsOccupied || towerPrefab == null)
                return false;

            _currentTower = Instantiate(towerPrefab, TowerAnchor.position, TowerAnchor.rotation, TowerAnchor);
            _playerStats = playerStats;
            _audioManager = audioManager;
            _currentTower.Initialize(playerStats, audioManager);
            _currentTower.CreateTower();
            BindDragHandler(_currentTower);
            ApplyFireState(_currentTower);
            RefreshDropCollider();

            return true;
        }

        public TowerUnit DetachTower()
        {
            if (_currentTower == null)
                return null;

            var tower = _currentTower;
            _currentTower = null;
            tower.transform.SetParent(null);
            RefreshDropCollider();
            return tower;
        }

        public bool TryAttachExistingTower(TowerUnit tower)
        {
            if (tower == null)
                return false;

            if (IsOccupied)
                return TryMergeTower(tower);

            _currentTower = tower;
            _playerStats = tower.PlayerStats;
            _audioManager = tower.AudioManager;
            _currentTower.transform.SetParent(TowerAnchor);
            _currentTower.transform.SetPositionAndRotation(TowerAnchor.position, TowerAnchor.rotation);

            BindDragHandler(_currentTower);
            ApplyFireState(_currentTower);
            RefreshDropCollider();

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

            _currentTower.Initialize(_playerStats, _audioManager);
            _currentTower.CreateTower();
            BindDragHandler(_currentTower);
            ApplyFireState(_currentTower);
            RefreshDropCollider();

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
            RefreshDropCollider();
        }

        public void ClearTower()
        {
            if (_currentTower != null)
                Destroy(_currentTower.gameObject);

            _currentTower = null;
            RefreshDropCollider();
        }

        private void ApplyFireState(TowerUnit towerObject)
        {
            var towerUnit = towerObject.GetComponent<TowerUnit>();
            if (towerUnit != null)
                towerUnit.SetCanFire(IsActiveOnly);
        }
    }
}
