using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MortierFu
{
    public class SaveService : IGameService
    {
        private const string SettingsFile = "settings.json";
        private const string TutorialFile = "tutorial.json";
        private const string GameFile = "game.json";

        public SettingsData Settings { get; private set; }
        public GameData Game { get; private set; }
        public TutorialData Tutorial { get; private set; }

        private string _settingsPath;
        private string _gamePath;
        private string _tutorialPath;
        
        private readonly object _settingsLock = new ();
        private readonly object _tutorialLock = new ();

        public bool IsInitialized { get; set; }

        public async UniTask OnInitialize()
        {
            _settingsPath = Path.Combine(Application.persistentDataPath, SettingsFile);
            _gamePath = Path.Combine(Application.persistentDataPath, GameFile);
            _tutorialPath = Path.Combine(Application.persistentDataPath, TutorialFile);
            
            await LoadOrCreateSettings();
            await LoadOrCreateTutorial();
            await LoadOrCreateGame();

            IsInitialized = true;
        }

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

        public async UniTask LoadOrCreateTutorial()
        {
            if (!File.Exists(_tutorialPath))
            {
                Tutorial = TutorialData.CreateTutorialData();
                await SaveTutorial();
                return;
            }

            await LoadTutorial();
        }
        
        public async UniTask SaveTutorial()
        {
            string json = JsonUtility.ToJson(Tutorial, true);

            await UniTask.Run(() =>
            {
                lock (_tutorialLock)
                {
                    File.WriteAllText(_tutorialPath, json);
                }
            });
        }

        public async UniTask SaveSettings()
        {
            string json = JsonUtility.ToJson(Settings, true);

            await UniTask.Run(() =>
            {
                // Ensure thread safety when writing settings
                lock (_settingsLock)
                {
                    File.WriteAllText(_settingsPath, json);
                }
            });
        }

        public async UniTask LoadSettings()
        {
            string json = await UniTask.Run(() => File.ReadAllText(_settingsPath));
            Settings = JsonUtility.FromJson<SettingsData>(json)
                       ?? SettingsData.CreateDefault();
        }

        public async UniTask LoadTutorial()
        {
            string json = await UniTask.Run(() => File.ReadAllText(_tutorialPath));
            Tutorial = JsonUtility.FromJson<TutorialData>(json)
                       ?? TutorialData.CreateTutorialData();
        }
        
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
        
        public async UniTask ResetTutorial()
        {
            Tutorial = TutorialData.CreateTutorialData();
            await SaveTutorial();
        }

        public void Dispose() { }
    }
}
