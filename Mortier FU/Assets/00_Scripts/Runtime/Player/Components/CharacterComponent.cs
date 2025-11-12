using System;
using MortierFu.Shared;
using UnityEngine.InputSystem;

namespace MortierFu {
    public abstract class CharacterComponent : IDisposable {
        protected readonly PlayerCharacter character;

        public PlayerCharacter Character => character;
        public PlayerInput PlayerInput => character.PlayerInput;
        public SO_CharacterStats Stats => character.CharacterStats;
        
        protected CharacterComponent(PlayerCharacter character) {
            if (character == null) {
                Logs.LogError("Trying to create a character component for a null character!");
                return;
            }

            this.character = character;
        }

        public virtual void Initialize() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void OnDrawGizmos() { }
        public virtual void OnDrawGizmosSelected() { }
        public virtual void Reset() { }
        public virtual void Dispose() { }
    }
}