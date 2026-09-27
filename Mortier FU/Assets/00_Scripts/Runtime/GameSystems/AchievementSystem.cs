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
        private const string VAMPIRE_FULL_HEALTH_ACHIEVEMENT_ID = "VAMPIRE_FULL_HEALTH";
        private const string GHOST_PLACE_OBJECT_ACHIEVEMENT_ID = "GHOST_PLACE_OBJECT";
        private const string KILL_ALL_IN_POOP_ACHIEVEMENT_ID = "KILL_ALL_IN_POOP";
        private const string BREAK_ALL_PLATFORMS_ACHIEVEMENT_ID = "BREAK_ALL_PLATFORMS";
        private const string EQUIP_ALL_COSMETICS_ACHIEVEMENT_ID = "EQUIP_ALL_COSMETICS";
        private const string CLUTCH_WIN_ACHIEVEMENT_ID = "CLUTCH_WIN";
        private const string WATER_HOLE_ACHIEVEMENT_ID = "WATER_HOLE";
        
        private const string ISLANDS_ON_STILTS_DAY_MAP_KEY = "Map_06_Day";
        private const string ISLANDS_ON_STILTS_NIGHT_MAP_KEY = "Map_06_Night";
        private const string WATER_HOLE_DAY_MAP_KEY = "Map_04_Day";
        private const string WATER_HOLE_NIGHT_MAP_KEY = "Map_04_Night";
        
        private const int FIFTY_TAUNTS_REQUIRED_COUNT = 50;
        private const int THIRTY_KILLS_REQUIRED_COUNT = 30;
        private const int DIFFERENT_DEATHS_REQUIRED_COUNT = 3;
        private const int GHOST_PLACE_OBJECT_REQUIRED_COUNT = 3;
        private const int COSMETIC_SKIN_COUNT = 16;
        private const int COSMETIC_FACE_COLUMN_COUNT = 4;
        private const int COSMETIC_FACE_ROW_COUNT = 4;
        private const int COSMETIC_FACE_COUNT = COSMETIC_FACE_COLUMN_COUNT * COSMETIC_FACE_ROW_COUNT;
        
        private const float TAUNT_THEN_KILL_WINDOW = 5f;
        private const float VAMPIRE_LOW_HEALTH_THRESHOLD = 0.25f;
        
        private readonly HashSet<PlayerManager> _playersWhoFiredThisRound = new();
        private readonly HashSet<PlayerManager> _playersWhoTookDamageThisGame = new();
        private readonly HashSet<PlayerManager> _eliminatedPlayersThisRound = new();
        private readonly HashSet<PlayerManager> _vampireLowHealthPlayers = new();
        private readonly HashSet<PlayerManager> _clutchWinCandidates = new();
        
        private readonly HashSet<DifferentDeathType> _deathTypesThisRound = new();
        
        private readonly HashSet<BreakablePlateform> _activeBreakablePlatforms = new();
        private readonly HashSet<BreakablePlateform> _destroyedPlatformsThisRound = new();
        
        private readonly Dictionary<PlayerManager, float> _lastTauntTimes = new();
      
        private readonly Dictionary<PlayerManager, int> _tauntsThisGame = new();
        private readonly Dictionary<PlayerManager, int> _killsThisGame = new();
        private readonly Dictionary<PlayerManager, int> _ghostPlacedObjectsThisRound = new();
    
        private readonly Dictionary<PlayerManager, PlayerManager> _activeRevengeTargets = new();
        private readonly Dictionary<PlayerManager, PlayerManager> _pendingRevengeTargets = new();
        
        private EventBinding<TriggerShootBombshell> _shootBombshellBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        private EventBinding<TriggerHealthChanged> _healthChangedBinding;
        private EventBinding<TriggerTaunt> _tauntBinding;
        private EventBinding<EventPlayerDeath> _playerDeathBinding;
        private EventBinding<TriggerGhostPropPlaced> _ghostPropPlacedBinding;
        
        private EventBinding<TriggerBreakablePlatformDestroyed> _breakablePlatformDestroyedBinding;
        private EventBinding<TriggerBreakablePlatformRegistered> _breakablePlatformRegisteredBinding;
        private EventBinding<TriggerBreakablePlatformUnregistered> _breakablePlatformUnregisteredBinding;
        
        private int _trackedDeathsThisRound;
        
        private GameModeBase _gameMode;
        private LobbyService _lobbyService;
        private LevelSystem _levelSystem;

        public bool IsInitialized { get; set; }

        public UniTask OnInitialize()
        {
            _gameMode = GameService.CurrentGameMode as GameModeBase;
            _lobbyService = ServiceManager.Instance?.Get<LobbyService>();
            _levelSystem = SystemManager.Instance.Get<LevelSystem>();
            
            RegisterEvents();
            RegisterAlreadyActiveBreakablePlatforms();
            
            Logs.Log("[AchievementSystem] Initialized.");

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            DeregisterEvents();

            ClearRuntimeData();

            _gameMode = null;
            _lobbyService = null;
            _levelSystem = null;
            
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
            
            _ghostPropPlacedBinding = new EventBinding<TriggerGhostPropPlaced>(OnGhostPropPlaced);
            EventBus<TriggerGhostPropPlaced>.Register(_ghostPropPlacedBinding);
            
            _breakablePlatformRegisteredBinding = new EventBinding<TriggerBreakablePlatformRegistered>(OnBreakablePlatformRegistered);
            EventBus<TriggerBreakablePlatformRegistered>.Register(_breakablePlatformRegisteredBinding);

            _breakablePlatformUnregisteredBinding = new EventBinding<TriggerBreakablePlatformUnregistered>(OnBreakablePlatformUnregistered);
            EventBus<TriggerBreakablePlatformUnregistered>.Register(_breakablePlatformUnregisteredBinding);

            _breakablePlatformDestroyedBinding = new EventBinding<TriggerBreakablePlatformDestroyed>(OnBreakablePlatformDestroyed);
            EventBus<TriggerBreakablePlatformDestroyed>.Register(_breakablePlatformDestroyedBinding);
            
            if (_gameMode == null)
                return;

            _gameMode.OnGameStarted += OnGameStarted;
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
            
            if (_ghostPropPlacedBinding != null)
            {
                EventBus<TriggerGhostPropPlaced>.Deregister(_ghostPropPlacedBinding);
                _ghostPropPlacedBinding = null;
            }

            if (_breakablePlatformRegisteredBinding != null)
            {
                EventBus<TriggerBreakablePlatformRegistered>.Deregister(_breakablePlatformRegisteredBinding);
                _breakablePlatformRegisteredBinding = null;
            }

            if (_breakablePlatformUnregisteredBinding != null)
            {
                EventBus<TriggerBreakablePlatformUnregistered>.Deregister(_breakablePlatformUnregisteredBinding);
                _breakablePlatformUnregisteredBinding = null;
            }

            if (_breakablePlatformDestroyedBinding != null)
            {
                EventBus<TriggerBreakablePlatformDestroyed>.Deregister(_breakablePlatformDestroyedBinding);
                _breakablePlatformDestroyedBinding = null;
            }
            
            if (_gameMode == null)
                return;

            _gameMode.OnGameStarted -= OnGameStarted;
            _gameMode.OnRoundGameplayStarted -= OnRoundGameplayStarted;
            _gameMode.OnGameEnded -= OnGameEnded;
        }

        private void OnRoundGameplayStarted(RoundInfo roundInfo)
        {
            _playersWhoFiredThisRound.Clear();
            _lastTauntTimes.Clear();
            _deathTypesThisRound.Clear();
            _eliminatedPlayersThisRound.Clear();
            _vampireLowHealthPlayers.Clear();
            _ghostPlacedObjectsThisRound.Clear();
            _destroyedPlatformsThisRound.Clear();
            
            _trackedDeathsThisRound = 0;
            
            CheckClutchWinCandidates();
            
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
            PlayerManager player = evt.Character?.Owner;
            if (player == null)
                return;

            if (evt.Delta < 0f)
                _playersWhoTookDamageThisGame.Add(player);

            CheckVampireFullHealthAchievement(evt, player);
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
            CheckWaterHoleAchievement(evt);
            
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
        
        private void OnBreakablePlatformDestroyed(TriggerBreakablePlatformDestroyed evt)
        {
            if (!IsIslandsOnStilts())
                return;

            if (_activeBreakablePlatforms.Count == 0)
                return;

            if (!evt.Platform || !_activeBreakablePlatforms.Contains(evt.Platform))
                return;

            if (!_destroyedPlatformsThisRound.Add(evt.Platform))
                return;

            if (_destroyedPlatformsThisRound.Count != _activeBreakablePlatforms.Count)
                return;

            SteamManager.UnlockAchievement(BREAK_ALL_PLATFORMS_ACHIEVEMENT_ID);
        }
        
        private void OnGhostPropPlaced(TriggerGhostPropPlaced evt)
        {
            PlayerManager player = evt.Player;
            if (player == null)
                return;

            _ghostPlacedObjectsThisRound.TryGetValue(player, out int count);
            count++;

            _ghostPlacedObjectsThisRound[player] = count;

            if (count != GHOST_PLACE_OBJECT_REQUIRED_COUNT)
                return;

            SteamManager.UnlockAchievement(GHOST_PLACE_OBJECT_ACHIEVEMENT_ID);
        }

        private void OnEndRound(TriggerEndRound evt)
        {
            if (!TryGetRoundWinner(evt.WinningTeam, out PlayerManager winner))
                return;

            CheckNoShootingAchievement(winner);
            CheckDifferentDeathsAchievement();
            CheckWinInPoopAchievement(winner);
        }
        
        private void OnGameStarted()
        {
            RegisterCosmeticsUsedInGame();
        }

        private void OnGameEnded(int winnerPlayerIndex)
        {
            PlayerManager winner = _gameMode.GetWinnerPlayer();
            if (winner == null)
                return;

            SteamManager.AddProgressToStat("GAME_PLAYED");
            
            CheckNoDamageReceivedAchievement(winner);
            CheckThirtyKillsAchievement(winner);
            CheckClutchWinAchievement(winner);
        }
        
        private void OnBreakablePlatformRegistered(TriggerBreakablePlatformRegistered evt)
        {
            if (!evt.Platform)
                return;

            _activeBreakablePlatforms.Add(evt.Platform);
        }
        
        private void OnBreakablePlatformUnregistered(TriggerBreakablePlatformUnregistered evt)
        {
            if (!evt.Platform)
                return;

            _activeBreakablePlatforms.Remove(evt.Platform);
            _destroyedPlatformsThisRound.Remove(evt.Platform);
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
        
        private bool IsIslandsOnStilts() => _levelSystem?.CurrentLoadedMapKey is ISLANDS_ON_STILTS_DAY_MAP_KEY or ISLANDS_ON_STILTS_NIGHT_MAP_KEY;
        private bool IsWaterHoleMap() => _levelSystem?.CurrentLoadedMapKey is WATER_HOLE_DAY_MAP_KEY or WATER_HOLE_NIGHT_MAP_KEY;

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
        
        private void CheckWinInPoopAchievement(PlayerManager winner)
        {
            if (winner?.Character?.Properties == null)
                return;

            if (!winner.Character.Properties.Has(EntityProperties.mud))
                return;

            SteamManager.UnlockAchievement(KILL_ALL_IN_POOP_ACHIEVEMENT_ID);
        }

        private void CheckNoDamageReceivedAchievement(PlayerManager winner)
        {
            if (_lobbyService is not { CurrentPlayerCount: 4 })
                return;

            if (_playersWhoTookDamageThisGame.Contains(winner))
                return;

            SteamManager.UnlockAchievement(NO_DAMAGE_RECEIVED_ACHIEVEMENT_ID);
        }
        
        private void CheckClutchWinAchievement(PlayerManager winner)
        {
            if (!_clutchWinCandidates.Contains(winner))
                return;

            SteamManager.UnlockAchievement(CLUTCH_WIN_ACHIEVEMENT_ID);
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
            if (GetAugment<AGM_Ascension>(killer) is not { IsActive: true })
                return;

            SteamManager.UnlockAchievement(ASCENSION_KILL_ACHIEVEMENT_ID);
        }
        
        private void CheckVampireFullHealthAchievement(TriggerHealthChanged evt, PlayerManager player)
        {
            if (GetAugment<AGM_Vampire>(player) == null || evt.MaxHealth <= 0f)
                return;

            if (evt.NewHealth / evt.MaxHealth < VAMPIRE_LOW_HEALTH_THRESHOLD)
            {
                _vampireLowHealthPlayers.Add(player);
                return;
            }

            if (!_vampireLowHealthPlayers.Contains(player) || !Mathf.Approximately(evt.NewHealth, evt.MaxHealth))
                return;

            SteamManager.UnlockAchievement(VAMPIRE_FULL_HEALTH_ACHIEVEMENT_ID);
            _vampireLowHealthPlayers.Remove(player);
        }
        
        private void CheckClutchWinCandidates()
        {
            if (_lobbyService is not { CurrentPlayerCount: 4 })
                return;

            if (_gameMode?.Teams == null || _gameMode.Teams.Count != 4)
                return;

            int lowestScore = int.MaxValue;

            foreach (PlayerTeam team in _gameMode.Teams)
            {
                if (team.Score < lowestScore)
                    lowestScore = team.Score;
            }

            foreach (PlayerTeam candidateTeam in _gameMode.Teams)
            {
                if (candidateTeam.Score != lowestScore)
                    continue;

                bool opponentAtMatchPoint = false;

                foreach (PlayerTeam otherTeam in _gameMode.Teams)
                {
                    if (otherTeam == candidateTeam)
                        continue;

                    if (otherTeam.Score < _gameMode.ScoreToWin)
                        continue;

                    opponentAtMatchPoint = true;
                    break;
                }

                if (!opponentAtMatchPoint)
                    continue;

                foreach (PlayerManager player in candidateTeam.Members)
                {
                    if (player)
                        _clutchWinCandidates.Add(player);
                }
            }
        }
        
        private static void CheckEquipAllCosmeticsAchievement(GameData gameData)
        {
            if (gameData.usedCosmeticSkins.Count < COSMETIC_SKIN_COUNT)
                return;

            if (gameData.usedCosmeticFaces.Count < COSMETIC_FACE_COUNT)
                return;

            SteamManager.UnlockAchievement(EQUIP_ALL_COSMETICS_ACHIEVEMENT_ID);
        }

        private void CheckThirtyKillsAchievement(PlayerManager winner)
        {
            if (!_killsThisGame.TryGetValue(winner, out int killCount) || killCount < THIRTY_KILLS_REQUIRED_COUNT)
                return;

            SteamManager.UnlockAchievement(THIRTY_KILLS_ACHIEVEMENT_ID);
        }
        
        private void CheckWaterHoleAchievement(EventPlayerDeath evt)
        {
            if (!IsWaterHoleMap())
                return;

            if (evt.Context.DeathCause != E_DeathCause.Fall)
                return;

            if (evt.Context.Killer == null)
                return;

            SteamManager.UnlockAchievement(WATER_HOLE_ACHIEVEMENT_ID);
        }
        
        private void RegisterCosmeticsUsedInGame()
        {
            if (_gameMode?.Teams == null)
                return;

            SaveService saveService = ServiceManager.Instance.Get<SaveService>();
            if (saveService?.Game == null)
                return;

            bool progressionChanged = false;

            foreach (PlayerTeam team in _gameMode.Teams)
            {
                if (team?.Members == null)
                    continue;

                foreach (PlayerManager player in team.Members)
                {
                    if (!player)
                        continue;

                    if (RegisterPlayerCosmetics(saveService.Game, player))
                        progressionChanged = true;
                }
            }

            CheckEquipAllCosmeticsAchievement(saveService.Game);

            if (progressionChanged)
                saveService.SaveGame().Forget();
        }
        
        private static bool RegisterPlayerCosmetics(GameData gameData, PlayerManager player)
        {
            if (player?.Customization == null)
                return false;

            bool changed = false;

            int skinIndex = player.Customization.SkinIndex;

            if (!gameData.usedCosmeticSkins.Contains(skinIndex))
            {
                gameData.usedCosmeticSkins.Add(skinIndex);
                changed = true;
            }

            int faceIndex = GetFaceLinearIndex(player.Customization);

            if (gameData.usedCosmeticFaces.Contains(faceIndex)) return changed;
            
            gameData.usedCosmeticFaces.Add(faceIndex);
            changed = true;

            return changed;
        }
        
        private static int GetFaceLinearIndex(PlayerCustomizationData customization)
        {
            int column = Mathf.Clamp(customization.FaceColumn, 1, COSMETIC_FACE_COLUMN_COUNT) - 1;

            int row = Mathf.Clamp(customization.FaceRow, 1, COSMETIC_FACE_ROW_COUNT) - 1;

            return row * COSMETIC_FACE_COLUMN_COUNT + column;
        }
        
        private void RegisterEliminatedPlayer(EventPlayerDeath evt)
        {
            PlayerManager player = evt.Character?.Owner;
            if (player == null)
                return;

            _eliminatedPlayersThisRound.Add(player);
            _vampireLowHealthPlayers.Remove(player);
        }
        
        private void RegisterAlreadyActiveBreakablePlatforms()
        {
            foreach (BreakablePlateform platform in BreakablePlateform.ActivePlatforms)
                if (platform)
                    _activeBreakablePlatforms.Add(platform);
        }
        
        private static bool TryGetRoundWinner(PlayerTeam winningTeam, out PlayerManager winner)
        {
            winner = null;

            if (winningTeam?.Members == null || winningTeam.Members.Count == 0)
                return false;

            winner = winningTeam.Members[0];

            return winner != null;
        }
        
        private static T GetAugment<T>(PlayerManager player) where T : class, IAugment
        {
            if (player?.Character?.Augments == null)
                return null;

            foreach (IAugment augment in player.Character.Augments)
                if (augment is T typedAugment)
                    return typedAugment;

            return null;
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
            
            _ghostPlacedObjectsThisRound.Clear();
            
            _eliminatedPlayersThisRound.Clear();
            
            _activeBreakablePlatforms.Clear();
            _destroyedPlatformsThisRound.Clear();
            
            _clutchWinCandidates.Clear();
        }
    }
}