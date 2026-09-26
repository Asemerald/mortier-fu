using MortierFu.Analytics;
using MortierFu.Shared;

namespace MortierFu
{
    public static class GameplaySystemRegistrar
    {
        public static void Register(SystemManager systemManager)
        {
            if (systemManager == null)
            {
                Logs.LogError("[GameplaySystemRegistrar] SystemManager is missing.");
                return;
            }

            systemManager.CreateAndRegister<GamePauseSystem>();
            systemManager.CreateAndRegister<GhostSystem>();
            systemManager.CreateAndRegister<CameraSystem>();
            systemManager.CreateAndRegister<LevelSystem>();
            systemManager.CreateAndRegister<BombshellSystem>();
            systemManager.CreateAndRegister<AugmentProviderSystem>();
            systemManager.CreateAndRegister<AugmentSelectionSystem>();
            systemManager.CreateAndRegister<AnalyticsSystem>();

            RegisterAchievementSystem(systemManager);
            
            Logs.Log("[GameplaySystemRegistrar] Gameplay systems registered.");
        }
        
        private static void RegisterAchievementSystem(SystemManager systemManager)
        {
            GameService gameService = ServiceManager.Instance?.Get<GameService>();

            if (gameService == null)
            {
                Logs.LogWarning("[GameplaySystemRegistrar] GameService is missing. AchievementSystem will not be registered.");
                return;
            }

            if (gameService.IsCustomMatch)
            {
                Logs.Log("[GameplaySystemRegistrar] Custom match detected. AchievementSystem disabled.");
                return;
            }

            systemManager.CreateAndRegister<AchievementSystem>();

            Logs.Log("[GameplaySystemRegistrar] AchievementSystem registered.");
        }
    }
}