using System.Collections.Generic;
using Cysharp.Threading.Tasks; // 🎯 Utilisation de UniTask pour la sécurité du Main Thread
using UnityEngine;

namespace MortierFu
{
    public class WarmupManager : MonoBehaviour
    {
        [Header("Materials")]
        [SerializeField] private List<Material> materialsToWarmup = new();

        [Header("Particle Systems")]
        [SerializeField] private List<ParticleSystem> particlesToWarmup = new();
        
        [SerializeField] private ShaderVariantCollection shaderVariantCollectionToWarmup;

        [Header("Settings")]
        [Tooltip("Nombre de warmups par frame pour éviter les freeze")]
        [SerializeField] private int itemsPerFrame = 2;

        private Mesh _warmupMesh;
        private readonly Matrix4x4 _farMatrix = Matrix4x4.TRS(new Vector3(10000f, 10000f, 10000f), Quaternion.identity, Vector3.one);

        private void Awake()
        {
            // Génère un quad/cube simple en mémoire pour ne pas dépendre des assets internes
            GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _warmupMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempCube);
        }

        /// <summary>
        /// À appeler pendant l'écran de chargement
        /// </summary>
        public async UniTask WarmupAllAsync()
        {
            WarmupShaders();
            await WarmupMaterialsAsync();
            await WarmupParticlesAsync();
        }

        private async UniTask WarmupMaterialsAsync()
        {
            int counter = 0;

            for (int i = 0; i < materialsToWarmup.Count; i++)
            {
                Material mat = materialsToWarmup[i];
                if (mat == null) continue;

                WarmupMaterial(mat);
                counter++;

                if (counter >= itemsPerFrame)
                {
                    counter = 0;
                    await UniTask.Yield(); // Garantit le retour sur le Main Thread
                }
            }
        }

        private void WarmupMaterial(Material material)
        {
            // Compiles TOUTES les passes du shader (Shadows, Light, Pass de base, etc.)
            for (int pass = 0; pass < material.passCount; pass++)
            {
                if (material.SetPass(pass))
                {
                    Graphics.DrawMeshNow(_warmupMesh, _farMatrix);
                }
            }
        }

        private async UniTask WarmupParticlesAsync()
        {
            int counter = 0;

            // Création d'un conteneur temporaire hors-champ pour éviter d'instancier/détruire en boucle
            GameObject warmupContainer = new GameObject("Particle_Warmup_Container");
            warmupContainer.transform.position = new Vector3(10000f, 10000f, 10000f);

            try
            {
                for (int i = 0; i < particlesToWarmup.Count; i++)
                {
                    ParticleSystem psPrefab = particlesToWarmup[i];
                    if (psPrefab == null) continue;

                    var instance = Instantiate(psPrefab, warmupContainer.transform);
                    instance.Play(true);
                    instance.Simulate(0.05f, true, true);

                    counter++;

                    if (counter >= itemsPerFrame)
                    {
                        counter = 0;
                        await UniTask.Yield();
                    }
                }
            }
            finally
            {
                // Nettoyage unique de toutes les particules créées
                Destroy(warmupContainer);
            }
        }
        
        private void WarmupShaders()
        {
            if (!shaderVariantCollectionToWarmup.isWarmedUp)
            {
                shaderVariantCollectionToWarmup.WarmUp(); 
            }
        }
    }
}