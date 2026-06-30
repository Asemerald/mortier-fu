using System;
using System.Collections.Generic;
using MortierFu.Shared;

namespace MortierFu
{
    public sealed class ScorePhaseController
    {
        private readonly IReadOnlyList<PlayerTeam> _teams;
        private readonly CameraSystem _cameraSystem;
        private readonly Action<GameState> _updateGameState;
        private readonly Action _stopCountdown;
        private readonly Action _onScoreDisplayOver;

        public ScorePhaseController(
            IReadOnlyList<PlayerTeam> teams,
            CameraSystem cameraSystem,
            Action<GameState> updateGameState,
            Action stopCountdown,
            Action onScoreDisplayOver)
        {
            _teams = teams ?? throw new ArgumentNullException(nameof(teams));
            _cameraSystem = cameraSystem ?? throw new ArgumentNullException(nameof(cameraSystem));
            _updateGameState = updateGameState ?? throw new ArgumentNullException(nameof(updateGameState));
            _stopCountdown = stopCountdown;
            _onScoreDisplayOver = onScoreDisplayOver;
        }

        public void DisplayScores()
        {
            _updateGameState.Invoke(GameState.DisplayScores);

            Logs.Log("[ScorePhaseController] Displaying scores...");

            foreach (var team in _teams)
            {
                Logs.Log($"[ScorePhaseController] Team {team.Index} Score: {team.Score}");
            }
        }

        public void HideScores()
        {
            _cameraSystem.Controller?.ResetToMainCamera();

            _stopCountdown?.Invoke();

            _onScoreDisplayOver?.Invoke();
        }
    }
}