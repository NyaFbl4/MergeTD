using UnityEngine;

namespace Project.Scripts.Gameplay.Enemies
{
    [RequireComponent(typeof(Collider2D))]
    public class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private int _maxHealth = 3;
        private int _currentHealth;

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0)
                return;

            _currentHealth -= damage;
            if (_currentHealth <= 0)
                Destroy(gameObject);
        }
    }
}
