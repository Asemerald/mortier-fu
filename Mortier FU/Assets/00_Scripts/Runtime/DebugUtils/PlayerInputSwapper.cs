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

            // TODO: Change Input parce que c'est la meme que le dash
            if (Gamepad.current != null &&
                Gamepad.current.rightShoulder.wasPressedThisFrame)
            {
                CycleControl();
            }
            
            
        }

        void SpawnDummy()
        {
            if (playerInputManager != null)
            {
                
                var newPlayer = playerInputManager.JoinPlayer(
                    playerIndex: -1,  // Auto-assign
                    splitScreenIndex: -1,  // Auto-assign
                    controlScheme: null,  
                    pairWithDevices: Gamepad.current
                );
        
                Debug.Log($"Dummy player spawned: {newPlayer?.name}");
            }
        }

        void CycleControl()
        {
            var allPlayers = FindObjectsOfType<PlayerInput>();

            if (allPlayers.Length <= 1 || Gamepad.current == null)
                return;

            // Trouve le player actuel qui possède la manette
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

            // Détermine le prochain player
            int currentIndex = System.Array.IndexOf(allPlayers, currentPlayer);
            int nextIndex = (currentIndex + 1) % allPlayers.Length;
            var nextPlayer = allPlayers[nextIndex];

            // Récupère TOUS les devices du current player
            var allDevices = currentPlayer.devices.ToArray();

            Debug.Log($"Transferring {allDevices.Length} devices from {currentPlayer.name} to {nextPlayer.name}");

            // Unpair TOUS les devices du current player
            foreach (var device in allDevices)
            {
                InputUser.PerformPairingWithDevice(device, 
                    currentPlayer.user, 
                    InputUserPairingOptions.UnpairCurrentDevicesFromUser);
            }

            // Pair TOUS les devices au next player
            foreach (var device in allDevices)
            {
                InputUser.PerformPairingWithDevice(device, nextPlayer.user);
            }

            Debug.Log($"Next player now has {nextPlayer.devices.Count()} devices: {string.Join(", ", nextPlayer.devices.Select(d => d.name))}");
        }
    }
}
