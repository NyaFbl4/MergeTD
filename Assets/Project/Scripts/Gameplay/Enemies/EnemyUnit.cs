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

        public void Initialize(LanePath lanePath, BaseHealth baseHealth, EnemyConfig config)
        {
            _lanePath = lanePath;
            _baseHealth = baseHealth;
            _targetWaypointIndex = 0;
            _isInitialized = _lanePath != null;

            _moveSpeed = config.StartMoveSpeed;
            _damageToBase = config.StartDamage;

            var enemyHP = this.gameObject.GetComponent<IEnemyHealth>();
            enemyHP.SetHealth(config.StartHealth);

            transform.position = _lanePath != null ? _lanePath.GetSpawnPosition() : transform.position;
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
            if (!_isInitialized || _lanePath == null)
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
            _moveSpeed = 0f;
            _animator.SetTrigger("IsDie");
        }

        public void DestroyEnemy()
        {
            Destroy(gameObject);
        }

        private void ReachBase()
        {
            _baseHealth?.ApplyDamage(_damageToBase);
            Destroy(gameObject);
        }
    }
}
