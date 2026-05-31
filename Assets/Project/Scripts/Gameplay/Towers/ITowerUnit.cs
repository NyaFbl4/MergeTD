using Project.Scripts.Gameplay.Field;
using Project.Scripts.System.Audio;
using Project.Scripts.System.UseCases;

namespace Project.Scripts.Gameplay.Towers
{
    public interface ITowerUnit
    {
        void Initialize(IPlayerStatsUseCase playerStats, IAudioManager audioManager);
        void CreateTower();
        void OnAttackFireEvent();
        void OnAttackFinishedEvent();
    }
}
