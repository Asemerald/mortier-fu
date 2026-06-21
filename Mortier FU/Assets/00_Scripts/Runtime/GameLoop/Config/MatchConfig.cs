namespace MortierFu
{
    public readonly struct MatchConfig
    {
        public int ScoreToWin { get; }

        public MatchConfig(int scoreToWin)
        {
            ScoreToWin = scoreToWin;
        }

        public static MatchConfig Default => new MatchConfig(scoreToWin: 1000);
    }
}