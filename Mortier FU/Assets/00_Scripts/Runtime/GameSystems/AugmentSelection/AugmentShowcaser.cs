using System.Collections.ObjectModel;
using MortierFu.Shared;
using UnityEngine;
using PrimeTween;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;

namespace MortierFu
{
    public class AugmentShowcaser : IDisposable
    {
        private readonly Camera _cam;
        private readonly CameraSystem _cameraSystem;
        private readonly ConfirmationService _confirmationService;
        private readonly LobbyService _lobbyService;
        private readonly ReadOnlyCollection<AugmentCardUI> _pickups;
        private readonly ReadOnlyCollection<AugmentPickup> _pickupsVFX;
        private readonly AugmentSelectionSystem _system;
        private readonly ShakeService _shakeService;
        private readonly SO_GameFlowSettings _flowSettings;
        private CancellationTokenSource _cts;

        private Transform[] _augmentPoints;

        public AugmentShowcaser(AugmentSelectionSystem system, ReadOnlyCollection<AugmentCardUI> pickups, ReadOnlyCollection<AugmentPickup> pickupsVFX)
        {
            _pickups = pickups;
            _pickupsVFX = pickupsVFX;
            _system = system;

            _confirmationService = ServiceManager.Instance.Get<ConfirmationService>();
            _cameraSystem = SystemManager.Instance.Get<CameraSystem>();
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _shakeService = ServiceManager.Instance.Get<ShakeService>();
            _cam = _cameraSystem.Controller.Camera;
            _flowSettings = (GameService.CurrentGameMode as GameModeBase)?.FlowSettings;
        }

        public void Dispose() => StopShowcase();

        public async UniTask Showcase(RaceAugmentLayout layout, int augmentCount)
        {
            if (layout == null || !layout.IsValid(augmentCount))
            {
                Logs.LogWarning("[AugmentShowcaser] Invalid layout.");
                return;
            }

            Transform pivot = layout.Pivot;
            var augmentPoints = layout.Points;
            
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            float alpha = (augmentCount - 3) / 2f;
            float cardScale = Mathf.Lerp(_system.Settings.DisplayedCardScaleRange.Min, _system.Settings.DisplayedCardScaleRange.Max, alpha);
            float cardSpace = Mathf.Lerp(_system.Settings.CardSpacingRange.Min, _system.Settings.CardSpacingRange.Max, alpha);

            const int k_referenceOrthoSize = 20;
            cardScale *= _cam.orthographicSize / k_referenceOrthoSize;

            float step = cardScale * 2f + cardSpace;
            Vector3 origin = _cam.transform.position + _cam.transform.forward * 2f - _cam.transform.right * (step * (_pickups.Count - 1)) / 2f;

            for (int i = 0; i < _pickups.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                AugmentCardUI pickup = _pickups[i];
                pickup.ResetUI();
                pickup.SetFaceCameraEnabled(true);
                pickup.transform.position = origin + _cam.transform.right * (step * i);
               
                AugmentPickup pickupVFX = _pickupsVFX[i];

                pickupVFX.gameObject.SetActive(false);
                pickup.transform.localScale = Vector3.zero;
                pickup.Show();
                
                //pickupVFX.transform.localPosition = pickup.transform.position;
                pickupVFX.transform.localScale = new Vector3(2, 2, 2);
                // TODO : Atroce hack to fix VFX rotation
                pickupVFX.transform.rotation *= Quaternion.Euler(0f, 15f, 0f);
                
                await GrowPickup(pickup, cardScale, ct);

                pickupVFX.transform.position = pickup.AnchorIncon.position;
                pickupVFX.gameObject.SetActive(true);
                pickupVFX.HideVfx();
                
                float stagger = _system.Settings.CardPopInStagger.GetRandomValue();
                await UniTask.Delay(TimeSpan.FromSeconds(stagger), cancellationToken: ct);
            }

            ct.ThrowIfCancellationRequested();

            await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.CardPopInDuration), cancellationToken: ct);

            await WaitBeforePlayerConfirmationAsync(ct);

            _confirmationService.ShowConfirmation(_lobbyService.GetPlayers().Count);

            bool confirmed = await _confirmationService.WaitUntilAllConfirmed();

            ct.ThrowIfCancellationRequested();

            if (!confirmed)
                return;

            var flipTasks = new UniTask[_pickups.Count];
            UniTaskCompletionSource previousMidFlip = null;

            await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.FlipDelay), cancellationToken: ct);

            for (int i = 0; i < _pickups.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                _pickupsVFX[i].gameObject.SetActive(false);
                AugmentCardUI pickup = _pickups[i];
                UniTaskCompletionSource midFlipSignal = new();

                flipTasks[i] = UniTask.Create(async () =>
                {
                    if (previousMidFlip != null)
                        await previousMidFlip.Task;
                    
                    await FlipPickupStair(pickup, ct, midFlipSignal, _system.Settings.FlipDuration);
                });

                previousMidFlip = midFlipSignal;
            }

            await UniTask.WhenAll(flipTasks);

            await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.RevealDelay), cancellationToken: ct);
            int[] shuffled = GetShuffledIndices(_pickups.Count);
            int j = -1;
            foreach (int idx in shuffled)
            {
                j++;

                ct.ThrowIfCancellationRequested();

                AugmentCardUI pickup = _pickups[idx];
                AugmentPickup pickupVFX = _pickupsVFX[idx];

                pickup.PlayRevealSequence().Forget();

                float t = (shuffled.Length - j) / (float)shuffled.Length;

                await UniTask.Delay(TimeSpan.FromSeconds(t * t * shuffled.Length * 0.05f + _system.Settings.VFXStagger), cancellationToken: ct);
                
                pickupVFX.transform.localScale = new Vector3(4, 4, 4);
                pickupVFX.transform.position = pickup.transform.position;  
                pickupVFX.gameObject.SetActive(true);
                pickupVFX.SetVfx();
                
                var children = pickupVFX.GetComponentsInChildren<Transform>(true);
                foreach (var child in children)
                {
                    child.gameObject.layer = 0;
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.BoonDelay), cancellationToken: ct);

            if (_pickups.Count != augmentPoints.Length)
            {
                Logs.LogWarning("[AugmentShowcaser] Number of pickups and positions do not match.");
                return;
            }

            _augmentPoints = new Transform[_pickups.Count];
            var moveTasks = new UniTask[_pickups.Count];

            for (int i = 0; i < _pickups.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                AugmentPickup pickupVFX = _pickupsVFX[i];

                pickupVFX.transform.localScale = new Vector3(4, 4, 4);

                Transform augmentPoint = new GameObject("Augment Point #" + i).transform;
                augmentPoint.position = augmentPoints[i].Add(y: layout.HeightOffset);

                if (layout.ParentPointsToPivot && pivot)
                    augmentPoint.SetParent(pivot, true);

                _augmentPoints[i] = augmentPoint;

                float duration = _system.Settings.CardMoveDurationRange.GetRandomValue();

                moveTasks[i] = MovePickupToAugmentPoint(pickupVFX, i, duration, _system.Settings.CarouselCardScale, layout, ct);

                await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.CardMoveStaggerRange.GetRandomValue()), cancellationToken: ct);
            }

            await UniTask.WhenAll(moveTasks);

            ct.ThrowIfCancellationRequested();
        }

        private async UniTask GrowPickup(AugmentCardUI cardUI, float scale, CancellationToken ct)
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Showcase, cardUI.transform.position);
            _shakeService.ShakeControllers(ShakeService.ShakeType.LITTLE);
            await Tween.Scale(cardUI.transform, scale, _system.Settings.CardPopInDuration, Ease.OutBounce).ToUniTask(cancellationToken: ct);
        }

        private async UniTask MovePickupToAugmentPoint(AugmentPickup pickup, int i, float duration, float scale, RaceAugmentLayout layout, CancellationToken ct)
        {
            Vector3 targetPosition = _augmentPoints[i].position;

            if (layout is { UseRotatorPrediction: true, Pivot: not null })
            {
                Rotator rotator = layout.Pivot.GetComponentInParent<Rotator>();

                if (rotator)
                {
                    Vector3 localPos = _augmentPoints[i].position - rotator.transform.position;
                    targetPosition = rotator.TransposePoint(localPos, duration);
                }
            }

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_ToWorld, pickup.transform.position);
            await Tween.Scale(pickup.transform, scale, 1, duration, Ease.OutBack)
                .Group(Tween.Position(pickup.transform, targetPosition, duration, Ease.InOutQuad))
                .OnComplete(() =>
                {
                    pickup.AttachToPoint(_augmentPoints[i]);
                    pickup.SetVisible(true);
                    pickup.SetInteractable(true);
                })
                .ToUniTask(cancellationToken: ct);
        }
        
        private async UniTask FlipPickupStair(AugmentCardUI cardUI, CancellationToken ct, UniTaskCompletionSource onMidFlip, float duration = 0.5f)
        {
            cardUI.SetFaceCameraEnabled(false);
            Transform t = cardUI.transform;

            Quaternion startRot = t.localRotation;
            Quaternion midRot = startRot * Quaternion.Euler(0f, -120f, 0f);
            Quaternion endRot = startRot * Quaternion.Euler(0f, -180f, 0f);

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Flip, cardUI.transform.position);

            await Tween.LocalRotation(t, midRot, duration * 0.5f, Ease.InQuad).ToUniTask(cancellationToken: ct);

            ct.ThrowIfCancellationRequested();

            cardUI.DisableObjectsOnFlip();
            onMidFlip.TrySetResult();

            await Tween.LocalRotation(t, endRot, duration * 0.5f, Ease.OutQuad).ToUniTask(cancellationToken: ct);
        }

        private async UniTask WaitBeforePlayerConfirmationAsync(CancellationToken ct)
        {
            if (!_flowSettings)
                return;

            float delay = Mathf.Max(0f, _flowSettings.AugmentShowcasePreConfirmationDelay);

            if (delay <= 0f)
                return;

            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);
        }

        public void StopShowcase()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _system.RestorePickupParent();

            foreach (AugmentCardUI pickup in _pickups)
            {
                pickup.ResetUI();
                pickup.Reset();
                pickup.Hide();
            }

            if (_augmentPoints == null) return;

            for (int i = _augmentPoints.Length - 1; i >= 0; i--)
            {
                if (_augmentPoints[i] != null)
                    Object.Destroy(_augmentPoints[i].gameObject);
            }

            _augmentPoints = null;
        }

        private int[] GetShuffledIndices(int count)
        {
            int[] indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = i;

            for (int i = 0; i < count; i++)
            {
                int j = UnityEngine.Random.Range(i, count);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            return indices;
        }
    }
}