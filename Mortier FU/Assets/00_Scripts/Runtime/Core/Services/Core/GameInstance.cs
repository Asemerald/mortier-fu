namespace MortierFu
{
    public class GameInstance : IGameService
    {
        public static GameState gameState;

        public static void SetGameState(GameState newGameState)
        {
            gameState = newGameState;
        }
        public void Dispose()
        {
            
        }
    }
    
    public enum GameState
    {
        Lobby,
        StartGame,
        StartRound,
        EndRound,
        StartBonusSelection,
        EndBonusSelection,
        EndGame
    }
}