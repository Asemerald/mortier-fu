using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public abstract class RaceModeRuntimeBase
    {
        protected RaceModeContext Context;
        protected SO_RaceModeDefinition Definition;
        protected RaceReporter Reporter;
        protected PlayerManager BullyPlayer;

        public virtual void Initialize(RaceModeContext context, SO_RaceModeDefinition definition)
        {
            Context = context;
            Definition = definition;
            Reporter = context.LevelSystem.CurrentRaceReporter;
            BullyPlayer = ResolveBullyPlayer();
        }

        public virtual Transform ResolveSpawnPoint(PlayerTeam team, PlayerManager player, int racerIndex, Transform fallback) => fallback;

        public virtual List<PlayerManager> GetAugmentPickers()
        {
            List<PlayerManager> pickers = new();

            if (Context.Teams == null)
                return pickers;

            for (int teamIndex = 0; teamIndex < Context.Teams.Count; teamIndex++)
            {
                PlayerTeam team = Context.Teams[teamIndex];

                if (team?.Members == null)
                    continue;

                for (int memberIndex = 0; memberIndex < team.Members.Count; memberIndex++)
                {
                    PlayerManager player = team.Members[memberIndex];

                    if (!player || IsBully(player))
                        continue;

                    pickers.Add(player);
                }
            }

            if (pickers.Count == 0)
                Logs.LogWarning($"[{GetType().Name}] Found no augment pickers.");

            return pickers;
        }

        public virtual RaceAugmentLayout BuildAugmentLayout(int augmentCount) => RaceAugmentLayout.FromLevelSystem(Context.LevelSystem, augmentCount);

        public virtual UniTask AfterShowcaseCompleted(CancellationToken cancellationToken) => UniTask.CompletedTask;
        
        public virtual void BeginGameplay()
        {
            ApplyBullyStatsOverrideIfNeeded();
            ApplyBullySizeIfNeeded();
            ApplyPlayerContexts();
        }

        public virtual void Tick(float deltaTime)
        { }

        public virtual float GetRaceDuration(float defaultDuration) => Definition.ResolveRaceDuration(defaultDuration);

        public virtual void End()
        {
            Context.ClearBullySize?.Invoke();
            RestoreBullyStatsOverrideIfNeeded();
        }

        public virtual void Dispose()
        { }

        protected bool IsBully(PlayerManager player) => BullyPlayer && ReferenceEquals(BullyPlayer, player);

        protected PlayerCharacter BullyCharacter => BullyPlayer ? BullyPlayer.Character : null;

        protected PlayerManager ResolveBullyPlayer()
        {
            if (!Definition.UsePreviousRoundWinnerAsBully)
                return null;

            PlayerTeam winnerTeam = Context.PreviousRoundWinnerTeam;

            return winnerTeam?.Members is not { Count: > 0 } ? null : winnerTeam.Members[0];
        }

        protected virtual void ApplyPlayerContexts()
        {
            if (Context.Teams == null)
                return;

            for (int teamIndex = 0; teamIndex < Context.Teams.Count; teamIndex++)
            {
                PlayerTeam team = Context.Teams[teamIndex];

                if (team?.Members == null)
                    continue;

                for (int memberIndex = 0; memberIndex < team.Members.Count; memberIndex++)
                {
                    PlayerManager player = team.Members[memberIndex];

                    if (!player)
                        continue;

                    PlayerControlContext context = IsBully(player) ? Definition.BullyContext : Definition.RacerContext;

                    player.SetControlContext(context);
                }
            }
        }

        protected void ApplyBullySizeIfNeeded()
        {
            PlayerCharacter bullyCharacter = BullyCharacter;

            if (!bullyCharacter)
                return;

            float targetSize = Definition.ResolveBullyTargetSize();
            Context.ApplyBullySize?.Invoke(bullyCharacter, targetSize);
        }
        
        protected virtual void ApplyBullyStatsOverrideIfNeeded()
        {
            PlayerCharacter bullyCharacter = BullyCharacter;

            if (!bullyCharacter)
                return;

            if (!Definition.BullyStatsOverride)
                return;

            bullyCharacter.ApplyTemporaryRaceStats(Definition.BullyStatsOverride);
        }

        protected virtual void RestoreBullyStatsOverrideIfNeeded()
        {
            PlayerCharacter bullyCharacter = BullyCharacter;

            if (!bullyCharacter)
                return;

            bullyCharacter.RestoreBaseStats();
        }
    }
}
