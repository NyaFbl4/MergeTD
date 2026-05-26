using Project.Scripts.Configs;

namespace Project.Scripts.Gameplay.UpgradeItem
{
    public interface IUpgradeItem
    {
        UpgradeItemConfig Config { get; }
        int CurrentLevel { get; }
        int MaxLevel { get; }
        bool IsMaxLevel { get; }
        int NextPrice { get; }

        bool TryUpgrade();
    }
}