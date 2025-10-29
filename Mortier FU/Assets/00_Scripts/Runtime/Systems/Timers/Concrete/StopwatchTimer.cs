using UnityEngine;

namespace MortierFu
{
    public class StopwatchTimer : Timer
    {
        public StopwatchTimer(float initialTime) : base(initialTime) { }

        public override void Tick()
        {
            if (IsRunning) {
                CurrentTime += Time.deltaTime;
            }
        }
        
        public override bool IsFinished => false;
    }
}