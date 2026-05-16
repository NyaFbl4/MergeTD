using System;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using VContainer.Unity;

namespace Project.Scripts.System.UseCases
{
    public class PlayerStatsUseCase : IPlayerStatsUseCase, IInitializable, IDisposable
    {
        private readonly LevelConfig _levelConfig;
        
        private int _gold;
        private int _currentWave;
        
        public int Gold => _gold;
        public int Wave => _currentWave;
        
        public event Action<int> OnGoldChanged;
        public event Action<int> WaveChanged;

        public void Initialize()
        {
            _gold = _levelConfig.StartGold;
            _currentWave = 1;
            OnGoldChanged?.Invoke(_gold);
            WaveChanged?.Invoke(_currentWave);
        }
        
        public PlayerStatsUseCase(LevelConfig levelConfig)
        {
            _levelConfig = levelConfig;
            
            //IGameListener.Register(this);
        }
        
        public bool CanSpend(int amount) => amount > 0 && _gold >= amount;

        public bool TrySpend(int amount)
        {
            if (!CanSpend(amount))
                return false;
            
            _gold -= amount;
            OnGoldChanged?.Invoke(_gold);
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            _gold += amount;
            OnGoldChanged?.Invoke(_gold);
        }

        public void SetWave(int amount)
        {
            var safeWave = Math.Max(1, amount);
            if (_currentWave == safeWave) return;
            _currentWave = safeWave;
            WaveChanged?.Invoke(_currentWave);
        }

        public void Dispose()
        {
            
        }
    }
}