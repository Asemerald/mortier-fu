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
        private readonly ReadOnlyCollection<GameObject> _pickupsVFX;
        private readonly AugmentSelectionSystem _system;
        private CancellationTokenSource _cts;

        private Transform[] _augmentPoints;

        public AugmentShowcaser(AugmentSelectionSystem system, ReadOnlyCollection<AugmentCardUI> pickups,
            ReadOnlyCollection<GameObject> pickupsVFX)
        {
            _pickups = pickups;
            _pickupsVFX = pickupsVFX;
            _system = system;

            _confirmationService = ServiceManager.Instance.Get<ConfirmationService>();
            _cameraSystem = SystemManager.Instance.Get<CameraSystem>();
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
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

                GrowPickup(pickup, cardScale, ct).Forget();

                float stagger = _system.Settings.CardPopInStagger.GetRandomValue();
                await UniTask.Delay(TimeSpan.FromSeconds(stagger), cancellationToken: ct);
            }

            ct.ThrowIfCancellationRequested();
            await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.CardPopInDuration), cancellationToken: ct);

            _confirmationService.ShowConfirmation(_lobbyService.GetPlayers().Count);
            await _confirmationService.WaitUntilAllConfirmed();

            for (int i = 0; i < _pickups.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var pickup = _pickups[i];
                var pickupVFX = _pickupsVFX[i];

                await FlipPickup(pickup, ct);
                await pickup.PlayRevealSequence(pickupVFX);
            }

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
                augmentPoint.position = augmentPoints[i];
                _augmentPoints[i] = augmentPoint;

                pickupVFX.transform.SetParent(_augmentPoints[i]);

                var duration = _system.Settings.CardMoveDurationRange.GetRandomValue();
                MovePickupToAugmentPoint(pickupVFX, i, duration, _system.Settings.CarouselCardScale, ct).Forget();

                await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.CardMoveStaggerRange.GetRandomValue()),
                    cancellationToken: ct);
            }
        }

        private async UniTaskVoid GrowPickup(AugmentCardUI cardUI, float scale, CancellationToken ct)
        {
            await Tween.Scale(cardUI.transform, scale, _system.Settings.CardPopInDuration, Ease.OutBounce)
                .ToUniTask(cancellationToken: ct);
        }

        private async UniTask MovePickupToAugmentPoint(GameObject pickup, int i, float duration, float scale,
            CancellationToken ct)
        {
            await Tween.Position(pickup.transform, _augmentPoints[i].position.Add(y: 1.8f + i * 0.06f), duration,
                    Ease.InOutQuad)
                .Group(Tween.Scale(pickup.transform, scale, 1, duration,
                    Ease.OutBack)).ToUniTask(cancellationToken: ct);
        }

        private async UniTask FlipPickup(AugmentCardUI cardUI, CancellationToken ct, float duration = 0.5f)
        {
            cardUI.SetFaceCameraEnabled(false);
            Transform t = cardUI.transform;

            Quaternion startRot = t.localRotation;
            Quaternion midRot = startRot * Quaternion.Euler(0f, 90f, 0f);
            Quaternion endRot = startRot * Quaternion.Euler(0f, 180f, 0f);

            await Tween.LocalRotation(
                t,
                midRot,
                duration * 0.5f,
                Ease.InQuad
            ).ToUniTask(cancellationToken: ct);

            ct.ThrowIfCancellationRequested();

            cardUI.DisableObjectsOnFlip();

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
    }
}