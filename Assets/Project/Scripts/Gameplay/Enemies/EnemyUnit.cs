using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Field;
using UnityEngine;

namespace Project.Scripts.Gameplay.Enemies
{
    public class EnemyUnit : MonoBehaviour, IGameUpdateListener
    {
        [SerializeField, Min(0.1f)] private float _moveSpeed = 2f;
        [SerializeField, Min(1)] private int _damageToBase = 1;

        private LanePath _lanePath;
        private BaseHealth _baseHealth;
        private int _targetWaypointIndex;
        private bool _isInitialized;

        public void Initialize(LanePath lanePath, BaseHealth baseHealth)
        {
            _lanePath = lanePath;
            _baseHealth = baseHealth;
            _targetWaypointIndex = 0;
            _isInitialized = _lanePath != null;

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

        private void ReachBase()
        {
            _baseHealth?.ApplyDamage(_damageToBase);
            Destroy(gameObject);
        }
    }
}
