using System;
using System.Collections.Generic;
using System.Linq;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace MortierFu
{
    public class PlayerInputSwapper : MonoBehaviour
    {
        public PlayerInputManager playerInputManager;
        private PlayerInput activePlayer;
        
        public static PlayerInputSwapper Instance;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Active uniquement le premier player au démarrage
            var firstPlayer = FindObjectOfType<PlayerInput>();
            if (firstPlayer != null)
            {
                activePlayer = firstPlayer;
                UpdateActivePlayer();
            }
        }

        void Update()
        {
            // Spawn dummy avec clavier
            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                SpawnDummy();
            }

            // Cycle avec RB
            if (Gamepad.current != null &&
                Gamepad.current.rightStickButton.wasPressedThisFrame)
            {
                CycleControl();
            }
            
            if (Gamepad.current != null &&
                Gamepad.current.leftStickButton.wasPressedThisFrame)
            {
                UpdateActivePlayer();
            }
        }

        void SpawnDummy()
        {
            if (playerInputManager != null)
            {
                var newPlayer = playerInputManager.JoinPlayer(
                    playerIndex: -1,
                    splitScreenIndex: -1,
                    controlScheme: null,
                    pairWithDevices: Gamepad.current
                );

                Debug.Log($"Dummy player spawned: {newPlayer?.name}");
                
                // Désactive le nouveau player immédiatement
                if (newPlayer != null)
                {
                    newPlayer.DeactivateInput();
                }
            }
        }

        void CycleControl()
        {
            var allPlayers = FindObjectsOfType<PlayerInput>();

            if (allPlayers.Length <= 1)
                return;

            // Si pas de player actif, prend le premier
            if (activePlayer == null || !allPlayers.Contains(activePlayer))
            {
                activePlayer = allPlayers[0];
            }

            // Détermine le prochain player
            int currentIndex = System.Array.IndexOf(allPlayers, activePlayer);
            int nextIndex = (currentIndex + 1) % allPlayers.Length;
            activePlayer = allPlayers[nextIndex];

            UpdateActivePlayer();

            Debug.Log($"Switched to player: {activePlayer.name}");
        }

        public void UpdateActivePlayer()
        {
            var allPlayers = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);

            // Désactive TOUS les players
            foreach (var player in allPlayers)
            {
                player.DeactivateInput();
            }

            // Active UNIQUEMENT le player actif
            if (activePlayer != null)
            {
                activePlayer.ActivateInput();
            }
        }
    }
}