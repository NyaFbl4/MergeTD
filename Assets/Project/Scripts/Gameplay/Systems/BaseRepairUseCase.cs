using System;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Run;
using UnityEngine;
using VContainer.Unity;

namespace Project.Scripts.Gameplay.Systems
{
    public sealed class BaseRepairUseCase : IStartable, IDisposable, IGameUpdateListener, IGameFinishListener
    {
        private readonly RunState _runState;
        private readonly RunEnergyService _energy;
        private readonly BaseHealth _baseHealth;
        private readonly BaseRepairConfig _config;

        private float _cooldownRemaining;
        private int _displayedCooldownSeconds;

        public event Action StateChanged;

        public string DisplayName => _config.DisplayName;
        public int HealAmount => _config.HealAmount;
        public float CooldownRemaining => _cooldownRemaining;
        public bool IsBaseFull => _baseHealth.CurrentHealth >= _baseHealth.MaxHealth;
        public bool CanUse => _runState.CanUseAbilities
            && _cooldownRemaining <= 0f
            && _energy.Current >= _config.ManaCost
            && !IsBaseFull;

        public BaseRepairUseCase(
            RunState runState,
            RunEnergyService energy,
            BaseHealth baseHealth,
            SpellCatalog spellCatalog)
        {
            _runState = runState;
            _energy = energy;
            _baseHealth = baseHealth;
            _config = spellCatalog.Get<BaseRepairConfig>();
        }

        public void Start()
        {
            IGameListener.Register(this);
        }

        public bool TryUse()
        {
            if (!CanUse || !_energy.TrySpend(_config.ManaCost))
                return false;

            _cooldownRemaining = _config.Cooldown;
            _displayedCooldownSeconds = Mathf.CeilToInt(_cooldownRemaining);
            _baseHealth.AddCurrentHealth(_config.HealAmount);
            if (SpellPrefabVfx.PlayAt(_config, _baseHealth.transform.position) == null)
                BaseRepairVfx.Play(_baseHealth.transform.position);
            StateChanged?.Invoke();
            return true;
        }

        public void OnUpdate(float deltaTime)
        {
            if (_cooldownRemaining <= 0f)
                return;

            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);
            var cooldownSeconds = Mathf.CeilToInt(_cooldownRemaining);
            if (cooldownSeconds == _displayedCooldownSeconds)
                return;

            _displayedCooldownSeconds = cooldownSeconds;
            StateChanged?.Invoke();
        }

        public void OnFinishGame()
        {
            _cooldownRemaining = 0f;
            _displayedCooldownSeconds = 0;
            StateChanged?.Invoke();
        }

        public void Dispose()
        {
            IGameListener.Unregister(this);
        }
    }
}
