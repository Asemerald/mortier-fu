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

            Logs.Log("[GameplaySystemRegistrar] Gameplay systems registered.");
        }
    }
}