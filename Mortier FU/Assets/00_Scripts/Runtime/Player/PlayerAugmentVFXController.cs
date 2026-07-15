using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public sealed class PlayerAugmentVFXController : MonoBehaviour
    {
        private sealed class ActiveAugmentVFX
        {
            public GameObject Instance;
            public ParticleSystem[] Particles;
            public int ReferenceCount;
        }
        
        [SerializeField] private Transform _vfxRoot;

        private readonly Dictionary<GameObject, ActiveAugmentVFX> _activeVfxByPrefab = new();

        public void Show(GameObject prefab)
        {
            if (!prefab)
                return;

            if (_activeVfxByPrefab.TryGetValue(prefab, out ActiveAugmentVFX activeVfx))
            {
                activeVfx.ReferenceCount++;

                if (activeVfx.Instance && !activeVfx.Instance.activeSelf)
                    SetActive(activeVfx, true);

                return;
            }

            Transform parent = _vfxRoot ? _vfxRoot : transform;
            GameObject instance = Instantiate(prefab, parent, false);

            activeVfx = new ActiveAugmentVFX
            {
                Instance = instance,
                Particles = instance.GetComponentsInChildren<ParticleSystem>(true),
                ReferenceCount = 1
            };

            _activeVfxByPrefab.Add(prefab, activeVfx);
            SetActive(activeVfx, true);
        }

        public void Hide(GameObject prefab)
        {
            if (!prefab)
                return;

            if (!_activeVfxByPrefab.TryGetValue(prefab, out ActiveAugmentVFX activeVfx))
                return;

            activeVfx.ReferenceCount = Mathf.Max(0, activeVfx.ReferenceCount - 1);

            if (activeVfx.ReferenceCount > 0)
                return;

            SetActive(activeVfx, false);
        }

        public void ClearAll()
        {
            foreach (ActiveAugmentVFX activeVfx in _activeVfxByPrefab.Values)
            {
                if (activeVfx.Instance)
                    Destroy(activeVfx.Instance);
            }

            _activeVfxByPrefab.Clear();
        }

        private static void SetActive(ActiveAugmentVFX activeVfx, bool active)
        {
            if (!activeVfx.Instance)
                return;

            activeVfx.Instance.SetActive(active);

            for (int i = 0; i < activeVfx.Particles.Length; i++)
            {
                ParticleSystem particle = activeVfx.Particles[i];

                if (!particle)
                    continue;

                if (active)
                    particle.Play(true);
                else
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void OnDestroy() => ClearAll();
    }
}