using System.Collections.Generic;
using Project.Scripts.Gameplay.UpgradeItem;
using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Upgrade item Config", fileName = "Upgrade item Config")]
    public class UpgradeItemConfig : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private List<UpgradeLevelData> _levels;

        public string Id => _id;
        public string Description => _description;
        public Sprite Icon => _icon;
        public IReadOnlyList<UpgradeLevelData> Levels => _levels;
    }
}