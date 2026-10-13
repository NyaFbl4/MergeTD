namespace Project.Scripts.Gameplay.Towers
{
    public enum ETowerPurchaseCurrency
    {
        Gold,
        Gems
    }

    public static class TowerEconomy
    {
        public const int CombatPurchaseCost = 100;
        public const int GoldPerGem = 16;

        public static int GetDefaultPurchaseCost(TowerUnit tower)
        {
            return tower.TowerType == ETowerType.Generator
                ? tower.TowerConfig.StartTowerPrice
                : CombatPurchaseCost;
        }

        public static int GetSellRefund(int purchaseCost) => purchaseCost / 2;

        public static int GetTierGoldValue(int level)
        {
            if (level < 1)
                return 0;

            var value = CombatPurchaseCost;
            for (var currentLevel = 1; currentLevel < level; currentLevel++)
            {
                if (value > int.MaxValue / 2)
                    return int.MaxValue;

                value *= 2;
            }

            return value;
        }

        public static ETowerPurchaseCurrency GetTierPurchaseCurrency(
            ETowerType towerType,
            int level)
        {
            if (towerType == ETowerType.Generator)
                return level >= 3 ? ETowerPurchaseCurrency.Gems : ETowerPurchaseCurrency.Gold;

            return level >= 4 ? ETowerPurchaseCurrency.Gems : ETowerPurchaseCurrency.Gold;
        }

        public static int GetTierPurchaseCost(ETowerType towerType, int level)
        {
            var goldValue = GetTierGoldValue(level);
            return GetTierPurchaseCurrency(towerType, level) == ETowerPurchaseCurrency.Gems
                ? goldValue / GoldPerGem
                : goldValue;
        }

        public static int CombinePurchaseCosts(int firstCost, int secondCost)
        {
            return firstCost > int.MaxValue - secondCost
                ? int.MaxValue
                : firstCost + secondCost;
        }
    }
}
