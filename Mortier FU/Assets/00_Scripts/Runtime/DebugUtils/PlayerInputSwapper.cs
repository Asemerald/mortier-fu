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
        private void Start()
        {
            // TA GRAND MERE UNITY ÇA SERT A QUOI DE METTRE DES ACTIONS SI ELLES MARCHENT PAS
            
            /*if (playerInputManager == null)
            {
                Logs.LogError("PlayerInputManager not found");
                return;
            }
            playerInputManager.onPlayerJoined += RegisterNewPlayer;
            playerInputManager.onPlayerLeft += DeregisterPlayer;*/
        }

        /*private void RegisterNewPlayer(PlayerInput playerInput)
        {
            players.Add(playerInput);
            Debug.Log($"Player joined: {playerInput.name}");
        }

        private void DeregisterPlayer(PlayerInput playerInput)
        {
            players.Remove(playerInput);
            Debug.Log($"Player left: {playerInput.name}");
        }*/

        void Update()
        {
            // Spawn dummy avec clavier
            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                SpawnDummy();
            }

            /*// Cycle avec RB
            if (Gamepad.current != null &&
                Gamepad.current.rightShoulder.wasPressedThisFrame)
            {
                CycleControl();
            }*/
        }

        void SpawnDummy()
        {
            if (playerInputManager != null)
            {
                playerInputManager.JoinPlayer();
                Debug.Log("Dummy player spawned");
            }
        }

        void CycleControl()
        {
            var allPlayers = FindObjectsOfType<PlayerInput>();

            if (allPlayers.Length <= 1 || Gamepad.current == null)
                return;

            PlayerInput currentPlayer = null;
            foreach (var player in allPlayers)
            {
                if (player.devices.Contains(Gamepad.current))
                {
                    currentPlayer = player;
                    break;
                }
            }

            if (currentPlayer == null)
                currentPlayer = allPlayers[0];

            int currentIndex = System.Array.IndexOf(allPlayers, currentPlayer);
            int nextIndex = (currentIndex + 1) % allPlayers.Length;
            var nextPlayer = allPlayers[nextIndex];

            // Désactive TOUS les players sauf le next
            foreach (var player in allPlayers)
            {
                player.DeactivateInput();
            }

            // Active uniquement le next avec la manette
            nextPlayer.ActivateInput();
            nextPlayer.SwitchCurrentControlScheme("Gamepad", Gamepad.current);

            Debug.Log($"Swapped to player: {nextPlayer.name}");
        }
    }
}
