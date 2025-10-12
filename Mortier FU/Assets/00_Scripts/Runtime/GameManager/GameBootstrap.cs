using UnityEngine;

namespace MortierFu
{
    [DefaultExecutionOrder(-100)] // Ensure it initializes before everything else
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }
        public ServiceManager Services { get; private set; }
        public SystemManager Systems { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Init managers
            Services = new ServiceManager();
            Systems = new SystemManager();

            // Register systems and services
            Services.RegisterAll<IGameService>();
        }
    }
}