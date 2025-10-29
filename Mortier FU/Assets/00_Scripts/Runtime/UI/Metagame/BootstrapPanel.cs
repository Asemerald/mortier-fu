using System;
using MortierFu;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.UI;

public class BootstrapPanel : MonoBehaviour
{
    [SerializeField] private GameInitializer gameInitializer;
    [SerializeField] private Image _loadingBar;
    
    private void Awake()
    {
        if (gameInitializer == null)
        {
            Logs.LogWarning("BootstrapPanel: GameBootstrap reference is missing.", this);
        }
        
        if (_loadingBar == null)
        {
            Logs.LogWarning("BootstrapPanel: LoadingBar reference is missing.", this);
        }
    }

    private void Update()
    {
        if (gameInitializer && _loadingBar)
        {
            //_loadingBar.fillAmount = _gameBootstrap.GetProgress();
            //TODO Do something, ça load tellement vite la progress bar sert a r mais a test avec des mods fat
        }
    }
}
