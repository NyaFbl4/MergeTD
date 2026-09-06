using System;
using System.Collections.Generic;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Enemies;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Wave;
using Project.Scripts.System.UseCases;
using UnityEngine;
using Random = UnityEngine.Random;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Project.Scripts.Gameplay.Systems
{
    public class BattlefieldRuntime : IStartable, IDisposable, IGameFinishListener, IGameUpdateListener
    {
        private readonly BattlefieldContext _context;
        private readonly RunState _runState;
        private const float MinSpawnInterval = 0.22f;

        private readonly IPlayerStatsUseCase _playerStatsUseCase;
        
        private bool _isWaitingWaveStart;
        private float _waveStartTimer;
        private float _spawnCooldown;
        private int _nextSequenceRuntimeIndex;
        
        private readonly List<EnemySpawnSequenceRuntime> _sequenceRuntimes = new();
        private int _aliveEnemies;

        private bool _isWaveRunning;
        private bool _isGameRunning;
        
        private int CurrentWaveNumber => _runState.CurrentWave;
        public event Action<int> WaveExecutionCompleted;

        public BattlefieldRuntime(
            BattlefieldContext context,
            RunState runState,
            IPlayerStatsUseCase playerStatsUseCase)
        {
            _context = context;
            _runState = runState;
            _playerStatsUseCase = playerStatsUseCase;
        }

        public void Start()
        {
            IGameListener.Register(this);
        }

        public void Dispose()
        {
            IGameListener.Unregister(this);
        }

        public bool PrepareForRun()
        {
            if (_context == null || !_context.IsReady())
            {
                Debug.LogWarning("BattlefieldRuntime: BattlefieldContext is not ready. Check lanes, base, and enemy prefab.");
                return false;
            }
            
            Debug.Log("BattlefieldRuntime: Battlefield Start");
            
            _sequenceRuntimes.Clear();
            ClearEnemiesRoot();
            _aliveEnemies = 0;
            
            _isWaitingWaveStart = false;
            _waveStartTimer = 0f;
            _spawnCooldown = 0f;
            _nextSequenceRuntimeIndex = 0;

            _isGameRunning = true;
            _isWaveRunning = false;
            return true;
        }

        private void ClearEnemiesRoot()
        {
            var enemiesRoot = _context.EnemiesRoot;
            if (enemiesRoot == null)
                return;

            for (var i = enemiesRoot.childCount - 1; i >= 0; i--)
            {
                var enemy = enemiesRoot.GetChild(i);
                if (enemy == null)
                    continue;

                enemy.gameObject.SetActive(false);
                Object.Destroy(enemy.gameObject);
            }
        }
        public void OnFinishGame()
        {
            _isGameRunning = false;
            _isWaveRunning = false;
            _sequenceRuntimes.Clear();
        }

        public void OnUpdate(float deltaTime)
        {
            if (!_isGameRunning)
                return;

            if (_isWaitingWaveStart)
            {
                UpdateWaveStartDelay(deltaTime);
                return;
            }

            if (_isWaveRunning)
            {
                UpdateWaveSpawn(deltaTime);
                return;
            }

        }
        
        public void StartCurrentWave()
        {
            if (_runState.CurrentWave > _runState.MaxWaves)
            {
                Debug.Log("All waves completed");
                return;
            }

            _sequenceRuntimes.Clear();
            _spawnCooldown = 0f;
            _nextSequenceRuntimeIndex = 0;

            var wave = _runState.CurrentWaveConfig.WaveConfig;
            Debug.Log($"Wave started: #{CurrentWaveNumber}");
            for (var i = 0; i < wave.Sequence.Count; i++)
            {
                var sequence = wave.Sequence[i];

                if (sequence == null)
                    continue;

                _sequenceRuntimes.Add(new EnemySpawnSequenceRuntime(sequence));
            }

            _isWaveRunning = true;

            _playerStatsUseCase.SetWave(CurrentWaveNumber);
        }
        
        private void UpdateWaveStartDelay(float deltaTime)
        {
            _waveStartTimer -= deltaTime;

            if (_waveStartTimer > 0f)
                return;

            BeginWaveSpawn();
        }
        
        private void BeginWaveSpawn()
        {
            _isWaitingWaveStart = false;
            _isWaveRunning = true;
        }

        private void UpdateWaveSpawn(float deltaTime)
        {
            if (_spawnCooldown > 0f)
            {
                _spawnCooldown -= deltaTime;
                TryCompleteWave();
                return;
            }

            if (_sequenceRuntimes.Count == 0)
            {
                TryCompleteWave();
                return;
            }

            var checksCount = _sequenceRuntimes.Count;
            for (var offset = 0; offset < checksCount; offset++)
            {
                var index = (_nextSequenceRuntimeIndex + offset) % _sequenceRuntimes.Count;
                var sequenceRuntime = _sequenceRuntimes[index];

                if (!sequenceRuntime.CanSpawn(deltaTime))
                    continue;

                SpawnEnemy(sequenceRuntime.Config);
                sequenceRuntime.MarkSpawned();
                _spawnCooldown = MinSpawnInterval;
                _nextSequenceRuntimeIndex = (index + 1) % _sequenceRuntimes.Count;
                break;
            }

            TryCompleteWave();
        }
        
        private void SpawnEnemy(EnemySpawnSequenceConfig sequence)
        {
            var lane = _context.GetRandomLane();

            if (lane == null || sequence.EnemyPrefab == null || sequence.EnemyConfig == null)
                return;

            var enemy = Object.Instantiate(
                sequence.EnemyPrefab,
                lane.GetSpawnPosition(),
                Quaternion.identity,
                _context.EnemiesRoot);

            var wave = _runState.CurrentWaveConfig.WaveConfig;
            
            var typeHealthMultiplier = sequence.EnemyConfig.GetHealthMultiplier(enemy.EnemyType);
            var scaledHealth = Mathf.Max(1, Mathf.RoundToInt(
                sequence.EnemyConfig.StartHealth * sequence.HealthMultiplier * typeHealthMultiplier));
            
            enemy.Initialize(lane, _context.BaseHealth, sequence.EnemyConfig, wave.KillRewardGold, scaledHealth);
            enemy.Finished += OnEnemyFinished;

            _aliveEnemies++;
        }
        
        private void OnEnemyFinished(EnemyUnit enemy)
        {
            enemy.Finished -= OnEnemyFinished;

            _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);

            TryCompleteWave();
        }
        
        private void TryCompleteWave()
        {
            if (!_isGameRunning || !_isWaveRunning || _runState.Phase != ERunPhase.Wave)
                return;

            for (var i = 0; i < _sequenceRuntimes.Count; i++)
            {
                if (!_sequenceRuntimes[i].IsComplete)
                    return;
            }

            if (_aliveEnemies > 0)
                return;

            var completedWaveNumber = CurrentWaveNumber;
            var wave = _runState.CurrentWaveConfig.WaveConfig;
            Debug.Log($"Wave completed: #{completedWaveNumber}, sequences: {wave.Sequence.Count}");
            
            _isWaveRunning = false;
            WaveExecutionCompleted?.Invoke(completedWaveNumber);
        }
        
        private EnemyUnit GetRandomEnemy(List<EnemyUnit> enemies)
        {
            if (enemies == null || enemies.Count == 0)
                return null;

            var validCount = 0;
            for (var i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                    validCount++;
            }

            if (validCount == 0)
                return null;

            var pick = Random.Range(0, validCount);
            for (var i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null)
                    continue;

                if (pick == 0)
                    return enemies[i];

                pick--;
            }

            return null;
        }
    }
}
