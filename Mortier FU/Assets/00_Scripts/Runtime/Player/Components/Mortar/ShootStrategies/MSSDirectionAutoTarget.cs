using UnityEngine.InputSystem;

namespace MortierFu
{
    public class MSSDirectionAutoTarget : MortarShootStrategy
    {
        public MSSDirectionAutoTarget(Mortar mortar, InputAction aimAction, InputAction shootAction) : base(mortar, aimAction, shootAction)
        { }
    }
}