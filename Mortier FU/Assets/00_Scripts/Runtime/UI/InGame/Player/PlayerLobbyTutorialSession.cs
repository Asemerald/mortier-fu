using System.Collections.Generic;

namespace MortierFu
{
    public static class PlayerLobbyTutorialSession
    {
        private static readonly HashSet<int> _completedPlayers = new();

        public static bool HasCompleted(int playerIndex) => _completedPlayers.Contains(playerIndex);

        public static void MarkCompleted(int playerIndex)
        {
            if (playerIndex < 0)
                return;

            _completedPlayers.Add(playerIndex);
        }

        public static void Clear() => _completedPlayers.Clear();
    }
}