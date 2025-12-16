using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MortierFu
{
    public class OnRaceUI : MonoBehaviour
    {
        [SerializeField] private PlayerConfirmationUI _playerConfirmationUI;
        [SerializeField] private RacePressureUI _racePressureUI;

        [SerializeField] private CountdownUI _raceCountdownUI;

        private ConfirmationService _confirmationService;
        private AugmentSelectionSystem _augmentSelectionSystem;
        
        private void Awake()
        {
            _confirmationService = ServiceManager.Instance.Get<ConfirmationService>();

            _playerConfirmationUI.gameObject.SetActive(false);
            _racePressureUI.gameObject.SetActive(false);
            _raceCountdownUI.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_confirmationService != null)
            {
                _confirmationService.OnStartConfirmation += ShowConfirmation;
                _confirmationService.OnPlayerConfirmed += _playerConfirmationUI.NotifyPlayerConfirmed;
                _confirmationService.OnAllPlayersConfirmed += _playerConfirmationUI.OnConfirmation;
            }
            else
            {
                Debug.LogError($"OnRaceUI] No ConfirmationService found for {gameObject.name}");
            }
        }

        private void OnDisable()
        {
            if (_confirmationService == null) return;

            _confirmationService.OnStartConfirmation -= ShowConfirmation;
            _confirmationService.OnPlayerConfirmed -= _playerConfirmationUI.NotifyPlayerConfirmed;
            _confirmationService.OnAllPlayersConfirmed -= _playerConfirmationUI.OnConfirmation;
        }

        private void Start()
        {
            _augmentSelectionSystem = SystemManager.Instance.Get<AugmentSelectionSystem>();

            if (_augmentSelectionSystem == null)
            {
                Debug.LogError($"[RacePressureUI] No AugmentSelectionSystem found for {gameObject.name}");
                return;
            }

            _augmentSelectionSystem.OnPressureStart += StartVignettePressure;
            _augmentSelectionSystem.OnPressureStop += _racePressureUI.StopVignettePressure;
            _augmentSelectionSystem.OnStopShowcase += StartRaceCountdown;
        }

        void OnDestroy()
        {
            if (_augmentSelectionSystem == null)
            {
                Debug.LogWarning(
                    "[OnRaceUI] No AugmentSelectionSystem found: Potential memory leak.");
                return;
            }

            _augmentSelectionSystem.OnPressureStart -= StartVignettePressure;
            _augmentSelectionSystem.OnPressureStop -= _racePressureUI.StopVignettePressure;
            _augmentSelectionSystem.OnStopShowcase -= StartRaceCountdown;
        }

        private void ShowConfirmation(int activePlayerCount)
        {
            _playerConfirmationUI.gameObject.SetActive(true);
            _playerConfirmationUI.ShowConfirmation(activePlayerCount);
        }

        private void StartVignettePressure(float duration)
        {
            _racePressureUI.gameObject.SetActive(true);
            _racePressureUI.StartVignettePressure(duration);
        }

        private void StartRaceCountdown()
        {
            _raceCountdownUI.gameObject.SetActive(true);
            _raceCountdownUI.PlayCountdown().Forget();
        }
    }
}