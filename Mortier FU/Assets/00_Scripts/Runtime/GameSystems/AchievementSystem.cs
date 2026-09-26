using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;

namespace MortierFu
{
    public sealed class AchievementSystem : IGameSystem
    {
        private const string NO_SHOOTING_ACHIEVEMENT_ID = "NO_SHOOTING";
        private const string NO_DAMAGE_RECEIVED_ACHIEVEMENT_ID = "NO_DAMAGE_RECEIVED";
        
        private readonly HashSet<PlayerManager> _playersWhoFiredThisRound = new();
        private readonly HashSet<PlayerManager> _playersWhoTookDamageThisGame = new();
        
        private EventBinding<TriggerShootBombshell> _shootBombshellBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        private EventBinding<TriggerHealthChanged> _healthChangedBinding;
        
        private GameModeBase _gameMode;

        public bool IsInitialized { get; set; }

        public UniTask OnInitialize()
        {
            _gameMode = GameService.CurrentGameMode as GameModeBase;

            RegisterEvents();

            Logs.Log("[AchievementSystem] Initialized.");

            return UniTask.CompletedTask;
        }

        private void RegisterEvents()
        {
            _shootBombshellBinding = new EventBinding<TriggerShootBombshell>(OnShootBombshell);

            EventBus<TriggerShootBombshell>.Register(_shootBombshellBinding);

            _endRoundBinding = new EventBinding<TriggerEndRound>(OnEndRound);

            EventBus<TriggerEndRound>.Register(_endRoundBinding);

            _healthChangedBinding = new EventBinding<TriggerHealthChanged>(OnHealthChanged);

            EventBus<TriggerHealthChanged>.Register(_healthChangedBinding);

            if (_gameMode == null) return;
            
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

            if (_gameMode == null) return;
            
            _gameMode.OnRoundGameplayStarted -= OnRoundGameplayStarted;
            _gameMode.OnGameEnded -= OnGameEnded;
        }

        private void OnRoundGameplayStarted(RoundInfo roundInfo) => _playersWhoFiredThisRound.Clear();

        private void OnShootBombshell(TriggerShootBombshell evt)
        {
            if (evt.Character == null || evt.Character.Owner == null)
                return;

            _playersWhoFiredThisRound.Add(evt.Character.Owner);
        }

        private void OnEndRound(TriggerEndRound evt) => CheckNoShootingAchievement(evt.WinningTeam);
        
        private void OnGameEnded(int winnerPlayerIndex) => CheckNoDamageReceivedAchievement(winnerPlayerIndex);
        
        private void OnHealthChanged(TriggerHealthChanged evt)
        {
            if (evt.Character == null)
                return;

            if (evt.Delta >= 0f)
                return;

            PlayerManager player = evt.Character.Owner;

            if (player == null)
                return;

            _playersWhoTookDamageThisGame.Add(player);
        }

        private void CheckNoShootingAchievement(PlayerTeam winningTeam)
        {
            if (winningTeam?.Members == null || winningTeam.Members.Count == 0)
                return;

            PlayerManager winner = winningTeam.Members[0];

            if (winner == null)
                return;

            if (_playersWhoFiredThisRound.Contains(winner))
                return;

            SteamManager.UnlockAchievement(NO_SHOOTING_ACHIEVEMENT_ID);
        }
        
        private void CheckNoDamageReceivedAchievement(int winnerPlayerIndex)
        {
            LobbyService lobbyService = ServiceManager.Instance?.Get<LobbyService>();

            if (lobbyService == null)
                return;

            if (lobbyService.CurrentPlayerCount != 4)
                return;

            PlayerManager winner = lobbyService.GetPlayerByIndex(winnerPlayerIndex);

            if (winner == null)
                return;

            if (_playersWhoTookDamageThisGame.Contains(winner))
                return;

            SteamManager.UnlockAchievement(NO_DAMAGE_RECEIVED_ACHIEVEMENT_ID);
        }

        public void Dispose()
        {
            DeregisterEvents();

            _playersWhoFiredThisRound.Clear();
            _playersWhoTookDamageThisGame.Clear();

            _gameMode = null;

            Logs.Log("[AchievementSystem] Disposed.");
        }
    }
}