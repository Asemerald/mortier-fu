using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine.SceneManagement;

namespace MortierFu
{
    public class SceneService : IGameService
    {
        private UnityEngine.GameObject _loadingGO;

        public void ShowLoadingScreen() => _loadingGO.SetActive(true);
        public void HideLoadingScreen() => _loadingGO.SetActive(false);
        
        public async UniTask LoadScene(string sceneName, bool active = false) {
            Logs.Log($"[SceneService]: Loading {sceneName}...");
            var handle =  SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (handle == null) {
                Logs.LogError($"[SceneService]: Failed to load the following scene {sceneName} !");
                return;
            }
            
            await handle;
            Logs.Log($"[SceneService]: Successfully loaded {sceneName}.");

            if (active) {
                var activeScene = SceneManager.GetSceneByName(sceneName);
                if (activeScene.IsValid())
                {
                    SceneManager.SetActiveScene(activeScene);
                }   
            }
        }
        
        public async UniTask UnloadScene(string sceneName)
        {
            Logs.Log($"[SceneService]: Unloading {sceneName}...");
            var handle = SceneManager.UnloadSceneAsync(sceneName);
            if (handle == null) {
                Logs.LogError($"[SceneService]: Failed to unload the following scene {sceneName} !");
                return;
            }
            
            await handle; 
            Logs.Log($"[SceneService]: Successfully unloaded {sceneName}.");
        }

        public UniTask OnInitialize()
        {
            _loadingGO = UnityEngine.GameObject.Find("Loading");
            if (_loadingGO == null)
            {
                Logs.LogError("[SceneService]: Could not find the Loading game object in the bootstrap scene !");
            }
            
            return UniTask.CompletedTask;
        }

        public void Dispose()
        { }
        
        public bool IsInitialized { get; set; }
    }
}