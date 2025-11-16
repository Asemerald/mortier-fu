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

        /// <summary>
        /// Updates the timer duration while preserving the elapsed time.
        /// This allows the timer to smoothly adapt to a new total duration
        /// (e.g., when an attack cooldown changes mid-way) without resetting
        /// or losing progress. If the elapsed time exceeds the new duration,
        /// the timer completes immediately.
        /// </summary>
        public virtual void DynamicUpdate(float newTime) {
            if (!IsRunning) {
                Reset(newTime);
                return;
            }

            float elapsed = InitialTime - CurrentTime;
            InitialTime = newTime;
            CurrentTime = newTime - elapsed;

            if (CurrentTime <= 0) {
                Stop();
            }
        }

        public override bool IsFinished => CurrentTime <= 0f;
    }
}