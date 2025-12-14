namespace MortierFu
{
    public struct RoundInfo
    {
        public int RoundIndex;
        public PlayerTeam WinningTeam;
        
        public RoundInfo(int roundIndex, PlayerTeam winningTeam)
        {
            RoundIndex = roundIndex;
            WinningTeam = winningTeam;
        }
    }
}