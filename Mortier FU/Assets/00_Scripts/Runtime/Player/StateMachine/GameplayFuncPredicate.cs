using System;

namespace MortierFu
{
    /// <summary>
    /// Legacy predicate kept for compatibility.
    /// 
    /// Do not use this class anymore for player gameplay permissions.
    /// Use PlayerActionPredicate instead when the condition depends on
    /// CanMove, CanAim, CanShoot, CanDash, etc.
    /// </summary>
    [Obsolete("Use PlayerActionPredicate instead for gameplay permissions.")]
    public class GameplayFuncPredicate : IPredicate
    {
        private readonly Func<bool> _func;
        
        public GameplayFuncPredicate(Func<bool> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }
        
        public bool Evaluate()
        {
            return _func.Invoke();
        }
    }
}