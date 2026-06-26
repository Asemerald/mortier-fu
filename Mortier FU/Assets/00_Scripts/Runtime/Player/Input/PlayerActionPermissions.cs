namespace MortierFu
{
    public readonly struct PlayerActionPermissions
    {
        public readonly bool CanMove;
        public readonly bool CanAim;
        public readonly bool CanShoot;
        public readonly bool CanDash;
        public readonly bool CanTaunt;
        public readonly bool CanInteract;

        public readonly bool CanPause;
        public readonly bool CanNavigateUI;
        public readonly bool CanConfirmUI;
        public readonly bool CanCancelUI;

        public PlayerActionPermissions(
            bool canMove,
            bool canAim,
            bool canShoot,
            bool canDash,
            bool canTaunt,
            bool canInteract,
            bool canPause,
            bool canNavigateUI,
            bool canConfirmUI,
            bool canCancelUI)
        {
            CanMove = canMove;
            CanAim = canAim;
            CanShoot = canShoot;
            CanDash = canDash;
            CanTaunt = canTaunt;
            CanInteract = canInteract;

            CanPause = canPause;
            CanNavigateUI = canNavigateUI;
            CanConfirmUI = canConfirmUI;
            CanCancelUI = canCancelUI;
        }

        public static PlayerActionPermissions FromContext(PlayerControlContext context)
        {
            return context switch
            {
                PlayerControlContext.Lobby => new PlayerActionPermissions(
                    canMove: false,
                    canAim: false,
                    canShoot: false,
                    canDash: false,
                    canTaunt: false,
                    canInteract: false,
                    canPause: false,
                    canNavigateUI: true,
                    canConfirmUI: true,
                    canCancelUI: true
                ),

                PlayerControlContext.Menu => new PlayerActionPermissions(
                    canMove: false,
                    canAim: false,
                    canShoot: false,
                    canDash: false,
                    canTaunt: false,
                    canInteract: false,
                    canPause: false,
                    canNavigateUI: true,
                    canConfirmUI: true,
                    canCancelUI: true
                ),

                PlayerControlContext.PauseMenu => new PlayerActionPermissions(
                    canMove: false,
                    canAim: false,
                    canShoot: false,
                    canDash: false,
                    canTaunt: false,
                    canInteract: false,
                    canPause: true,
                    canNavigateUI: true,
                    canConfirmUI: true,
                    canCancelUI: true
                ),

                PlayerControlContext.LobbySandbox => new PlayerActionPermissions(
                    canMove: true,
                    canAim: true,
                    canShoot: true,
                    canDash: true,
                    canTaunt: true,
                    canInteract: true,
                    canPause: false,
                    canNavigateUI: false,
                    canConfirmUI: false,
                    canCancelUI: false
                ),

                PlayerControlContext.LobbyCustomization => new PlayerActionPermissions(
                    canMove: false,
                    canAim: false,
                    canShoot: false,
                    canDash: false,
                    canTaunt: false,
                    canInteract: false,
                    canPause: false,
                    canNavigateUI: true,
                    canConfirmUI: true,
                    canCancelUI: true
                ),

                PlayerControlContext.LobbySettingsOwner => new PlayerActionPermissions(
                    canMove: false,
                    canAim: false,
                    canShoot: false,
                    canDash: false,
                    canTaunt: false,
                    canInteract: false,
                    canPause: false,
                    canNavigateUI: true,
                    canConfirmUI: true,
                    canCancelUI: true
                ),

                PlayerControlContext.LobbyLocked => new PlayerActionPermissions(
                    canMove: false,
                    canAim: false,
                    canShoot: false,
                    canDash: false,
                    canTaunt: false,
                    canInteract: false,
                    canPause: false,
                    canNavigateUI: false,
                    canConfirmUI: false,
                    canCancelUI: false
                ),

                PlayerControlContext.LobbyReturnConfirmationOwner => new PlayerActionPermissions(
                    canMove: false,
                    canAim: false,
                    canShoot: false,
                    canDash: false,
                    canTaunt: false,
                    canInteract: false,
                    canPause: false,
                    canNavigateUI: true,
                    canConfirmUI: true,
                    canCancelUI: true
                ),

                PlayerControlContext.AugmentShowcase => new PlayerActionPermissions(
                    canMove: false,
                    canAim: false,
                    canShoot: false,
                    canDash: false,
                    canTaunt: false,
                    canInteract: false,
                    canPause: true,
                    canNavigateUI: false,
                    canConfirmUI: true,
                    canCancelUI: true
                ),

                PlayerControlContext.AugmentRace => new PlayerActionPermissions(
                    canMove: true,
                    canAim: false,
                    canShoot: false,
                    canDash: true,
                    canTaunt: true,
                    canInteract: false,
                    canPause: true,
                    canNavigateUI: false,
                    canConfirmUI: false,
                    canCancelUI: true
                ),

                PlayerControlContext.RoundCountdown => new PlayerActionPermissions(
                    canMove: false,
                    canAim: false,
                    canShoot: false,
                    canDash: false,
                    canTaunt: true,
                    canInteract: false,
                    canPause: true,
                    canNavigateUI: false,
                    canConfirmUI: false,
                    canCancelUI: true
                ),

                PlayerControlContext.RoundGameplay => new PlayerActionPermissions(
                    canMove: true,
                    canAim: true,
                    canShoot: true,
                    canDash: true,
                    canTaunt: true,
                    canInteract: false,
                    canPause: true,
                    canNavigateUI: false,
                    canConfirmUI: false,
                    canCancelUI: true
                ),

                PlayerControlContext.RoundEnded => new PlayerActionPermissions(
                    canMove: false,
                    canAim: false,
                    canShoot: false,
                    canDash: false,
                    canTaunt: true,
                    canInteract: false,
                    canPause: true,
                    canNavigateUI: false,
                    canConfirmUI: false,
                    canCancelUI: true
                ),

                PlayerControlContext.Scoreboard => new PlayerActionPermissions(
                    canMove: false,
                    canAim: false,
                    canShoot: false,
                    canDash: false,
                    canTaunt: false,
                    canInteract: false,
                    canPause: false,
                    canNavigateUI: true,
                    canConfirmUI: true,
                    canCancelUI: true
                ),

                PlayerControlContext.EndGame => new PlayerActionPermissions(
                    canMove: false,
                    canAim: false,
                    canShoot: false,
                    canDash: false,
                    canTaunt: false,
                    canInteract: false,
                    canPause: false,
                    canNavigateUI: true,
                    canConfirmUI: true,
                    canCancelUI: true
                ),

                _ => new PlayerActionPermissions(
                    canMove: false,
                    canAim: false,
                    canShoot: false,
                    canDash: false,
                    canTaunt: false,
                    canInteract: false,
                    canPause: false,
                    canNavigateUI: false,
                    canConfirmUI: false,
                    canCancelUI: false
                )
            };
        }
    }
}