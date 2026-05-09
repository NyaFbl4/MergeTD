using Project.Scripts.GameManager;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Installers
{
    [DisallowMultipleComponent]
    public class ProjectLifetimeScope : LifetimeScope
    {
        [Header("Scene Components")]
        [SerializeField] private GameManagerHelper _gameManagerHelper;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterSystem(builder);
            RegisterSceneComponents(builder);
            RegisterGameplay(builder);
            RegisterUI(builder);
            RegisterConfigs(builder);
        }

        private static void RegisterSystem(IContainerBuilder builder)
        {
            // BubbleShooter-style: register game loop entry points in system block.
            builder.RegisterEntryPoint<GameManagerService>(Lifetime.Singleton).As<IGameManagerService>();
            builder.RegisterEntryPoint<GameBootstrap>(Lifetime.Singleton).As<IGameBootstrapControl>();
        }

        private void RegisterSceneComponents(IContainerBuilder builder)
        {
            if (_gameManagerHelper == null)
                _gameManagerHelper = FindFirstObjectByType<GameManagerHelper>();

            if (_gameManagerHelper != null)
                builder.RegisterComponent(_gameManagerHelper).AsSelf();
            else
                Debug.LogWarning("ProjectLifetimeScope: GameManagerHelper is not assigned and was not found in scene.");
        }

        private static void RegisterGameplay(IContainerBuilder builder)
        {
            // Register gameplay services and systems here.
        }

        private static void RegisterUI(IContainerBuilder builder)
        {
            // Register UI presenters/controllers here.
        }

        private static void RegisterConfigs(IContainerBuilder builder)
        {
            // Register ScriptableObject configs here.
        }
    }

    // Backward compatibility for scenes that already reference GameLifetimeScope.
    public class GameLifetimeScope : ProjectLifetimeScope
    {
    }
}
