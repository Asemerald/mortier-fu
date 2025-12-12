using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class UIInputModeSwitcher : MonoBehaviour
    {

        private EventSystem eventSystem;
        private bool usingController = false;

        private GameObject lastSelected;

        void Start()
        {
            eventSystem = EventSystem.current;
        }

        void Update()
        {
            // Store last selected GameObject
            if (eventSystem.currentSelectedGameObject != null && eventSystem.currentSelectedGameObject != lastSelected)
            {
                lastSelected = eventSystem.currentSelectedGameObject;
            }

            var mouse = Mouse.current;
            var gamepad = Gamepad.current;

            // --- Detect mouse movement or click ---
            if (mouse != null && 
                (mouse.delta.ReadValue() != Vector2.zero || mouse.leftButton.wasPressedThisFrame))
            {
                if (usingController)
                {
                    usingController = false;
                    eventSystem.SetSelectedGameObject(null); // Mouse wants no selection
                }
            }

            // --- Detect gamepad / keyboard navigation ---
            if (gamepad != null && 
                (gamepad.leftStick.ReadValue() != Vector2.zero ||
                 gamepad.dpad.ReadValue() != Vector2.zero ||
                 gamepad.buttonSouth.wasPressedThisFrame))
            {
                if (!usingController)
                {
                    usingController = true;
                    eventSystem.SetSelectedGameObject(lastSelected); // Restore navigation focus
                }
            }
        }
    }

}