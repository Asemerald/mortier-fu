using System.Collections.Generic;
using System.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MortierFu
{
    public class AugmentSelectionSystem : IGameSystem
    {
        private List<AugmentPickup> _pickups;
        private List<DA_Augment> _augmentBag;
        
        private LobbyService _lobbyService;
        private readonly LootTable<DA_Augment> _lootTable;

        private int _playerCount;
        
        private const string k_augmentLibLabel = "AugmentLib";
        
        public AugmentSelectionSystem()
        {
            var config = new LootTableConfig
            {
                AllowDuplicates = false,
                RemoveOnPull = true
            };
            
            _lootTable = new LootTable<DA_Augment>();
        }
        
        public async Task OnInitialize()
        {
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _playerCount = _lobbyService.GetPlayers().Count;
            
            _augmentBag = new List<DA_Augment>(_playerCount);

            await PopulateLootTable();
            await InstantiatePickups();
        }

        private async Task PopulateLootTable()
        {
            // Load with addressable all augment libraries.
            var handle = Addressables.LoadAssetsAsync<DA_AugmentLibrary>(k_augmentLibLabel);
            await handle.Task;

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
        
        private async Task InstantiatePickups()
        {
            _pickups = new  List<AugmentPickup>(_playerCount);
            
            var pickupParent = new GameObject("AugmentPickups").transform;
            
            for (int i = 0; i < _playerCount; i++)
            {
                var pickupHandle = SystemManager.Config.AugmentPickupPrefab.InstantiateAsync(pickupParent);
                await pickupHandle.Task;
                
                if(pickupHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Logs.LogError($"Failed to instantiate augment pickup prefab: {pickupHandle.OperationException.Message}");
                    continue;
                }
                
                var pickupGO = pickupHandle.Result;
                pickupGO.SetActive(false);
                
                var pickup = pickupGO.GetComponent<AugmentPickup>();
                _pickups.Add(pickup);
            }
        }
        
        public bool IsInitialized { get; set; }

        public void Dispose()
        {
            _pickups.Clear();
            _augmentBag.Clear();
        }

        // Rename, pas forcément utile à voir après avec ce qu'a fait Antoine.
        public async Task RetakeAugments()
        {
            // Il faudra ici récupérer selon la loot table les augments via un shuffle.
            _pickups = new  List<AugmentPickup>(_playerCount);
            _augmentBag = new List<DA_Augment>(_playerCount);

            for (int i = 0; i < _playerCount; i++)
            {
                var pickup = await SystemManager.Config.AugmentPickupPrefab.LoadAndInstantiate<AugmentPickup>();
                _pickups[i] = pickup;
                _pickups[i].gameObject.SetActive(false);

                _augmentBag[i] = _pickups[i].AugmentData;
            }
        }
    }
}