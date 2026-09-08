using System.Collections.Generic;
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
        private readonly WorldService _world;

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
            RunState runState,
            WorldService world)
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
            _world = world;
        }

        public int RestoreCheckpointOrDefaults()
        {
            if (!_saveService.TryLoad(out var data))
            {
                RestoreDefaults();
                RestoreWorldTowers();
                return 1;
            }

            ClearTowers(false);
            ImportLegacyWorld(data);

            var wave = ClampWave(data.nextWave);
            _runState.MoveToWave(wave);
            _energy.Restore(data.energy);
            _playerStatsUseCase.ApplyState(
                _world.Gold,
                wave,
                data.selectedTowerLevel,
                data.towerDamageBonus,
                data.towerAttackSpeedBonus,
                data.towerCritChanceBonus,
                data.towerCritDamageBonus,
                data.upgrades);

            _questService.RestoreQuests(data.quests);
            _buyTowerUseCase.SetTowerCost(data.towerCost);
            _baseHealth.SetHealthState(data.currentBaseHealth, _world.MaxBaseHealth);
            RestoreWorldTowers();

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
            var maxBaseHealth = Mathf.Max(1, _world.MaxBaseHealth);
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
            ClearTowers(false);
            _runState.Reset();
            _energy.Reset();
            _playerStatsUseCase.ResetState();
            _buyTowerUseCase.ResetTowerCost();
            _baseHealth.SetHealthState(_world.MaxBaseHealth, _world.MaxBaseHealth);
        }

        private int ClampWave(int wave)
        {
            var wavesCount = Mathf.Max(1, _runConfig.Waves.Count);
            return Mathf.Clamp(wave, 1, wavesCount);
        }

        private void RestoreWorldTowers()
        {
            for (var i = 0; i < _world.Towers.Count; i++)
            {
                var towerData = _world.Towers[i];
                var slot = _battlefieldContext.FindSlotByPersistentId(towerData.slotId);
                if (slot == null)
                    continue;

                var towerPrefab = _unitsCatalog.GetTowerPrefabByLevel(towerData.towerLevel, towerData.towerType);
                if (towerPrefab == null)
                    continue;

                slot.TryPlaceTower(towerPrefab, _playerStatsUseCase, _audioManager, false);
            }
        }

        private void ImportLegacyWorld(ProgressSaveData data)
        {
            if (_world.HasPersistedData)
                return;

            var towers = new List<WorldTowerSaveData>();
            var slots = _battlefieldContext.TowerSlots;

            if (data.towers != null && slots != null)
            {
                for (var i = 0; i < data.towers.Count; i++)
                {
                    var tower = data.towers[i];
                    if (tower.slotIndex < 0 || tower.slotIndex >= slots.Length || slots[tower.slotIndex] == null)
                        continue;

                    towers.Add(new WorldTowerSaveData(
                        slots[tower.slotIndex].PersistentId,
                        tower.towerLevel,
                        tower.towerType));
                }
            }

            _world.ImportLegacy(data.gold, data.maxBaseHealth, towers);
        }

        private void ClearTowers(bool persistWorldChange)
        {
            var slots = _battlefieldContext.TowerSlots;
            if (slots == null)
                return;

            for (var i = 0; i < slots.Length; i++)
                slots[i]?.ClearTower(persistWorldChange);
        }
    }
}
