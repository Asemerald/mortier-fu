using MortierFu.Shared;

namespace MortierFu
{
    public static class LobbySandboxSystemRegistrar
    {
        public static void Register(SystemManager systemManager)
        {
            if (systemManager == null)
            {
                Logs.LogError("[LobbySandboxSystemRegistrar] SystemManager is missing.");
                return;
            }

            systemManager.CreateAndRegisterIfMissing<GamePauseSystem>();
            systemManager.CreateAndRegisterIfMissing<CameraSystem>();
            systemManager.CreateAndRegisterIfMissing<BombshellSystem>();

            Logs.Log("[LobbySandboxSystemRegistrar] Lobby sandbox systems registered.");
        }
    }
}