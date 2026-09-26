using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class AchievementSystem : IGameSystem
    {
        private const string NO_SHOOTING_ACHIEVEMENT_ID = "NO_SHOOTING";
        private const string NO_DAMAGE_RECEIVED_ACHIEVEMENT_ID = "NO_DAMAGE_RECEIVED";
        private const string FIFTY_TAUNTS_ACHIEVEMENT_ID = "FIFTY_TAUNTS";
        private const string TAUNT_THEN_KILL_ACHIEVEMENT_ID = "TAUNT_THEN_KILL";
        private const string REVENGE_KILL_ACHIEVEMENT_ID = "REVENGE_KILL";
        private const string THIRTY_KILLS_ACHIEVEMENT_ID = "THIRTY_KILLS";

        private const int FIFTY_TAUNTS_REQUIRED_COUNT = 50;
        private const int THIRTY_KILLS_REQUIRED_COUNT = 30;

        private const float TAUNT_THEN_KILL_WINDOW = 5f;

        private readonly HashSet<PlayerManager> _playersWhoFiredThisRound = new();
        private readonly HashSet<PlayerManager> _playersWhoTookDamageThisGame = new();

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
            if (!TryGetValidKill(evt, out PlayerManager killer, out PlayerManager victim))
                return;

            CheckTauntThenKillAchievement(killer);
            CheckRevengeKillAchievement(killer, victim);

            TrackKill(killer);
            RegisterRevengeTarget(killer, victim);
        }

        private void OnEndRound(TriggerEndRound evt)
        {
            if (!TryGetRoundWinner(evt.WinningTeam, out PlayerManager winner))
                return;

            CheckNoShootingAchievement(winner);
        }

        private void OnGameEnded(int winnerPlayerIndex)
        {
            PlayerManager winner = _gameMode.GetWinnerPlayer();
            if (winner == null)
                return;

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

        private void CheckThirtyKillsAchievement(PlayerManager winner)
        {
            if (!_killsThisGame.TryGetValue(winner, out int killCount) || killCount < THIRTY_KILLS_REQUIRED_COUNT)
                return;

            SteamManager.UnlockAchievement(THIRTY_KILLS_ACHIEVEMENT_ID);
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

            _playersWhoTookDamageThisGame.Clear();
            _tauntsThisGame.Clear();
            _killsThisGame.Clear();

            _activeRevengeTargets.Clear();
            _pendingRevengeTargets.Clear();
        }
    }
}