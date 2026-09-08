using System;
using Project.Scripts.System.Save;

namespace Project.Scripts.Gameplay.Run
{
    public sealed class RunEnergyService : IDisposable
    {
        private readonly IWorldService _world;

        public int Current { get; private set; }
        public int Max => _world.MaxEnergy;
        public event Action<int> Changed;

        public RunEnergyService(IWorldService world)
        {
            _world = world;
            _world.MaxEnergyChanged += OnMaxEnergyChanged;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            Restore((int)Math.Min(Max, (long)Current + amount));
        }

        public bool TrySpend(int cost)
        {
            if (cost < 0 || Current < cost)
                return false;

            Restore(Current - cost);
            return true;
        }

        public void Reset() => Restore(0);

        public void Restore(int amount)
        {
            var value = Math.Max(0, Math.Min(Max, amount));
            if (value == Current)
                return;

            Current = value;
            Changed?.Invoke(Current);
        }

        private void OnMaxEnergyChanged(int value)
        {
            Current = Math.Min(Current, Max);
            Changed?.Invoke(Current);
        }

        public void Dispose()
        {
            _world.MaxEnergyChanged -= OnMaxEnergyChanged;
        }
    }
}
