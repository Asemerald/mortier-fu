using System.Collections.Generic;
using System.Linq;
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
        private List<AugmentState> _augmentBag;
        private List<PlayerManager> _pickers;
        
        private LobbyService _lobbyService;
        private readonly LootTable<SO_Augment> _lootTable;

        private int _playerCount;
        private int _augmentCount;
        
        private const string k_augmentLibLabel = "AugmentLib";
        
        private CountdownTimer _augmentTimer;
        
        public bool IsSelectionOver => _pickers.Count <= 0 || _augmentTimer.IsFinished;
        
        public AugmentSelectionSystem()
        {
            var config = new LootTableConfig
            {
                AllowDuplicates = false,
                RemoveOnPull = false // TODO Swap
            };
            
            _lootTable = new LootTable<SO_Augment>();
        }
        
        public async Task OnInitialize()
        {
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _playerCount = _lobbyService.GetPlayers().Count;
            _augmentCount = _playerCount + 1;
            _augmentBag = new List<AugmentState>(_augmentCount);

            await PopulateLootTable();
            await InstantiatePickups();
        }

        private async Task PopulateLootTable()
        {
            // Load with addressable all augment libraries.
            var handle = Addressables.LoadAssetsAsync<SO_AugmentLibrary>(k_augmentLibLabel);
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
            _pickups = new  List<AugmentPickup>(_augmentCount);
            
            var pickupParent = new GameObject("AugmentPickups").transform;
            
            for (int i = 0; i < _augmentCount; i++)
            {
                var pos = (Random.insideUnitSphere.With(y: 0).normalized * 10f).With(y: 0.85f);
                
                var pickupHandle = SystemManager.Config.AugmentPickupPrefab.InstantiateAsync(pos, Quaternion.identity, pickupParent);
                await pickupHandle.Task;
                
                if(pickupHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Logs.LogError($"Failed to instantiate augment pickup prefab: {pickupHandle.OperationException.Message}");
                    continue;
                }
                
                var pickupGO = pickupHandle.Result;
                
                var pickup = pickupGO.GetComponent<AugmentPickup>();
                pickup.Initialize(this, i);
                pickup.Hide();
                
                _pickups.Add(pickup);
            }
        }
        
        public bool IsInitialized { get; set; }

        public void Dispose()
        {
            _pickups.Clear();
            _augmentBag.Clear();
            
            _augmentTimer.Dispose();
        }
        
        public void StartAugmentSelection(List<PlayerManager> pickers, float duration)
        {
            if (pickers == null || pickers.Count == 0)
            {
                Logs.LogWarning("[AugmentSelectionSystem]: No players provided for augment selection.");
                return;
            }
            
            _pickers = pickers;
            _augmentTimer ??= new CountdownTimer(duration);
            _augmentTimer.Start();
            
            var augments = _lootTable.BatchPull(_augmentCount);

            _augmentBag.Clear();
            
            for (var i = 0; i < augments.Length; i++)
            {
                var augment = augments[i];
                _augmentBag.Add(new AugmentState()
                {
                    Augment = augment,
                    IsPicked = false
                });

                _pickups[i].SetAugmentVisual(augment);
            }
        }

        public void EndAugmentSelection()
        {
            foreach (var pickup in _pickups)
            {
                pickup.Hide();
            }
            
            _augmentBag.Clear();
            _augmentTimer.Stop();
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
        
        private class AugmentState // TODO Better rename
        {
            public SO_Augment Augment;
            public bool IsPicked;
        }
    }
}