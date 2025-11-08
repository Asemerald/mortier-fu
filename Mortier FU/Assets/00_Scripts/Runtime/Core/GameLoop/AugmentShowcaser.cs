using System.Collections.ObjectModel;
using Random = UnityEngine.Random;
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
        private readonly AugmentSelectionSystem _system;
        private Camera _cam;

        public AugmentShowcaser (AugmentSelectionSystem system, ReadOnlyCollection<AugmentPickup> augments)
        {
            _pickups = augments;
            _system = system;
            _cam = Camera.main;
        }

        public async Task Showcase(List<Vector3> positions)
        {
            await Task.Delay(TimeSpan.FromSeconds(_system.Settings.LaunchShowcaseDelay));
            
            float step = _system.Settings.CardScale * 2f + _system.Settings.Offset;
            Vector3 origin = _cam.transform.position + _cam.transform.forward * 5f - _cam.transform.right * (step * (_pickups.Count - 1)) / 2f;
            
            for (int i = 0; i < _pickups.Count; i++)
            {
                var pickup = _pickups[i];
                pickup.SetFaceCameraEnabled(true);
                pickup.transform.position = origin + _cam.transform.right * (step * i);
                pickup.transform.localScale = Vector3.zero;
                pickup.Show();
                
                Tween.Scale(pickup.transform, _system.Settings.CardScale,_system.Settings.ScaleDuration, Ease.OutBounce);
                
                await Task.Delay(TimeSpan.FromSeconds(0.05f));
            }
            
            await Task.Delay(TimeSpan.FromSeconds(_system.Settings.PlaceAugmentsDelay));

            if (_pickups.Count != positions.Count)
            {
                Logs.LogWarning("[AugmentShowcaser] Number of pickups and positions do not match.");
                return;
            }
            
            for (var i = 0; i < _pickups.Count; i++)
            {
                var pickup = _pickups[i];
                var duration = Random.Range(0.4f, 0.8f);
                Tween.Position(pickup.transform, positions[i].Add(y: i * 0.06f), duration, Ease.InOutQuad)
                    .Group(Tween.Scale(pickup.transform, _system.Settings.CardScale, 1f, duration * 0.7f, Ease.OutBack)).OnComplete(() =>
                    {
                        pickup.SetFaceCameraEnabled(false);
                        pickup.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    });
                
                await Task.Delay(TimeSpan.FromSeconds(Random.Range(0.12f, 0.4f)));
            }
        }

        public void Dispose()
        {
            // Noop
        }
    }
}