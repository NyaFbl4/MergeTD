namespace Project.Scripts.Gameplay.Towers
{
    public static class TowerEconomy
    {
        public const int CombatPurchaseCost = 100;

        public static int GetDefaultPurchaseCost(TowerUnit tower)
        {
            return tower.TowerType == ETowerType.Generator
                ? tower.TowerConfig.StartTowerPrice
                : CombatPurchaseCost;
        }

        public static int GetSellRefund(int purchaseCost) => purchaseCost / 2;

        public static int CombinePurchaseCosts(int firstCost, int secondCost)
        {
            return firstCost > int.MaxValue - secondCost
                ? int.MaxValue
                : firstCost + secondCost;
        }
    }
}
