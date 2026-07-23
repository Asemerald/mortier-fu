using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MortierFu.Shared;

namespace MortierFu.Analytics
{
    public partial class AnalyticsSystem
    {
        private EventBinding<TriggerHit> _triggerHitBinding;
        private EventBinding<TriggerShootBombshell> _triggerShootBombshellBinding;
        private EventBinding<TriggerHealthChanged> _triggerHealthChangedBinding;
        private EventBinding<TriggerDash> _triggerDashBinding;
        private EventBinding<TriggerStrike> _triggerStrikeBinding;
        private EventBinding<EventPlayerDeath> _triggerDeathBinding;
        private EventBinding<TriggerEndRound> _triggerEndRoundBinding;
        private EventBinding<TriggerSuccessfulPush> _triggerSuccessfulPushBinding;

        private GameState _currentGameState;

        private void RegisterEvents()
        {
            _triggerShootBombshellBinding = new EventBinding<TriggerShootBombshell>(OnTriggerShootBombshell);
            EventBus<TriggerShootBombshell>.Register(_triggerShootBombshellBinding);

            _triggerHitBinding = new EventBinding<TriggerHit>(OnTriggerHit);
            EventBus<TriggerHit>.Register(_triggerHitBinding);

            _triggerHealthChangedBinding = new EventBinding<TriggerHealthChanged>(OnTriggerHealthChanged);
            EventBus<TriggerHealthChanged>.Register(_triggerHealthChangedBinding);

            _triggerDashBinding = new EventBinding<TriggerDash>(OnTriggerDash);
            EventBus<TriggerDash>.Register(_triggerDashBinding);

            _triggerStrikeBinding = new EventBinding<TriggerStrike>(OnTriggerStrike);
            EventBus<TriggerStrike>.Register(_triggerStrikeBinding);

            _triggerDeathBinding = new EventBinding<EventPlayerDeath>(OnTriggerDeath);
            EventBus<EventPlayerDeath>.Register(_triggerDeathBinding);

            _triggerEndRoundBinding = new EventBinding<TriggerEndRound>(OnTriggerEndRound);
            EventBus<TriggerEndRound>.Register(_triggerEndRoundBinding);

            _triggerSuccessfulPushBinding = new EventBinding<TriggerSuccessfulPush>(OnTriggerSuccessfulPush);
            EventBus<TriggerSuccessfulPush>.Register(_triggerSuccessfulPushBinding);

            if (GameService.CurrentGameMode != null)
            {
                GameService.CurrentGameMode.OnGameEnded += OnGameEndedHandler;
                GameService.CurrentGameMode.OnGameStateChanged += OnGameStateChangedHandler;

                _currentGameState = (GameService.CurrentGameMode as GameModeBase)?.CurrentGameState ?? GameState.Lobby;
            }
            else
                Logs.LogWarning("[AnalyticsSystem] CurrentGameMode est null à l'initialisation, donc pas reçu");
        }

        private void DeregisterEvents()
        {
            EventBus<TriggerShootBombshell>.Deregister(_triggerShootBombshellBinding);
            EventBus<TriggerHit>.Deregister(_triggerHitBinding);
            EventBus<TriggerHealthChanged>.Deregister(_triggerHealthChangedBinding);
            EventBus<TriggerDash>.Deregister(_triggerDashBinding);
            EventBus<TriggerStrike>.Deregister(_triggerStrikeBinding);
            EventBus<EventPlayerDeath>.Deregister(_triggerDeathBinding);
            EventBus<TriggerEndRound>.Deregister(_triggerEndRoundBinding);
            EventBus<TriggerSuccessfulPush>.Deregister(_triggerSuccessfulPushBinding);

            if (GameService.CurrentGameMode != null)
            {
                GameService.CurrentGameMode.OnGameEnded -= OnGameEndedHandler;
                GameService.CurrentGameMode.OnGameStateChanged -= OnGameStateChangedHandler;
            }
        }

        private void OnGameStateChangedHandler(GameState newState)
        {
            bool wasInCombat = IsInCombatPhase();
            
            _currentGameState = newState;

            if (!wasInCombat && IsInCombatPhase())
            {
                _roundStartTime = System.DateTime.UtcNow;
            }
        }

        private bool IsInCombatPhase()
        {
            return _currentGameState != GameState.AugmentIntro
                && _currentGameState != GameState.AugmentRace
                && _currentGameState != GameState.EndAugmentRace;
        }

        private string GetPlayerIdFromCharacter(PlayerManager character)
        {
            return character.PlayerIndex.ToString();
        }

        private AnalyticsPlayerData GetOrCreatePlayerData(PlayerCharacter character)
        {
            string playerId = GetPlayerIdFromCharacter(character.Owner);

            if (!_currentRoundPlayers.ContainsKey(playerId))
            {
                var newPlayerData = new AnalyticsPlayerData()
                {
                    playerId = playerId,
                    rank = 0,
                    score = 0,
                    kills = 0,
                    selectedAugment = null,
                    damageDealt = 0f,
                    damageTaken = 0f,
                    shotsFired = 0,
                    shotsHit = 0,
                    dashesPerformed = 0,
                    bumpsMade = 0,
                    stunsPerformed = 0,
                    stunsUnderwented = 0,
                    killerId = -1,
                    deathCause = E_DeathCause.Unknown
                };
                _currentRoundPlayers[playerId] = newPlayerData;
            }

            return _currentRoundPlayers[playerId];
        }

        private void OnTriggerShootBombshell(TriggerShootBombshell shootBombshell)
        {
            if (!IsInCombatPhase()) return;
            if (shootBombshell.Character == null) return;

            var playerData = GetOrCreatePlayerData(shootBombshell.Character);
            playerData.shotsFired++;
        }

        private void OnTriggerHit(TriggerHit hit)
        {
            if (!IsInCombatPhase()) return;
            if (hit.ShooterId == null) return;

            var shooterData = GetOrCreatePlayerData(hit.ShooterId);

            if (hit.HitCharacters != null && hit.HitCharacters.Length > 0)
            {
                shooterData.shotsHit++;
            }
        }

        private void OnTriggerHealthChanged(TriggerHealthChanged healthChanged)
        {
            if (healthChanged.Instigator != null && healthChanged.Delta < 0)
            {
                var instigatorData = GetOrCreatePlayerData(healthChanged.Instigator);
                instigatorData.damageDealt += Mathf.Abs(healthChanged.Delta);
            }

            if (healthChanged.Character != null && healthChanged.Delta < 0)
            {
                var victimData = GetOrCreatePlayerData(healthChanged.Character);
                victimData.damageTaken += Mathf.Abs(healthChanged.Delta);
            }
        }

        private void OnTriggerDash(TriggerDash dash)
        {
            if (!IsInCombatPhase()) return;
            if (dash.Character == null) return;

            var playerData = GetOrCreatePlayerData(dash.Character);
            playerData.dashesPerformed++;
        }

        private void OnTriggerStrike(TriggerStrike strike)
        {
            if (!IsInCombatPhase()) return;
            if (strike.Character == null) return;

            var playerData = GetOrCreatePlayerData(strike.Character);

            if (strike.HitCharacters != null)
            {
                playerData.bumpsMade += strike.HitCharacters.Length;
            }
        }

        private void OnTriggerSuccessfulPush(TriggerSuccessfulPush push)
        {
            if (!IsInCombatPhase()) return;
            if (push.Character == null) return;

            bool causedByPlayer = push.Source is PlayerCharacter instigator && instigator != push.Character;
            if (!causedByPlayer) return;

            var victimData = GetOrCreatePlayerData(push.Character);
            victimData.stunsUnderwented++;

            var instigatorPlayer = (PlayerCharacter)push.Source;
            var instigatorData = GetOrCreatePlayerData(instigatorPlayer);
                instigatorData.stunsPerformed++;
            
        }

        private void OnTriggerDeath(EventPlayerDeath death)
        {
            if (death.Character == null) return;

            var victimData = GetOrCreatePlayerData(death.Character);
            bool isSelfKill = death.Context.Killer && death.Context.Killer == death.Character;

            if (death.Context.Killer && !isSelfKill)
            {
                var killerData = GetOrCreatePlayerData(death.Context.Killer);
                killerData.kills++;
            }

            if (death.Context.Killer)
                victimData.killerId = death.Context.Killer.Owner.PlayerIndex;
            victimData.deathCause = death.Context.DeathCause;
        }

        private void AssignRanks(List<AnalyticsPlayerData> players)
        {
            var sortedPlayers = players.OrderByDescending(p => p.score)
                                      .ThenByDescending(p => p.kills)
                                      .ToList();

            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                sortedPlayers[i].rank = i + 1;
            }
        }

        public void OnAugmentSelected(PlayerCharacter character, SO_Augment augment = null)
        {
            var playerData = GetOrCreatePlayerData(character);
            playerData.selectedAugment = augment;

            string playerId = GetPlayerIdFromCharacter(character.Owner);
        }

        public void OnScoreChanged(PlayerCharacter character, int newScore)
        {
            var playerData = GetOrCreatePlayerData(character);
            playerData.score = newScore;
            
            string playerId = GetPlayerIdFromCharacter(character.Owner);
            _lastKnownScorePerPlayer[playerId] = newScore;
        }
    }
}