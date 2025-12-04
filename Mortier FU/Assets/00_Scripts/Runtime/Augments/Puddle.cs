using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MortierFu
{
    public class Puddle : MonoBehaviour
    {
        public struct Data
        {
            // Meta
            public PlayerCharacter Owner;

            // Movement
            public Vector3 InstantiatePos;
            public Vector3 Scale;

            public float Lifetime;
        }

        private readonly HashSet<PlayerCharacter> _inside = new();

        private CancellationTokenSource _cts;

        private PuddleSystem _system;
        private Data _data;

        public List<Ability> Abilities;

        public void AddAbility(Ability ability)
        {
            if (!Abilities.Contains(ability))
            {
                Abilities.Add(ability);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null || !rb.TryGetComponent(out PlayerCharacter character)) return;

            _inside.Add(character);
            ApplyEffects(character);
        }

        private void OnTriggerExit(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null || !rb.TryGetComponent(out PlayerCharacter character)) return;

            _inside.Remove(character);
        }

        public void SetData(Data data)
        {
            _data = data;
            transform.position = data.InstantiatePos;
            transform.localScale = data.Scale;
        }

        public void Initialize(PuddleSystem system)
        {
            _system = system;
            Abilities = new List<Ability>();
        }

        public void OnGet()
        {
            Abilities.Clear();
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            StartLifetimeAsync(_cts.Token).Forget();
        }

        public void OnRelease()
        {
            _cts?.Cancel();
            _cts = null;

            foreach (var player in _inside)
                CancelEffects(player);

            _inside.Clear();

            Abilities.Clear();
        }

        private void ApplyEffects(PlayerCharacter target)
        {
            foreach (Ability t in Abilities)
            {
                t.Execute(target);
            }
        }

        private void CancelEffects(PlayerCharacter target)
        {
            foreach (Ability t in Abilities)
            {
                t.Cancel(target);
            }
        }

        private async UniTaskVoid StartLifetimeAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_data.Lifetime), cancellationToken: token);

                _system.ReleasePuddle(this);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}