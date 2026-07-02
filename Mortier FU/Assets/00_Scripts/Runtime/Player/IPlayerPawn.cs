using UnityEngine;

namespace MortierFu
{
    public interface IPlayerPawn
    {
        PlayerManager Owner { get; }
        Transform PawnTransform { get; }
        bool IsPawnActive { get; }

        void EnterPawn();
        void ExitPawn();

        void SetMoveInput(Vector2 input);
        void SetAimInput(Vector2 input);
        void SetAimHeld(bool isHeld);
        void ShootPressed();
        void ShootReleased();
    }
}