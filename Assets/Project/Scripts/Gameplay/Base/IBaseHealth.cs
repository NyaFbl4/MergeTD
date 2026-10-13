namespace Project.Scripts.Gameplay.Base
{
    public interface IBaseHealth
    {
        void AddMaxHealth(int health);
        void AddTemporaryMaxHealth(int health);
        void AddCurrentHealth(int health);
    }
}
