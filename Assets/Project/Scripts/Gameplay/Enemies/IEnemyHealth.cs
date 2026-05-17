namespace Project.Scripts.Gameplay.Enemies
{
    public interface IEnemyHealth
    {
        void SetHealth(int health);
        void TakeDamage(int damage);
    }
}