using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MortierFu
{
    public class SceneService : IGameService
    {
        private GameObject _loadingGO;

        public void ShowLoadingScreen() => _loadingGO.SetActive(true);
        public void HideLoadingScreen() => _loadingGO.SetActive(false);
        
        public async UniTask LoadScene(string sceneName, bool active = false)
        {
            var handle = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (handle == null)
            {
                Logs.LogError($"[SceneService]: Encountered an error trying to load {sceneName} !");
                return;
            }
            
            await handle.ToUniTask();

            if (!active) return;
                
            var activeScene = SceneManager.GetSceneByName(sceneName);
            if (activeScene.IsValid())
            {
                SceneManager.SetActiveScene(activeScene);
            }
        }
        
        public async UniTask UnloadScene(string sceneName)
        {
            var handle = SceneManager.UnloadSceneAsync(sceneName);
            if (handle == null)
            {
                Logs.LogError($"[SceneService]: Encountered an error trying to unload {sceneName} !");
                return;
            }

            await handle.ToUniTask();
        }

        public UniTask OnInitialize()
        {
            _loadingGO = GameObject.Find("Loading");
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