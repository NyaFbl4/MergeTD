namespace Project.Scripts.Gameplay.QuestEvents
{
    public readonly struct TowerBoughtQuestEventDTO
    {
        public readonly int TowerLevel;
        public readonly int Cost;

        public TowerBoughtQuestEventDTO(int towerLevel, int cost)
        {
            TowerLevel = towerLevel;
            Cost = cost;
        }
    }
}