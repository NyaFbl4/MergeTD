using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Enemies;
using TMPro;
using UnityEngine;

namespace Project.Scripts.Gameplay.Towers
{
    public class TowerUnit : MonoBehaviour, ITowerUnit, IGameUpdateListener
    {
        [SerializeField] private TowerConfig _towerConfig;
        [SerializeField] private Transform _turretPivot;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private TMP_Text _currentLevelText;
        [SerializeField] private Animator _animator;
        [SerializeField] private Projectile _projectilePrefab;

        [Header("Fire parametrs")]
        [SerializeField] private float _range = 3f;
        private int _damage;
        private float _fireRate;
        
        [Header("Tower parametrs")]
        [SerializeField] private int _towerLevel = 1;
        [SerializeField] private float _angleOffset = -90f;
        [SerializeField] private float _rotationSpeed = 360f;
        [SerializeField] private float _animationSpeed;
        
        public int CurrentLevel => _towerLevel;
        public TowerConfig TowerConfig => _towerConfig;
        
        private float _cooldown;
        private EnemyHealth _currentTarget;
        private bool _canFire;
        private bool _isFire;

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

            _damage = _towerConfig.StartTowerDamage;
            _fireRate = _towerConfig.StartAttackSpeed;
            _animationSpeed = _towerConfig.AnimationSpeed;
        }
        
        public bool CanMergeWith(TowerUnit other)
        {
            if (other == null)
                return false;

            if (other == this)
                return false;

            if (other.TowerConfig != TowerConfig)
                return false;

            return other.CurrentLevel == CurrentLevel;
        }

        public void OnAttackFireEvent()
        {
            if (_currentTarget == null) return;
            Shoot(_currentTarget);
        }
        
        public void OnAttackFinishedEvent()
        {
            _isFire = false;
            _currentTarget = null;
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

            TryAttack(_currentTarget);
            _cooldown = 1f / Mathf.Max(0.01f, _fireRate);
        }

        private void TryAttack(EnemyHealth target)
        {
            if (_isFire || target == null) return;
            
            //_animator.SetFloat("Shoot", _animationSpeed);
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

        private void Shoot(EnemyHealth target)
        {
            var origin = _firePoint != null ? _firePoint.position : transform.position;
            var targetPosition = target.transform.position;
            var projectile = Instantiate(_projectilePrefab, origin, Quaternion.identity);
            projectile.Launch(origin, targetPosition, _damage);
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
