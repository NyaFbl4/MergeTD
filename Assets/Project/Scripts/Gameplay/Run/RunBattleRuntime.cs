using Project.Scripts.System.Analytics;
using System;
using System.Collections.Generic;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.Gameplay.Systems;
using Project.Scripts.Gameplay.UpgradeItem;
using Project.Scripts.Configs;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;
using VContainer.Unity;

namespace Project.Scripts.Gameplay.Run
{
    public class RunBattleRuntime : IStartable, IDisposable, IGameStartListener, IGameFinishListener
    {
        private readonly RunState _state;
        private readonly BattlefieldRuntime _battlefieldRuntime;
        private readonly ProgressCheckpointUseCase _progressCheckpointUseCase;
        private readonly IGameManagerService _gameManagerService;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;
        private readonly BaseHealth _baseHealth;
        private readonly SpellProgressionService _spellProgression;

        private bool _isGameRunning;

        public event Action<int, IReadOnlyList<WaveRewardConfig>, ERunPhase> WaveCompleted;
        public bool IsSpawnWarningActive => _battlefieldRuntime.IsSpawnWarningActive;
        public event Action<bool> SpawnWarningChanged
        {
            add => _battlefieldRuntime.SpawnWarningChanged += value;
            remove => _battlefieldRuntime.SpawnWarningChanged -= value;
        }
        public bool CanUseNextWaveButton => _isGameRunning
                                           && _state.Phase == ERunPhase.Preparation;

        public RunBattleRuntime(
            RunState state,
            BattlefieldRuntime battlefieldRuntime,
            ProgressCheckpointUseCase progressCheckpointUseCase,
            IGameManagerService gameManagerService,
            IPlayerStatsUseCase playerStatsUseCase,
            BaseHealth baseHealth,
            SpellProgressionService spellProgression)
        {
            _state = state;
            _battlefieldRuntime = battlefieldRuntime;
            _progressCheckpointUseCase = progressCheckpointUseCase;
            _gameManagerService = gameManagerService;
            _playerStatsUseCase = playerStatsUseCase;
            _baseHealth = baseHealth;
            _spellProgression = spellProgression;

            _battlefieldRuntime.WaveExecutionCompleted += OnWaveExecutionCompleted;
            _baseHealth.Destroyed += OnBaseDestroyed;
            IGameListener.Register(this);
        }

        public void Start()
        {
            // Wait for GameManagerService.StartGame.
        }

        public void OnStartGame()
        {
            _isGameRunning = true;

            if (!_battlefieldRuntime.PrepareForRun())
            {
                _isGameRunning = false;
                return;
            }

            _progressCheckpointUseCase.StartFreshRun();
            GameAnalytics.BeginRun(_state.RunId);
        }

        public void StartWave()
        {
            if (!_isGameRunning || !_state.CanEditDefense || !_state.TutorialInput.CanStartWave || !_state.SpellTutorialInput.CanStartWave)
                return;

            _state.StartWave();
            _battlefieldRuntime.StartCurrentWave();
            GameAnalytics.StartWave(_state.CurrentWave, _state.IsBossWave);
        }

        public void AdvanceFromLevelButton()
        {
            if (!CanUseNextWaveButton)
                return;

            if (_state.Phase == ERunPhase.Preparation)
            {
                StartWave();
                return;
            }

            ContinueToNextPreparation();
        }

        public void ContinueToNextPreparation()
        {
            if (!_isGameRunning)
                return;

            if (_state.Phase != ERunPhase.Reward && _state.Phase != ERunPhase.CardChoice)
                return;

            _state.ContinueToNextPreparation();
            _playerStatsUseCase.SetWave(_state.CurrentWave);
        }

        public void FinishVictory()
        {
            if (!_isGameRunning || _state.Phase != ERunPhase.Victory)
                return;

            _spellProgression.RecordVictory(_state.RunId);
            _isGameRunning = false;
            _gameManagerService.FinishGame();
        }

        public bool TryReviveCurrentWave()
        {
            if (_state.Phase != ERunPhase.Defeat)
                return false;

            if (!_battlefieldRuntime.TryResumeDefeatedWave())
                return false;

            if (!_state.TryResumeCurrentWave())
                return false;

            _baseHealth.ResetToStartHealth();
            _isGameRunning = true;
            _gameManagerService.ResumeGame();
            GameAnalytics.Revive();
            return _state.Phase == ERunPhase.Wave;
        }

        public void OnFinishGame()
        {
            GameAnalytics.EndRun(_state.Phase == ERunPhase.Defeat ? "defeat" : "finished");
            _isGameRunning = false;
        }

        private void OnWaveExecutionCompleted(int completedWaveNumber)
        {
            if (!_isGameRunning || _state.Phase != ERunPhase.Wave
                                || completedWaveNumber != _state.CurrentWave)
                return;

            var runWave = _state.CurrentWaveConfig;
            GameAnalytics.CompleteWave(_baseHealth.CurrentHealth, _baseHealth.MaxHealth);
            _state.CompleteWave();
            if (_state.Phase == ERunPhase.Victory)
                GameAnalytics.CompleteRun();
            WaveCompleted?.Invoke(completedWaveNumber, runWave.Rewards, _state.Phase);
        }

        private void OnBaseDestroyed()
        {
            if (_isGameRunning && _state.Phase == ERunPhase.Wave)
                GameAnalytics.DefeatWave();
            _state.Defeat();
        }

        public void Dispose()
        {
            _battlefieldRuntime.WaveExecutionCompleted -= OnWaveExecutionCompleted;
            _baseHealth.Destroyed -= OnBaseDestroyed;
            IGameListener.Unregister(this);
        }
    }

    public sealed class BaseUpgradeWaveRecoveryUseCase : IInitializable, IDisposable
    {
        private readonly RunBattleRuntime _runBattleRuntime;
        private readonly BaseUpgradeConfigProvider _configs;
        private readonly IPlayerStatsUseCase _playerStats;
        private readonly BaseHealth _baseHealth;
        private readonly RunEnergyService _energy;

        public BaseUpgradeWaveRecoveryUseCase(
            RunBattleRuntime runBattleRuntime,
            BaseUpgradeConfigProvider configs,
            IPlayerStatsUseCase playerStats,
            BaseHealth baseHealth,
            RunEnergyService energy)
        {
            _runBattleRuntime = runBattleRuntime;
            _configs = configs;
            _playerStats = playerStats;
            _baseHealth = baseHealth;
            _energy = energy;
        }

        public void Initialize()
        {
            _runBattleRuntime.WaveCompleted += OnWaveCompleted;
        }

        private void OnWaveCompleted(
            int _,
            IReadOnlyList<WaveRewardConfig> __,
            ERunPhase ___)
        {
            RestoreBaseHealth();
            RestoreEnergy();
        }

        private void RestoreBaseHealth()
        {
            var repairRatio = GetUpgradeValue(_configs.Repair);
            var repairAmount = repairRatio <= 0f
                ? 0
                : Math.Max(1, UnityEngine.Mathf.RoundToInt(_baseHealth.MaxHealth * repairRatio));
            if (repairAmount > 0)
                _baseHealth.AddCurrentHealth(repairAmount);
        }

        private void RestoreEnergy()
        {
            var restorationRatio = GetUpgradeValue(_configs.ManaRestoration);
            var restorationAmount = restorationRatio <= 0f
                ? 0
                : Math.Max(1, UnityEngine.Mathf.RoundToInt(_energy.Max * restorationRatio));
            _energy.Add(restorationAmount);
        }

        private float GetUpgradeValue(UpgradeItemConfig config)
        {
            return BaseUpgradeValueCalculator.GetCumulativeValue(
                config,
                _playerStats.GetUpgradeLevel(config.Id));
        }

        public void Dispose()
        {
            _runBattleRuntime.WaveCompleted -= OnWaveCompleted;
        }
    }
}
