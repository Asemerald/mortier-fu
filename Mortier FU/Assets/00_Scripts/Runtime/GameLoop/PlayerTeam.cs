using System.Collections.Generic;

namespace MortierFu 
{
    public class PlayerTeam
    {
        public int Score;
        public int Rank;
        
        public readonly int Index;
        public readonly List<PlayerManager> Members;
        
        public PlayerTeam(int index, List<PlayerManager> members)
        {
            Index = index;
            Members = members ??  new List<PlayerManager>();
            Score = 0;
            Rank = -1;
        }
        
        public PlayerTeam(int index, PlayerManager member) : this(index, new List<PlayerManager> { member }) { }
    }
}