using System;
using System.Collections.Generic;
using System.Threading;
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
        public event Action<float> OnPressureStart;
        public event Action OnPressureStop;
        public event Action OnStopShowcase;

        private AugmentFXPickUp _particleSystem;
        
        private CancellationTokenSource _pressureTokenSource;

        private List<AugmentCardUI> _pickups;
        private List<AugmentPickup> _pickupsVFX;
        private List<AugmentState> _augmentBag;
        
        private List<PlayerManager> _pickers;
        
        private LobbyService _lobbyService;
        private ShakeService _shakeService;
        private LevelSystem _levelSystem;
        private AugmentProviderSystem _augmentProviderSys;
        private AugmentShowcaser _augmentShowcaser;

        private Transform _pickupParent;

        private int _playerCount;
        private int _augmentCount;
        private bool _raceInProgress;
        private int _currentRaceNumber = 1;
        
        private CountdownTimer _augmentTimer;
        private SO_Augment[] _selectedAugments;

        private AsyncOperationHandle<SO_AugmentSelectionSettings> _settingsHandle;
        public SO_AugmentSelectionSettings Settings => _settingsHandle.Result;

        private readonly Dictionary<PlayerCharacter, List<SO_Augment>> _pickedAugments = new();

        public Dictionary<PlayerCharacter, List<SO_Augment>> PickedAugments => _pickedAugments;
        
        public int AugmentCount => _augmentCount;
        
        public int PickupCount => _pickupsVFX?.Count ?? 0;

        public bool IsSelectionOver
        {
            get
            {
                if (!_raceInProgress)
                    return false;

                if (_pickers is null || _pickers.Count <= 0)
                    return true;

                return _augmentTimer is { IsFinished: true };
            }
        }
        
        public void SetCurrentRaceNumber(int raceNumber) => _currentRaceNumber = Mathf.Max(1, raceNumber);

        public async UniTask OnInitialize()
        {
            _settingsHandle = await SystemManager.Config.AugmentSelectionSettings.LazyLoadAssetRef();

            _pickupParent = new GameObject("AugmentPickups").transform;

            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _shakeService = ServiceManager.Instance.Get<ShakeService>();
            _levelSystem = SystemManager.Instance.Get<LevelSystem>();
            _augmentProviderSys = SystemManager.Instance.Get<AugmentProviderSystem>();
            _playerCount = _lobbyService.GetPlayers().Count;
            _augmentCount = Settings.EnforceAugmentCount ? Settings.ForcedAugmentCount : _playerCount + 1;
            _augmentBag = new List<AugmentState>(_augmentCount);
            _selectedAugments = new SO_Augment[_augmentCount];

            await InstantiatePickups();

            _augmentShowcaser = new AugmentShowcaser(this, _pickups.AsReadOnly(), _pickupsVFX.AsReadOnly());
        }

        private async UniTask InstantiatePickups()
        {
            _pickups = new List<AugmentCardUI>(_augmentCount);
            _pickupsVFX = new List<AugmentPickup>(_lobbyService.CurrentPlayerCount);

            for (var i = 0; i < _augmentCount; i++)
            {
                GameObject pickupGo = await Settings.AugmentPickupPrefab.InstantiateAsync(_pickupParent);
                GameObject pickupVFX = await Settings.AugmentVFXPrefab.InstantiateAsync(_pickupParent);

                AugmentCardUI pickup = pickupGo.GetComponent<AugmentCardUI>();

                pickup.Initialize();
                pickup.Hide();
                
                AugmentPickup pickupNewAugment = pickupVFX.GetComponent<AugmentPickup>();
                pickupNewAugment.Initialize(this, i);
                pickupNewAugment.Reset();

                
                _pickups.Add(pickup);
                _pickupsVFX.Add(pickupNewAugment);
                pickupVFX.SetActive(false);
            }
        }

        public bool IsInitialized { get; set; }

        public void Dispose()
        {
            for (var i = _pickups.Count - 1; i >= 0; i--)
                Addressables.ReleaseInstance(_pickups[i].gameObject);

            Addressables.Release(_settingsHandle);

            _pickups.Clear();
            _augmentBag.Clear();

            _augmentTimer?.Dispose();
        }

        private bool TryGetPickup(int index, out AugmentPickup pickup)
        {
            pickup = null;

            if (_pickupsVFX == null)
                return false;

            if (index < 0 || index >= _pickupsVFX.Count)
                return false;

            pickup = _pickupsVFX[index];
            return pickup;
        }

        public async UniTask AttachPickupToAsync(int index, Transform target, Vector3 localOffset, float duration, CancellationToken cancellationToken = default)
        {
            if (TryGetPickup(index, out AugmentPickup pickup))
                await pickup.AttachToAsync(target, localOffset, duration, cancellationToken);
        }
        
        public UniTask DropPickupAsync(int index, Vector3 position, float jumpHeight, float duration, CancellationToken cancellationToken) => 
            !TryGetPickup(index, out AugmentPickup pickup) ? UniTask.CompletedTask : pickup.DropToAsync(position, jumpHeight, duration, cancellationToken);
        
        public async UniTask PrepareAugmentSelection(List<PlayerManager> pickers, RaceAugmentLayout layout, CancellationToken cancellationToken)
        {
            if (pickers == null || pickers.Count == 0)
            {
                Logs.LogWarning("[AugmentSelectionSystem]: No players provided for augment selection.");
                return;
            }

            _raceInProgress = false;
            _pickers = pickers;

            _augmentTimer?.Dispose();
            _augmentTimer = null;

            _pressureTokenSource?.Cancel();
            _pressureTokenSource?.Dispose();
            _pressureTokenSource = null;

            _augmentProviderSys.PopulateAugmentsNonAlloc(_selectedAugments, _currentRaceNumber, _playerCount);
            _augmentBag.Clear();

            for (var i = 0; i < _selectedAugments.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                SO_Augment augment = _selectedAugments[i];

                _augmentBag.Add(new AugmentState
                {
                    Augment = augment,
                    IsPicked = false
                });

                _pickups[i].SetAugmentVisual(augment);
                _pickups[i].SetIconCardVisual(augment);
                _pickupsVFX[i].SetAugmentVisual(augment);
            }

            layout ??= RaceAugmentLayout.FromLevelSystem(_levelSystem, _augmentCount);

            if (!layout.IsValid(_augmentCount))
            {
                Logs.LogWarning("[AugmentSelectionSystem] Invalid race augment layout. Falling back to LevelSystem layout.");
                layout = RaceAugmentLayout.FromLevelSystem(_levelSystem, _augmentCount);
            }

            try
            {
                await _augmentShowcaser.Showcase(layout, _augmentCount);
            }
            finally
            {
                //Noop
            }

            cancellationToken.ThrowIfCancellationRequested();

            OnStopShowcase?.Invoke();
        }
       
        public void StartRaceTimer(float duration)
        {
            duration = Mathf.Max(0.1f, duration);

            _augmentTimer?.Dispose();
            _augmentTimer = new CountdownTimer(duration);
            _augmentTimer.Start();

            _raceInProgress = true;

            _pressureTokenSource?.Cancel();
            _pressureTokenSource?.Dispose();
            _pressureTokenSource = new CancellationTokenSource();

            HandlePressure(duration, _pressureTokenSource.Token).Forget();

            Logs.Log($"[AugmentSelectionSystem] Augment race started for {duration:0.##} seconds.");
        }

        private async UniTaskVoid HandlePressure(float duration, CancellationToken cancellationToken)
        {
            try
            {
                float pressureStartTime = 5f;
                float delay = Mathf.Max(0f, duration - pressureStartTime);

                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                OnPressureStart?.Invoke(pressureStartTime);
            }
            catch (OperationCanceledException)
            { }
        }

        public void EndRace()
        {
            _raceInProgress = false;
            
            _pressureTokenSource?.Cancel();
            _pressureTokenSource?.Dispose();
            _pressureTokenSource = null;

            OnPressureStop?.Invoke();

            var remainingAugments = _augmentBag.FindAll(a => !a.IsPicked);

            foreach (var picker in _pickers)
            {
                if (!_pickedAugments.ContainsKey(picker.Character))
                    _pickedAugments[picker.Character] = new List<SO_Augment>();

                if (remainingAugments.Count == 0)
                {
                    Logs.LogWarning(
                        "[AugmentSelectionSystem] No more augments available to assign to remaining pickers !");
                    break;
                }

                AugmentState randomAugment = remainingAugments.RandomElement();
                picker.Character.AddAugment(randomAugment.Augment);

                _pickedAugments[picker.Character].Add(randomAugment.Augment);
                _augmentProviderSys?.ApplyDamping(randomAugment.Augment);

                randomAugment.IsPicked = true;

                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_NoPick,
                    picker.Character.transform.position);
                _shakeService.ShakeController(picker.Character.Owner, ShakeService.ShakeType.MID);

                var prefab = _settingsHandle.Result.AugmentCharaVFX[(int)randomAugment.Augment.Rarity];
                Object.Instantiate(prefab, picker.Character.transform.position.Add(y: 0.6f),
                    Quaternion.Euler(-90f, 0f, 0f), picker.Character.transform);

                remainingAugments.Remove(randomAugment);

                Logs.Log("[AugmentSelectionSystem] Assigned random augment " + randomAugment.Augment.name + " to player " + picker.PlayerIndex);
            }

            foreach (var pickup in _pickupsVFX)
            {
                AugmentPickup pickupVFX = pickup.GetComponent<AugmentPickup>();
                pickupVFX.Reset();
            }

            _augmentShowcaser.StopShowcase();

            _augmentBag.Clear();
            _augmentTimer?.Dispose();
            _augmentTimer = null;
        }

        public void RestorePickupParent()
        {
            for (int i = 0; i < _pickups.Count; i++)
            {
                AugmentCardUI pickup = _pickups[i];
                pickup.transform.SetParent(_pickupParent);

                AugmentPickup pickupVFX = _pickupsVFX[i];
                pickupVFX.AttachToPoint(null);
            }
        }

        public bool NotifyPlayerInteraction(PlayerCharacter character, int augmentIndex)
        {
            if (!_raceInProgress)
                return false;
            
            if (character == null || augmentIndex < 0 || augmentIndex >= _augmentBag.Count)
                return false;

            AugmentState augment = _augmentBag[augmentIndex];
            PlayerManager picker = character.Owner;

            if (!_pickers.Contains(picker) || augment.IsPicked)
                return false;

            augment.IsPicked = true;
            _pickers.Remove(picker);

            character.AddAugment(augment.Augment);

            GameObject prefab = _settingsHandle.Result.AugmentCharaVFX[(int)augment.Augment.Rarity];
            GameObject particleGO = Object.Instantiate(prefab, character.transform.position.Add(y: 0.6f), Quaternion.Euler(-90f, 0f, 0f),
                character.transform);
            bool particle = particleGO.TryGetComponent(out _particleSystem);
            
            if (particle)
                _particleSystem.Init();
            
            if (!_pickedAugments.ContainsKey(character))
                _pickedAugments[character] = new List<SO_Augment>();
            _pickedAugments[character].Add(augment.Augment);

            _augmentProviderSys?.ApplyDamping(augment.Augment);

            return true;
        }

        private class AugmentState // TODO Better rename
        {
            public SO_Augment Augment;
            public bool IsPicked;
        }
    }
}