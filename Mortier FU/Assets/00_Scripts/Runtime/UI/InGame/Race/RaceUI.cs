using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MortierFu
{
    public class RaceUI : MonoBehaviour
    {
        [SerializeField] private PlayerConfirmationUI _playerConfirmationUI;
        [SerializeField] private RacePressureUI _racePressureUI;
        [SerializeField] private AugmentSummaryUI _augmentSummaryUI;
        [SerializeField] private CountdownUI _raceCountdownUI;

        private ConfirmationService _confirmationService;
        private AugmentSelectionSystem _augmentSelectionSystem;
        private LobbyService _lobbyService;
        
        private GameModeBase _gm;
        
        private void Awake()
        {
            _confirmationService = ServiceManager.Instance.Get<ConfirmationService>();
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            
            _playerConfirmationUI.gameObject.SetActive(false);
            _racePressureUI.gameObject.SetActive(false);
            _raceCountdownUI.gameObject.SetActive(false);
            _augmentSummaryUI.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _gm = GameService.CurrentGameMode as GameModeBase;
            
            if (_gm != null)
                _gm.OnRaceEndedUI += PlayAugmentSummary;
            
            if (_confirmationService != null)
            {
                _confirmationService.OnStartConfirmation += HandleStartConfirmation;
                _confirmationService.OnPlayerConfirmed += HandlePlayerConfirmed;
                _confirmationService.OnAllPlayersConfirmed += HandleAllPlayersConfirmed;
            }
            else
            {
                Debug.LogError($"OnRaceUI] No ConfirmationService found for {gameObject.name}");
            }
        }

        private void Start()
        {
            _augmentSelectionSystem = SystemManager.Instance.Get<AugmentSelectionSystem>();

            if (_augmentSelectionSystem == null)
            {
                Debug.LogError($"[RacePressureUI] No AugmentSelectionSystem found for {gameObject.name}");
                return;
            }

            _augmentSelectionSystem.OnPressureStart += HandlePressureStart;
            _augmentSelectionSystem.OnPressureStop += HandlePressureStop;
            _augmentSelectionSystem.OnStopShowcase += HandleStopShowcase;
        }
        
        private void OnDisable()
        {
            if (_gm != null)
                _gm.OnRaceEndedUI -= PlayAugmentSummary;
            
            if (_confirmationService == null) return;

            _confirmationService.OnStartConfirmation -= HandleStartConfirmation;
            _confirmationService.OnPlayerConfirmed -= HandlePlayerConfirmed;
            _confirmationService.OnAllPlayersConfirmed -=HandleAllPlayersConfirmed;
        }

        void OnDestroy()
        {
            if (_augmentSelectionSystem == null)
            {
                Debug.LogWarning(
                    "[OnRaceUI] No AugmentSelectionSystem found: Potential memory leak.");
                return;
            }

            _augmentSelectionSystem.OnPressureStart -= HandlePressureStart;
            _augmentSelectionSystem.OnPressureStop -= HandlePressureStop;
            _augmentSelectionSystem.OnStopShowcase -= HandleStopShowcase;
        }

        private void HandleStartConfirmation(int activePlayerCount)
        {
            _playerConfirmationUI.gameObject.SetActive(true);
            _playerConfirmationUI.ShowConfirmation(activePlayerCount);
        }
        
        private void HandlePlayerConfirmed(int playerIndex)
        {
            _playerConfirmationUI.NotifyPlayerConfirmed(playerIndex);
        }

        private void HandleAllPlayersConfirmed()
        {
            _playerConfirmationUI.OnConfirmation();
        }

        private void HandlePressureStart(float duration)
        {
            _racePressureUI.gameObject.SetActive(true);
            _racePressureUI.StartVignettePressure(duration);
        }
        
        private void HandlePressureStop()
        {
            _racePressureUI.StopVignettePressure();
            _racePressureUI.gameObject.SetActive(false);
        }

        private void HandleStopShowcase()
        {
            _raceCountdownUI.gameObject.SetActive(true);
            _raceCountdownUI.PlayCountdown().Forget();
        }

        private async UniTask PlayAugmentSummary()
        {
            _augmentSummaryUI.gameObject.SetActive(true);

            var players = _lobbyService.GetPlayers();

            List<List<SO_Augment>> playerAugments = new();
            foreach (var player in players)
            {
                if (_augmentSelectionSystem.PickedAugments.TryGetValue(player.Character, out var augments))
                    playerAugments.Add(augments);
                else
                    playerAugments.Add(new List<SO_Augment>());
            }

            await _augmentSummaryUI.AnimatePlayerImagesWithAugments(playerAugments);

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            _augmentSummaryUI.gameObject.SetActive(false);
        }

    }
}