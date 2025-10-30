namespace MortierFu
{
    public abstract class BaseState : IState
    {
        protected readonly PlayerCharacter character;

        protected bool debug = false;
        
        protected BaseState(PlayerCharacter character)
        {
            this.character = character;
        }
        
        public virtual void OnEnter() {}

        public virtual void Update() {}

        public virtual void FixedUpdate() {}

        public virtual void OnExit() {}
    }    
}