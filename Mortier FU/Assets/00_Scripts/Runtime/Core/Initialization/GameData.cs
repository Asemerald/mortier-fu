using System.Collections.Generic;

namespace MortierFu
{
    [System.Serializable]
    public class GameData
    {
        public HashSet<string> visitedMaps = new HashSet<string>();

        public HashSet<string> pickedAugments = new HashSet<string>();

        public static GameData CreateDefault() => new GameData();
    }
}