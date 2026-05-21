using Project.Scripts.Gameplay.Wave;

namespace Project.Scripts.Gameplay.Systems
{
    public class EnemySpawnSequenceRuntime
    {
        private readonly EnemySpawnSequenceConfig _config;

        private int _spawnedCount;
        private float _startTimer;
        private float _spawnTimer;

        public EnemySpawnSequenceConfig Config => _config;
        public bool IsComplete => _spawnedCount >= _config.Count;

        public EnemySpawnSequenceRuntime(EnemySpawnSequenceConfig config)
        {
            _config = config;
            _startTimer = config.StartDelay;
            _spawnTimer = 0f;
            _spawnedCount = 0;
        }

        public bool CanSpawn(float deltaTime)
        {
            if (IsComplete)
                return false;

            if (_startTimer > 0f)
            {
                _startTimer -= deltaTime;
                return false;
            }

            if (_spawnTimer > 0f)
            {
                _spawnTimer -= deltaTime;
                return false;
            }

            return true;
        }

        public void MarkSpawned()
        {
            _spawnedCount++;
            _spawnTimer = _config.SpawnInterval;
        }
    }
}