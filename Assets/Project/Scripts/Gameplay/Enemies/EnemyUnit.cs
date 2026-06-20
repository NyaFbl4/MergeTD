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

        private float _moveSpeed;
        private int _damageToBase = 1;

        [SerializeField] private Animator _animator;
        [SerializeField] private EEnemyType _enemyType;
        [SerializeField] private SortingGroup _sortingGroup;
        
        private LanePath _lanePath;
        private BaseHealth _baseHealth;
        private Canvas[] _canvases;
        private int _targetWaypointIndex;
        private bool _isInitialized;
        private int _killRewardGold;
        private bool _isDead;
        private bool _isFinished;
        private EnemyConfig _config;
        
        public EnemyConfig Config => _config;
        public static event Action<EnemyUnit, int> DieEnemy;
        public event Action<EnemyUnit> Finished;
        public EEnemyType EnemyType => _enemyType;

        private void Awake()
        {
            CacheRenderOrderComponents();
        }
        
        public void Initialize(LanePath lanePath, 
            BaseHealth baseHealth, EnemyConfig config, 
            int killRewardGold, int startHealth)
        {
            CacheRenderOrderComponents();

            _lanePath = lanePath;
            _baseHealth = baseHealth;
            _targetWaypointIndex = 0;
            _isInitialized = _lanePath != null;
            _isDead = false;
            _isFinished = false;
            _config = config;

            _moveSpeed = _config.StartMoveSpeed * _config.GetMoveSpeedMultiplier(_enemyType);
            _damageToBase = _enemyType == EEnemyType.Boss ? BossDamageToBase : _config.StartDamage;
            _killRewardGold = killRewardGold;

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

            if (_targetWaypointIndex >= _lanePath.WaypointCount)
            {
                ReachBase();
                return;
            }

            var targetPosition = _lanePath.GetWaypointPosition(_targetWaypointIndex);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * deltaTime);
            UpdateRenderOrder();

            if (Vector3.SqrMagnitude(transform.position - targetPosition) <= 0.0001f)
                _targetWaypointIndex++;
        }

        private void CacheRenderOrderComponents()
        {
            if (_sortingGroup == null)
                _sortingGroup = GetComponent<SortingGroup>();

            _canvases ??= GetComponentsInChildren<Canvas>(true);
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

        public void IsDie()
        {
            if (_isDead)
                return;

            _isDead = true;
            _moveSpeed = 0f;

            if (_animator != null)
                _animator.SetTrigger("IsDie");
            
            DieEnemy?.Invoke(this, _killRewardGold);
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
