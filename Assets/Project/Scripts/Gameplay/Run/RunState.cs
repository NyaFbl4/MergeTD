using Project.Scripts.Gameplay.Run.Configs;

namespace Project.Scripts.Gameplay.Run
{
    public class RunState
    {
        private readonly RunConfig _config;
        
        private int _currentWave;
        private ERunPhase _phase;
        private RunWaveConfig _currentWaveConfig;

        private bool _canEditDefense => _phase == ERunPhase.Preparation;
        private bool _canUseAbilities => _phase == ERunPhase.Wave;
        private bool _isBossWave => _currentWaveConfig.IsBossWave;
        
        public int CurrentWave => _currentWave;
        public int MaxWaves => _config.Waves.Count;
        public ERunPhase Phase => _phase;
        public bool CanEditDefense => _canEditDefense;
        public bool CanUseAbilities => _canUseAbilities;
        public bool IsBossWave => _isBossWave;
        public RunWaveConfig CurrentWaveConfig => _currentWaveConfig;

        public RunState(RunConfig runConfig)
        {
            _config = runConfig;
        }
        
        public void Reset()
        {
            MoveToWave(1);
        }

        public void MoveToWave(int waveNumber)
        {
            _currentWave = UnityEngine.Mathf.Clamp(waveNumber, 1, MaxWaves);
            _currentWaveConfig = _config.Waves[_currentWave - 1];
            _phase = ERunPhase.Preparation;
        }

        public void StartWave()
        {
            _phase = ERunPhase.Wave;
        }
        
        public void CompleteWave()
        {
            if (_currentWave >= MaxWaves)
            {
                _phase = ERunPhase.Victory;
                return;
            }
            
            _phase = CurrentWaveConfig.PhaseAfterComplete;
        }
        
        public void ContinueToNextPreparation()
        {
            MoveToWave(_currentWave + 1);
        }
        
        public void Defeat()
        {
            _phase = ERunPhase.Defeat;
        }
    }
}
