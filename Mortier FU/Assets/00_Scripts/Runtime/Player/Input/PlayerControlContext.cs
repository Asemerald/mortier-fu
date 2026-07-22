namespace MortierFu
{
    public enum PlayerControlContext
    {
        Lobby,
        Menu,
        PauseMenu,

        LobbySandbox,
        LobbyCustomization,
        LobbySettingsOwner,
        LobbyLocked,
        LobbyReturnConfirmationOwner,

        AugmentShowcase,
        AugmentRace,
        AugmentRaceBullyClassic,
        AugmentRaceBullyMoveOnly,
        AugmentRaceBullyShootOnly,
        AugmentRaceBullyLocked,
        AugmentRaceSummary,
        
        RoundCountdown,
        RoundGameplay,
        RoundGhost,
        RoundEnded,
        
        Loading,
        
        Scoreboard,
        EndGame
    }
}