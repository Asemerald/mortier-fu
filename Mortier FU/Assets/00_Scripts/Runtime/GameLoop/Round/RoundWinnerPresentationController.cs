using System;

namespace MortierFu
{
    public sealed class RoundWinnerPresentationController
    {
        private readonly PlayerSpawnController _playerSpawnController;
        private readonly CameraSystem _cameraSystem;

        public RoundWinnerPresentationController(
            PlayerSpawnController playerSpawnController,
            CameraSystem cameraSystem)
        {
            _playerSpawnController = playerSpawnController ?? throw new ArgumentNullException(nameof(playerSpawnController));
            _cameraSystem = cameraSystem ?? throw new ArgumentNullException(nameof(cameraSystem));
        }

        public void PresentWinner(PlayerTeam winningTeam)
        {
            if (winningTeam == null)
                return;

            if (winningTeam.Members == null || winningTeam.Members.Count == 0)
                return;

            PlayerManager winner = winningTeam.Members[0];

            if (winner == null || winner.Character == null)
                return;

            winner.Character.Reset();

            _playerSpawnController.SpawnWinnerTeam(winningTeam);

            if (winner.Character == null)
                return;

            _cameraSystem.Controller.EndFightCameraMovement(
                winner.Character.transform,
                2f
            );

            winner.Character.WinRoundDance();
        }
    }
}