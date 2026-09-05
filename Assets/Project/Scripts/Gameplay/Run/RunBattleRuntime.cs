using System;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Systems;
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

        private bool _isGameRunning;

        public event Action<int, int, ERunPhase> WaveCompleted;

        public RunBattleRuntime(
            RunState state,
            BattlefieldRuntime battlefieldRuntime,
            ProgressCheckpointUseCase progressCheckpointUseCase,
            IGameManagerService gameManagerService,
            IPlayerStatsUseCase playerStatsUseCase,
            BaseHealth baseHealth)
        {
            _state = state;
            _battlefieldRuntime = battlefieldRuntime;
            _progressCheckpointUseCase = progressCheckpointUseCase;
            _gameManagerService = gameManagerService;
            _playerStatsUseCase = playerStatsUseCase;
            _baseHealth = baseHealth;

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

            var startWave = _progressCheckpointUseCase.RestoreCheckpointOrDefaults();
            _state.MoveToWave(startWave);
            _playerStatsUseCase.SetWave(_state.CurrentWave);
        }

        public void StartWave()
        {
            if (!_isGameRunning || !_state.CanEditDefense)
                return;

            _state.StartWave();
            _battlefieldRuntime.StartCurrentWave();
        }

        public void ContinueAfterEndWavePopup()
        {
            if (!_isGameRunning)
                return;

            if (_state.Phase == ERunPhase.Victory)
            {
                _gameManagerService.FinishGame();
                return;
            }

            if (_state.Phase == ERunPhase.Defeat)
                return;

            _state.ContinueToNextPreparation();
            _playerStatsUseCase.SetWave(_state.CurrentWave);
        }

        public void OnFinishGame()
        {
            _isGameRunning = false;
        }

        private void OnWaveExecutionCompleted(int completedWaveNumber)
        {
            var runWave = _state.CurrentWaveConfig;
            _state.CompleteWave();
            WaveCompleted?.Invoke(completedWaveNumber, runWave.CompleteRewardGold, _state.Phase);
        }

        private void OnBaseDestroyed()
        {
            _state.Defeat();
        }

        public void Dispose()
        {
            _battlefieldRuntime.WaveExecutionCompleted -= OnWaveExecutionCompleted;
            _baseHealth.Destroyed -= OnBaseDestroyed;
            IGameListener.Unregister(this);
        }
    }
}
