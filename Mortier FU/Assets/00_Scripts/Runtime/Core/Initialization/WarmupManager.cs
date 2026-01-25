using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MortierFu
{
    public class WarmupManager : MonoBehaviour
    {
        [Header("Materials")]
        [SerializeField] private List<Material> materialsToWarmup = new();

        [Header("Particle Systems")]
        [SerializeField] private List<ParticleSystem> particlesToWarmup = new();

        [Header("Settings")]
        [Tooltip("Nombre de warmups par frame pour éviter les spikes")]
        [SerializeField] private int itemsPerFrame = 1;

        private Mesh _warmupMesh;

        private void Awake()
        {
            _warmupMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        }

        
        /// <summary>
        /// To Call to warmup all materials and particle systems
        /// </summary>
        public async Task WarmupAllAsync()
        {
            await WarmupMaterialsAsync();
            await WarmupParticlesAsync();
        }
        
        
        private async Task WarmupMaterialsAsync()
        {
            int counter = 0;

            foreach (var mat in materialsToWarmup)
            {
                if (mat == null)
                    continue;

                WarmupMaterial(mat);
                counter++;

                if (counter >= itemsPerFrame)
                {
                    counter = 0;
                    await Task.Yield(); 
                }
            }
        }

        private void WarmupMaterial(Material material)
        {
            for (int pass = 0; pass < material.passCount; pass++)
            {
                if (material.SetPass(pass))
                {
                    Graphics.DrawMeshNow(
                        _warmupMesh,
                        Matrix4x4.TRS(
                            new Vector3(10000, 10000, 10000),
                            Quaternion.identity,
                            Vector3.one
                        )
                    );
                    return;
                }
            }

            Debug.LogWarning(
                $"[Warmup] Aucun pass valide pour le matériau {material.name}",
                material
            );
        }

        

        private async Task WarmupParticlesAsync()
        {
            int counter = 0;

            foreach (var ps in particlesToWarmup)
            {
                if (ps == null)
                    continue;

                WarmupParticleSystem(ps);
                counter++;

                if (counter >= itemsPerFrame)
                {
                    counter = 0;
                    await Task.Yield();
                }
            }
        }

        private void WarmupParticleSystem(ParticleSystem ps)
        {
            var go = Instantiate(ps.gameObject, Vector3.one * 10000f, Quaternion.identity);
            var warmupParticleSystem = go.GetComponent<ParticleSystem>();

            warmupParticleSystem.Play(true);
            warmupParticleSystem.Simulate(0.05f, true, true);

            Destroy(go);
        }
    }
}
