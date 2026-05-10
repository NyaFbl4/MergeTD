using System;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Field;
using UnityEngine;
using Random = UnityEngine.Random;
using VContainer.Unity;

namespace Project.Scripts.Gameplay.Systems
{
    public class BattlefieldRuntime : IStartable, IDisposable, IGameStartListener, IGameFinishListener, IGameUpdateListener
    {
        private readonly BattlefieldContext _context;

        private bool _isRunning;
        private bool _isSpawningWave;
        private float _spawnTimer;
        private float _waveDelayTimer;
        private int _currentWave;
        private int _spawnedInWave;
        private int _targetEnemiesInWave;

        public BattlefieldRuntime(BattlefieldContext context)
        {
            _context = context;
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
                _isRunning = false;
                return;
            }

            _isRunning = true;
            _currentWave = 1;
            BeginWave();
        }

        public void OnFinishGame()
        {
            _isRunning = false;
            _isSpawningWave = false;
        }

        public void OnUpdate(float deltaTime)
        {
            if (!_isRunning)
                return;

            if (_isSpawningWave)
            {
                _spawnTimer += deltaTime;
                if (_spawnTimer >= _context.SpawnInterval)
                {
                    _spawnTimer = 0f;
                    SpawnEnemy();
                    _spawnedInWave++;

                    if (_spawnedInWave >= _targetEnemiesInWave)
                    {
                        _isSpawningWave = false;
                        _waveDelayTimer = 0f;
                    }
                }

                return;
            }

            _waveDelayTimer += deltaTime;
            if (_waveDelayTimer < _context.WaveDelay)
                return;

            _currentWave++;
            BeginWave();
        }

        private void BeginWave()
        {
            _spawnedInWave = 0;
            _spawnTimer = 0f;
            _targetEnemiesInWave = _context.EnemiesPerWave + (_currentWave - 1);
            _isSpawningWave = true;
        }

        private void SpawnEnemy()
        {
            var lanes = _context.Lanes;
            if (lanes == null || lanes.Length == 0 || _context.EnemyPrefab == null)
                return;

            var lane = lanes[Random.Range(0, lanes.Length)];
            if (lane == null)
                return;

            var enemy = UnityEngine.Object.Instantiate(
                _context.EnemyPrefab,
                lane.GetSpawnPosition(),
                Quaternion.identity,
                _context.EnemiesRoot);

            enemy.Initialize(lane, _context.BaseHealth);
        }
    }
}

