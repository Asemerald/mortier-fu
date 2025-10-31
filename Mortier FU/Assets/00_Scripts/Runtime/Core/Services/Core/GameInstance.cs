using System.Threading.Tasks;

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

        public Task OnInitialize()
        {
            return Task.CompletedTask;
        }

        public bool IsInitialized { get; set; }
    }
    
    public enum GameState
    {
        Lobby,
        StartGame,
        Round,
        EndRound,
        DisplayScores,
        ShowcaseAugments,
        AugmentSelection,
        EndAugmentSelection,
        EndGame,
    }
}