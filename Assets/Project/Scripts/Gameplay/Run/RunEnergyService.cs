using System;

namespace Project.Scripts.Gameplay.Run
{
    public sealed class RunEnergyService
    {
        public const int MaxEnergy = 12;

        public int Current { get; private set; }
        public int Max => MaxEnergy;
        public event Action<int> Changed;

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            Restore((int)Math.Min(MaxEnergy, (long)Current + amount));
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
            var value = Math.Max(0, Math.Min(MaxEnergy, amount));
            if (value == Current)
                return;

            Current = value;
            Changed?.Invoke(Current);
        }
    }
}
