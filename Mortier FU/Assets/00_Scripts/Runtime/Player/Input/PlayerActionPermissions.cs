namespace MortierFu
{
    public readonly struct PlayerActionPermissions
    {
        public readonly bool CanMove;
        public readonly bool CanAim;
        public readonly bool CanShoot;
        public readonly bool CanDash;
        public readonly bool CanTaunt;

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
                    canPause: true,
                    canNavigateUI: false,
                    canConfirmUI: false,
                    canCancelUI: true
                ),

                PlayerControlContext.AugmentRace => new PlayerActionPermissions(
                    canMove: true,
                    canAim: false,
                    canShoot: false,
                    canDash: true,
                    canTaunt: true,
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
                    canPause: false,
                    canNavigateUI: false,
                    canConfirmUI: false,
                    canCancelUI: false
                )
            };
        }
    }
}