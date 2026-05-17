using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Gameplay.Enemies
{
    [RequireComponent(typeof(Collider2D))]
    public class EnemyHealth : MonoBehaviour, IEnemyHealth
    {
        [SerializeField] private EnemyUnit _enemy;
        [Header("Health")]
        [SerializeField] private int _maxHealth;
        [SerializeField] private int _currentHealth;
        
        [Header("HP Bar")]
        [SerializeField] private Canvas _hpCanvas;
        [SerializeField] private Image _hpFill;
        [SerializeField] private bool _hideWhenFull;
        [SerializeField] private bool _hideWhenDead = true;

        public void SetHealth(int health)
        {
            _maxHealth = health;
            _currentHealth = _maxHealth;
            UpdateHpBar();
        }
        
        public void TakeDamage(int damage)
        {
            if (damage <= 0)
                return;

            _currentHealth -= damage;
            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                UpdateHpBar();

                if (_hideWhenDead && _hpCanvas != null)
                    _hpCanvas.gameObject.SetActive(false);

                //Destroy(gameObject);
                _enemy.IsDie();
                return;
            }
            UpdateHpBar();
        }
        
        private void UpdateHpBar()
        {
            if (_hpFill == null)
                return;

            var t = _maxHealth <= 0 ? 0f : Mathf.Clamp01((float)_currentHealth / _maxHealth);
            _hpFill.fillAmount = t;

            if (_hpCanvas != null)
            {
                if (_hideWhenFull)
                    _hpCanvas.gameObject.SetActive(t < 0.999f);
                else
                    _hpCanvas.gameObject.SetActive(true);
            }
        }        
    }
}
