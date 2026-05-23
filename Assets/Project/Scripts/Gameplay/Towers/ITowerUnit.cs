using Project.Scripts.Gameplay.Field;

namespace Project.Scripts.Gameplay.Towers
{
    public interface ITowerUnit
    {
        void CreateTower();
        void OnAttackFireEvent();
        void OnAttackFinishedEvent();
    }
}