using System;

namespace MortierFu
{
    public abstract class GhostPawnComponent : IDisposable
    {
        protected readonly PlayerGhostPawn pawn;

        protected GhostPawnComponent(PlayerGhostPawn pawn)
        {
            this.pawn = pawn;
        }

        public PlayerGhostPawn Pawn => pawn;
        public PlayerManager Owner => pawn ? pawn.Owner : null;
        public SO_GhostSettings Settings => pawn ? pawn.Settings : null;

        public virtual void Initialize() { }
        public virtual void OnEnterPawn() { }
        public virtual void OnExitPawn() { }
        public virtual void Tick() { }
        public virtual void FixedTick() { }
        public virtual void Reset() { }
        public virtual void Dispose() { }
    }
}