using MessagePipe;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Quests;
using Project.Scripts.Gameplay.Systems;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;
using Project.Scripts.Systems.UI;
using Project.Scripts.UI.EndWaveUI;
using Project.Scripts.UI.EndWaveLoseUI;
using Project.Scripts.UI.LevelUI;
using Project.Scripts.UI.QuestUI;
using Project.Scripts.UI.SettingsUI;
using Project.Scripts.UI.ShopUI;
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
        [SerializeField] private BattlefieldContext _battlefieldContext;
        [SerializeField] private BaseHealth _baseHealth;

        [Header("Configs")]
        [SerializeField] private LayoutsRepository _layoutsRepository;
        [SerializeField] private UnitsConfig _unitsConfig;
        [SerializeField] private TowerConfig _towerConfig;
        [SerializeField] private LevelConfig _levelConfig;
        [SerializeField] private EnemyConfig _enemyConfig;
        [SerializeField] private QuestCatalog _questCatalog;
        [SerializeField] private SoundLibrary _soundLibrary;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterSystem(builder);
            RegisterGameplay(builder);
            RegisterConfigs(builder);
            RegisterSceneComponents(builder);
            RegisterViews(builder);
            RegisterPresenters(builder);
            RegisterUseCases(builder);
        }

        private void RegisterSystem(IContainerBuilder builder)
        {
            builder.RegisterMessagePipe();

            // Game loop
            builder.RegisterEntryPoint<GameManagerService>(Lifetime.Singleton).As<IGameManagerService>();
            builder.RegisterEntryPoint<QuestService>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<GameBootstrap>(Lifetime.Singleton);
            builder.RegisterEntryPoint<BattlefieldRuntime>(Lifetime.Singleton).AsSelf();;
            builder.RegisterEntryPoint<AudioManager>(Lifetime.Singleton).As<IAudioManager>().AsSelf();
            builder.Register<LocalizationService>(Lifetime.Singleton).As<ILocalizationService>();
            builder.Register<ProgressSaveService>(Lifetime.Singleton).AsSelf();
            builder.Register<ProgressCheckpointUseCase>(Lifetime.Singleton).AsSelf();
            
            // UI core
            builder.RegisterEntryPoint<UIController>(Lifetime.Singleton).As<IUIController>();
            builder.RegisterEntryPoint<UIMessageHandler>(Lifetime.Singleton);
        }

        private void RegisterSceneComponents(IContainerBuilder builder)
        {
            if (_gameManagerHelper == null)
                _gameManagerHelper = FindFirstObjectByType<GameManagerHelper>();

            if (_battlefieldContext == null)
                _battlefieldContext = FindFirstObjectByType<BattlefieldContext>();

            if (_baseHealth == null)
                _baseHealth = FindFirstObjectByType<BaseHealth>();

            if (_gameManagerHelper != null)
                builder.RegisterComponent(_gameManagerHelper).AsSelf();

            if (_battlefieldContext != null)
                builder.RegisterComponent(_battlefieldContext).AsSelf();

            if (_baseHealth != null)
                builder.RegisterComponent(_baseHealth).As<IBaseHealth>().AsSelf();
        }

        private void RegisterPresenters(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<LevelUIPresenter>().As<ILevelUIPresenter>();
            builder.RegisterEntryPoint<EndWaveUIPresenter>().As<IEndWaveUIPresenter>();
            builder.RegisterEntryPoint<EndWaveLoseUIPresenter>().As<IEndWaveLoseUIPresenter>();
            builder.RegisterEntryPoint<ShopUIPresenter>().As<IShopUIPresenter>();
            builder.RegisterEntryPoint<QuestUIPresenter>().As<IQuestUIPresenter>();
            builder.RegisterEntryPoint<SettingsUIPresenter>().As<ISettingsUIPresenter>();
        }

        private void RegisterUseCases(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<ShowPopUpUseCase>();
            builder.RegisterEntryPoint<HidePopUpUseCase>();
            builder.RegisterEntryPoint<PlayerStatsUseCase>().As<IPlayerStatsUseCase>();
            builder.RegisterEntryPoint<BuyTowerUseCase>().As<IBuyTowerUseCase>();
            builder.RegisterEntryPoint<EnemyDeathUseCase>();
            builder.RegisterEntryPoint<LevelUIUseCase>().As<ILevelUIUseCase>();
            builder.RegisterEntryPoint<EndWaveUseCase>();
            builder.RegisterEntryPoint<EndWaveLoseUseCase>();
            builder.RegisterEntryPoint<QuestDamageEventsUseCase>();
            builder.RegisterEntryPoint<QuestWaveEventsUseCase>();
            
            builder.Register<UnitsCatalog>(Lifetime.Singleton).As<IUnitsCatalog>();
        }
        
        private void RegisterGameplay(IContainerBuilder builder)
        {
            // Register gameplay services and systems here.
        }

        private void RegisterViews(IContainerBuilder builder)
        {
            if (_layoutsRepository == null)
            {
                Debug.LogWarning("ProjectLifetimeScope: LayoutsRepository is not assigned. UI views will not be spawned.");
                return;
            }

            var views = _layoutsRepository.Views;
            if (views == null)
                return;

            foreach (var prefab in views)
            {
                if (prefab == null)
                    continue;

                builder.RegisterComponentInNewPrefab(prefab, Lifetime.Scoped).AsSelf().AsImplementedInterfaces();
            }
        }

        private void RegisterConfigs(IContainerBuilder builder)
        {
            builder.RegisterInstance(_soundLibrary != null ? _soundLibrary : ScriptableObject.CreateInstance<SoundLibrary>());
            builder.RegisterInstance(_unitsConfig);
            builder.RegisterInstance(_towerConfig);
            builder.RegisterInstance(_levelConfig);
            builder.RegisterInstance(_enemyConfig);
            builder.RegisterInstance(_questCatalog);
        }
    }

    // Backward compatibility for scenes that already reference GameLifetimeScope.
    public class GameLifetimeScope : ProjectLifetimeScope
    {
    }
}
