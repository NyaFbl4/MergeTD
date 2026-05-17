using Project.Scripts.Gameplay.Enemies;
using UnityEngine;

namespace Project.Scripts.Gameplay.Towers
{
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _lifeTime = 3f;
        [SerializeField] private int _defaultDamage = 1;
        [SerializeField] private float _angleOffset = -90f;

        private Vector3 _direction;
        private int _damage;
        private bool _isLaunched;

        private Rigidbody2D _rigidbody2D;
        private Collider2D _collider2D;

        private void Awake()
        {
            _collider2D = GetComponent<Collider2D>();
            if (_collider2D != null)
                _collider2D.isTrigger = true;

            _rigidbody2D = GetComponent<Rigidbody2D>();
            if (_rigidbody2D == null)
                _rigidbody2D = gameObject.AddComponent<Rigidbody2D>();

            _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void Launch(Vector3 origin, Vector3 targetPosition, int damage)
        {
            transform.position = origin;

            _direction = (targetPosition - origin).normalized;
            if (_direction.sqrMagnitude <= 0.000001f)
                _direction = Vector3.right;

            _damage = damage > 0 ? damage : _defaultDamage;
            _isLaunched = true;

            var angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + _angleOffset);

            Destroy(gameObject, _lifeTime);
        }

        private void Update()
        {
            if (!_isLaunched)
                return;

            transform.position += _direction * (_speed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isLaunched)
                return;

            var enemy = other.GetComponent<IEnemyHealth>();
            if (enemy == null)
                return;

            enemy.TakeDamage(_damage);
            Destroy(gameObject);
        }
    }
}
