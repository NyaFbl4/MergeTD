using Project.Scripts.GameManager;
using UnityEngine;
using VContainer;

namespace Project.Scripts.Gameplay.Base
{
    public class BaseHealth : MonoBehaviour, IGameStartListener
    {
        [SerializeField, Min(1)] private int _maxHealth = 10;

        private int _currentHealth;
        private IGameManagerService _gameManagerService;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;

        [Inject]
        public void Construct(IGameManagerService gameManagerService)
        {
            _gameManagerService = gameManagerService;
        }

        private void OnEnable()
        {
            IGameListener.Register(this);
            ResetHealth();
        }

        private void OnDisable()
        {
            IGameListener.Unregister(this);
        }

        public void OnStartGame()
        {
            ResetHealth();
        }

        public void ApplyDamage(int damage)
        {
            if (damage <= 0 || _currentHealth <= 0)
                return;

            _currentHealth = Mathf.Max(0, _currentHealth - damage);

            if (_currentHealth > 0)
                return;

            _gameManagerService?.FinishGame();
        }

        private void ResetHealth()
        {
            _currentHealth = _maxHealth;
        }
    }
}
