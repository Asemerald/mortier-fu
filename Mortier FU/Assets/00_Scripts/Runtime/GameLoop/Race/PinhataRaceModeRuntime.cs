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
        
        private readonly List<int> _previewPickupIndexes = new(4);

        private GameObject _bullyPreviewVfxInstance;
        private GameObject _currentBullyPreviewVfxPrefab;

        public override Transform ResolveSpawnPoint(PlayerTeam team, PlayerManager player, int racerIndex, Transform fallback)
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
            ClearBullyPreviewVfx();

            _hiddenPickupIndexes.Clear();
            _previewPickupIndexes.Clear();

            base.End();
        }

        public override void Dispose()
        {
            UnregisterStrikeListener();
            CancelDrops();
            ClearBullyPreviewVfx();

            _hiddenPickupIndexes.Clear();
            _previewPickupIndexes.Clear();
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

            RefreshBullyPreviewVfx();
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

            int pickupsToDrop = ResolvePickupsToDrop();

            if (pickupsToDrop <= 0)
                return;

            _lastDropTime = Time.time;
            DropNextPickups(evt.Character, pickupsToDrop).Forget();
        }

        private int ResolvePickupsToDrop()
        {
            int remainingPickups = _hiddenPickupIndexes.Count;

            if (remainingPickups <= 0)
                return 0;

            if (!PinhataDefinition)
                return Mathf.Min(1, remainingPickups);

            int playerCount = GetActivePlayerCount();

            return PinhataDefinition.ResolvePickupsPerHit(playerCount, remainingPickups);
        }

        private int GetActivePlayerCount()
        {
            if (Context?.Teams == null)
                return 1;

            int playerCount = 0;

            for (int i = 0; i < Context.Teams.Count; i++)
            {
                PlayerTeam team = Context.Teams[i];

                if (team == null || team.Members == null)
                    continue;

                playerCount += team.Members.Count;
            }

            return Mathf.Max(1, playerCount);
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

        private async UniTaskVoid DropNextPickups(PlayerCharacter striker, int amount)
        {
            AugmentSelectionSystem selectionSystem = Context?.AugmentSelectionSystem;

            if (selectionSystem == null || _hiddenPickupIndexes.Count == 0)
                return;

            amount = Mathf.Clamp(amount, 1, _hiddenPickupIndexes.Count);

            CancellationToken token = _dropCancellation?.Token ?? CancellationToken.None;

            var tasks = new List<UniTask>(amount);
            var reservedPositions = new List<Vector3>(amount);

            for (int i = 0; i < amount; i++)
            {
                int pickupIndex = _hiddenPickupIndexes.Dequeue();
                Vector3 dropPosition = GetDropPosition(striker, i, reservedPositions);

                reservedPositions.Add(dropPosition);

                float delay = PinhataDefinition ? PinhataDefinition.MultiDropDelay * i : 0f;

                tasks.Add(DropPickupAsync(selectionSystem, pickupIndex, dropPosition, delay, token));
            }

            RefreshBullyPreviewVfx();
            
            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            { }
        }
        
        private void RefreshBullyPreviewVfx()
        {
            if (!PinhataDefinition || !PinhataDefinition.ShowBullyPreviewVfx)
            {
                ClearBullyPreviewVfx();
                return;
            }

            GameObject previewPrefab = ResolveNextPreviewVfxPrefab();

            SetBullyPreviewVfx(previewPrefab);
        }
        
        private GameObject ResolveNextPreviewVfxPrefab()
        {
            AugmentSelectionSystem selectionSystem = Context?.AugmentSelectionSystem;

            if (selectionSystem == null || _hiddenPickupIndexes.Count == 0)
                return null;

            int pickupsToPreview = ResolvePickupsToDrop();

            if (pickupsToPreview <= 0)
                return null;

            PopulatePreviewPickupIndexes(pickupsToPreview, _previewPickupIndexes);

            if (_previewPickupIndexes.Count == 0)
                return null;
            
            return selectionSystem.TryGetRarestAugmentCharacterVfxPrefab(_previewPickupIndexes, out GameObject prefab) ? prefab : null;
        }
        
        private void PopulatePreviewPickupIndexes(int amount, List<int> result)
        {
            result.Clear();

            if (amount <= 0)
                return;

            foreach (int pickupIndex in _hiddenPickupIndexes)
            {
                result.Add(pickupIndex);

                if (result.Count >= amount)
                    return;
            }
        }
        
        private void SetBullyPreviewVfx(GameObject prefab)
        {
            if (!prefab)
            {
                ClearBullyPreviewVfx();
                return;
            }

            if (_currentBullyPreviewVfxPrefab == prefab && _bullyPreviewVfxInstance)
                return;

            ClearBullyPreviewVfx();

            PlayerCharacter bullyCharacter = BullyCharacter;
            AugmentSelectionSystem selectionSystem = Context?.AugmentSelectionSystem;

            if (!bullyCharacter || selectionSystem == null)
                return;

            GameObject instance = selectionSystem.SpawnAugmentCharacterVfxPrefab(prefab, bullyCharacter.transform, PinhataDefinition.BullyPreviewVfxLocalPosition, PinhataDefinition.BullyPreviewVfxLocalEuler, PinhataDefinition.BullyPreviewVfxLocalScale);

            if (!instance)
                return;

            _currentBullyPreviewVfxPrefab = prefab;
            _bullyPreviewVfxInstance = instance;
        }
        
        private void ClearBullyPreviewVfx()
        {
            _currentBullyPreviewVfxPrefab = null;

            if (!_bullyPreviewVfxInstance)
                return;

            UnityEngine.Object.Destroy(_bullyPreviewVfxInstance);
            _bullyPreviewVfxInstance = null;
        }

        private async UniTask DropPickupAsync(AugmentSelectionSystem selectionSystem, int pickupIndex, Vector3 dropPosition, float delay, CancellationToken cancellationToken)
        {
            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);

            await selectionSystem.DropPickupAsync(pickupIndex, dropPosition, PinhataDefinition.DropHeight, PinhataDefinition.DropDuration, cancellationToken);
        }

        private Vector3 GetDropPosition(PlayerCharacter striker, int batchIndex, List<Vector3> reservedPositions)
        {
            PlayerCharacter bullyCharacter = BullyCharacter;

            if (!bullyCharacter)
                return Vector3.zero;

            Vector3 center = bullyCharacter.transform.position;
            Vector3 baseDirection = GetDropDirection(center, striker, batchIndex);
            float radius = PinhataDefinition ? Mathf.Max(0.1f, PinhataDefinition.DropRadius) : 2.5f;

            for (int radiusIndex = 0; radiusIndex < DropRadiusMultipliers.Length; radiusIndex++)
            {
                float currentRadius = radius * DropRadiusMultipliers[radiusIndex];

                for (int angleIndex = 0; angleIndex < DropAngleOffsets.Length; angleIndex++)
                {
                    Vector3 direction = Quaternion.AngleAxis(DropAngleOffsets[angleIndex], Vector3.up) * baseDirection;
                    Vector3 candidate = center + direction * currentRadius;

                    candidate.y = ResolveDropY(center);

                    if (IsSafeDropPosition(center, candidate, reservedPositions))
                        return candidate;
                }
            }

            Vector3 fallback = center;
            fallback.y = ResolveDropY(center);

            return fallback;
        }

        private Vector3 GetDropDirection(Vector3 center, PlayerCharacter striker, int batchIndex)
        {
            Vector3 direction = striker ? striker.transform.position - center : UnityEngine.Random.insideUnitSphere;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                direction = UnityEngine.Random.insideUnitSphere;

            direction.y = 0f;
            direction.Normalize();

            if (batchIndex <= 0)
                return direction;

            float angleStep = PinhataDefinition ? PinhataDefinition.MultiDropAngleStep : 25f;

            int pairIndex = (batchIndex + 1) / 2;
            float sign = batchIndex % 2 == 1 ? 1f : -1f;
            float angle = sign * pairIndex * angleStep;

            return Quaternion.AngleAxis(angle, Vector3.up) * direction;
        }

        private float ResolveDropY(Vector3 center) => PinhataDefinition && PinhataDefinition.OverrideDropWorldY ? PinhataDefinition.DropWorldY : center.y;

        private bool IsSafeDropPosition(Vector3 center, Vector3 candidate, List<Vector3> reservedPositions)
        {
            if (!IsFarEnoughFromReservedPositions(candidate, reservedPositions))
                return false;

            if (!PinhataDefinition || PinhataDefinition.DropBlockingMask.value == 0)
                return true;

            Vector3 probeOffset = Vector3.up * PinhataDefinition.DropProbeHeight;

            Vector3 from = center + probeOffset;
            Vector3 to = candidate + probeOffset;

            if (Physics.Linecast(from, to, PinhataDefinition.DropBlockingMask, QueryTriggerInteraction.Ignore))
                return false;

            return !Physics.CheckSphere(to, PinhataDefinition.DropClearanceRadius, PinhataDefinition.DropBlockingMask, QueryTriggerInteraction.Ignore);
        }

        private bool IsFarEnoughFromReservedPositions(Vector3 candidate, List<Vector3> reservedPositions)
        {
            if (reservedPositions == null || reservedPositions.Count == 0)
                return true;

            float minDistance = PinhataDefinition ? Mathf.Max(0.1f, PinhataDefinition.DropClearanceRadius * 2f) : 1.2f;

            float minSqrDistance = minDistance * minDistance;

            for (int i = 0; i < reservedPositions.Count; i++)
            {
                Vector3 delta = candidate - reservedPositions[i];
                delta.y = 0f;

                if (delta.sqrMagnitude < minSqrDistance)
                    return false;
            }

            return true;
        }

        private void CancelDrops()
        {
            _dropCancellation?.Cancel();
            _dropCancellation?.Dispose();
            _dropCancellation = null;
        }
    }
}