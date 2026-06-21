using System;

namespace MortierFu
{
    public class PlayerActionPredicate : IPredicate
    {
        private readonly PlayerCharacter _character;
        private readonly Func<PlayerActionPermissions, bool> _permissionPredicate;
        private readonly Func<bool> _condition;

        public PlayerActionPredicate(
            PlayerCharacter character,
            Func<PlayerActionPermissions, bool> permissionPredicate,
            Func<bool> condition)
        {
            _character = character;
            _permissionPredicate = permissionPredicate;
            _condition = condition;
        }

        public bool Evaluate()
        {
            if (_character == null)
                return false;

            return _permissionPredicate.Invoke(_character.ActionPermissions)
                   && _condition.Invoke();
        }
    }
}