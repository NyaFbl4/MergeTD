using System;
using Project.Scripts.GameManager;
using Project.Scripts.System.Save;
using UnityEngine;
using VContainer;

namespace Project.Scripts.Gameplay.Base
{
    public class BaseHealth : MonoBehaviour, IBaseHealth, IGameStartListener
    {
        private int _maxHealth;
        private int _currentHealth;
        private IGameManagerService _gameManagerService;
        private IWorldService _world;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        
        public event Action<int> OnCurrentHealthChanged;
        public event Action<int> OnMaxHealthChanged;
        public event Action Destroyed;

        [Inject]
        public void Construct(
            IGameManagerService gameManagerService,
            IWorldService world)
        {
            _gameManagerService = gameManagerService;
            _world = world;
        }
        
        public void AddMaxHealth(int health)
        {
            if (health <= 0)
                return;

            _maxHealth += health;
            _currentHealth += health;
            _world.SetMaxBaseHealth(_maxHealth);

            OnMaxHealthChanged?.Invoke(_maxHealth);
            OnCurrentHealthChanged?.Invoke(_currentHealth);
        }

        public void AddCurrentHealth(int health)
        {
            _currentHealth += health;
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            OnCurrentHealthChanged?.Invoke(_currentHealth);
        }

        public void SetHealthState(int currentHealth, int maxHealth)
        {
            _maxHealth = Mathf.Max(1, maxHealth);
            _currentHealth = Mathf.Clamp(currentHealth, 0, _maxHealth);

            OnMaxHealthChanged?.Invoke(_maxHealth);
            OnCurrentHealthChanged?.Invoke(_currentHealth);
        }

        public void ResetToStartHealth()
        {
            SetHealthState(_world.MaxBaseHealth, _world.MaxBaseHealth);
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
            // ProgressCheckpointUseCase restores saved/default health on game start.
        }

        public void ApplyDamage(int damage)
        {
            if (damage <= 0 || _currentHealth <= 0)
                return;

            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            OnCurrentHealthChanged?.Invoke(_currentHealth);

            if (_currentHealth > 0)
                return;

            Destroyed?.Invoke();
            _gameManagerService?.FinishGame();
        }

        private void ResetHealth()
        {
            ResetToStartHealth();
        }
    }
}
