using System;
using MortierFu;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuButton : MonoBehaviour
{
    private EventSystem _eventSystem;
    private MenuManager _menuManager;
    private Button _button;

    private void Start()
    {
        _eventSystem = EventSystem.current;
        _menuManager = MenuManager.Instance;
        _button = gameObject.GetComponent<Button>();
    }

    private void Update()
    {
        if (_eventSystem.currentSelectedGameObject != gameObject)
            return;

        if (_menuManager.LastButton == _button)
            return;
        
        _menuManager.LastButton = _button;
        _menuManager.ChangeDiscordSelectOnLeftButton(_button);
    }
}
