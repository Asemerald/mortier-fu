using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MortierFu
{
    public class AugmentSelectionSystem : IGameSystem
    {
        public event Action<float> OnPressureStart;
        public event Action OnPressureStop;
        public event Action OnStopShowcase;

        private CancellationTokenSource _pressureTokenSource;

        private List<AugmentCardUI> _pickups;
        private List<GameObject> _pickupsVFX;
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

        private Dictionary<PlayerCharacter, List<SO_Augment>> _pickedAugments = new();

        public Dictionary<PlayerCharacter, List<SO_Augment>> PickedAugments => _pickedAugments;

        public bool IsSelectionOver => !_showcaseInProgress &&
                                       (_pickers.Count <= 0 || (_augmentTimer != null && _augmentTimer.IsFinished));

        public async UniTask OnInitialize()
        {
            _settingsHandle = await SystemManager.Config.AugmentSelectionSettings.LazyLoadAssetRef();

            _pickupParent = new GameObject("AugmentPickups").transform;

            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
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
            _pickupsVFX = new List<GameObject>(_lobbyService.CurrentPlayerCount);

            for (int i = 0; i < _augmentCount; i++)
            {
                var pickupGo = await Settings.AugmentPickupPrefab.InstantiateAsync(_pickupParent);
                var pickupVFX = await Settings.AugmentVFXPrefab.InstantiateAsync(_pickupParent);

                var pickup = pickupGo.GetComponent<AugmentCardUI>();

                pickup.Initialize();
                pickup.Hide();

                var pickupNewAugment = pickupVFX.GetComponent<AugmentPickup>();
                pickupNewAugment.Initialize(this, i);
                pickupNewAugment.Reset();

                _pickups.Add(pickup);
                _pickupsVFX.Add(pickupVFX);
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

                var vfxRoot = _pickupsVFX[i].transform;
                var childVFX = vfxRoot.GetChild(0);

                var ps = childVFX.GetComponent<ParticleSystem>();
                ps.textureSheetAnimation.SetSprite(0, augment.SmallSprite);
            }

            var augmentPivot = _levelSystem.GetAugmentPivot();
            var augmentPoints = new Vector3[_augmentCount];
            _levelSystem.PopulateAugmentPoints(augmentPoints);

            _showcaseInProgress = true;
            await _augmentShowcaser.Showcase(augmentPivot, augmentPoints, _augmentCount);
            _showcaseInProgress = false;

            OnStopShowcase?.Invoke();
            await UniTask.Delay(TimeSpan.FromSeconds(Settings.PlayerInputReenableDelay));

            var gm = GameService.CurrentGameMode as GameModeBase;
            gm?.EnablePlayerInputs();

            _augmentTimer = new CountdownTimer(duration);
            _augmentTimer.Start();

            _pressureTokenSource = new CancellationTokenSource();
            HandlePressure(duration).Forget();
        }

        private async UniTaskVoid HandlePressure(float duration)
        {
            float pressureStartTime = 5f;
            float delay = Mathf.Max(0, duration - pressureStartTime);

            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: _pressureTokenSource.Token);

            _pressureTokenSource = null;
            OnPressureStart?.Invoke(pressureStartTime);
        }

        public void EndRace()
        {
            _pressureTokenSource?.Cancel();
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
                remainingAugments.Remove(randomAugment);

                Logs.Log("[AugmentSelectionSystem] Assigned random augment " + randomAugment.Augment.name +
                         " to player " + picker.PlayerIndex);
            }

            foreach (var pickup in _pickupsVFX)
            {
                var pickupVFX = pickup.GetComponent<AugmentPickup>();
                pickupVFX.Reset();
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
                
                var pickupVFX = _pickupsVFX[i];
                pickupVFX.transform.SetParent(_pickupParent);
            }
        }

        public bool NotifyPlayerInteraction(PlayerCharacter character, int augmentIndex)
        {
            if (character == null || augmentIndex < 0 || augmentIndex >= _augmentBag.Count)
                return false;

            var augment = _augmentBag[augmentIndex];
            var picker = character.Owner;

            if (!_pickers.Contains(picker) || augment.IsPicked)
                return false;

            augment.IsPicked = true;
            _pickers.Remove(picker);

            character.AddAugment(augment.Augment);

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