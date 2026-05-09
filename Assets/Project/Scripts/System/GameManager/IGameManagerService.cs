namespace Project.Scripts.GameManager
{
    public interface IGameManagerService
    {
        void StartGame();
        void FinishGame();
        void PauseGame();
        void ResumeGame();
    }
}
