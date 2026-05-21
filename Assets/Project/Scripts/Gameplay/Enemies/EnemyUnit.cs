using System;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Field;
using UnityEngine;

namespace Project.Scripts.Gameplay.Enemies
{
    public class EnemyUnit : MonoBehaviour, IGameUpdateListener
    {
        private float _moveSpeed;
        private int _damageToBase = 1;

        [SerializeField] private Animator _animator;
        
        private LanePath _lanePath;
        private BaseHealth _baseHealth;
        private int _targetWaypointIndex;
        private bool _isInitialized;
        private int _killRewardGold;
        private bool _isDead;
        private bool _isFinished;

        public static event Action<EnemyUnit, int> DieEnemy;
        public event Action<EnemyUnit> Finished;

        public void Initialize(LanePath lanePath, BaseHealth baseHealth, EnemyConfig config)
        {
            _lanePath = lanePath;
            _baseHealth = baseHealth;
            _targetWaypointIndex = 0;
            _isInitialized = _lanePath != null;
            _isDead = false;
            _isFinished = false;

            _moveSpeed = config.StartMoveSpeed;
            _damageToBase = config.StartDamage;
            _killRewardGold = config.KillRewardGold;

            var enemyHP = gameObject.GetComponent<IEnemyHealth>();
            enemyHP?.SetHealth(config.StartHealth);

            transform.position = _lanePath != null ? _lanePath.GetSpawnPosition() : transform.position;
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

            if (Vector3.SqrMagnitude(transform.position - targetPosition) <= 0.0001f)
                _targetWaypointIndex++;
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
