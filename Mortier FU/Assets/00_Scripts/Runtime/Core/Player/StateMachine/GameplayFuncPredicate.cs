using System;

namespace MortierFu
{
    public class GameplayFuncPredicate : IPredicate
    {
        private readonly Func<bool> _func;
        
        public GameplayFuncPredicate(Func<bool> func)
        {
            _func = func;
        }
        
        public bool Evaluate() => PlayerCharacter.AllowGameplayActions && _func.Invoke();
    }
}