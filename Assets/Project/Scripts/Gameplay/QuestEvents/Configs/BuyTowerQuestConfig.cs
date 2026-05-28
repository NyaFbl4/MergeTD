using UnityEngine;

namespace Project.Scripts.Gameplay.Quests
{
    [CreateAssetMenu(menuName = "Project/Quests/Buy Tower Quest", fileName = "BuyTowerQuest")]
    public class BuyTowerQuestConfig : BaseQuestConfig
    {
        [SerializeField] private int _minTowerLevel = 1;

        public int MinTowerLevel => _minTowerLevel;
    }
}