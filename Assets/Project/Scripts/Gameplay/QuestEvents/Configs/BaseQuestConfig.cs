using UnityEngine;

namespace Project.Scripts.Gameplay.Quests
{
    public abstract class BaseQuestConfig : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private Sprite _icon;
        [SerializeField] private string _description;
        [SerializeField] private int _rewardGold;
        [SerializeField] private int _targetValue;
        [SerializeField, Min(0f)] private float _targetScalePerWave = 0.15f;
        [SerializeField, Min(0f)] private float _rewardScalePerWave = 0.1f;

        public string Id => _id;
        public Sprite Icon => _icon;
        public string Description => _description;
        public int RewardGold => _rewardGold;
        public int TargetValue => _targetValue;
        public float TargetScalePerWave => _targetScalePerWave;
        public float RewardScalePerWave => _rewardScalePerWave;
    }
}