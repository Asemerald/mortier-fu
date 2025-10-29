using UnityEngine;

namespace MortierFu {
    /// <summary>
    /// Timer that counts down from a specific value to zero.
    /// | Credits: <see href="https://www.youtube.com/@git-amend">git-amend</see>
    /// </summary>
    public class CountdownTimer : Timer {
        public CountdownTimer(float value) : base(value) { }

        public override void Tick() {
            if(IsRunning && CurrentTime > 0) {
                CurrentTime -= Time.deltaTime;
            }

            if(IsRunning && CurrentTime <= 0) {
                Stop();
            } 
        }

        public override bool IsFinished => CurrentTime <= 0f;
    }
}