using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Level Config", fileName = "Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [SerializeField] private int _startGold;
        [SerializeField] private int _towerPriceIncreaseOnBuy;
        [SerializeField] private int _levelTowerSelected;
        
        public int StartGold => _startGold;
        public int TowerPriceIncreaseOnBuy => _towerPriceIncreaseOnBuy;
        public int LevelTowerSelected => _levelTowerSelected;
    }
    
}