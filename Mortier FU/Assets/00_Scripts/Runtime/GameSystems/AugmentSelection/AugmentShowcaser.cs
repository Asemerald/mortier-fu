using System.Collections.ObjectModel;
using MortierFu.Shared;
using UnityEngine;
using PrimeTween;
using System;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace MortierFu
{
    public class AugmentShowcaser : IDisposable
    {
        private readonly ReadOnlyCollection<AugmentPickup> _pickups;
        private readonly ConfirmationService _confirmationService;
        private readonly AugmentSelectionSystem _system;
        private readonly CameraSystem _cameraSystem;
        private readonly Camera _cam;
        
        private Transform[] _augmentPoints;

        public AugmentShowcaser (AugmentSelectionSystem system, ReadOnlyCollection<AugmentPickup> pickups)
        {
            _pickups = pickups;
            _system = system;
           
            _confirmationService = ServiceManager.Instance.Get<ConfirmationService>();
            _cameraSystem = SystemManager.Instance.Get<CameraSystem>();
            _cam = _cameraSystem.Controller.Camera;
        }

        public async UniTask Showcase(Transform pivot, Vector3[] augmentPoints, int augmentCount)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.ShowcaseStartDelay));

            float alpha = (augmentCount - 3) / 2f;
            float cardScale = Mathf.Lerp(_system.Settings.DisplayedCardScaleRange.Min,
                                         _system.Settings.DisplayedCardScaleRange.Max,
                                         alpha);
            float cardSpace = Mathf.Lerp(_system.Settings.CardSpacingRange.Min, 
                                         _system.Settings.CardSpacingRange.Max, 
                                         alpha);
            Debug.Log("Card scale: " + cardScale + ", card space: " + cardSpace);
            
            float step = cardScale * 2f + cardSpace;
            Vector3 origin = _cam.transform.position + _cam.transform.forward * 2f - _cam.transform.right * (step * (_pickups.Count - 1)) / 2f;
            
            for (int i = 0; i < _pickups.Count; i++)
            {
                var pickup = _pickups[i];
                pickup.SetFaceCameraEnabled(true);
                pickup.transform.position = origin + _cam.transform.right * (step * i);
                pickup.transform.localScale = Vector3.zero;
                pickup.Show();
                
                GrowPickup(pickup, cardScale).Forget();

                float stagger = _system.Settings.CardPopInStagger.GetRandomValue();
                await UniTask.Delay(TimeSpan.FromSeconds(stagger));
            }
            
            await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.CardPopInDuration));
            await _confirmationService.WaitUntilAllConfirmed();

            if (_pickups.Count != augmentPoints.Length)
            {
                Logs.LogWarning("[AugmentShowcaser] Number of pickups and positions do not match.");
                return;
            }
            
            _augmentPoints = new Transform[_pickups.Count];
            
            for (var i = 0; i < _pickups.Count; i++)
            {
                var pickup = _pickups[i];
                
                var augmentPoint = new GameObject("Augment Point #" + i).transform;
                augmentPoint.SetParent(pivot);
                augmentPoint.position = augmentPoints[i];
                _augmentPoints[i] = augmentPoint;
                
                pickup.transform.SetParent(_augmentPoints[i]);
                
                var duration = _system.Settings.CardMoveDurationRange.GetRandomValue();
                MovePickupToAugmentPoint(pickup, i, duration, cardScale).Forget();

                await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.CardMoveStaggerRange.GetRandomValue()));
            }
        }
        private async UniTaskVoid GrowPickup(AugmentPickup pickup, float scale)
        {
            await Tween.Scale(pickup.transform, scale, _system.Settings.CardPopInDuration, Ease.OutBounce);
        }
        private async UniTask MovePickupToAugmentPoint(AugmentPickup pickup, int i, float duration, float scale)
        {
            await Tween.Position(pickup.transform, _augmentPoints[i].position.Add(y: 1.8f + i * 0.06f), duration, Ease.InOutQuad)
                .Group(Tween.Scale(pickup.transform, scale, _system.Settings.CarouselCardScale, duration * 0.7f, Ease.OutBack)).OnComplete(() =>
                {
                    pickup.SetFaceCameraEnabled(true);
                });
        }

        public void StopShowcase()
        {
            _system.RestorePickupParent();

            for (int i = _augmentPoints.Length - 1; i >= 0; i--)
            {
                Object.Destroy(_augmentPoints[i].gameObject);
            }
        }

        public void Dispose()
        {
            // Noop
        }
    }
}