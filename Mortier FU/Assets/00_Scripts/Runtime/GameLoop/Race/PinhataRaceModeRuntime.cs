using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class PinhataRaceModeRuntime : RaceModeRuntimeBase
    {
        private readonly Queue<int> _hiddenPickupIndexes = new();

        private EventBinding<TriggerStrike> _strikeBinding;
        private CancellationTokenSource _dropCancellation;
        private float _lastDropTime = -999f;

        private SO_PinhataRaceModeDefinition PinhataDefinition => Definition as SO_PinhataRaceModeDefinition;

        public override Transform ResolveSpawnPoint(PlayerTeam team, PlayerManager player, int racerIndex,
            Transform fallback)
        {
            if (!Reporter)
                return fallback;

            if (IsBully(player))
                return Reporter.BullySpawnPoint ? Reporter.BullySpawnPoint : fallback;

            var racerSpawn = Reporter.GetRacerSpawnPoint(racerIndex);
            return racerSpawn ? racerSpawn : fallback;
        }

        public override UniTask AfterShowcaseCompleted(CancellationToken cancellationToken) => PrepareHiddenPickupsInsideBully(cancellationToken);

        public override void BeginGameplay()
        {
            base.BeginGameplay();

            RegisterStrikeListener();
        }

        public override void End()
        {
            UnregisterStrikeListener();
            CancelDrops();
            _hiddenPickupIndexes.Clear();

            base.End();
        }

        public override void Dispose()
        {
            UnregisterStrikeListener();
            CancelDrops();
            _hiddenPickupIndexes.Clear();
        }

        private async UniTask PrepareHiddenPickupsInsideBully(CancellationToken cancellationToken)
        {
            _hiddenPickupIndexes.Clear();

            var selectionSystem = Context?.AugmentSelectionSystem;
            var bullyCharacter = BullyCharacter;

            if (selectionSystem == null || !bullyCharacter)
                return;

            CancelDrops();
            _dropCancellation = new CancellationTokenSource();

            var token = _dropCancellation.Token;
            var pickupCount = selectionSystem.PickupCount;

            var tasks = new List<UniTask>();

            for (var i = 0; i < pickupCount; i++)
            {
                var pickupIndex = i;

                tasks.Add(selectionSystem.AttachPickupToAsync(pickupIndex, bullyCharacter.transform, Vector3.zero, PinhataDefinition.InhalePickupDuration, token));

                _hiddenPickupIndexes.Enqueue(pickupIndex);
            }

            await UniTask.WhenAll(tasks);
        }

        private void RegisterStrikeListener()
        {
            if (_strikeBinding != null)
                return;

            _strikeBinding = new EventBinding<TriggerStrike>(OnStrike);
            EventBus<TriggerStrike>.Register(_strikeBinding);
        }

        private void UnregisterStrikeListener()
        {
            if (_strikeBinding == null)
                return;

            EventBus<TriggerStrike>.Deregister(_strikeBinding);
            _strikeBinding = null;
        }

        private void OnStrike(TriggerStrike evt)
        {
            if (!IsValidPinhataHit(evt))
                return;

            float cooldown = PinhataDefinition ? Mathf.Max(0f, PinhataDefinition.HitCooldown) : 0.25f;

            if (Time.time - _lastDropTime < cooldown)
                return;

            _lastDropTime = Time.time;
            DropNextPickup(evt.Character).Forget();
        }

        private bool IsValidPinhataHit(TriggerStrike evt)
        {
            PlayerCharacter striker = evt.Character;
            PlayerCharacter bullyCharacter = BullyCharacter;

            if (!striker || !bullyCharacter)
                return false;

            if (ReferenceEquals(striker, bullyCharacter))
                return false;

            if (evt.HitCharacters == null || evt.HitCharacters.Length == 0)
                return false;

            for (int i = 0; i < evt.HitCharacters.Length; i++)
            {
                if (ReferenceEquals(evt.HitCharacters[i], bullyCharacter))
                    return true;
            }

            return false;
        }

        private async UniTaskVoid DropNextPickup(PlayerCharacter striker)
        {
            AugmentSelectionSystem selectionSystem = Context?.AugmentSelectionSystem;

            if (selectionSystem == null || _hiddenPickupIndexes.Count == 0)
                return;

            int pickupIndex = _hiddenPickupIndexes.Dequeue();
            Vector3 dropPosition = GetDropPosition(striker);

            try
            {
                await selectionSystem.DropPickupAsync(pickupIndex, dropPosition, GetDropHeight(), GetDropDuration(),
                    _dropCancellation?.Token ?? CancellationToken.None);

                Logs.Log($"[PinhataRaceModeRuntime] Dropped pickup {pickupIndex}.");
            }
            catch (OperationCanceledException)
            {
            }
        }

        private Vector3 GetDropPosition(PlayerCharacter striker)
        {
            PlayerCharacter bullyCharacter = BullyCharacter;

            if (!bullyCharacter)
                return Vector3.zero;

            Vector3 center = bullyCharacter.transform.position;
            Vector3 direction = striker ? center - striker.transform.position : UnityEngine.Random.insideUnitSphere;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                direction = UnityEngine.Random.insideUnitSphere;

            direction.y = 0f;

            direction.Normalize();

            float radius = PinhataDefinition ? Mathf.Max(0.1f, PinhataDefinition.DropRadius) : 2.5f;
            Vector3 position = center + direction * radius;
            position.y = center.y;

            if (PinhataDefinition && PinhataDefinition.OverrideDropWorldY)
                position.y = PinhataDefinition.DropWorldY;

            return position;
        }

        private float GetDropHeight() => PinhataDefinition ? Mathf.Max(0f, PinhataDefinition.DropHeight) : 1.2f;

        private float GetDropDuration() => PinhataDefinition ? Mathf.Max(0.05f, PinhataDefinition.DropDuration) : 0.35f;

        private void CancelDrops()
        {
            _dropCancellation?.Cancel();
            _dropCancellation?.Dispose();
            _dropCancellation = null;
        }
    }
}