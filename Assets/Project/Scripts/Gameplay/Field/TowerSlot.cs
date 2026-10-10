using System;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;
using UnityEngine;

namespace Project.Scripts.Gameplay.Field
{
    public class TowerSlot : MonoBehaviour
    {
        private const string LockSpriteResourcePath = "UI/new/Icons/Icon_ImageIcon_Lock01_m";
        private const string LockIndicatorName = "LockIndicator";
        private const float LockIndicatorMaxSize = 0.6f;

        [SerializeField] private Transform _towerAnchor;
        [SerializeField] private ETowerSlotType _slotType = ETowerSlotType.SpawnOnly;
        [SerializeField] private string _persistentId;

        private IPlayerStatsUseCase _playerStats;
        private IAudioManager _audioManager;
        private TowerUnit _currentTower;
        private IUnitsCatalog _unitsCatalog;
        private Collider2D _dropCollider;
        private RunState _runState;
        private RunEnergyService _energy;
        private IWorldService _world;
        private TowerSellZone _sellZone;
        private SpriteRenderer _slotRenderer;
        private SpriteRenderer _lockIndicator;
        private ETowerSlotType _unlockedSlotType;
        private bool _isInitialized;

        private static Sprite _lockSprite;

        public bool IsOccupied => _currentTower != null;
        public Transform TowerAnchor => _towerAnchor != null ? _towerAnchor : transform;
        public TowerUnit CurrentTower => _currentTower;
        public bool IsSpawnOnly => _slotType == ETowerSlotType.SpawnOnly;
        public bool IsActiveOnly => _slotType == ETowerSlotType.ActiveOnly;
        public bool CanPlaceTower => _slotType != ETowerSlotType.Locked;
        public bool CanEditTower => CanPlaceTower && (_runState == null || _runState.CanEditDefense);
        public ETowerSlotType SlotType => _slotType;
        public string PersistentId => _persistentId;

        public void SetSlotType(ETowerSlotType slotType)
        {
            if (slotType != ETowerSlotType.Locked)
                _unlockedSlotType = slotType;

            _slotType = slotType;
            if (_currentTower != null)
                ApplyFireState(_currentTower);

            RefreshDropCollider();
            RefreshLockIndicator();
        }

        public void SetWorldUnlocked(bool isUnlocked) =>
            SetSlotType(isUnlocked ? _unlockedSlotType : ETowerSlotType.Locked);

        private void Awake() => InitializeComponents();

        private void InitializeComponents()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            _dropCollider = GetComponent<Collider2D>();
            _slotRenderer = GetComponent<SpriteRenderer>();
            _unlockedSlotType = _slotType;
            RefreshDropCollider();
            RefreshLockIndicator();
        }

        private void RefreshDropCollider()
        {
            if (_dropCollider == null)
                return;

            _dropCollider.enabled = _currentTower == null && CanEditTower;
        }

        private void RefreshLockIndicator()
        {
            if (_slotType != ETowerSlotType.Locked)
            {
                if (_lockIndicator != null)
                    _lockIndicator.enabled = false;

                return;
            }

            if (_lockIndicator == null)
                _lockIndicator = CreateLockIndicator();

            _lockIndicator.enabled = true;
        }

        private SpriteRenderer CreateLockIndicator()
        {
            _lockSprite ??= Resources.LoadAll<Sprite>(LockSpriteResourcePath)[0];

            var lockObject = new GameObject(LockIndicatorName);
            lockObject.layer = gameObject.layer;
            lockObject.transform.SetParent(transform, false);
            lockObject.transform.localPosition = -_lockSprite.bounds.center;

            var largestSpriteSize = Mathf.Max(_lockSprite.bounds.size.x, _lockSprite.bounds.size.y);
            lockObject.transform.localScale = Vector3.one * (LockIndicatorMaxSize / largestSpriteSize);

            var indicator = lockObject.AddComponent<SpriteRenderer>();
            indicator.sprite = _lockSprite;
            indicator.sharedMaterial = _slotRenderer.sharedMaterial;
            indicator.sortingLayerID = _slotRenderer.sortingLayerID;
            indicator.sortingOrder = _slotRenderer.sortingOrder + 1;
            return indicator;
        }

        public void Construct(
            IUnitsCatalog unitsCatalog,
            RunState runState,
            RunEnergyService energy,
            IWorldService world,
            TowerSellZone sellZone,
            string fallbackPersistentId)
        {
            InitializeComponents();
            _unitsCatalog = unitsCatalog;
            _energy = energy;
            _world = world;
            _sellZone = sellZone;

            if (string.IsNullOrWhiteSpace(_persistentId))
                _persistentId = fallbackPersistentId;
            
            if (_runState != null)
                _runState.PhaseChanged -= OnRunPhaseChanged;

            _runState = runState;
            _runState.PhaseChanged += OnRunPhaseChanged;
            RefreshDropCollider();
        }

        public bool TryPlaceTower(
            TowerUnit towerPrefab,
            IPlayerStatsUseCase playerStats,
            IAudioManager audioManager,
            bool persistWorldChange = true,
            int purchaseCost = 0)
        {
            if (!CanEditTower || IsOccupied || towerPrefab == null)
                return false;

            _currentTower = Instantiate(towerPrefab, TowerAnchor.position, TowerAnchor.rotation, TowerAnchor);
            _playerStats = playerStats;
            _audioManager = audioManager;
            _currentTower.Initialize(playerStats, audioManager);
            _currentTower.InitializeRun(_runState, _energy);
            _currentTower.SetPurchaseCost(purchaseCost > 0
                ? purchaseCost
                : TowerEconomy.GetDefaultPurchaseCost(_currentTower));
            _currentTower.CreateTower();
            BindDragHandler(_currentTower);
            ApplyFireState(_currentTower);
            RefreshDropCollider();

            if (persistWorldChange)
                PersistCurrentTower();

            return true;
        }

        public TowerUnit DetachTower(bool persistWorldChange = true)
        {
            if (!CanEditTower || _currentTower == null)
                return null;

            var tower = _currentTower;
            _currentTower = null;
            tower.transform.SetParent(null);
            RefreshDropCollider();

            if (persistWorldChange)
                _world.RemoveTower(PersistentId);

            return tower;
        }

        public bool TryAttachExistingTower(TowerUnit tower, bool persistWorldChange = true)
        {
            if (!CanEditTower || tower == null)
                return false;

            if (IsOccupied)
                return TryMergeTower(tower, persistWorldChange);

            _currentTower = tower;
            _playerStats = tower.PlayerStats;
            _audioManager = tower.AudioManager;
            _currentTower.transform.SetParent(TowerAnchor);
            _currentTower.transform.SetPositionAndRotation(TowerAnchor.position, TowerAnchor.rotation);

            BindDragHandler(_currentTower);
            ApplyFireState(_currentTower);
            RefreshDropCollider();

            if (persistWorldChange)
                PersistCurrentTower();

            return true;
        }

        private bool TryMergeTower(TowerUnit incomingTower, bool persistWorldChange)
        {
            if (_currentTower == null)
                return false;

            if (!_currentTower.CanMergeWith(incomingTower))
                return false;

            var nextLevel = _currentTower.CurrentLevel + 1;
            var nextPrefab = _unitsCatalog.GetTowerPrefabByLevel(nextLevel, _currentTower.TowerType);

            if (nextPrefab == null)
                return false;

            var mergedPurchaseCost = TowerEconomy.CombinePurchaseCosts(
                _currentTower.PurchaseCost,
                incomingTower.PurchaseCost);

            Destroy(_currentTower.gameObject);
            Destroy(incomingTower.gameObject);

            _currentTower = Instantiate(
                nextPrefab,
                TowerAnchor.position,
                TowerAnchor.rotation,
                TowerAnchor
            );

            _currentTower.Initialize(_playerStats, _audioManager);
            _currentTower.InitializeRun(_runState, _energy);
            _currentTower.SetPurchaseCost(mergedPurchaseCost);
            _currentTower.CreateTower();
            BindDragHandler(_currentTower);
            ApplyFireState(_currentTower);
            RefreshDropCollider();

            if (persistWorldChange)
                PersistCurrentTower();

            return true;
        }

        private void BindDragHandler(TowerUnit towerObject)
        {
            var drag = towerObject.GetComponent<Project.Scripts.Gameplay.Towers.TowerDragHandler>();
            if (drag != null)
                drag.Init(this, _sellZone);
        }

        public void SetTower(TowerUnit towerInstance)
        {
            _currentTower = towerInstance;
            if (_currentTower != null)
                ApplyFireState(_currentTower);
            RefreshDropCollider();

            if (_currentTower == null)
                _world.RemoveTower(PersistentId);
            else
                PersistCurrentTower();
        }

        public void ClearTower(bool persistWorldChange = true)
        {
            if (_currentTower != null)
                Destroy(_currentTower.gameObject);

            _currentTower = null;
            RefreshDropCollider();

            if (persistWorldChange)
                _world.RemoveTower(PersistentId);
        }

        private void PersistCurrentTower()
        {
            _world.SetTower(
                PersistentId,
                _currentTower.CurrentLevel,
                _currentTower.TowerType,
                _currentTower.PurchaseCost);
        }

        public void SellDetachedTower(TowerUnit tower, int refund)
        {
            _world.RemoveTower(PersistentId);
            _playerStats.AddGold(refund);
            _audioManager.PlaySound(ESoundId.UiButtonClick);
            Destroy(tower.gameObject);
        }

        public void CommitMoveFrom(TowerSlot sourceSlot)
        {
            _world.MoveTower(
                sourceSlot.PersistentId,
                PersistentId,
                _currentTower.CurrentLevel,
                _currentTower.TowerType,
                _currentTower.PurchaseCost);
        }

        private void ApplyFireState(TowerUnit towerObject)
        {
            var towerUnit = towerObject.GetComponent<TowerUnit>();
            if (towerUnit != null)
                towerUnit.SetCanFire(CanPlaceTower);
        }

        private void OnRunPhaseChanged(ERunPhase phase)
        {
            RefreshDropCollider();
        }

        private void OnDestroy()
        {
            if (_runState != null)
                _runState.PhaseChanged -= OnRunPhaseChanged;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_persistentId))
                _persistentId = Guid.NewGuid().ToString("N");
        }
#endif
    }
}
