using Project.Scripts.Gameplay.Run.Configs;

using System;

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
        
        public event Action<ERunPhase> PhaseChanged;

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
            SetPhase(ERunPhase.Preparation);
        }

        public void StartWave()
        {
            if (_phase != ERunPhase.Preparation)
                return;

            SetPhase(ERunPhase.Wave);
        }
        
        public void CompleteWave()
        {
            if (_phase != ERunPhase.Wave)
                return;

            if (_currentWave >= MaxWaves)
            {
                SetPhase(ERunPhase.Victory);
                return;
            }
            
            SetPhase(CurrentWaveConfig.PhaseAfterComplete);
        }
        
        public void ContinueToNextPreparation()
        {
            if (_phase != ERunPhase.Reward && _phase != ERunPhase.CardChoice)
                return;

            MoveToWave(_currentWave + 1);
        }
        
        public void Defeat()
        {
            if (_phase == ERunPhase.Victory)
                return;

            SetPhase(ERunPhase.Defeat);
        }

        private void SetPhase(ERunPhase phase)
        {
            if (_phase == phase)
                return;

            _phase = phase;
            PhaseChanged?.Invoke(_phase);
        }
    }
}
