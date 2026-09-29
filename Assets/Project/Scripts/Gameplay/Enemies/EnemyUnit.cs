using System;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Field;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.Scripts.Gameplay.Enemies
{
    public class EnemyUnit : MonoBehaviour, IGameUpdateListener
    {
        private const int SortingOrderBase = 20;
        private const int SortingOrderPerProgress = 100;
        private const int CanvasSortingOffset = 10;
        private const int BossDamageToBase = 5;
        private const float MinBaseReachDistance = 0.25f;

        private float _baseMoveSpeed;
        private float _moveSpeed;
        private float _slowMoveSpeedMultiplier = 1f;
        private float _slowDurationRemaining;
        private float _stunDurationRemaining;
        private Vector2 _gravityTarget;
        private float _gravityPullSpeed;
        private float _gravityPullDurationRemaining;
        private int _damageToBase = 1;

        [SerializeField] private Animator _animator;
        [SerializeField] private EEnemyType _enemyType;
        [SerializeField] private SortingGroup _sortingGroup;
        
        private LanePath _lanePath;
        private BaseHealth _baseHealth;
        private Canvas[] _canvases;
        private Collider2D[] _colliders;
        private int _targetWaypointIndex;
        private bool _isInitialized;
        private bool _isDead;
        private bool _isFinished;
        private EnemyConfig _config;
        
        public EnemyConfig Config => _config;
        public static event Action<EnemyUnit> DieEnemy;
        public event Action<EnemyUnit> Finished;
        public EEnemyType EnemyType => _enemyType;

        private void Awake()
        {
            CacheRenderOrderComponents();
        }
        
        public void Initialize(LanePath lanePath, 
            BaseHealth baseHealth, EnemyConfig config, 
            int startHealth)
        {
            CacheRenderOrderComponents();

            _lanePath = lanePath;
            _baseHealth = baseHealth;
            _targetWaypointIndex = 0;
            _isInitialized = _lanePath != null;
            _isDead = false;
            _isFinished = false;
            _config = config;

            _baseMoveSpeed = _config.StartMoveSpeed * _config.GetMoveSpeedMultiplier(_enemyType);
            _slowMoveSpeedMultiplier = 1f;
            _slowDurationRemaining = 0f;
            _stunDurationRemaining = 0f;
            _gravityPullDurationRemaining = 0f;
            RefreshMoveSpeed();
            _damageToBase = _enemyType == EEnemyType.Boss ? BossDamageToBase : _config.StartDamage;

            var enemyHP = gameObject.GetComponent<IEnemyHealth>();
            enemyHP?.SetHealth(startHealth);

            transform.position = _lanePath != null ? _lanePath.GetSpawnPosition() : transform.position;
            UpdateRenderOrder();
        }

        private void Finish()
        {
            if (_isFinished)
                return;

            _isFinished = true;
            Finished?.Invoke(this);
        }
        
        private void OnEnable()
        {
            IGameListener.Register(this);
        }

        private void OnDisable()
        {
            IGameListener.Unregister(this);
        }

        public void OnUpdate(float deltaTime)
        {
            if (!_isInitialized || _lanePath == null || _isDead)
                return;

            UpdateSlow(deltaTime);
            UpdateStun(deltaTime);

            if (UpdateGravityPull(deltaTime))
                return;

            if (_targetWaypointIndex >= _lanePath.WaypointCount)
            {
                ReachBase();
                return;
            }

            var targetPosition = _lanePath.GetWaypointPosition(_targetWaypointIndex);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * deltaTime);
            UpdateRenderOrder();

            if (IsCloseEnoughToBase(targetPosition))
            {
                ReachBase();
                return;
            }

            if (Vector3.SqrMagnitude(transform.position - targetPosition) <= 0.0001f)
                _targetWaypointIndex++;
        }

        public bool TryApplySlow(float moveSpeedMultiplier, float duration)
        {
            if (!_isInitialized || _isDead || duration <= 0f)
                return false;

            _slowMoveSpeedMultiplier = Mathf.Min(
                _slowMoveSpeedMultiplier,
                Mathf.Clamp(moveSpeedMultiplier, 0.01f, 1f));
            _slowDurationRemaining = Mathf.Max(_slowDurationRemaining, duration);
            RefreshMoveSpeed();
            return true;
        }

        public bool TryApplyStun(float duration)
        {
            if (!_isInitialized || _isDead || duration <= 0f)
                return false;

            _stunDurationRemaining = Mathf.Max(_stunDurationRemaining, duration);
            RefreshMoveSpeed();
            return true;
        }

        public bool TryApplyGravityPull(Vector2 target, float pullSpeed, float duration)
        {
            if (!_isInitialized || _isDead || pullSpeed <= 0f || duration <= 0f)
                return false;

            var wasInactive = _gravityPullDurationRemaining <= 0f;
            _gravityTarget = target;
            _gravityPullSpeed = pullSpeed;
            _gravityPullDurationRemaining = Mathf.Max(_gravityPullDurationRemaining, duration);

            if (wasInactive)
                RefreshMoveSpeed();

            return true;
        }

        public void ClearGravityPull()
        {
            if (_gravityPullDurationRemaining <= 0f)
                return;

            _gravityPullDurationRemaining = 0f;
            RefreshMoveSpeed();
        }

        private void UpdateSlow(float deltaTime)
        {
            if (_slowDurationRemaining <= 0f)
                return;

            _slowDurationRemaining = Mathf.Max(0f, _slowDurationRemaining - deltaTime);
            if (_slowDurationRemaining > 0f)
                return;

            _slowMoveSpeedMultiplier = 1f;
            RefreshMoveSpeed();
        }

        private void UpdateStun(float deltaTime)
        {
            if (_stunDurationRemaining <= 0f)
                return;

            _stunDurationRemaining = Mathf.Max(0f, _stunDurationRemaining - deltaTime);
            if (_stunDurationRemaining <= 0f)
                RefreshMoveSpeed();
        }

        private bool UpdateGravityPull(float deltaTime)
        {
            if (_gravityPullDurationRemaining <= 0f)
                return false;

            transform.position = Vector3.MoveTowards(
                transform.position,
                _gravityTarget,
                _gravityPullSpeed * deltaTime);
            UpdateRenderOrder();

            _gravityPullDurationRemaining = Mathf.Max(
                0f,
                _gravityPullDurationRemaining - deltaTime);
            if (_gravityPullDurationRemaining <= 0f)
                RefreshMoveSpeed();

            return true;
        }

        private void RefreshMoveSpeed()
        {
            var controlMultiplier = _stunDurationRemaining > 0f
                                    || _gravityPullDurationRemaining > 0f
                ? 0f
                : _slowMoveSpeedMultiplier;
            _moveSpeed = _baseMoveSpeed * controlMultiplier;
            if (_animator != null)
                _animator.speed = controlMultiplier;
        }

        private void CacheRenderOrderComponents()
        {
            if (_sortingGroup == null)
                _sortingGroup = GetComponent<SortingGroup>();

            _canvases ??= GetComponentsInChildren<Canvas>(true);
            _colliders ??= GetComponentsInChildren<Collider2D>(true);
        }

        private void UpdateRenderOrder()
        {
            var sortingOrder = CalculateSortingOrder();

            if (_sortingGroup != null)
                _sortingGroup.sortingOrder = sortingOrder;

            if (_canvases == null)
                return;

            for (var i = 0; i < _canvases.Length; i++)
            {
                if (_canvases[i] != null)
                    _canvases[i].sortingOrder = sortingOrder + CanvasSortingOffset;
            }
        }

        private int CalculateSortingOrder()
        {
            if (_lanePath == null || _lanePath.WaypointCount == 0)
                return SortingOrderBase + Mathf.RoundToInt(-transform.position.y * SortingOrderPerProgress);

            var progress = Mathf.Clamp01(_lanePath.GetProgressToEnd(transform.position, _targetWaypointIndex));
            return SortingOrderBase + Mathf.RoundToInt(progress * SortingOrderPerProgress);
        }

        private bool IsCloseEnoughToBase(Vector3 targetPosition)
        {
            if (_lanePath == null || _targetWaypointIndex < _lanePath.WaypointCount - 1)
                return false;

            var reachDistance = GetBaseReachDistance();
            return Vector3.SqrMagnitude(transform.position - targetPosition) <= reachDistance * reachDistance;
        }

        private float GetBaseReachDistance()
        {
            var reachDistance = MinBaseReachDistance;

            if (_colliders == null)
                return reachDistance;

            for (var i = 0; i < _colliders.Length; i++)
            {
                var enemyCollider = _colliders[i];
                if (enemyCollider == null || !enemyCollider.enabled)
                    continue;

                var extents = enemyCollider.bounds.extents;
                reachDistance = Mathf.Max(reachDistance, extents.x, extents.y);
            }

            return reachDistance;
        }

        public void IsDie()
        {
            if (_isDead)
                return;

            _isDead = true;
            _moveSpeed = 0f;

            if (_animator != null)
            {
                _animator.speed = 1f;
                _animator.SetTrigger("IsDie");
            }
            
            DieEnemy?.Invoke(this);
            Finish();
        }

        public void DestroyEnemy()
        {
            Destroy(gameObject);
        }

        private void ReachBase()
        {
            _isDead = true;
            _baseHealth?.ApplyDamage(_damageToBase);
            Finish();
            Destroy(gameObject);
        }
    }
}
