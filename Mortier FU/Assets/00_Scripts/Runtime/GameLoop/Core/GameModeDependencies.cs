using MortierFu.Analytics;

namespace MortierFu
{
    public sealed class GameModeDependencies
    {
        public LobbyService LobbyService { get; private set; }
        public SceneService SceneService { get; private set; }
        public AudioService AudioService { get; private set; }

        public AugmentSelectionSystem AugmentSelectionSystem { get; private set; }
        public CameraSystem CameraSystem { get; private set; }
        public BombshellSystem BombshellSystem { get; private set; }
        public LevelSystem LevelSystem { get; private set; }
        public AnalyticsSystem AnalyticsSystem { get; private set; }

        private GameModeDependencies()
        { }

        public static GameModeDependencies ResolveServices()
        {
            return new GameModeDependencies
            {
                LobbyService = ServiceManager.Instance.Get<LobbyService>(),
                SceneService = ServiceManager.Instance.Get<SceneService>(),
                AudioService = ServiceManager.Instance.Get<AudioService>()
            };
        }

        public void ResolveGameplaySystems()
        {
            AugmentSelectionSystem = SystemManager.Instance.Get<AugmentSelectionSystem>();
            CameraSystem = SystemManager.Instance.Get<CameraSystem>();
            BombshellSystem = SystemManager.Instance.Get<BombshellSystem>();
            LevelSystem = SystemManager.Instance.Get<LevelSystem>();
            AnalyticsSystem = SystemManager.Instance.Get<AnalyticsSystem>();
        }

        public bool HasRequiredServices()
        {
            return LobbyService != null
                   && SceneService != null
                   && AudioService != null;
        }

        public bool HasRequiredGameplaySystems()
        {
            return AugmentSelectionSystem != null
                   && CameraSystem != null
                   && BombshellSystem != null
                   && LevelSystem != null;
        }
    }
}