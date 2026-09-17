using System;
using Project.Scripts.System.Save;
using VContainer.Unity;

namespace Project.Scripts.System.UseCases
{
    public class PlayerStatsUseCase : IPlayerStatsUseCase, IInitializable, IDisposable
    {
        private readonly IWorldService _world;

        private int _currentWave;

        public int Gold => _world.Gold;
        public int Wave => _currentWave;
        public int SelectedTowerLevel => _world.SelectedTowerLevel;
        public float TowerCritChanceBonus => _world.TowerCritChanceBonus;
        public float TowerDamageBonus => _world.TowerDamageBonus;
        public float TowerAttackSpeedBonus => _world.TowerAttackSpeedBonus;
        public float TowerCritDamageBonus => _world.TowerCritDamageBonus;

        public event Action<int> OnGoldChanged;
        public event Action<int> WaveChanged;
        public event Action<int> SelectedTowerLevelChanged;
        public event Action UpgradesChanged;

        public PlayerStatsUseCase(IWorldService world)
        {
            _world = world;
            ResetValues();
        }

        public void Initialize()
        {
            _world.GoldChanged += OnWorldGoldChanged;
            _world.UpgradesChanged += OnWorldUpgradesChanged;
            NotifyStateChanged();
        }

        public void ResetState()
        {
            ResetValues();
            NotifyStateChanged();
        }

        public void ApplyState(int wave)
        {
            _currentWave = Math.Max(1, wave);
            NotifyStateChanged();
        }

        public int GetUpgradeLevel(string upgradeId) => _world.GetUpgradeLevel(upgradeId);

        public bool TryPurchaseUpgrade(string upgradeId, int expectedLevel, int price, EWorldUpgradeType type, float value) =>
            _world.TryPurchaseUpgrade(upgradeId, expectedLevel, price, type, value);

        public bool CanSpend(int amount) => _world.CanSpendGold(amount);

        public bool TrySpend(int amount)
        {
            return _world.TrySpendGold(amount);
        }

        public void AddGold(int amount)
        {
            _world.AddGold(amount);
        }

        public void SetWave(int amount)
        {
            var safeWave = Math.Max(1, amount);
            if (_currentWave == safeWave)
                return;

            _currentWave = safeWave;
            WaveChanged?.Invoke(_currentWave);
        }

        private void ResetValues()
        {
            _currentWave = 1;
        }

        private void NotifyStateChanged()
        {
            OnGoldChanged?.Invoke(Gold);
            SelectedTowerLevelChanged?.Invoke(SelectedTowerLevel);
            WaveChanged?.Invoke(_currentWave);
            UpgradesChanged?.Invoke();
        }

        public void Dispose()
        {
            _world.GoldChanged -= OnWorldGoldChanged;
            _world.UpgradesChanged -= OnWorldUpgradesChanged;
        }

        private void OnWorldGoldChanged(int value) => OnGoldChanged?.Invoke(value);
        private void OnWorldUpgradesChanged()
        {
            SelectedTowerLevelChanged?.Invoke(SelectedTowerLevel);
            UpgradesChanged?.Invoke();
        }
    }
}
