using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyStartTarget : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LobbySandboxController _sandboxController;
        [SerializeField] private LobbyMatchLauncher _matchLauncher;

        [Header("Feedback")]
        [SerializeField] private GameObject[] _playerReadyIndicators;

        private readonly HashSet<PlayerManager> _playersWhoHitTarget = new();

        private EventBinding<TriggerBombshellImpact> _bombshellImpactBinding;

        private void Awake()
        {
            _bombshellImpactBinding = new EventBinding<TriggerBombshellImpact>(OnBombshellImpact);
        }

        private void OnEnable()
        {
            EventBus<TriggerBombshellImpact>.Register(_bombshellImpactBinding);
            RefreshFeedback();
        }

        private void OnDisable()
        {
            EventBus<TriggerBombshellImpact>.Deregister(_bombshellImpactBinding);
        }

        private void OnBombshellImpact(TriggerBombshellImpact evt)
        {
            if (!IsTargetHit(evt.HitObject))
                return;

            var shooterCharacter = evt.Bombshell != null
                ? evt.Bombshell.Owner
                : null;

            if (shooterCharacter == null || shooterCharacter.Owner == null)
                return;

            RegisterPlayerHit(shooterCharacter.Owner);
        }

        private bool IsTargetHit(GameObject hitObject)
        {
            if (hitObject == null)
                return false;

            if (hitObject == gameObject)
                return true;

            return hitObject.transform.IsChildOf(transform);
        }

        private void RegisterPlayerHit(PlayerManager player)
        {
            if (player == null)
                return;

            if (!IsPlayerInSandbox(player))
                return;

            if (!_playersWhoHitTarget.Add(player))
                return;

            Logs.Log($"[LobbyStartTarget] Player {player.PlayerIndex + 1} hit the start target.");

            RefreshFeedback();

            if (AreAllSandboxPlayersReady())
            {
                Logs.Log("[LobbyStartTarget] All sandbox players hit the start target.");

                if (_matchLauncher == null)
                {
                    Logs.LogError("[LobbyStartTarget] MatchLauncher reference is missing.");
                    return;
                }

                _matchLauncher.LaunchMatch();
            }
        }

        private bool IsPlayerInSandbox(PlayerManager player)
        {
            if (_sandboxController == null)
                return false;

            var players = _sandboxController.GetSpawnedPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] == player)
                    return true;
            }

            return false;
        }

        private bool AreAllSandboxPlayersReady()
        {
            if (_sandboxController == null)
                return false;

            var players = _sandboxController.GetSpawnedPlayers();

            if (_matchLauncher != null && !_matchLauncher.CanLaunch(players))
                return false;

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                if (player == null)
                    continue;

                if (!_playersWhoHitTarget.Contains(player))
                    return false;
            }

            return true;
        }

        private void RefreshFeedback()
        {
            if (_playerReadyIndicators == null)
                return;

            for (int i = 0; i < _playerReadyIndicators.Length; i++)
            {
                if (_playerReadyIndicators[i] == null)
                    continue;

                bool ready = IsPlayerIndexReady(i);
                _playerReadyIndicators[i].SetActive(ready);
            }
        }

        private bool IsPlayerIndexReady(int playerIndex)
        {
            foreach (var player in _playersWhoHitTarget)
            {
                if (player != null && player.PlayerIndex == playerIndex)
                    return true;
            }

            return false;
        }

        public void ResetTarget()
        {
            _playersWhoHitTarget.Clear();
            RefreshFeedback();
        }
    }
}