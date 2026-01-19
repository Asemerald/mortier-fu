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
        private CancellationTokenSource _cts;

        private Transform[] _augmentPoints;

        public AugmentShowcaser(AugmentSelectionSystem system, ReadOnlyCollection<AugmentCardUI> pickups,
            ReadOnlyCollection<AugmentPickup> pickupsVFX)
        {
            _pickups = pickups;
            _pickupsVFX = pickupsVFX;
            _system = system;

            _confirmationService = ServiceManager.Instance.Get<ConfirmationService>();
            _cameraSystem = SystemManager.Instance.Get<CameraSystem>();
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _shakeService = ServiceManager.Instance.Get<ShakeService>();
            _cam = _cameraSystem.Controller.Camera;
        }

        public void Dispose()
        {
            StopShowcase();
        }

        public async UniTask Showcase(Transform pivot, Vector3[] augmentPoints, int augmentCount)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            float alpha = (augmentCount - 3) / 2f;
            float cardScale = Mathf.Lerp(_system.Settings.DisplayedCardScaleRange.Min,
                _system.Settings.DisplayedCardScaleRange.Max,
                alpha);
            float cardSpace = Mathf.Lerp(_system.Settings.CardSpacingRange.Min,
                _system.Settings.CardSpacingRange.Max,
                alpha);

            float step = cardScale * 2f + cardSpace;
            Vector3 origin = _cam.transform.position + _cam.transform.forward * 2f -
                             _cam.transform.right * (step * (_pickups.Count - 1)) / 2f;

            for (int i = 0; i < _pickups.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var pickup = _pickups[i];
                pickup.ResetUI();
                pickup.SetFaceCameraEnabled(true);
                pickup.transform.position = origin + _cam.transform.right * (step * i);
                pickup.transform.localScale = Vector3.zero;
                pickup.Show();

                var pickupVFX = _pickupsVFX[i];
                pickupVFX.transform.localPosition = pickup.transform.position;
                pickupVFX.transform.localScale = new Vector3(4, 4, 4);
                // TODO : Atroce hack to fix VFX rotation
                pickupVFX.transform.rotation *= Quaternion.Euler(0f, 15f, 0f);

                GrowPickup(pickup, cardScale, ct).Forget();

                float stagger = _system.Settings.CardPopInStagger.GetRandomValue();
                await UniTask.Delay(TimeSpan.FromSeconds(stagger), cancellationToken: ct);
            }

            ct.ThrowIfCancellationRequested();
            await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.CardPopInDuration), cancellationToken: ct);

            _confirmationService.ShowConfirmation(_lobbyService.GetPlayers().Count);
            await _confirmationService.WaitUntilAllConfirmed();

            var flipTasks = new UniTask[_pickups.Count];
            UniTaskCompletionSource previousMidFlip = null;
            for (int i = 0; i < _pickups.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var pickup = _pickups[i];
                var midFlipSignal = new UniTaskCompletionSource();

                int index = i;

                flipTasks[i] = UniTask.Create(async () =>
                {
                    if (previousMidFlip != null)
                        await previousMidFlip.Task;

                    await FlipPickupStair(
                        pickup,
                        ct,
                        midFlipSignal,
                        _system.Settings.FlipDuration
                    );
                });

                previousMidFlip = midFlipSignal;
            }

            await UniTask.WhenAll(flipTasks);

            await UniTask.Delay(TimeSpan.FromSeconds(0.3f), cancellationToken: ct);

            int[] shuffled = GetShuffledIndices(_pickups.Count);
            int j = -1;
            foreach (int idx in shuffled)
            {
                j++;

                ct.ThrowIfCancellationRequested();

                var pickup = _pickups[idx];
                var pickupVFX = _pickupsVFX[idx];

                pickup.PlayRevealSequence(pickupVFX).Forget();

                float t = (shuffled.Length - j) / (float)shuffled.Length;
                await UniTask.Delay(TimeSpan.FromSeconds(t * t * shuffled.Length * 0.05f + 0.09f),
                    cancellationToken: ct);
            }
            
            await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.RevealDelay), cancellationToken: ct);

            if (_pickups.Count != augmentPoints.Length)
            {
                Logs.LogWarning("[AugmentShowcaser] Number of pickups and positions do not match.");
                return;
            }

            _augmentPoints = new Transform[_pickups.Count];

            for (var i = 0; i < _pickups.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var pickupVFX = _pickupsVFX[i];

                pickupVFX.transform.localScale = new Vector3(4, 4, 4);

                var augmentPoint = new GameObject("Augment Point #" + i).transform;
                augmentPoint.SetParent(pivot);
                augmentPoint.position = augmentPoints[i].Add(y: 1.8f);
                _augmentPoints[i] = augmentPoint;

                var duration = _system.Settings.CardMoveDurationRange.GetRandomValue();
                MovePickupToAugmentPoint(pickupVFX, i, duration, _system.Settings.CarouselCardScale, ct,  _augmentPoints[i])
                    .Forget();

                await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.CardMoveStaggerRange.GetRandomValue()),
                    cancellationToken: ct);
            }
        }

        private async UniTaskVoid GrowPickup(AugmentCardUI cardUI, float scale, CancellationToken ct)
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Showcase, cardUI.transform.position);
            _shakeService.ShakeControllers(ShakeService.ShakeType.MID);
            await Tween.Scale(cardUI.transform, scale, _system.Settings.CardPopInDuration, Ease.OutBounce)
                .ToUniTask(cancellationToken: ct);
        }

        private async UniTask MovePickupToAugmentPoint(AugmentPickup pickup, int i, float duration, float scale,
            CancellationToken ct, Transform augmentPoint)
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_ToWorld, pickup.transform.position);

            pickup.transform.SetParent(augmentPoint, true);

            await Tween.LocalPosition(
                    pickup.transform,
                    Vector3.zero,
                    duration,
                    Ease.InOutQuad
                )
                .Group(
                    Tween.Scale(
                        pickup.transform,
                        scale,
                        1,
                        duration,
                        Ease.OutBack
                    )
                )
                .ToUniTask(cancellationToken: ct);
        }

        private async UniTask FlipPickupStair(AugmentCardUI cardUI, CancellationToken ct,
            UniTaskCompletionSource onMidFlip, float duration = 0.5f)
        {
            cardUI.SetFaceCameraEnabled(false);
            Transform t = cardUI.transform;

            Quaternion startRot = t.localRotation;
            Quaternion midRot = startRot * Quaternion.Euler(0f, -120f, 0f);
            Quaternion endRot = startRot * Quaternion.Euler(0f, -180f, 0f);

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_ToWorld, cardUI.transform.position);

            await Tween.LocalRotation(
                t,
                midRot,
                duration * 0.5f,
                Ease.InQuad
            ).ToUniTask(cancellationToken: ct);

            ct.ThrowIfCancellationRequested();

            cardUI.DisableObjectsOnFlip();
            onMidFlip.TrySetResult();

            await Tween.LocalRotation(
                t,
                endRot,
                duration * 0.5f,
                Ease.OutQuad
            ).ToUniTask(cancellationToken: ct);
        }

        public void StopShowcase()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _system.RestorePickupParent();

            foreach (var pickup in _pickups)
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
            var indices = new int[count];
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