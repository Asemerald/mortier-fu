using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;

namespace MortierFu
{
    public sealed class RaceRuntimeController
    {
        private RaceModeRuntimeBase _runtime;
        private RaceModeContext _context;

        public void PrepareRace(RaceModeContext context, SO_RaceModeDefinition fallbackDefinition)
        {
            EndRace();

            _context = context;

            SO_RaceModeDefinition definition = ResolveDefinition(context, fallbackDefinition);

            if (!definition)
            {
                Logs.LogWarning("[RaceRuntimeController] No race mode definition found. Falling back to legacy race behaviour.");
                return;
            }

            _runtime = definition.CreateRuntime();
            _runtime.Initialize(context, definition);

            context.PlayerSpawnController?.SetSpawnResolver(_runtime.ResolveSpawnPoint);

            Logs.Log($"[RaceRuntimeController] Prepared race mode: {definition.name}.");
        }

        public List<PlayerManager> GetAugmentPickers() => _runtime?.GetAugmentPickers();

        public RaceAugmentLayout BuildAugmentLayout(int augmentCount) => _runtime?.BuildAugmentLayout(augmentCount);

        public void BeginGameplay()
        {
            if (_runtime != null)
            {
                _runtime.BeginGameplay();
                return;
            }

            _context?.SetAllPlayersControlContext?.Invoke(PlayerControlContext.AugmentRace);
        }

        public void Tick(float deltaTime) => _runtime?.Tick(deltaTime);

        public float GetRaceDuration(float defaultDuration) => _runtime != null ? _runtime.GetRaceDuration(defaultDuration) : defaultDuration;

        public void EndRace()
        {
            _runtime?.End();
            _runtime?.Dispose();
            _runtime = null;

            _context?.PlayerSpawnController?.ClearSpawnResolver();
            _context = null;
        }

        public void Dispose() => EndRace();

        private static SO_RaceModeDefinition ResolveDefinition(RaceModeContext context, SO_RaceModeDefinition fallbackDefinition)
        {
            RaceReporter reporter = context?.LevelSystem?.CurrentRaceReporter;

            if (reporter && reporter.RaceModeDefinition)
                return reporter.RaceModeDefinition;

            return fallbackDefinition;
        }
        
        public UniTask AfterShowcaseCompleted(CancellationToken cancellationToken) => _runtime?.AfterShowcaseCompleted(cancellationToken) ?? UniTask.CompletedTask;
    }
}
