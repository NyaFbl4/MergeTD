using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Gameplay.Run.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Run Config", fileName = "Run Config")]
    public class RunConfig : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private List<RunWaveConfig> _waves;
        
        public string DisplayName => _displayName;
        public string Description => _description;
        public IReadOnlyList<RunWaveConfig> Waves => _waves;
    }
}