using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MortierFu
{
    public class SliderController : MonoBehaviour
    {
        [SerializeField] private Slider _fakeSlider;
        [SerializeField] private Slider _sliderToControl;

        private bool _isEditing;

        private Navigation _cachedNavigation;

        private InputAction _submitAction;

        private LobbyService _lobbyService;
        private ShakeService _shakeService;
        
        private PlayerManager _playerManager;

        private void Awake()
        {
            _cachedNavigation = _sliderToControl.navigation;
            _sliderToControl.interactable = false;
        }

        private void Start()
        {
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _shakeService = ServiceManager.Instance.Get<ShakeService>();
            
            _playerManager = _lobbyService.GetPlayerByIndex(0);
            
            _submitAction = _lobbyService.Players[0].PlayerInput.actions.FindAction("Submit");
            _submitAction.started += SetNavigation;
        }

        private void OnDestroy()
        {
            _submitAction.started -= SetNavigation;
        }

        private void SetNavigation(InputAction.CallbackContext context)
        {
            if (EventSystem.current.currentSelectedGameObject != _fakeSlider.gameObject && 
                EventSystem.current.currentSelectedGameObject != _sliderToControl.gameObject)
                return;

            _isEditing = !_isEditing;

            if (_isEditing)
            {
                EnterEditMode();
            }
            else
            {
                ExitEditMode();
            }
        }

        private void EnterEditMode()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);
            
            _sliderToControl.interactable = true;

            var nav = _sliderToControl.navigation;
            nav.mode = Navigation.Mode.None;
            _sliderToControl.navigation = nav;

            EventSystem.current.SetSelectedGameObject(_sliderToControl.gameObject);
        }

        private void ExitEditMode()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);
            
            _sliderToControl.interactable = false;
            _sliderToControl.navigation = _cachedNavigation;

            EventSystem.current.SetSelectedGameObject(_fakeSlider.gameObject);
        }
    }
}