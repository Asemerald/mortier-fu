using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class AchievementSystem : IGameSystem
    {
        private enum DifferentDeathType
        {
            Bombshell,
            Fall,
            Vehicle
        }
        
        private const string NO_SHOOTING_ACHIEVEMENT_ID = "NO_SHOOTING";
        private const string NO_DAMAGE_RECEIVED_ACHIEVEMENT_ID = "NO_DAMAGE_RECEIVED";
        private const string FIFTY_TAUNTS_ACHIEVEMENT_ID = "FIFTY_TAUNTS";
        private const string TAUNT_THEN_KILL_ACHIEVEMENT_ID = "TAUNT_THEN_KILL";
        private const string REVENGE_KILL_ACHIEVEMENT_ID = "REVENGE_KILL";
        private const string THIRTY_KILLS_ACHIEVEMENT_ID = "THIRTY_KILLS";
        private const string DIFFERENT_DEATHS_ACHIEVEMENT_ID = "DIFFERENT_DEATHS";
        private const string FALL_IN_WATER_ACHIEVEMENT_ID = "FALL_IN_WATER";
        private const string POST_MORTEM_ACHIEVEMENT_ID = "POST_MORTEM";
        private const string ASCENSION_KILL_ACHIEVEMENT_ID = "ASCENSION_KILL";
        
        private const int FIFTY_TAUNTS_REQUIRED_COUNT = 50;
        private const int THIRTY_KILLS_REQUIRED_COUNT = 30;
        private const int DIFFERENT_DEATHS_REQUIRED_COUNT = 3;
        
        private const float TAUNT_THEN_KILL_WINDOW = 5f;

        private readonly HashSet<PlayerManager> _playersWhoFiredThisRound = new();
        private readonly HashSet<PlayerManager> _playersWhoTookDamageThisGame = new();
        private readonly HashSet<PlayerManager> _eliminatedPlayersThisRound = new();
        private readonly HashSet<DifferentDeathType> _deathTypesThisRound = new();
        
        private readonly Dictionary<PlayerManager, float> _lastTauntTimes = new();
        private readonly Dictionary<PlayerManager, int> _tauntsThisGame = new();
        private readonly Dictionary<PlayerManager, int> _killsThisGame = new();
        private readonly Dictionary<PlayerManager, PlayerManager> _activeRevengeTargets = new();
        private readonly Dictionary<PlayerManager, PlayerManager> _pendingRevengeTargets = new();

        private EventBinding<TriggerShootBombshell> _shootBombshellBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        private EventBinding<TriggerHealthChanged> _healthChangedBinding;
        private EventBinding<TriggerTaunt> _tauntBinding;
        private EventBinding<EventPlayerDeath> _playerDeathBinding;

        private int _trackedDeathsThisRound;
        
        private GameModeBase _gameMode;
        private LobbyService _lobbyService;

        public bool IsInitialized { get; set; }

        public UniTask OnInitialize()
        {
            _gameMode = GameService.CurrentGameMode as GameModeBase;
            _lobbyService = ServiceManager.Instance?.Get<LobbyService>();

            RegisterEvents();

            Logs.Log("[AchievementSystem] Initialized.");

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            DeregisterEvents();

            ClearRuntimeData();

            _gameMode = null;
            _lobbyService = null;

            Logs.Log("[AchievementSystem] Disposed.");
        }

        private void RegisterEvents()
        {
            _shootBombshellBinding = new EventBinding<TriggerShootBombshell>(OnShootBombshell);
            EventBus<TriggerShootBombshell>.Register(_shootBombshellBinding);

            _endRoundBinding = new EventBinding<TriggerEndRound>(OnEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);
            
            _healthChangedBinding = new EventBinding<TriggerHealthChanged>(OnHealthChanged);
            EventBus<TriggerHealthChanged>.Register(_healthChangedBinding);
            
            _tauntBinding = new EventBinding<TriggerTaunt>(OnTaunt);
            EventBus<TriggerTaunt>.Register(_tauntBinding);
            
            _playerDeathBinding = new EventBinding<EventPlayerDeath>(OnPlayerDeath);
            EventBus<EventPlayerDeath>.Register(_playerDeathBinding);
            
            if (_gameMode == null)
                return;

            _gameMode.OnRoundGameplayStarted += OnRoundGameplayStarted;
            _gameMode.OnGameEnded += OnGameEnded;
        }

        private void DeregisterEvents()
        {
            if (_shootBombshellBinding != null)
            {
                EventBus<TriggerShootBombshell>.Deregister(_shootBombshellBinding);
                _shootBombshellBinding = null;
            }

            if (_endRoundBinding != null)
            {
                EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
                _endRoundBinding = null;
            }

            if (_healthChangedBinding != null)
            {
                EventBus<TriggerHealthChanged>.Deregister(_healthChangedBinding);
                _healthChangedBinding = null;
            }

            if (_tauntBinding != null)
            {
                EventBus<TriggerTaunt>.Deregister(_tauntBinding);
                _tauntBinding = null;
            }

            if (_playerDeathBinding != null)
            {
                EventBus<EventPlayerDeath>.Deregister(_playerDeathBinding);
                _playerDeathBinding = null;
            }

            if (_gameMode == null)
                return;

            _gameMode.OnRoundGameplayStarted -= OnRoundGameplayStarted;
            _gameMode.OnGameEnded -= OnGameEnded;
        }

        private void OnRoundGameplayStarted(RoundInfo roundInfo)
        {
            _playersWhoFiredThisRound.Clear();
            _lastTauntTimes.Clear();
            _deathTypesThisRound.Clear();
            _eliminatedPlayersThisRound.Clear();

            _trackedDeathsThisRound = 0;

            ActivatePendingRevengeTargets();
        }

        private void OnShootBombshell(TriggerShootBombshell evt)
        {
            PlayerManager player = evt.Character?.Owner;

            if (player == null)
                return;

            _playersWhoFiredThisRound.Add(player);
        }

        private void OnHealthChanged(TriggerHealthChanged evt)
        {
            if (evt.Delta >= 0f || evt.Character?.Owner == null)
                return;

            _playersWhoTookDamageThisGame.Add(evt.Character.Owner);
        }

        private void OnTaunt(TriggerTaunt evt)
        {
            PlayerManager player = evt.Character?.Owner;

            if (player == null)
                return;

            TrackFiftyTaunts(player);

            _lastTauntTimes[player] = Time.time;
        }

        private void OnPlayerDeath(EventPlayerDeath evt)
        {
            CheckFallInWaterAchievement(evt);
            TrackDifferentDeath(evt.Context.DeathCause);

            if (TryGetValidKill(evt, out PlayerManager killer, out PlayerManager victim))
            {
                CheckPostMortemAchievement(killer);
                CheckTauntThenKillAchievement(killer);
                CheckRevengeKillAchievement(killer, victim);
                CheckAscensionKillAchievement(killer);

                TrackKill(killer);
                RegisterRevengeTarget(killer, victim);
            }

            RegisterEliminatedPlayer(evt);
        }

        private void OnEndRound(TriggerEndRound evt)
        {
            if (!TryGetRoundWinner(evt.WinningTeam, out PlayerManager winner))
                return;

            CheckNoShootingAchievement(winner);
            CheckDifferentDeathsAchievement();
        }

        private void OnGameEnded(int winnerPlayerIndex)
        {
            PlayerManager winner = _gameMode.GetWinnerPlayer();
            if (winner == null)
                return;

            SteamManager.AddProgressToStat("GAME_PLAYED");
            
            CheckNoDamageReceivedAchievement(winner);
            CheckThirtyKillsAchievement(winner);
        }

        private static bool TryGetValidKill(EventPlayerDeath evt, out PlayerManager killer, out PlayerManager victim)
        {
            killer = null;
            victim = null;

            if (evt.Context.Killer == null || evt.Character == null)
                return false;

            killer = evt.Context.Killer.Owner;
            victim = evt.Character.Owner;

            if (killer == null || victim == null)
                return false;

            return killer != victim;
        }

        private void TrackKill(PlayerManager killer)
        {
            _killsThisGame.TryGetValue(killer, out int currentKills);
            _killsThisGame[killer] = currentKills + 1;
            
            SteamManager.AddProgressToStat("ELIMINATIONS");
        }

        private void RegisterRevengeTarget(PlayerManager killer, PlayerManager victim) => _pendingRevengeTargets[victim] = killer;

        private void ActivatePendingRevengeTargets()
        {
            foreach (KeyValuePair<PlayerManager, PlayerManager> revenge in _pendingRevengeTargets)
                _activeRevengeTargets[revenge.Key] = revenge.Value;

            _pendingRevengeTargets.Clear();
        }

        private void CheckNoShootingAchievement(PlayerManager winner)
        {
            if (_playersWhoFiredThisRound.Contains(winner))
                return;

            SteamManager.UnlockAchievement(NO_SHOOTING_ACHIEVEMENT_ID);
        }
        
        private void CheckDifferentDeathsAchievement()
        {
            if (_lobbyService is not { CurrentPlayerCount: 4 })
                return;

            if (_trackedDeathsThisRound != DIFFERENT_DEATHS_REQUIRED_COUNT || _deathTypesThisRound.Count != DIFFERENT_DEATHS_REQUIRED_COUNT)
                return;

            SteamManager.UnlockAchievement(DIFFERENT_DEATHS_ACHIEVEMENT_ID);
        }

        private void CheckNoDamageReceivedAchievement(PlayerManager winner)
        {
            if (_lobbyService is not { CurrentPlayerCount: 4 })
                return;

            if (_playersWhoTookDamageThisGame.Contains(winner))
                return;

            SteamManager.UnlockAchievement(NO_DAMAGE_RECEIVED_ACHIEVEMENT_ID);
        }

        private void TrackFiftyTaunts(PlayerManager player)
        {
            _tauntsThisGame.TryGetValue(player, out int currentTaunts);

            if (currentTaunts >= FIFTY_TAUNTS_REQUIRED_COUNT)
                return;

            currentTaunts++;

            _tauntsThisGame[player] = currentTaunts;

            if (currentTaunts == FIFTY_TAUNTS_REQUIRED_COUNT)
                SteamManager.UnlockAchievement(FIFTY_TAUNTS_ACHIEVEMENT_ID);
        }
        
        private void TrackDifferentDeath(E_DeathCause deathCause)
        {
            if (!TryGetDifferentDeathType(deathCause, out DifferentDeathType deathType))
                return;

            _trackedDeathsThisRound++;
            _deathTypesThisRound.Add(deathType);
        }
        
        private static bool TryGetDifferentDeathType(E_DeathCause deathCause, out DifferentDeathType deathType)
        {
            switch (deathCause)
            {
                case E_DeathCause.BombshellExplosion:
                    deathType = DifferentDeathType.Bombshell;
                    return true;

                case E_DeathCause.Fall:
                case E_DeathCause.FallAfterExplosion:
                    deathType = DifferentDeathType.Fall;
                    return true;

                case E_DeathCause.VehicleCrash:
                    deathType = DifferentDeathType.Vehicle;
                    return true;

                default:
                    deathType = default;
                    return false;
            }
        }
        
        private void CheckPostMortemAchievement(PlayerManager killer)
        {
            if (!_eliminatedPlayersThisRound.Contains(killer))
                return;

            SteamManager.UnlockAchievement(POST_MORTEM_ACHIEVEMENT_ID);
        }
        
        private void CheckFallInWaterAchievement(EventPlayerDeath evt)
        {
            if (evt.Context.DeathCause != E_DeathCause.Fall || evt.Context.Killer != null)
                return;

            SteamManager.UnlockAchievement(FALL_IN_WATER_ACHIEVEMENT_ID);
        }

        private void CheckTauntThenKillAchievement(PlayerManager killer)
        {
            if (!_lastTauntTimes.TryGetValue(killer, out float lastTauntTime))
                return;

            if (Time.time - lastTauntTime > TAUNT_THEN_KILL_WINDOW)
            {
                _lastTauntTimes.Remove(killer);
                return;
            }

            SteamManager.UnlockAchievement(TAUNT_THEN_KILL_ACHIEVEMENT_ID);

            _lastTauntTimes.Remove(killer);
        }

        private void CheckRevengeKillAchievement(PlayerManager killer, PlayerManager victim)
        {
            if (!_activeRevengeTargets.TryGetValue(killer, out PlayerManager revengeTarget) || revengeTarget != victim)
                return;

            SteamManager.UnlockAchievement(REVENGE_KILL_ACHIEVEMENT_ID);
            _activeRevengeTargets.Remove(killer);
        }
        
        private void CheckAscensionKillAchievement(PlayerManager killer)
        {
            if (!HasActiveAscension(killer))
                return;

            SteamManager.UnlockAchievement(ASCENSION_KILL_ACHIEVEMENT_ID);
        }

        private void CheckThirtyKillsAchievement(PlayerManager winner)
        {
            if (!_killsThisGame.TryGetValue(winner, out int killCount) || killCount < THIRTY_KILLS_REQUIRED_COUNT)
                return;

            SteamManager.UnlockAchievement(THIRTY_KILLS_ACHIEVEMENT_ID);
        }
        
        private void RegisterEliminatedPlayer(EventPlayerDeath evt)
        {
            PlayerManager player = evt.Character?.Owner;
            if (player == null)
                return;

            _eliminatedPlayersThisRound.Add(player);
        }
        
        private static bool HasActiveAscension(PlayerManager player)
        {
            if (player?.Character?.Augments == null)
                return false;

            foreach (IAugment augment in player.Character.Augments)
            {
                if (augment is AGM_Ascension { IsActive: true })
                    return true;
            }

            return false;
        }

        private static bool TryGetRoundWinner(PlayerTeam winningTeam, out PlayerManager winner)
        {
            winner = null;

            if (winningTeam?.Members == null || winningTeam.Members.Count == 0)
                return false;

            winner = winningTeam.Members[0];

            return winner != null;
        }

        private void ClearRuntimeData()
        {
            _playersWhoFiredThisRound.Clear();
            _lastTauntTimes.Clear();

            _deathTypesThisRound.Clear();
            _trackedDeathsThisRound = 0;
            
            _playersWhoTookDamageThisGame.Clear();
            _tauntsThisGame.Clear();
            _killsThisGame.Clear();

            _activeRevengeTargets.Clear();
            _pendingRevengeTargets.Clear();
            
            _eliminatedPlayersThisRound.Clear();
        }
    }
}