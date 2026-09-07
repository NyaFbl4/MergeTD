using System;
using Project.Scripts.Gameplay.Run;

namespace Project.Scripts.Gameplay.Towers
{
    public sealed class GeneratorProduction : IDisposable
    {
        private readonly RunState _runState;
        private readonly RunEnergyService _energy;
        private readonly double _interval;
        private readonly int _amount;
        private double _elapsed;
        private bool _isDisposed;

        public GeneratorProduction(RunState runState, RunEnergyService energy, float interval, int amount)
        {
            if (interval <= 0f || float.IsNaN(interval) || float.IsInfinity(interval))
                throw new ArgumentOutOfRangeException(nameof(interval));
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            _runState = runState;
            _energy = energy;
            _interval = interval;
            _amount = amount;
            _runState.PhaseChanged += OnPhaseChanged;
        }

        // Called by the game's update loop, which stops while paused or finished.
        public void Tick(float deltaTime, bool isPlaced)
        {
            if (_isDisposed || !isPlaced || _runState.Phase != ERunPhase.Wave)
                return;
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                return;

            _elapsed += deltaTime;
            while (_elapsed >= _interval)
            {
                _elapsed -= _interval;
                _energy.Add(_amount);
            }
        }

        private void OnPhaseChanged(ERunPhase phase) => _elapsed = 0d;

        public void Dispose()
        {
            _isDisposed = true;
            _runState.PhaseChanged -= OnPhaseChanged;
        }
    }
}
