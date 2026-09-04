using System.Collections.Generic;
using Project.Scripts.Gameplay.Base;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using NotImplementedException = System.NotImplementedException;

namespace Project.Scripts.Gameplay.Run
{
    public class RunBattleRuntime : MonoBehaviour, IInitializable
    {
        [SerializeField] private List<Transform> _enemySpawnPoints;
        [SerializeField] private Transform _baseHitPoint;
        [SerializeField] private Transform _enemyRoot;
        [SerializeField] private Transform _defenseGridRoot;
        [SerializeField] private BaseHealth _baseHealth;

        private RunState _state;

        [Inject]
        public void Construct(RunState state)
        {
            _state = state;
        }
        
        public void Initialize()
        {
            OnStartsRun();
        }

        public void OnStartsRun()
        {
            if (_state.MaxWaves != 10)
            {
                return;
            }
            
            _state.Reset();
        }

        public void SwitchPhase(ERunPhase phase)
        {
            
        }

        public void StartWave()
        {
            if (!_state.CanEditDefense)
            {
                return;
            }
            
            _state.StartWave();
        }

        public void EndWave()
        {
            
        }

        public void ClaimWaveReward()
        {
            
        }

        public void OpenRewardsPanel()
        {
            
        }

        public void OnEndRun()
        {
            
        }
    }
}