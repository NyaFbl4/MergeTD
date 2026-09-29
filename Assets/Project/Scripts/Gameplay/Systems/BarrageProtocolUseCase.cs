using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Towers;
using UnityEngine;
using VContainer.Unity;

namespace Project.Scripts.Gameplay.Systems
{
    public sealed class BarrageProtocolUseCase : IStartable, IDisposable, IGameUpdateListener, IGameFinishListener
    {
        private readonly RunState _runState;
        private readonly BattlefieldContext _battlefieldContext;
        private readonly BarrageProtocolConfig _config;
        private readonly List<BarrageProtocolVfx> _activeEffects = new();

        private float _cooldownRemaining;
        private float _activeDurationRemaining;

        public event Action StateChanged;

        public float CooldownRemaining => _cooldownRemaining;
        public float ActiveDurationRemaining => _activeDurationRemaining;
        public float AttackSpeedMultiplier => _config.AttackSpeedMultiplier;
        public bool IsActive => _activeDurationRemaining > 0f;
        public bool HasCombatTower => FindCombatTower();
        public bool CanUse => _runState.CanUseAbilities
            && !IsActive
            && _cooldownRemaining <= 0f
            && HasCombatTower;

        public BarrageProtocolUseCase(
            RunState runState,
            BattlefieldContext battlefieldContext,
            SpellCatalog spellCatalog)
        {
            _runState = runState;
            _battlefieldContext = battlefieldContext;
            _config = spellCatalog.Get<BarrageProtocolConfig>();
        }

        public void Start()
        {
            IGameListener.Register(this);
        }

        public bool TryUse()
        {
            if (!CanUse)
                return false;

            _cooldownRemaining = _config.Cooldown;
            _activeDurationRemaining = _config.Duration;
            ApplyAttackSpeedMultiplier(_config.AttackSpeedMultiplier, true);
            StateChanged?.Invoke();
            return true;
        }

        public void OnUpdate(float deltaTime)
        {
            var previousCooldownSeconds = Mathf.CeilToInt(_cooldownRemaining);
            var previousActiveSeconds = Mathf.CeilToInt(_activeDurationRemaining);

            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);

            if (IsActive)
            {
                if (_runState.CanUseAbilities)
                {
                    _activeDurationRemaining = Mathf.Max(0f, _activeDurationRemaining - deltaTime);
                    if (!IsActive)
                        StopActiveEffect();
                }
                else
                {
                    StopActiveEffect();
                }
            }

            if (previousCooldownSeconds != Mathf.CeilToInt(_cooldownRemaining)
                || previousActiveSeconds != Mathf.CeilToInt(_activeDurationRemaining))
            {
                StateChanged?.Invoke();
            }
        }

        public void OnFinishGame()
        {
            StopActiveEffect();
            _cooldownRemaining = 0f;
            StateChanged?.Invoke();
        }

        public void Dispose()
        {
            StopActiveEffect();
            IGameListener.Unregister(this);
        }

        private bool FindCombatTower()
        {
            var slots = _battlefieldContext.TowerSlots;
            for (var i = 0; i < slots.Length; i++)
            {
                var tower = slots[i]?.CurrentTower;
                if (tower != null && tower.TowerType != ETowerType.Generator)
                    return true;
            }

            return false;
        }

        private void ApplyAttackSpeedMultiplier(float multiplier, bool playEffect)
        {
            var slots = _battlefieldContext.TowerSlots;
            for (var i = 0; i < slots.Length; i++)
            {
                var tower = slots[i]?.CurrentTower;
                if (tower == null || tower.TowerType == ETowerType.Generator)
                    continue;

                tower.SetSpellAttackSpeedMultiplier(multiplier);
                if (playEffect)
                    _activeEffects.Add(BarrageProtocolVfx.Play(tower.transform, _config.Duration));
            }
        }

        private void StopActiveEffect()
        {
            if (!IsActive && _activeEffects.Count == 0)
                return;

            _activeDurationRemaining = 0f;
            ApplyAttackSpeedMultiplier(1f, false);

            for (var i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i] != null)
                    _activeEffects[i].Stop();
            }

            _activeEffects.Clear();
        }
    }
}
