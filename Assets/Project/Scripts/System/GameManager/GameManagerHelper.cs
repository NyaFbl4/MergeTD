using UnityEngine;
using VContainer;

namespace Project.Scripts.GameManager
{
    public class GameManagerHelper : MonoBehaviour
    {
        [SerializeField] private bool _autoStartOnEnable;

        private IGameManagerService _gameManagerService;

        [Inject]
        public void Construct(IGameManagerService gameManagerService)
        {
            _gameManagerService = gameManagerService;
        }

        private void OnEnable()
        {
            if (_autoStartOnEnable && _gameManagerService != null)
                _gameManagerService.StartGame();
        }

        [ContextMenu("Start Game")]
        public void StartGame()
        {
            if (_gameManagerService == null)
            {
                Debug.LogError("GameManagerService is null. Ensure GameManager is registered in ProjectLifetimeScope.");
                return;
            }

            _gameManagerService.StartGame();
        }

        [ContextMenu("Finish Game")]
        public void FinishGame()
        {
            if (_gameManagerService == null)
            {
                Debug.LogError("GameManagerService is null. Ensure GameManager is registered in ProjectLifetimeScope.");
                return;
            }

            _gameManagerService.FinishGame();
        }

        [ContextMenu("Pause Game")]
        public void PauseGame()
        {
            if (_gameManagerService == null)
            {
                Debug.LogError("GameManagerService is null. Ensure GameManager is registered in ProjectLifetimeScope.");
                return;
            }

            _gameManagerService.PauseGame();
        }

        [ContextMenu("Resume Game")]
        public void ResumeGame()
        {
            if (_gameManagerService == null)
            {
                Debug.LogError("GameManagerService is null. Ensure GameManager is registered in ProjectLifetimeScope.");
                return;
            }

            _gameManagerService.ResumeGame();
        }
    }
}

