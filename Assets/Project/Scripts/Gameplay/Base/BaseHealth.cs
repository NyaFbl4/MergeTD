using System;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using UnityEngine;
using VContainer;

namespace Project.Scripts.Gameplay.Base
{
    public class BaseHealth : MonoBehaviour, IBaseHealth, IGameStartListener
    {
        private LevelConfig _levelConfig;
        private int _maxHealth;
        private int _currentHealth;
        private IGameManagerService _gameManagerService;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        
        public event Action<int> OnCurrentHealthChanged;
        public event Action<int> OnMaxHealthChanged;
        public event Action Destroyed;

        [Inject]
        public void Construct(IGameManagerService gameManagerService, LevelConfig levelConfig)
        {
            _gameManagerService = gameManagerService;
            _levelConfig = levelConfig;
        }
        
        public void AddMaxHealth(int health)
        {
            _maxHealth += health;
            _currentHealth += health;

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
            SetHealthState(_levelConfig.StartBaseHealth, _levelConfig.StartBaseHealth);
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