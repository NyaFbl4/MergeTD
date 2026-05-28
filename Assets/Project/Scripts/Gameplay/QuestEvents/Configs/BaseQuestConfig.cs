using UnityEngine;

namespace Project.Scripts.Gameplay.Quests
{
    public abstract class BaseQuestConfig : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _title;
        [SerializeField] private string _description;
        [SerializeField] private int _rewardGold;
        [SerializeField] private int _targetValue;

        public string Id => _id;
        public string Title => _title;
        public string Description => _description;
        public int RewardGold => _rewardGold;
        public int TargetValue => _targetValue;
    }
}