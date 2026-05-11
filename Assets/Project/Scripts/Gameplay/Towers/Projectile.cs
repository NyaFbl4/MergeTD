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

        private EnemyHealth _target;
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

        public void Launch(EnemyHealth target, int damage)
        {
            _target = target;
            _damage = damage > 0 ? damage : _defaultDamage;
            _isLaunched = true;

            Destroy(gameObject, _lifeTime);
        }

        private void Update()
        {
            if (!_isLaunched)
                return;

            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            var targetPosition = _target.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _speed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isLaunched)
                return;

            var enemy = other.GetComponent<EnemyHealth>();
            if (enemy == null)
                return;

            enemy.TakeDamage(_damage);
            Destroy(gameObject);
        }
    }
}
