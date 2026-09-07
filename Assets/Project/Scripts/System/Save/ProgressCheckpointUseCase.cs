using Project.Scripts.Gameplay;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Quests;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.System.Audio;
using Project.Scripts.System.UseCases;
using UnityEngine;

namespace Project.Scripts.System.Save
{
    public class ProgressCheckpointUseCase
    {
        private readonly ProgressSaveService _saveService;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;
        private readonly IBuyTowerUseCase _buyTowerUseCase;
        private readonly BattlefieldContext _battlefieldContext;
        private readonly BaseHealth _baseHealth;
        private readonly IUnitsCatalog _unitsCatalog;
        private readonly IAudioManager _audioManager;
        private readonly RunConfig _runConfig;
        private readonly QuestService _questService;
        private readonly RunEnergyService _energy;
        private readonly RunState _runState;

        public ProgressCheckpointUseCase(
            ProgressSaveService saveService,
            IPlayerStatsUseCase playerStatsUseCase,
            IBuyTowerUseCase buyTowerUseCase,
            BattlefieldContext battlefieldContext,
            BaseHealth baseHealth,
            IUnitsCatalog unitsCatalog,
            IAudioManager audioManager,
            RunConfig runConfig,
            QuestService questService,
            RunEnergyService energy,
            RunState runState)
        {
            _saveService = saveService;
            _playerStatsUseCase = playerStatsUseCase;
            _buyTowerUseCase = buyTowerUseCase;
            _battlefieldContext = battlefieldContext;
            _baseHealth = baseHealth;
            _unitsCatalog = unitsCatalog;
            _audioManager = audioManager;
            _runConfig = runConfig;
            _questService = questService;
            _energy = energy;
            _runState = runState;
        }

        public int RestoreCheckpointOrDefaults()
        {
            if (!_saveService.TryLoad(out var data))
            {
                RestoreDefaults();
                return 1;
            }

            ClearTowers();

            var wave = ClampWave(data.nextWave);
            _runState.MoveToWave(wave);
            _energy.Restore(data.energy);
            _playerStatsUseCase.ApplyState(
                data.gold,
                wave,
                data.selectedTowerLevel,
                data.towerDamageBonus,
                data.towerAttackSpeedBonus,
                data.towerCritChanceBonus,
                data.towerCritDamageBonus,
                data.upgrades);

            _questService.RestoreQuests(data.quests);
            _buyTowerUseCase.SetTowerCost(data.towerCost);
            _baseHealth.SetHealthState(data.currentBaseHealth, data.maxBaseHealth);
            RestoreTowers(data);

            return wave;
        }

        public void SaveCheckpoint(int nextWave)
        {
            SaveCheckpointInternal(nextWave, _baseHealth.CurrentHealth);
        }

        public void ClearCheckpoint()
        {
            _saveService.ClearCheckpoint();
        }

        public void SaveRetryCheckpoint(int wave)
        {
            SaveCheckpointInternal(wave, _baseHealth.MaxHealth);
        }

        private void SaveCheckpointInternal(int nextWave, int currentBaseHealth)
        {
            var maxBaseHealth = Mathf.Max(1, _baseHealth.MaxHealth);
            var data = new ProgressSaveData
            {
                nextWave = ClampWave(nextWave),
                gold = _playerStatsUseCase.Gold,
                energy = _energy.Current,
                selectedTowerLevel = _playerStatsUseCase.SelectedTowerLevel,
                towerCost = _buyTowerUseCase.TowerCost,
                currentBaseHealth = Mathf.Clamp(currentBaseHealth, 1, maxBaseHealth),
                maxBaseHealth = maxBaseHealth,
                towerDamageBonus = _playerStatsUseCase.TowerDamageBonus,
                towerAttackSpeedBonus = _playerStatsUseCase.TowerAttackSpeedBonus,
                towerCritChanceBonus = _playerStatsUseCase.TowerCritChanceBonus,
                towerCritDamageBonus = _playerStatsUseCase.TowerCritDamageBonus
            };

            data.quests = _questService.CaptureState();
            
            foreach (var upgrade in _playerStatsUseCase.UpgradeLevels)
                data.upgrades.Add(new UpgradeLevelSaveData(upgrade.Key, upgrade.Value));

            var slots = _battlefieldContext.TowerSlots;
            if (slots != null)
            {
                for (var i = 0; i < slots.Length; i++)
                {
                    var slot = slots[i];
                    if (slot == null || slot.CurrentTower == null)
                        continue;

                    data.towers.Add(new TowerSlotSaveData(i, slot.CurrentTower.CurrentLevel, slot.CurrentTower.TowerType));
                }
            }

            _saveService.Save(data);
        }

        private void RestoreDefaults()
        {
            ClearTowers();
            _runState.Reset();
            _energy.Reset();
            _playerStatsUseCase.ResetState();
            _buyTowerUseCase.ResetTowerCost();
            _baseHealth.SetHealthState(_runConfig.StartBaseHealth, _runConfig.StartBaseHealth);
        }

        private int ClampWave(int wave)
        {
            var wavesCount = Mathf.Max(1, _runConfig.Waves.Count);
            return Mathf.Clamp(wave, 1, wavesCount);
        }

        private void RestoreTowers(ProgressSaveData data)
        {
            if (data.towers == null)
                return;

            var slots = _battlefieldContext.TowerSlots;
            if (slots == null)
                return;

            for (var i = 0; i < data.towers.Count; i++)
            {
                var towerData = data.towers[i];
                if (towerData.slotIndex < 0 || towerData.slotIndex >= slots.Length)
                    continue;

                var slot = slots[towerData.slotIndex];
                if (slot == null)
                    continue;

                var towerPrefab = _unitsCatalog.GetTowerPrefabByLevel(towerData.towerLevel, towerData.towerType);
                if (towerPrefab == null)
                    continue;

                slot.TryPlaceTower(towerPrefab, _playerStatsUseCase, _audioManager);
            }
        }

        private void ClearTowers()
        {
            var slots = _battlefieldContext.TowerSlots;
            if (slots == null)
                return;

            for (var i = 0; i < slots.Length; i++)
                slots[i]?.ClearTower();
        }
    }
}
