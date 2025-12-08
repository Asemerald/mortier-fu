using System;
using MortierFu;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.UI;

public class BootstrapPanel : MonoBehaviour
{
    [SerializeField] private GameInitializer gameInitializer;
    
    private void Awake()
    {
        if (gameInitializer == null)
        {
            Logs.LogWarning("BootstrapPanel: GameBootstrap reference is missing.", this);
        }
    }
}
