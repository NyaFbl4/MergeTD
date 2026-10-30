namespace Project.Scripts.GameManager
{
    public interface IGameManagerService
    {
        float SpeedMultiplier { get; }
        bool IsRunActive { get; }
        void StartGame();
        void FinishGame();
        void PauseGame();
        void ResumeGame();
    }
}
