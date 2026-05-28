using Project.Scripts.Gameplay.Enemies;

namespace Project.Scripts.Gameplay.QuestEvents
{
    public readonly struct DamageDealtQuestEventDTO
    {
        public readonly int Damage;
        public readonly bool IsCritical;
        public readonly EEnemyType EnemyType;

        public DamageDealtQuestEventDTO(int damage, bool isCritical, EEnemyType enemyType)
        {
            Damage = damage;
            IsCritical = isCritical;
            EnemyType = enemyType;
        }
    }
}