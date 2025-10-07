using System;
using UnityEngine;

namespace MortierFu.Shared {
    /// <summary>
    /// Credits: <see href="https://www.youtube.com/@git-amend">git-amend</see>
    /// </summary>
    public abstract class Timer : IDisposable {
        public float CurrentTime { get; protected set; }
        public bool IsRunning { get; private set; }

        protected float _initialTime;

        public virtual float Progress => Mathf.Clamp(CurrentTime / _initialTime, 0f, 1f);

        public Action OnTimerStart = delegate { };
        public Action OnTimerStop = delegate { };

        public Timer(float value) {
            _initialTime = value;
        }

        public void Start() {
            CurrentTime = _initialTime;
            if(!IsRunning) {
                IsRunning = true;
                TimerManager.RegisterTimer(this);
                OnTimerStart.Invoke();
            }
        }

        public void Stop() {
            if(IsRunning) {
                IsRunning = false;
                TimerManager.DeregisterTimer(this);
                OnTimerStop.Invoke();
            }
        }

        public abstract void Tick();
        public abstract bool IsFinished { get; }

        public void Resume() => IsRunning = true;
        public void Pause() => IsRunning = false;

        public virtual void Reset() => CurrentTime = _initialTime;

        public virtual void Reset(float newTime) {
            _initialTime = newTime;
            Reset();
        }

        bool _disposed;

        ~Timer() {
            Dispose(false);
        }

        // Call Dispose to ensure deregistration of the timer from the TimerManager
        // when the consumer is done with the timer or being destroyed
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (_disposed) return;

            if (disposing) {
                TimerManager.DeregisterTimer(this);
            }

            _disposed = true;
        }
    }
}