using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Enemies;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Wave;
using Project.Scripts.System.UseCases;
using UnityEngine;
using Random = UnityEngine.Random;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Project.Scripts.Gameplay.Systems
{
    public class BattlefieldRuntime : IStartable, IDisposable, IGameStartListener, IGameFinishListener, IGameUpdateListener
    {
        private readonly BattlefieldContext _context;
        //private readonly EnemyConfig _enemyConfig;
        private readonly LevelConfig _levelConfig;
        private readonly IPlayerStatsUseCase _playerStatsUseCase;
        
        private bool _isRunning;
        private bool _isSpawningWave;
        private float _spawnTimer;
        private float _waveDelayTimer;
        private bool _isWaitingWaveStart;
        private float _waveStartTimer;
        private int _currentWave;
        private int _spawnedInWave;
        private int _targetEnemiesInWave;
        
        private int _currentWaveIndex;
        private readonly List<EnemySpawnSequenceRuntime> _sequenceRuntimes = new();
        private int _aliveEnemies;

        private bool _isWaveRunning;
        private bool _isWaitingNextWave;
        private bool _isGameRunning;
        
        private int CurrentWaveNumber => _currentWaveIndex + 1;

        public BattlefieldRuntime(
            BattlefieldContext context,
            LevelConfig levelConfig,
            IPlayerStatsUseCase playerStatsUseCase)
        {
            _context = context;
            _levelConfig = levelConfig;
            _playerStatsUseCase = playerStatsUseCase;
            IGameListener.Register(this);
        }

        public void Start()
        {
            // Wait for GameManagerService.StartGame.
        }

        public void Dispose()
        {
            IGameListener.Unregister(this);
        }

        public void OnStartGame()
        {
            if (_context == null || !_context.IsReady())
            {
                Debug.LogWarning("BattlefieldRuntime: BattlefieldContext is not ready. Check lanes, base, and enemy prefab.");
                //_isRunning = false;
                return;
            }
            
            Debug.Log("BattlefieldRuntime: Battlefield Start");
            
            _sequenceRuntimes.Clear();
            _currentWaveIndex = 0;
            _aliveEnemies = 0;
            
            _spawnTimer = 0f;
            _waveDelayTimer = 0f;
            _isWaitingWaveStart = false;
            _waveStartTimer = 0f;

            _isGameRunning = true;
            _isWaitingNextWave = false;

            StartWave();

            _isRunning = true;
            _currentWave = 1;
        }

        public void OnFinishGame()
        {
            _isGameRunning = false;
            _isWaveRunning = false;
            _isWaitingNextWave = false;
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

            if (_isWaitingNextWave)
            {
                UpdateNextWaveDelay(deltaTime);
            }
        }
        
        private void StartWave()
        {
            if (_levelConfig.Waves == null || _currentWaveIndex >= _levelConfig.Waves.Count)
            {
                Debug.Log("All waves completed");
                FinishAllWaves();
                return;
            }

            _sequenceRuntimes.Clear();

            var wave = _levelConfig.Waves[_currentWaveIndex];
            Debug.Log($"Wave started: #{CurrentWaveNumber}");
            for (var i = 0; i < wave.Sequence.Count; i++)
            {
                var sequence = wave.Sequence[i];

                if (sequence == null)
                    continue;

                _sequenceRuntimes.Add(new EnemySpawnSequenceRuntime(sequence));
            }

            _isWaveRunning = true;
            _isWaitingNextWave = false;

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
            _spawnTimer = 0f;
        }

        private void UpdateWaveSpawn(float deltaTime)
        {
            for (var i = 0; i < _sequenceRuntimes.Count; i++)
            {
                var sequenceRuntime = _sequenceRuntimes[i];

                if (!sequenceRuntime.CanSpawn(deltaTime))
                    continue;

                SpawnEnemy(sequenceRuntime.Config);
                sequenceRuntime.MarkSpawned();
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

            enemy.Initialize(lane, _context.BaseHealth, sequence.EnemyConfig);
            enemy.Finished += OnEnemyFinished;

            _aliveEnemies++;
        }
        
        private void OnEnemyFinished(EnemyUnit enemy)
        {
            enemy.Finished -= OnEnemyFinished;

            _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);

            TryCompleteWave();
        }
        
        private void FinishAllWaves()
        {
            _isGameRunning = false;

            // Потом можно будет вызвать победу:
            // _gameManagerService.FinishGame();
            // или отдельный WinGame(), когда добавим состояние победы.
        }
        
        private void UpdateNextWaveDelay(float deltaTime)
        {
            _waveDelayTimer -= deltaTime;

            if (_waveDelayTimer > 0f)
                return;

            _currentWaveIndex++;
            StartWave();
        }
        
        private void TryCompleteWave()
        {
            for (var i = 0; i < _sequenceRuntimes.Count; i++)
            {
                if (!_sequenceRuntimes[i].IsComplete)
                    return;
            }

            if (_aliveEnemies > 0)
                return;

            var completedWaveNumber = CurrentWaveNumber;
            var wave = _levelConfig.Waves[_currentWaveIndex];
            Debug.Log($"Wave started: #{CurrentWaveNumber}, sequences: {wave.Sequence.Count}");

            _isWaveRunning = false;
            _isWaitingNextWave = true;
            _waveDelayTimer = wave.DelayAfterWave;
        }
        
        private void BeginWave()
        {
            _spawnedInWave = 0;
            _spawnTimer = 0f;
            _targetEnemiesInWave = _context.EnemiesPerWave + (_currentWave - 1);
            _isSpawningWave = true;
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
