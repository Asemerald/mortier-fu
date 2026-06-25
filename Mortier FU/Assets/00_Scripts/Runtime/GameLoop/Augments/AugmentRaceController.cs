using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using System.Threading;

namespace MortierFu
{
    public sealed class AugmentRaceController
    {
        private readonly IReadOnlyList<PlayerTeam> _teams;
        private readonly AugmentSelectionSystem _augmentSelectionSystem;
        private readonly PlayerSpawnController _playerSpawnController;
        private readonly Action<PlayerControlContext> _setPlayerControlContext;
        private readonly Action _onRaceStart;

        public AugmentRaceController(
            IReadOnlyList<PlayerTeam> teams,
            AugmentSelectionSystem augmentSelectionSystem,
            PlayerSpawnController playerSpawnController,
            Action<PlayerControlContext> setPlayerControlContext,
            Action onRaceStart)
        {
            _teams = teams ?? throw new ArgumentNullException(nameof(teams));
            _augmentSelectionSystem = augmentSelectionSystem ?? throw new ArgumentNullException(nameof(augmentSelectionSystem));
            _playerSpawnController = playerSpawnController ?? throw new ArgumentNullException(nameof(playerSpawnController));
            _setPlayerControlContext = setPlayerControlContext ?? throw new ArgumentNullException(nameof(setPlayerControlContext));
            _onRaceStart = onRaceStart;
        }

        public void BeginRace(int roundIndex)
        {
            _playerSpawnController.ResetPlayers();
            _playerSpawnController.SpawnPlayers(roundIndex);
            _playerSpawnController.SetPlayerGravity(true);

            _setPlayerControlContext(PlayerControlContext.AugmentShowcase);

            _onRaceStart?.Invoke();

            Logs.Log("[AugmentRaceController] Starting augment race.");
        }

        public async UniTask HandleSelectionAsync(float duration)
        {
            var pickers = GetAugmentPickers();

            await _augmentSelectionSystem.HandleAugmentSelection(
                pickers,
                duration
            );
        }

        public async UniTask WaitUntilSelectionOverAsync()
        {
            while (!_augmentSelectionSystem.IsSelectionOver)
            {
                await UniTask.Yield();
            }
        }

        public void EndSelection()
        {
            _augmentSelectionSystem.EndRace();
        }

        public void EndRace()
        {
            _setPlayerControlContext(PlayerControlContext.RoundEnded);

            _playerSpawnController.ResetPlayers();

            EventBus<TriggerEndRound>.Raise(new TriggerEndRound());

            Logs.Log("[AugmentRaceController] Ending augment race.");
        }

        private List<PlayerManager> GetAugmentPickers()
        {
            var pickers = new List<PlayerManager>();

            foreach (var team in _teams)
            {
                if (team.Rank == 1)
                    continue;

                pickers.AddRange(team.Members);
            }

            if (pickers.Count == 0)
            {
                Logs.LogWarning("[AugmentRaceController] Found no pickers for this augment selection phase.");
            }

            return pickers;
        }
        
        public async UniTask HandleSelectionAsync(float duration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pickers = GetAugmentPickers();

            await _augmentSelectionSystem.HandleAugmentSelection(
                pickers,
                duration
            );

            cancellationToken.ThrowIfCancellationRequested();
        }

        public async UniTask WaitUntilSelectionOverAsync(CancellationToken cancellationToken)
        {
            while (!_augmentSelectionSystem.IsSelectionOver)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await UniTask.Yield();

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}