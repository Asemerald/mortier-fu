using System;

namespace MortierFu
{
    public sealed class RoundWinnerPresentationController
    {
        public RoundWinnerPresentationController() {}

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

            if (winner.Character == null)
                return;

            winner.Character.WinRoundDance();
        }
    }
}