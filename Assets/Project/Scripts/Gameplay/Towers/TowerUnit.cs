using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Enemies;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Run.Cards;
using Project.Scripts.System.Audio;
using Project.Scripts.System.UseCases;
using TMPro;
using UnityEngine;

namespace Project.Scripts.Gameplay.Towers
{
    public class TowerUnit : MonoBehaviour, ITowerUnit, IGameUpdateListener
    {
        private const string GeneratorChargeFramesRootPath = "Guns/Generators";

        [SerializeField] private TowerConfig _towerConfig;
        [SerializeField] private Transform _turretPivot;
        [SerializeField] private TMP_Text _currentLevelText;
        [SerializeField] private Animator _animator;
        [SerializeField] private Projectile _projectilePrefab;

        [SerializeField] private Transform _firePoint;
        [SerializeField] private List<Transform> _firePoints;
        [SerializeField] private EFireMode _fireMode = EFireMode.Single;

        [Header("Fire parametrs")]
        [SerializeField] private float _range = 3f;
        private int _damage;
        private float _fireRate;
        private float _currentRange;
        private float _spellAttackSpeedMultiplier = 1f;

        [Header("Tower parametrs")]
        [SerializeField] private int _towerLevel = 1;
        [SerializeField, HideInInspector] private int _purchaseCost;
        [SerializeField] private float _angleOffset = -90f;
        [SerializeField] private float _rotationSpeed = 360f;
        [SerializeField] private float _animationSpeed;
        [SerializeField] private float _criticalDamageMultiplier = 2f;

        private float _criticalChance;

        public int CurrentLevel => _towerLevel;
        public ETowerType TowerType => _towerConfig.TowerType;
        public int PurchaseCost => _purchaseCost;
        private GeneratorProduction _generatorProduction;
        public TowerConfig TowerConfig => _towerConfig;
        public IPlayerStatsUseCase PlayerStats => _playerStats;
        public IAudioManager AudioManager => _audioManager;

        private float _cooldown;
        private EnemyHealth _currentTarget;
        private bool _canFire;
        private bool _isFire;
        private int _nextFirePointIndex;
        private IPlayerStatsUseCase _playerStats;
        private IAudioManager _audioManager;
        private float _currentCriticalDamageMultiplier;
        private SpriteRenderer _generatorSpriteRenderer;
        private Sprite[] _generatorChargeFrames;
        private Sprite _generatorIdleSprite;
        private int _generatorChargeFrameIndex = -1;
        private RunCardRuntimeState _runCardState;

        public void Initialize(IPlayerStatsUseCase playerStats, IAudioManager audioManager)
        {
            if (_playerStats != null)
                _playerStats.UpgradesChanged -= OnUpgradesChanged;

            _playerStats = playerStats;
            _audioManager = audioManager;

            if (_playerStats != null)
                _playerStats.UpgradesChanged += OnUpgradesChanged;
        }

        public void InitializeRun(
            RunState runState,
            RunEnergyService energy,
            RunCardRuntimeState runCardState)
        {
            _generatorProduction?.Dispose();
            if (_runCardState != null)
                _runCardState.ModifiersChanged -= OnRunCardModifiersChanged;

            _runCardState = runCardState;
            if (_runCardState != null)
                _runCardState.ModifiersChanged += OnRunCardModifiersChanged;

            if (TowerType == ETowerType.Generator)
            {
                _generatorProduction = new GeneratorProduction(
                    runState,
                    energy,
                    runCardState,
                    _towerConfig.GenerationInterval,
                    _towerConfig.EnergyPerPulse);
                InitializeGeneratorChargeAnimation();
            }

            RecalculateStats();
        }

        public void SetCanFire(bool canFire)
        {
            _canFire = canFire;
            if (!_canFire)
                _currentTarget = null;
        }

        public void SetPurchaseCost(int purchaseCost)
        {
            _purchaseCost = Mathf.Max(1, purchaseCost);
        }

        public void SetSpellAttackSpeedMultiplier(float multiplier)
        {
            _spellAttackSpeedMultiplier = Mathf.Max(0.01f, multiplier);
            RecalculateStats();
        }

        private void OnEnable() => IGameListener.Register(this);
        private void OnDisable() => IGameListener.Unregister(this);

        public void CreateTower()
        {
            _currentLevelText.text = _towerLevel.ToString();
            RecalculateStats();
            _nextFirePointIndex = 0;
        }

        public bool CanMergeWith(TowerUnit other)
        {
            if (other == null)
                return false;

            if (other.TowerType != TowerType)
                return false;

            if (other == this)
                return false;

            if (other.TowerConfig != TowerConfig)
                return false;

            return other.CurrentLevel == CurrentLevel;
        }

        public void OnAttackFireEvent()
        {
            if (TowerType == ETowerType.Generator)
                return;

            if (!IsTargetValid(_currentTarget))
            {
                _isFire = false;
                _currentTarget = null;
                return;
            }

            _audioManager?.PlaySound(ESoundId.TowerShoot);
            Fire(_currentTarget);
        }

        public void OnAttackFinishedEvent()
        {
            _isFire = false;
            _currentTarget = null;
        }

        public void OnUpdate(float deltaTime)
        {
            if (TowerType == ETowerType.Generator)
            {
                var isProducing = _generatorProduction.Tick(deltaTime, _canFire);
                UpdateGeneratorChargeAnimation(isProducing);
                return;
            }

            if (!_canFire)
                return;

            _cooldown -= deltaTime;

            if (!IsTargetValid(_currentTarget))
                _currentTarget = FindNearestEnemyInRange();

            if (_currentTarget != null)
                RotateToTargetSmooth(_currentTarget.transform.position, deltaTime);

            if (_cooldown > 0f || _currentTarget == null)
                return;

            TryAttack(_currentTarget);
            _cooldown = 1f / Mathf.Max(0.01f, _fireRate);
        }

        private void RecalculateStats()
        {
            if (TowerType == ETowerType.Generator)
                return;

            var damageBonus = (_playerStats?.TowerDamageBonus ?? 0f)
                              + (_runCardState?.GetTowerDamageBonus(TowerType) ?? 0f);
            var attackSpeedBonus = (_playerStats?.TowerAttackSpeedBonus ?? 0f)
                                   + (_runCardState?.TowerAttackSpeedBonus ?? 0f);
            var critDamageBonus = _playerStats?.TowerCritDamageBonus ?? 0f;

            _currentCriticalDamageMultiplier = _criticalDamageMultiplier + critDamageBonus;
            _damage = Mathf.RoundToInt(_towerConfig.StartTowerDamage * (1f + damageBonus));
            _fireRate = _towerConfig.StartAttackSpeed
                * (1f + attackSpeedBonus)
                * _spellAttackSpeedMultiplier;
            _criticalChance = _playerStats?.TowerCritChanceBonus ?? 0f;
            _currentRange = _range * Mathf.Max(0f, 1f + (_runCardState?.TowerRangeBonus ?? 0f));

            var attackSpeedRatio = _towerConfig.StartAttackSpeed <= 0f
                ? 1f
                : _fireRate / _towerConfig.StartAttackSpeed;

            _animationSpeed = _towerConfig.AnimationSpeed * attackSpeedRatio;
        }

        private void InitializeGeneratorChargeAnimation()
        {
            _generatorSpriteRenderer = _turretPivot.GetComponent<SpriteRenderer>();
            _generatorIdleSprite = _generatorSpriteRenderer.sprite;

            var chargeFramesPath = $"{GeneratorChargeFramesRootPath}/Generator{_towerLevel}/Charge";
            _generatorChargeFrames = Resources.LoadAll<Sprite>(chargeFramesPath);

            if (_generatorChargeFrames.Length == 0)
            {
                _generatorChargeFrames = new[] { _generatorIdleSprite };
            }
            else
            {
                global::System.Array.Sort(_generatorChargeFrames,
                    (left, right) => string.CompareOrdinal(left.name, right.name));
            }

            _generatorChargeFrameIndex = -1;
        }

        private void UpdateGeneratorChargeAnimation(bool isProducing)
        {
            if (!isProducing)
            {
                if (_generatorChargeFrameIndex >= 0)
                {
                    _generatorSpriteRenderer.sprite = _generatorIdleSprite;
                    _generatorChargeFrameIndex = -1;
                }

                return;
            }

            var frameIndex = Mathf.Min(
                _generatorChargeFrames.Length - 1,
                Mathf.FloorToInt(_generatorProduction.NormalizedProgress * _generatorChargeFrames.Length));

            if (frameIndex == _generatorChargeFrameIndex)
                return;

            _generatorSpriteRenderer.sprite = _generatorChargeFrames[frameIndex];
            _generatorChargeFrameIndex = frameIndex;
        }

        private void OnUpgradesChanged()
        {
            RecalculateStats();
        }

        private void OnRunCardModifiersChanged()
        {
            RecalculateStats();
        }

        private void TryAttack(EnemyHealth target)
        {
            if (_isFire || !IsTargetValid(target))
                return;

            _animator.SetFloat("ShootSpeedMultiplier", _animationSpeed);
            _currentTarget = target;
            _isFire = true;
            _animator.SetTrigger("Shoot");
        }

        private void RotateToTargetSmooth(Vector3 targetPosition, float deltaTime)
        {
            var pivot = _turretPivot != null ? _turretPivot : transform;
            var direction = targetPosition - pivot.position;

            var targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + _angleOffset;
            var currentAngle = pivot.eulerAngles.z;
            var nextAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, _rotationSpeed * deltaTime);

            pivot.rotation = Quaternion.Euler(0f, 0f, nextAngle);
        }

        private void Fire(EnemyHealth target)
        {
            var firePoints = GetAvailableFirePoints();

            switch (_fireMode)
            {
                case EFireMode.All:
                    for (var i = 0; i < firePoints.Length; i++)
                    {
                        Shoot(target, firePoints[i].position);
                    }
                    break;

                case EFireMode.Alternate:
                    var point = firePoints[_nextFirePointIndex];
                    Shoot(target, point.position);

                    _nextFirePointIndex++;
                    if (_nextFirePointIndex >= firePoints.Length)
                        _nextFirePointIndex = 0;
                    break;

                default:
                    Shoot(target, firePoints[0].position);
                    break;
            }
        }

        private bool IsTargetValid(EnemyHealth target)
        {
            if (target == null)
                return false;

            if (target.IsDead)
                return false;

            var sqrDistance = (target.transform.position - transform.position).sqrMagnitude;
            return sqrDistance <= _currentRange * _currentRange;
        }

        private Transform[] GetAvailableFirePoints()
        {
            if (_firePoints != null && _firePoints.Count > 0)
                return _firePoints.ToArray();

            if (_firePoint != null)
                return new[] { _firePoint };

            return new[] { transform };
        }

        private void Shoot(EnemyHealth target, Vector3 origin)
        {
            var targetPosition = target.transform.position;
            var projectile = Instantiate(_projectilePrefab, origin, Quaternion.identity);

            var isCritical = Random.value < _criticalChance;
            var finalDamage = isCritical
                ? Mathf.RoundToInt(_damage * _currentCriticalDamageMultiplier)
                : _damage;
            projectile.Launch(origin, targetPosition, finalDamage, isCritical);
        }

        private EnemyHealth FindNearestEnemyInRange()
        {
            var enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            EnemyHealth best = null;
            var bestSqr = _currentRange * _currentRange;

            for (var i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null || enemies[i].IsDead)
                    continue;

                var sqr = (enemies[i].transform.position - transform.position).sqrMagnitude;
                if (sqr > bestSqr)
                    continue;

                bestSqr = sqr;
                best = enemies[i];
            }

            return best;
        }

        private void OnDestroy()
        {
            _generatorProduction?.Dispose();
            if (_playerStats != null)
                _playerStats.UpgradesChanged -= OnUpgradesChanged;
            if (_runCardState != null)
                _runCardState.ModifiersChanged -= OnRunCardModifiersChanged;
        }
    }
}
