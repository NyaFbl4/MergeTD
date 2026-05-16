using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Level Config", fileName = "Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [SerializeField] private int _startGold;
        
        public int StartGold => _startGold;
    }
}