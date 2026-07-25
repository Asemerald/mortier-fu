using System.Collections.Generic;

namespace MortierFu
{
    public static class GhostTutorialSession
    {
        private static readonly HashSet<int> s_shownPlayerIndexes = new();

        public static bool TryMarkShown(int playerIndex) => playerIndex >= 0 && s_shownPlayerIndexes.Add(playerIndex);

        public static void Clear() => s_shownPlayerIndexes.Clear();
    }
}