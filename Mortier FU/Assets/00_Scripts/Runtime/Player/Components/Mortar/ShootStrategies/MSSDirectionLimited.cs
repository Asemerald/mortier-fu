using UnityEngine.InputSystem;

namespace MortierFu
{
    public class MSSDirectionLimited : MortarShootStrategy
    {
        public MSSDirectionLimited(Mortar mortar, InputAction aimAction, InputAction shootAction) : base(mortar, aimAction, shootAction)
        { }
    }
}