using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Upgrade item Config", fileName = "Upgrade item Config")]
    public class UpgradeViewConfig : ScriptableObject
    {
        [SerializeField] private string _description;
        [SerializeField] private List<int> _priceOnLevel;
        
        public string Description => _description;
        public IReadOnlyList<int> PriceOnLevel => _priceOnLevel;
    }
}