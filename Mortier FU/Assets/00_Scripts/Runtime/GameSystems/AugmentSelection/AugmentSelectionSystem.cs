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
                var pickupGo = await Settings.AugmentPickupPrefab.InstantiateAsync(_pickupParent);
                var pickupVFX = await Settings.AugmentVFXPrefab.InstantiateAsync(_pickupParent);

                var pickup = pickupGo.GetComponent<AugmentCardUI>();

                pickup.Initialize();
                pickup.Hide();
                
                var pickupNewAugment = pickupVFX.GetComponent<AugmentPickup>();
                pickupNewAugment.Initialize(this, i, pickup);
                //pickupNewAugment.Reset();

                _pickups.Add(pickup);
                _pickupsVFX.Add(pickupNewAugment);
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

        public async UniTask 
            PrepareAugmentSelection(List<PlayerManager> pickers, CancellationToken cancellationToken)
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

            _augmentProviderSys.PopulateAugmentsNonAlloc(_selectedAugments, _currentRaceNumber);
            _augmentBag.Clear();

            for (var i = 0; i < _selectedAugments.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var augment = _selectedAugments[i];

                _augmentBag.Add(new AugmentState
                {
                    Augment = augment,
                    IsPicked = false
                });

                _pickups[i].SetAugmentVisual(augment);
                _pickupsVFX[i].SetAugmentVisual(augment);
            }

            var augmentPivot = _levelSystem.GetAugmentPivot();
            var augmentPoints = new Vector3[_augmentCount];
            _levelSystem.PopulateAugmentPoints(augmentPoints);

            try
            {
                await _augmentShowcaser.Showcase(
                    augmentPivot,
                    augmentPoints,
                    _augmentCount
                );
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
                var pressureStartTime = 5f;
                var delay = Mathf.Max(0f, duration - pressureStartTime);

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

                var randomAugment = remainingAugments.RandomElement();
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
                var pickupVFX = pickup.GetComponent<AugmentPickup>();
                pickupVFX.Reset();
            }

            _augmentShowcaser.StopShowcase();

            _augmentBag.Clear();
            _augmentTimer?.Dispose();
            _augmentTimer = null;
        }

        public void RestorePickupParent()
        {
            for (var i = 0; i < _pickups.Count; i++)
            {
                var pickup = _pickups[i];
                pickup.transform.SetParent(_pickupParent);

                var pickupVFX = _pickupsVFX[i];
                pickupVFX.AttachToPoint(null);
            }
        }

        public bool NotifyPlayerInteraction(PlayerCharacter character, int augmentIndex)
        {
            if (!_raceInProgress)
                return false;
            
            if (character == null || augmentIndex < 0 || augmentIndex >= _augmentBag.Count)
                return false;

            var augment = _augmentBag[augmentIndex];
            var picker = character.Owner;

            if (!_pickers.Contains(picker) || augment.IsPicked)
                return false;

            augment.IsPicked = true;
            _pickers.Remove(picker);

            character.AddAugment(augment.Augment);

            var prefab = _settingsHandle.Result.AugmentCharaVFX[(int)augment.Augment.Rarity];
            var particleGO = Object.Instantiate(prefab, character.transform.position.Add(y: 0.6f), Quaternion.Euler(-90f, 0f, 0f),
                character.transform);
            var particle = particleGO.TryGetComponent(out _particleSystem);
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