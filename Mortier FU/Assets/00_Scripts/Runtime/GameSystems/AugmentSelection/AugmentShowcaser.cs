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
        
        private Transform[] _augmentPoints;
        private Tween _spinTween;
        
        private Camera _cam;

        public AugmentShowcaser (AugmentSelectionSystem system, ReadOnlyCollection<AugmentPickup> pickups)
        {
            _pickups = pickups;
            _system = system;
           
            _confirmationService = ServiceManager.Instance.Get<ConfirmationService>();
            
            _cam = Camera.main;
        }

        public async UniTask Showcase(Transform pivot, Vector3[] augmentPoints)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.ShowcaseStartDelay));
            
            float step = _system.Settings.DisplayedCardScale * 2f + _system.Settings.CardSpacing;
            Vector3 origin = _cam.transform.position + _cam.transform.forward * 2f - _cam.transform.right * (step * (_pickups.Count - 1)) / 2f;
            
            for (int i = 0; i < _pickups.Count; i++)
            {
                var pickup = _pickups[i];
                pickup.SetFaceCameraEnabled(true);
                pickup.transform.position = origin + _cam.transform.right * (step * i);
                pickup.transform.localScale = Vector3.zero;
                pickup.Show();
                
                Tween.Scale(pickup.transform, _system.Settings.DisplayedCardScale,_system.Settings.CardPopInDuration, Ease.OutBounce);
                
                await UniTask.Delay(TimeSpan.FromSeconds(0.05f));
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
                _augmentPoints[i] = augmentPoint;
                
                pickup.transform.SetParent(_augmentPoints[i]);
                
                var duration = _system.Settings.CardMoveDurationRange.GetRandomValue();
                Tween.Position(pickup.transform, _augmentPoints[i].position.Add(y: i * 0.06f), duration, Ease.InOutQuad)
                    .Group(Tween.Scale(pickup.transform, _system.Settings.DisplayedCardScale, 1f, duration * 0.7f, Ease.OutBack)).OnComplete(() =>
                    {
                        pickup.SetFaceCameraEnabled(false);
                        pickup.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    });

                await UniTask.Delay(TimeSpan.FromSeconds(_system.Settings.CardMoveStaggerRange.GetRandomValue()));
            }
            
            _spinTween = Tween.RotationAtSpeed(pivot, Vector3.up * 360f, 2f, Ease.Linear, -1, CycleMode.Incremental);
        }
        
        public void StopShowcase()
        {
            foreach (var pickup in _pickups)
            {
                pickup.transform.SetParent(null);
            }

            for (int i = _augmentPoints.Length - 1; i >= 0; i--)
            {
                Object.Destroy(_augmentPoints[i].gameObject);
            }
            
            _spinTween.Stop();
            _spinTween = default;
        }

        public void Dispose()
        {
            // Noop
        }
    }
}