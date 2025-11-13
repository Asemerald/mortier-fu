using System.Collections.Generic;

namespace MortierFu {
    /// <summary>
    /// Credits: <see href="https://www.youtube.com/@git-amend">git-amend</see>
    /// </summary>
    public static class TimerManager {
        static readonly List<Timer> _timers = new();

        public static void RegisterTimer(Timer timer) => _timers.Add(timer);
        public static void DeregisterTimer(Timer timer) => _timers.Remove(timer);

        public static void UpdateTimers() {
            foreach(var timer in new List<Timer>(_timers)) {
                timer.Tick();
            }
        }

        public static void Clear() => _timers.Clear();
    }
}