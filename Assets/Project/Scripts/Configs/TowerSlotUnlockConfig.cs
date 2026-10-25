using System;
using Project.Scripts.System.Save;
using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(menuName = "Project/Tower Slot Unlock", fileName = "TowerSlotUnlock")]
    public sealed class TowerSlotUnlockConfig : ScriptableObject
    {
        public const int ExtraSlotCount = 5;
        public const string ResourcePath = "Configs/TowerSlotUnlock";

        [SerializeField] private int[] _prices = { 750, 1500, 3000, 5000, 8000 };

        public static TowerSlotUnlockConfig LoadFromResources() =>
            Resources.Load<TowerSlotUnlockConfig>(ResourcePath)
            ?? throw new InvalidOperationException("Tower slot unlock config is missing: " + ResourcePath);

        public static int GetIndex(string slotId) =>
            slotId != null && slotId.Length == 2 && slotId[0] == '3'
            && slotId[1] >= '1' && slotId[1] <= '5'
                ? slotId[1] - '1'
                : -1;

        public int GetPrice(string slotId)
        {
            var index = GetIndex(slotId);
            if (index < 0)
                return 0;

            if (_prices.Length != ExtraSlotCount || _prices[index] <= 0)
                throw new InvalidOperationException("Tower slot unlock requires five positive prices.");

            return _prices[index];
        }

        public static bool IsNextLockedSlot(IWorldService world, string slotId)
        {
            var index = GetIndex(slotId);
            if (index < 0 || world.IsTowerSlotUnlocked(slotId))
                return false;

            for (var previous = 0; previous < index; previous++)
            {
                if (!world.IsTowerSlotUnlocked("3" + (previous + 1)))
                    return false;
            }

            return true;
        }
    }
}
