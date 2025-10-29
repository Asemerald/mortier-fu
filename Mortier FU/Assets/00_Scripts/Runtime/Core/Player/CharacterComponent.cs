using System;
using MortierFu.Shared;
using UnityEngine.InputSystem;

namespace MortierFu {
    public abstract class CharacterComponent : IDisposable {
        protected readonly Character character;

        public Character Character => character;
        public PlayerInput PlayerInput => Character.PlayerInput;
        public SO_CharacterStats Stats => Character.CharacterStats;
        
        protected CharacterComponent(Character character) {
            if (character == null) {
                Logs.LogError("Trying to create a character component for a null character!");
                return;
            }

            this.character = character;
        }

        public virtual void Initialize() { }
        public virtual void Dispose() { }
    }
}