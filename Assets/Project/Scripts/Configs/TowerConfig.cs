using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Tower Config", fileName = "Tower Config")]
    public class TowerConfig : ScriptableObject
    {
        [SerializeField] private int _startPrice;
        
        public int StartTowerPrice => _startPrice;
    }
}