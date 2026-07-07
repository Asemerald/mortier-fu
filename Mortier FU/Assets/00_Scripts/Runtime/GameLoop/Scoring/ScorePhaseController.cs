using System;
using System.Collections.Generic;
using MortierFu.Shared;

namespace MortierFu
{
    public sealed class ScorePhaseController
    {
        private readonly Action _stopCountdown;
        private readonly Action _onScoreDisplayOver;

        public ScorePhaseController(Action stopCountdown, Action onScoreDisplayOver)
        {
            _stopCountdown = stopCountdown;
            _onScoreDisplayOver = onScoreDisplayOver;
        }

        public void HideScores()
        {
            _stopCountdown?.Invoke();

            _onScoreDisplayOver?.Invoke();
        }
    }
}