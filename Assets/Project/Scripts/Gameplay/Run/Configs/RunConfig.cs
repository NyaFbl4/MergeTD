using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Gameplay.Run.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Run Config", fileName = "Run Config")]
    public class RunConfig : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField, Min(1)] private int _startBaseHealth = 10;
        [SerializeField, Min(0)] private int _startGold = 250;
        [SerializeField, Min(1)] private int _startSelectedTowerLevel = 1;
        [SerializeField] private List<RunWaveConfig> _waves;
        
        public string DisplayName => _displayName;
        public string Description => _description;
        public int StartBaseHealth => _startBaseHealth;
        public int StartGold => _startGold;
        public int StartSelectedTowerLevel => _startSelectedTowerLevel;
        public IReadOnlyList<RunWaveConfig> Waves => _waves;
    }
}
