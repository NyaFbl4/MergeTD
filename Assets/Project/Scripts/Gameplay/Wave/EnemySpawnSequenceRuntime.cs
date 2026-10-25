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
        public float TimeUntilStart => _startTimer;
        public bool IsComplete => _spawnedCount >= _config.Count;

        public EnemySpawnSequenceRuntime(EnemySpawnSequenceConfig config)
        {
            _config = config;
            _startTimer = config.StartDelay;
            _spawnTimer = 0f;
            _spawnedCount = 0;
        }

        public void Tick(float deltaTime)
        {
            if (IsComplete)
                return;

            if (_startTimer > 0f)
            {
                _startTimer -= deltaTime;
                return;
            }

            _spawnTimer -= deltaTime;
        }

        public bool CanSpawn() => !IsComplete && _startTimer <= 0f && _spawnTimer <= 0f;

        public void MarkSpawned()
        {
            _spawnedCount++;
            _spawnTimer = _config.SpawnInterval;
        }
    }
}
