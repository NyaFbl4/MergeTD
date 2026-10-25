using Project.Scripts.System.Analytics;
using System;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Run.Cards;
using UnityEngine;

namespace Project.Scripts.Gameplay.Towers
{
    public sealed class GeneratorProduction : IDisposable
    {
        private readonly RunState _runState;
        private readonly RunEnergyService _energy;
        private readonly RunCardRuntimeState _runCardState;
        private readonly double _interval;
        private readonly int _amount;
        private double _elapsed;
        private bool _isDisposed;

        public float NormalizedProgress => (float)(_elapsed / _interval);

        public GeneratorProduction(
            RunState runState,
            RunEnergyService energy,
            RunCardRuntimeState runCardState,
            float interval,
            int amount)
        {
            if (interval <= 0f || float.IsNaN(interval) || float.IsInfinity(interval))
                throw new ArgumentOutOfRangeException(nameof(interval));
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            _runState = runState;
            _energy = energy;
            _runCardState = runCardState;
            _interval = interval;
            _amount = amount;
            _runState.PhaseChanged += OnPhaseChanged;
        }

        // Called by the game's update loop, which stops while paused or finished.
        public bool Tick(float deltaTime, bool isPlaced)
        {
            if (_isDisposed || !isPlaced || _runState.Phase != ERunPhase.Wave)
                return false;
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                return true;

            _elapsed += deltaTime;
            while (_elapsed >= _interval)
            {
                _elapsed -= _interval;
                var amount = Mathf.Max(1, Mathf.RoundToInt(
                    _amount * (_runCardState?.GeneratorEnergyMultiplier ?? 1f)));
                var energyBefore = _energy.Current;
                _energy.Add(amount);
                GameAnalytics.Count("generator_used", _energy.Current - energyBefore);
            }

            return true;
        }

        private void OnPhaseChanged(ERunPhase phase) => _elapsed = 0d;

        public void Dispose()
        {
            _isDisposed = true;
            _runState.PhaseChanged -= OnPhaseChanged;
        }
    }
}
