using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MortierFu
{
    public sealed class PinhataRaceModeRuntime : RaceModeRuntimeBase
    {
        private readonly Queue<int> _hiddenPickupIndexes = new();

        private EventBinding<TriggerStrike> _strikeBinding;
        private CancellationTokenSource _dropCancellation;
        private float _lastDropTime = -999f;

        private static readonly float[] DropAngleOffsets =
        {
            0f,
            25f, -25f,
            50f, -50f,
            75f, -75f,
            110f, -110f,
            145f, -145f,
            180f
        };

        private static readonly float[] DropRadiusMultipliers =
        {
            1f,
            0.75f,
            0.5f,
            1.25f
        };

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

                tasks.Add(selectionSystem.AttachPickupToAsync(pickupIndex, bullyCharacter.transform, Vector3.zero,
                    PinhataDefinition.InhalePickupDuration, token));

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

            var cooldown = PinhataDefinition ? Mathf.Max(0f, PinhataDefinition.HitCooldown) : 0.25f;

            if (Time.time - _lastDropTime < cooldown)
                return;

            _lastDropTime = Time.time;
            DropNextPickup(evt.Character).Forget();
        }

        private bool IsValidPinhataHit(TriggerStrike evt)
        {
            var striker = evt.Character;
            var bullyCharacter = BullyCharacter;

            if (!striker || !bullyCharacter)
                return false;

            if (ReferenceEquals(striker, bullyCharacter))
                return false;

            if (evt.HitCharacters == null || evt.HitCharacters.Length == 0)
                return false;

            for (var i = 0; i < evt.HitCharacters.Length; i++)
            {
                if (ReferenceEquals(evt.HitCharacters[i], bullyCharacter))
                    return true;
            }

            return false;
        }

        private async UniTaskVoid DropNextPickup(PlayerCharacter striker)
        {
            var selectionSystem = Context?.AugmentSelectionSystem;

            if (selectionSystem == null || _hiddenPickupIndexes.Count == 0)
                return;

            var pickupIndex = _hiddenPickupIndexes.Dequeue();
            var dropPosition = GetDropPosition(striker);

            try
            {
                await selectionSystem.DropPickupAsync(pickupIndex, dropPosition, PinhataDefinition.DropHeight,
                    PinhataDefinition.DropDuration, _dropCancellation?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException)
            { }
        }

        private Vector3 GetDropPosition(PlayerCharacter striker)
        {
            PlayerCharacter bullyCharacter = BullyCharacter;

            if (!bullyCharacter)
                return Vector3.zero;

            Vector3 center = bullyCharacter.transform.position;
            Vector3 preferredDirection = GetPreferredDropDirection(center, striker);

            float radius = PinhataDefinition ? Mathf.Max(0.1f, PinhataDefinition.DropRadius) : 2.5f;

            return ResolveSafeDropPosition(center, preferredDirection, radius);
        }

        private Vector3 GetPreferredDropDirection(Vector3 center, PlayerCharacter striker)
        {
            Vector3 direction = striker ? striker.transform.position - center : UnityEngine.Random.insideUnitSphere;
            
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                direction = UnityEngine.Random.insideUnitSphere;

            direction.y = 0f;
            direction.Normalize();

            return direction;
        }

        private Vector3 ResolveSafeDropPosition(Vector3 center, Vector3 preferredDirection, float radius)
        {
            for (int radiusIndex = 0; radiusIndex < DropRadiusMultipliers.Length; radiusIndex++)
            {
                float currentRadius = radius * DropRadiusMultipliers[radiusIndex];

                for (int angleIndex = 0; angleIndex < DropAngleOffsets.Length; angleIndex++)
                {
                    Vector3 direction = Quaternion.AngleAxis(DropAngleOffsets[angleIndex], Vector3.up) * preferredDirection;
                    Vector3 candidate = center + direction * currentRadius;

                    candidate = ApplyDropY(candidate, center);

                    if (IsSafeDropPosition(center, candidate))
                        return candidate;
                }
            }

            Vector3 fallback = center + preferredDirection * Mathf.Min(radius, 1f);
            return ApplyDropY(fallback, center);
        }

        private Vector3 ApplyDropY(Vector3 position, Vector3 center)
        {
            position.y = center.y;

            if (PinhataDefinition && PinhataDefinition.OverrideDropWorldY)
                position.y = PinhataDefinition.DropWorldY;

            return position;
        }

        private bool IsSafeDropPosition(Vector3 center, Vector3 candidate)
        {
            if (!PinhataDefinition || PinhataDefinition.DropBlockingMask.value == 0)
                return true;

            Vector3 probeOffset = Vector3.up * PinhataDefinition.DropProbeHeight;

            Vector3 from = center + probeOffset;
            Vector3 to = candidate + probeOffset;

            if (Physics.Linecast(from, to, PinhataDefinition.DropBlockingMask, QueryTriggerInteraction.Ignore))
                return false;

            return !Physics.CheckSphere(to, PinhataDefinition.DropClearanceRadius, PinhataDefinition.DropBlockingMask, QueryTriggerInteraction.Ignore);
        }

        private void CancelDrops()
        {
            _dropCancellation?.Cancel();
            _dropCancellation?.Dispose();
            _dropCancellation = null;
        }
    }
}