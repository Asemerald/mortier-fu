using System;
using MortierFu.Shared;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MortierFu
{
    public sealed class RoundStartController : IDisposable
    {
        private readonly CountdownTimer _timer;
        private readonly SO_GameFlowSettings _data;
        private readonly Action<RoundInfo> _onRoundStarted;

        private RoundInfo _currentRound;
        private bool _hasCurrentRound;
        private bool _isListeningToTimerEvents;

        public RoundStartController(
            CountdownTimer timer,
            SO_GameFlowSettings data,
            Action<RoundInfo> onRoundStarted)
        {
            _timer = timer ?? throw new ArgumentNullException(nameof(timer));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _onRoundStarted = onRoundStarted;
        }

        public void StartCountdown(RoundInfo roundInfo)
        {
            _currentRound = roundInfo;
            _hasCurrentRound = true;

            float duration = _data.RoundStartCountdown;

#if UNITY_EDITOR
            int speedMult = EditorPrefs.GetInt("CountdownSpeedMult", 1);
            duration *= 1f / speedMult;
#endif

            _timer.Reset(duration - 0.01f);

            BindTimerEvents();

            _timer.Start();
        }

        public void StopCountdown()
        {
            UnbindTimerEvents();
            _timer.Stop();

            _hasCurrentRound = false;
        }

        public void Dispose()
        {
            StopCountdown();
        }

        private void BindTimerEvents()
        {
            if (_isListeningToTimerEvents)
                return;

            _timer.OnTimerStart += HandleStartOfCountdown;
            _timer.OnTimerStop += HandleEndOfCountdown;

            _isListeningToTimerEvents = true;
        }

        private void UnbindTimerEvents()
        {
            if (!_isListeningToTimerEvents)
                return;

            _timer.OnTimerStart -= HandleStartOfCountdown;
            _timer.OnTimerStop -= HandleEndOfCountdown;

            _isListeningToTimerEvents = false;
        }

        private void HandleStartOfCountdown()
        {
            if (!_hasCurrentRound)
                return;

            _onRoundStarted?.Invoke(_currentRound);
        }

        private void HandleEndOfCountdown()
        {
            if (!_hasCurrentRound)
                return;

            UnbindTimerEvents();

            Logs.Log($"Round #{_currentRound.RoundIndex} is starting...");
        }
    }
}