using MortierFu.Shared;

namespace MortierFu
{
    public class StunState : BaseState
    {
        private CountdownTimer _stunTimer;
        
        public bool IsActive => _stunTimer.IsRunning;

        public StunState(PlayerCharacter character) : base(character)
        {
            _stunTimer = new CountdownTimer(0f);
        }
        
        public void ReceiveStun(float duration)
        {
            // On autorise actuellement le "refresh" du stun
            if(IsActive && _stunTimer.CurrentTime > duration)
                return;
            
            _stunTimer.Reset(duration);
            _stunTimer.Start();
        }
        
        public override void OnEnter()
        {
            character.Controller.ResetVelocity();
            
            if(debug)
                Logs.Log("Entering Stun State");
        }

        public override void OnExit()
        {
            if(debug) 
                Logs.Log("Exiting Stun State");
        }
    }
}