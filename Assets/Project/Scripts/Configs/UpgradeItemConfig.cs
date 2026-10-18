using System.Collections.Generic;
using Project.Scripts.Gameplay.UpgradeItem;
using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(menuName = "Project/Configs/Upgrade item Config", fileName = "Upgrade item Config")]
    public class UpgradeItemConfig : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _title;
        [SerializeField] private string _description;
        [SerializeField] private string _details;
        [SerializeField] private Sprite _icon;
        [SerializeField] private List<UpgradeLevelData> _levels;

        public string Id => _id;
        public string Title => _title;
        public string Description => _description;
        public string Details => _details;
        public Sprite Icon => _icon;
        public IReadOnlyList<UpgradeLevelData> Levels => _levels;
    }

    public sealed class BaseUpgradeConfigProvider
    {
        private const string ResourceRoot = "Configs/BaseUpgrades/";

        public UpgradeItemConfig Repair { get; }
        public UpgradeItemConfig ManaRestoration { get; }
        public UpgradeItemConfig MaxEnergy { get; }

        private BaseUpgradeConfigProvider(
            UpgradeItemConfig repair,
            UpgradeItemConfig manaRestoration,
            UpgradeItemConfig maxEnergy)
        {
            Repair = repair;
            ManaRestoration = manaRestoration;
            MaxEnergy = maxEnergy;
        }

        public static BaseUpgradeConfigProvider LoadFromResources()
        {
            return new BaseUpgradeConfigProvider(
                Load("Base Repair Upgrade"),
                Load("Mana Restoration Upgrade"),
                Load("Max Energy Upgrade"));
        }

        private static UpgradeItemConfig Load(string assetName)
        {
            return Resources.Load<UpgradeItemConfig>(ResourceRoot + assetName)
                   ?? throw new MissingReferenceException(
                       $"Base upgrade config was not found at Resources/{ResourceRoot}{assetName}.");
        }
    }
}
