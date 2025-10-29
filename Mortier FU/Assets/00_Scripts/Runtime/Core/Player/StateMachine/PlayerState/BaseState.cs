namespace MortierFu
{
    public abstract class BaseState : IState
    {
        protected readonly PlayerController _playerController;

        protected bool _debug = false;
        
        protected BaseState(PlayerController playerController)
        {
            _playerController = playerController;
        }
        
        public virtual void OnEnter() {}

        public virtual void Update() {}

        public virtual void FixedUpdate() {}

        public virtual void OnExit() {}
    }    
}