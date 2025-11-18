using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MortierFu
{

    public class AugmentSelectionSystem : IGameSystem
    {
        public SO_AugmentSelectionSettings Settings { get; private set; }
        
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

        public bool IsSelectionOver => !_showcaseInProgress && (_pickers.Count <= 0 || (_augmentTimer != null && _augmentTimer.IsFinished)); // TODO Better condition?
        
        public async UniTask OnInitialize()
        {
            var settingsRef = SystemManager.Config.AugmentSelectionSettings;
            Settings = await AddressablesHelpers.LazyLoadAsset(settingsRef);
            if (Settings == null) return;
            
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
            // Load the augment pickup prefab
            var pickupPrefab = await AddressablesHelpers.LazyLoadAsset(Settings.AugmentPickupPrefab);
            if (pickupPrefab == null) return;
            
            _pickups = new  List<AugmentPickup>(_augmentCount);
            
            for (int i = 0; i < _augmentCount; i++)
            {
                var pickupGO = Object.Instantiate(pickupPrefab, _pickupParent);
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

        public void EndAugmentSelection()
        {
            foreach (var pickup in _pickups)
            {
                pickup.Hide();
            }

            _augmentShowcaser.StopShowcase();
            
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