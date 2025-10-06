using System;
using MortierFU;
using MortierFU.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                Logs.Log("GameManager instance created.");
                DontDestroyOnLoad(this.gameObject);
            }
        }
    }
}
