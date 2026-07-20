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

            systemManager.CreateAndRegisterIfMissing<GamePauseSystem>();
            systemManager.CreateAndRegisterIfMissing<GhostSystem>();
            systemManager.CreateAndRegisterIfMissing<CameraSystem>();
            systemManager.CreateAndRegisterIfMissing<LevelSystem>();
            systemManager.CreateAndRegisterIfMissing<BombshellSystem>();
            systemManager.CreateAndRegisterIfMissing<AugmentProviderSystem>();
            systemManager.CreateAndRegisterIfMissing<AugmentSelectionSystem>();
            systemManager.CreateAndRegisterIfMissing<AnalyticsSystem>();

            Logs.Log("[GameplaySystemRegistrar] Gameplay systems registered.");
        }
    }
}