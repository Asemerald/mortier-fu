using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public class PuddleFactory
    {
        private readonly PuddleSystem _system;
        
        public PuddleFactory(PuddleSystem system)
        {
            _system = system;
        }

        public Puddle CreatePuddle(PlayerCharacter owner, Vector3 pos, Vector3 scale, float lifetime, List<Ability> abilities)
        {
            //TODO: Refacto
            var puddleData = new Puddle.Data
            {
                Owner = owner,
                InstantiatePos = pos,
                Scale = scale,
                Lifetime = lifetime
            };

            var puddle = _system.RequestPuddle(puddleData);

            foreach (var ability in abilities)
            {
                puddle.AddAbility(ability);
            }
            
            return puddle;
        }
    }
}