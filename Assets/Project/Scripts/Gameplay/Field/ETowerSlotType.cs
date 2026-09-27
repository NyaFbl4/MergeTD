using System;
using System.Globalization;

namespace Project.Scripts.Gameplay.Field
{
    public enum ETowerSlotType
    {
        SpawnOnly,
        ActiveOnly,
        Locked
    }

    public static class TowerSlotGrid
    {
        public const int RowCount = 3;
        public const int ColumnCount = 5;
        public const int SlotCount = RowCount * ColumnCount;

        public static string GetSlotId(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            var row = slotIndex / ColumnCount + 1;
            var column = slotIndex % ColumnCount + 1;
            return (row * 10 + column).ToString(CultureInfo.InvariantCulture);
        }

        public static bool IsValidSlotId(string slotId)
        {
            return !string.IsNullOrEmpty(slotId)
                   && slotId.Length == 2
                   && slotId[0] >= '1' && slotId[0] <= '3'
                   && slotId[1] >= '1' && slotId[1] <= '5';
        }
    }
}
