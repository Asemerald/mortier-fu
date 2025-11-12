using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using PrimeTween;
using System;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace MortierFu
{
    public class AugmentShowcaser : IDisposable
    {
        private readonly ReadOnlyCollection<AugmentPickup> _pickups;
        private readonly ConfirmationService _confirmationService;
        private readonly AugmentSelectionSystem _system;
        
        private Camera _cam;

        public AugmentShowcaser (AugmentSelectionSystem system, ReadOnlyCollection<AugmentPickup> pickups)
        {
            _pickups = pickups;
            _system = system;
           
            _confirmationService = ServiceManager.Instance.Get<ConfirmationService>();
            
            _cam = Camera.main;
        }

        public async Task Showcase(List<Vector3> positions)
        {
            await Task.Delay(TimeSpan.FromSeconds(_system.Settings.ShowcaseStartDelay));
            
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
                
                await Task.Delay(TimeSpan.FromSeconds(0.05f));
            }
            
            await Task.Delay(TimeSpan.FromSeconds(_system.Settings.CardPopInDuration));
            await _confirmationService.WaitUntilAllConfirmed();

            if (_pickups.Count != positions.Count)
            {
                Logs.LogWarning("[AugmentShowcaser] Number of pickups and positions do not match.");
                return;
            }
            
            for (var i = 0; i < _pickups.Count; i++)
            {
                var pickup = _pickups[i];
                var duration = _system.Settings.CardMoveDurationRange.GetRandomValue();
                Tween.Position(pickup.transform, positions[i].Add(y: i * 0.06f), duration, Ease.InOutQuad)
                    .Group(Tween.Scale(pickup.transform, _system.Settings.DisplayedCardScale, 1f, duration * 0.7f, Ease.OutBack)).OnComplete(() =>
                    {
                        pickup.SetFaceCameraEnabled(false);
                        pickup.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    });

                await Task.Delay(TimeSpan.FromSeconds(_system.Settings.CardMoveStaggerRange.GetRandomValue()));
            }
        }

        public void Dispose()
        {
            // Noop
        }
    }
}