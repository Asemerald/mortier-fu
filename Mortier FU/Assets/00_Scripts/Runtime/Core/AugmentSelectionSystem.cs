using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MortierFu
{
    public class AugmentSelectionSystem : IGameSystem
    {
        private AsyncOperationHandle<SO_AugmentSelectionSettings> _settingsHandle; 
        private AsyncOperationHandle<GameObject> _prefabHandle;
        
        private List<AugmentPickup> _pickups;
        private List<AugmentState> _augmentBag;
        private List<PlayerManager> _pickers;
        
        private AugmentShowcaser _augmentShowcaser;
        
        private Transform _pickupParent;
        
        private LobbyService _lobbyService;
        private readonly LootTable<SO_Augment> _lootTable;

        private int _playerCount;
        private int _augmentCount;
        private bool _showcaseInProgress;
        
        private const string k_augmentLibLabel = "AugmentLib";
        
        private CountdownTimer _augmentTimer;
        private SO_Augment[] _selectedAugments;

        public bool IsSelectionOver => !_showcaseInProgress && (_pickers.Count <= 0 || (_augmentTimer != null && _augmentTimer.IsFinished)); // TODO Better condition?
        
        public SO_AugmentSelectionSettings Settings => _settingsHandle.Result;
        
        public AugmentSelectionSystem()
        {
            var config = new LootTableConfig
            {
                AllowDuplicates = false,
                RemoveOnPull = false // TODO Swap
            };
            
            _lootTable = new LootTable<SO_Augment>();
        }
        
        public async UniTask OnInitialize()
        {
            // Load the system settings
            _settingsHandle = SystemManager.Config.AugmentSelectionSettings.LoadAssetAsync();
            await _settingsHandle.Task;

            if (_settingsHandle.Status != AsyncOperationStatus.Succeeded)
            {
                if (Settings.EnableDebug)
                {
                    Logs.LogError("[AugmentSelectionSystem]: Failed while loading settings with Addressables. Error: " + _prefabHandle.OperationException.Message);
                }
                return;
            }
            
            _pickupParent = new GameObject("Bombshells").transform;
            _pickupParent.position = Vector3.down * 50;
            
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _playerCount = _lobbyService.GetPlayers().Count;
            _augmentCount = _playerCount + 1;
            _augmentBag = new List<AugmentState>(_augmentCount);
            _selectedAugments = new SO_Augment[_augmentCount];

            await PopulateLootTable();
            await InstantiatePickups();
            
            _augmentShowcaser = new AugmentShowcaser(this, _pickups.AsReadOnly());
        }

        private async UniTask PopulateLootTable()
        {
            // Load with addressable all augment libraries.
            var handle = Addressables.LoadAssetsAsync<SO_AugmentLibrary>(k_augmentLibLabel);
            await handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Logs.LogWarning($"Error occurred while loading Augment libs: {handle.OperationException.Message}");
                return;
            }
            
            var libs = handle.Result; 
            foreach (var lib in libs)
            {
                _lootTable.PopulateLootBag(lib.AugmentEntries);
                Logs.Log($"Successfully included augments from the following augment library: {lib.name}");
            }
            
            Logs.Log($"Successfully populate the augment loot table with {_lootTable.TotalWeight} total weight.");
        }
        
        private async UniTask InstantiatePickups()
        {
            // Load the augment pickup prefab
            _prefabHandle = Settings.AugmentPickupPrefab.LoadAssetAsync(); 
            await _prefabHandle;
            
            if (_prefabHandle.Status != AsyncOperationStatus.Succeeded)
            {
                if (Settings.EnableDebug)
                {
                    Logs.LogError("[AugmentSelectionSystem]: Failed while loading AugmentPickup prefab through Addressables. Error: " + _prefabHandle.OperationException.Message);
                }
                return;
            }
            
            _pickups = new  List<AugmentPickup>(_augmentCount);
            
            for (int i = 0; i < _augmentCount; i++)
            {
                var pickupGO = Object.Instantiate(_prefabHandle.Result, _pickupParent);
                var pickup = pickupGO.GetComponent<AugmentPickup>();
                
                pickup.Initialize(this, i);
                pickup.Hide();
                
                _pickups.Add(pickup);
            }
            
            Addressables.Release(_prefabHandle);
        }
        
        public bool IsInitialized { get; set; }

        public void Dispose()
        {
            _pickups.Clear();
            _augmentBag.Clear();
            
            _augmentTimer.Dispose();
            
            Addressables.Release(_settingsHandle);
        }
        
        public async UniTask HandleAugmentSelection(List<PlayerManager> pickers, float duration)
        {
            if (pickers == null || pickers.Count == 0)
            {
                Logs.LogWarning("[AugmentSelectionSystem]: No players provided for augment selection.");
                return;
            }
            
            _pickers = pickers;
            
            _selectedAugments = _lootTable.BatchPull(_augmentCount);
            _augmentBag.Clear();
            
            for (var i = 0; i < _selectedAugments.Length; i++)
            {
                var augment = _selectedAugments[i];
                _augmentBag.Add(new AugmentState()
                {
                    Augment = augment,
                    IsPicked = false
                });
                
                _pickups[i].SetAugmentVisual(augment);
            }
            
            var positions = new List<Vector3>(_augmentCount);
            for (int i = 0; i < _augmentCount; i++)
            {
                var pos = (Random.insideUnitSphere.With(y: 0).normalized * 10f).With(y: 0.85f);
                positions.Add(pos);
            }

            _showcaseInProgress = true;
            await _augmentShowcaser.Showcase(positions);
            _showcaseInProgress = false;

            await Task.Delay(TimeSpan.FromSeconds(Settings.PlayerInputReenableDelay));
            
            var gm = GameService.CurrentGameMode as GameModeBase;
            gm?.EnablePlayerInputs();
            
            _augmentTimer = new CountdownTimer(duration);
            _augmentTimer.Start();
        }

        public void EndAugmentSelection()
        {
            foreach (var pickup in _pickups)
            {
                pickup.Hide();
            }
            
            _selectedAugments = null;
            _augmentBag.Clear();
            _augmentTimer = null;
        }

        public bool NotifyPlayerInteraction(PlayerCharacter character, int augmentIndex)
        {
            if(character == null || augmentIndex < 0 || augmentIndex >= _augmentBag.Count)
                return false;

            var augment = _augmentBag[augmentIndex];
            var picker = character.Owner;

            if (!_pickers.Contains(picker) || augment.IsPicked)
                return false;

            augment.IsPicked = true;
            _pickers.Remove(picker);
            
            character.AddAugment(augment.Augment);

            return true;
        }
        
        public class AugmentState // TODO Better rename
        {
            public SO_Augment Augment;
            public bool IsPicked;
        }
    }
}