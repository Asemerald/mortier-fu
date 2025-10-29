    using MortierFu.Shared;

namespace MortierFu
{
    
    // Il faudra faire en sorte qu'on puisse passer dans cet état uniquement lorsque nous sommes dans la sélection des augments.
    // Donc on utilisera SetState et on ne pourra pas aller dans cet état autrement que par SetState.
    // Il faudra faire un At de AugmentSelectionState vers StrikeState et un StrikeState vers AugmentSelectionState.
    // Pour empêcher que j'aille de At StrikeState à autre chose, il faut que je fasse en sorte que lorsque nous sommes dans AugmentSelectionState,
    // je change un bool dans le PlayerController qui dit que je suis en AugmentSelectionState, et dans les conditions des transitions de StrikeState vers d'autres états,
    // je vérifie que ce bool est false.
    // Il faut que je réfléchisse à comment faire ça proprement, pour l'instant je n'ai que cette idée.
    
    public class AugmentSelectionState : BaseState
    {
        public AugmentSelectionState(PlayerController playerController) : base(playerController) {}
        
        public override void OnEnter()
        {
            if(_debug)
                Logs.Log("Entering AugmentSelectionState");
        }

        public override void Update()
        {
            _playerController.HandleMovementUpdate();
        }

        public override void FixedUpdate()
        {
            _playerController.HandleMovementFixedUpdate();
        }

        public override void OnExit()
        {
            if(_debug) 
                Logs.Log("Exiting AugmentSelectionState");
        }
    }
}