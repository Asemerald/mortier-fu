using System.IO;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class SaveService : IGameService
    {
        private const string SettingsFile = "settings.json";
        private const string GameFile = "game.json";

        public SettingsData Settings { get; private set; }
        public GameData Game { get; private set; }

        private string _settingsPath;
        private string _gamePath;

        public bool IsInitialized { get; set; }

        public async UniTask OnInitialize()
        {
            _settingsPath = Path.Combine(Application.persistentDataPath, SettingsFile);
            _gamePath = Path.Combine(Application.persistentDataPath, GameFile);
            
            await LoadOrCreateSettings();
            await LoadOrCreateGame();

            IsInitialized = true;
        }

        // ====================================================================
        // SETTINGS SAVE/LOAD
        // ====================================================================

        public async UniTask LoadOrCreateSettings()
        {
            if (!File.Exists(_settingsPath))
            {
                Settings = SettingsData.CreateDefault();
                await SaveSettings();
                return;
            }

            await LoadSettings();
        }

        public async UniTask SaveSettings()
        {
            string json = JsonUtility.ToJson(Settings, true);

            await UniTask.Run(() =>
            {
                File.WriteAllText(_settingsPath, json);
            });
        }

        public async UniTask LoadSettings()
        {
            string json = await UniTask.Run(() => File.ReadAllText(_settingsPath));
            Settings = JsonUtility.FromJson<SettingsData>(json)
                       ?? SettingsData.CreateDefault();
        }

        // ====================================================================
        // GAME SAVE/LOAD
        // ====================================================================

        public async UniTask LoadOrCreateGame()
        {
            if (!File.Exists(_gamePath))
            {
                Game = GameData.CreateDefault();
                await SaveGame();
                return;
            }

            await LoadGame();
        }

        public async UniTask SaveGame()
        {
            string json = JsonUtility.ToJson(Game, true);

            await UniTask.Run(() =>
            {
                File.WriteAllText(_gamePath, json);
            });
        }

        public async UniTask LoadGame()
        {
            string json = await UniTask.Run(() => File.ReadAllText(_gamePath));
            Game = JsonUtility.FromJson<GameData>(json)
                    ?? GameData.CreateDefault();
        }

        // ====================================================================

        public void Dispose() { }
    }
}
