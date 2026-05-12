using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Enemies;
using TMPro;
using UnityEngine;

namespace Project.Scripts.Gameplay.Towers
{
    public class TowerUnit : MonoBehaviour, ITowerUnit, IGameUpdateListener
    {
        [SerializeField] private Transform _turretPivot;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private TMP_Text _currentLevelText;
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private float _angleOffset = -90f;
        [SerializeField] private float _rotationSpeed = 360f;

        [Header("Fire parametrs")]
        [SerializeField] private float _range = 3f;
        [SerializeField] private int _damage = 1;
        [SerializeField] private float _fireRate = 1f;
        
        [Header("Tower parametrs")]
        [SerializeField] private int _towerLevel = 1;

        private float _cooldown;
        private EnemyHealth _currentTarget;
        private bool _canFire;

        public void SetCanFire(bool canFire)
        {
            _canFire = canFire;
            if (!_canFire)
                _currentTarget = null;
        }

        private void OnEnable() => IGameListener.Register(this);
        private void OnDisable() => IGameListener.Unregister(this);

        public void CreateTower()
        {
            _currentLevelText.text = _towerLevel.ToString();
        }

        public void UpdateTower()
        {
            
        }
        
        public void OnUpdate(float deltaTime)
        {
            if (!_canFire) return;
            
            _cooldown -= deltaTime;
            _currentTarget = FindNearestEnemyInRange();

            if (_currentTarget != null)
                RotateToTargetSmooth(_currentTarget.transform.position, deltaTime);

            if (_cooldown > 0f || _currentTarget == null)
                return;

            Shoot(_currentTarget);
            _cooldown = 1f / Mathf.Max(0.01f, _fireRate);
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

        private void Shoot(EnemyHealth target)
        {
            if (_projectilePrefab == null)
            {
                target.TakeDamage(_damage);
                return;
            }

            var origin = _firePoint != null ? _firePoint.position : transform.position;
            var projectile = Instantiate(_projectilePrefab, origin, Quaternion.identity);
            projectile.Launch(target, _damage);
        }

        private EnemyHealth FindNearestEnemyInRange()
        {
            var enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            EnemyHealth best = null;
            var bestSqr = _range * _range;

            for (var i = 0; i < enemies.Length; i++)
            {
                var sqr = (enemies[i].transform.position - transform.position).sqrMagnitude;
                if (sqr > bestSqr)
                    continue;

                bestSqr = sqr;
                best = enemies[i];
            }

            return best;
        }
    }
}
