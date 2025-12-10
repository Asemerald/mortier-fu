using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace MortierFu
{

    public class AugmentSelectionSystem : IGameSystem
    {
        private List<AugmentPickup> _pickups;
        private List<AugmentState> _augmentBag;
        private List<PlayerManager> _pickers;
        
        private LobbyService _lobbyService;
        private LevelSystem _levelSystem;
        private AugmentProviderSystem _augmentProviderSys;
        private AugmentShowcaser _augmentShowcaser;
        
        private Transform _pickupParent;
        
        private int _playerCount;
        private int _augmentCount;
        private bool _showcaseInProgress;
        
        private CountdownTimer _augmentTimer;
        private SO_Augment[] _selectedAugments;

        private AsyncOperationHandle<SO_AugmentSelectionSettings> _settingsHandle;
        public SO_AugmentSelectionSettings Settings => _settingsHandle.Result;
        
        public bool IsSelectionOver => !_showcaseInProgress && (_pickers.Count <= 0 || (_augmentTimer != null && _augmentTimer.IsFinished)); // TODO Better condition?
        
        public async UniTask OnInitialize()
        {
            _settingsHandle = await SystemManager.Config.AugmentSelectionSettings.LazyLoadAssetRef();
            
            _pickupParent = new GameObject("AugmentPickups").transform;
            _pickupParent.position = Vector3.down * 50;
            
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _levelSystem = SystemManager.Instance.Get<LevelSystem>();
            _augmentProviderSys = SystemManager.Instance.Get<AugmentProviderSystem>();
            _playerCount = _lobbyService.GetPlayers().Count;
            _augmentCount = _playerCount + 1;
            _augmentBag = new List<AugmentState>(_augmentCount);
            _selectedAugments = new SO_Augment[_augmentCount];
            
            await InstantiatePickups();
            
            _augmentShowcaser = new AugmentShowcaser(this, _pickups.AsReadOnly());
        }
        
        private async UniTask InstantiatePickups()
        {
            _pickups = new  List<AugmentPickup>(_augmentCount);
            
            for (int i = 0; i < _augmentCount; i++)
            {
                var pickupGo = await Settings.AugmentPickupPrefab.InstantiateAsync(_pickupParent);
                var pickup = pickupGo.GetComponent<AugmentPickup>();
                
                pickup.Initialize(this, i);
                pickup.Hide();
                
                _pickups.Add(pickup);
            }
        }
        
        public bool IsInitialized { get; set; }

        public void Dispose()
        {
            for (int i = _pickups.Count - 1; i >= 0; i--)
            {
                Addressables.ReleaseInstance(_pickups[i].gameObject);
            }
            
            Addressables.Release(_settingsHandle);
            
            _pickups.Clear();
            _augmentBag.Clear();
            
            _augmentTimer?.Dispose();
        }
        
        public async UniTask HandleAugmentSelection(List<PlayerManager> pickers, float duration)
        {
            if (pickers == null || pickers.Count == 0)
            {
                Logs.LogWarning("[AugmentSelectionSystem]: No players provided for augment selection.");
                return;
            }
            
            _pickers = pickers;

            _augmentProviderSys.PopulateAugmentsNonAlloc(_selectedAugments);
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

            var augmentPivot = _levelSystem.GetAugmentPivot();
            var augmentPoints = new Vector3[_augmentCount];
            _levelSystem.PopulateAugmentPoints(augmentPoints);

            _showcaseInProgress = true;
            await _augmentShowcaser.Showcase(augmentPivot, augmentPoints);
            _showcaseInProgress = false;

            await UniTask.Delay(TimeSpan.FromSeconds(Settings.PlayerInputReenableDelay));
            
            var gm = GameService.CurrentGameMode as GameModeBase;
            gm?.EnablePlayerInputs();
            
            _augmentTimer = new CountdownTimer(duration);
            _augmentTimer.Start();
        }

        public void EndRace()
        {
            // Give a random augment to remaining pickers
            var remainingAugments = _augmentBag.FindAll(a => !a.IsPicked);
            foreach (var picker in _pickers)
            {
                if (remainingAugments.Count == 0)
                {
                    Logs.LogWarning("[AugmentSelectionSystem] No more augments available to assign to remaining pickers !");
                    break;
                }

                var randomAugment = remainingAugments.RandomElement();
                picker.Character.AddAugment(randomAugment.Augment);
                randomAugment.IsPicked = true;
                remainingAugments.Remove(randomAugment);
                Logs.Log("[AugmentSelectionSystem] Assigned random augment " + randomAugment.Augment.name + " to player " + picker.PlayerIndex);
            }
            
            foreach (var pickup in _pickups)
            {
                pickup.Hide();
            }

            _augmentShowcaser.StopShowcase();
            
            _augmentBag.Clear();
            _augmentTimer = null;
        }
        
        public void RestorePickupParent()
        {
            for (int i = 0; i < _pickups.Count; i++)
            {
                var pickup = _pickups[i];
                pickup.transform.SetParent(_pickupParent);
            }
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