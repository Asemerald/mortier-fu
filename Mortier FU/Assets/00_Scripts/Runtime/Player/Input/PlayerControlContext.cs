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

        RoundCountdown,
        RoundGameplay,
        RoundGhost,
        RoundEnded,
        
        Scoreboard,
        EndGame
    }
}