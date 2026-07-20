using System;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MortierFu
{
    [Serializable]
    public struct GhostMaterialBinding
    {
        public SkinnedMeshRenderer Renderer;
        [Min(0)] public int SlotIndex;
        public Material Material;
    }

    [Serializable]
    public struct GhostAspectMaterials
    {
        public GhostMaterialBinding[] MaterialBindings;
        public GameObject SpawnVFXPrefab;
    }

    public sealed class GhostVisualComponent : GhostPawnComponent
    {
        private readonly GameObject _rootVisual;
        private readonly Dictionary<Material, Material> _runtimeMaterials = new();

        private GhostAspectMaterials AspectMaterials { get; set; }

        public GhostVisualComponent(PlayerGhostPawn pawn, GameObject rootVisual) : base(pawn) => _rootVisual = rootVisual;

        public override void Initialize()
        {
            Hide();

            if (!TryResolveAspectMaterials(out GhostAspectMaterials aspectMaterials))
                return;

            AspectMaterials = aspectMaterials;
            ApplyMaterialBindings();
        }

        public override void Reset() => Hide();

        public override void Dispose() => ClearRuntimeMaterials();

        public void PlaySpawnFeedback() => Show();

        private bool TryResolveAspectMaterials(out GhostAspectMaterials aspectMaterials)
        {
            aspectMaterials = default;

            if (Pawn.AspectMaterials == null || Pawn.AspectMaterials.Length == 0)
            {
                Logs.LogError("[GhostVisualComponent] No ghost aspect materials assigned.", Pawn);
                return false;
            }

            if (!Owner)
            {
                Logs.LogError("[GhostVisualComponent] Owner is missing.", Pawn);
                return false;
            }

            int playerIndex = Owner.PlayerIndex;

            if (playerIndex < 0 || playerIndex >= Pawn.AspectMaterials.Length)
            {
                Logs.LogError($"[GhostVisualComponent] No ghost aspect material found for player index {playerIndex}.", Pawn);
                return false;
            }

            aspectMaterials = Pawn.AspectMaterials[playerIndex];
            return true;
        }

        private void ApplyMaterialBindings()
        {
            if (AspectMaterials.MaterialBindings == null || AspectMaterials.MaterialBindings.Length == 0)
            {
                Logs.LogWarning("[GhostVisualComponent] No material bindings assigned.", Pawn);
                return;
            }

            for (int i = 0; i < AspectMaterials.MaterialBindings.Length; i++)
            {
                GhostMaterialBinding binding = AspectMaterials.MaterialBindings[i];

                if (!binding.Renderer)
                {
                    Logs.LogWarning($"[GhostVisualComponent] Material binding {i} has no renderer.", Pawn);
                    continue;
                }

                Material runtimeMaterial = GetOrCreateRuntimeMaterial(binding.Material);
                SetMaterialSlot(binding.Renderer, binding.SlotIndex, runtimeMaterial);
            }
        }

        private Material GetOrCreateRuntimeMaterial(Material source)
        {
            if (!source)
                return null;

            if (_runtimeMaterials.TryGetValue(source, out Material runtimeMaterial) && runtimeMaterial)
                return runtimeMaterial;

            runtimeMaterial = new Material(source);
            _runtimeMaterials[source] = runtimeMaterial;

            return runtimeMaterial;
        }

        private static void SetMaterialSlot(Renderer renderer, int slotIndex, Material material)
        {
            if (!renderer || !material)
                return;

            Material[] materials = renderer.sharedMaterials;

            if (slotIndex < 0 || slotIndex >= materials.Length)
            {
                Logs.LogWarning($"[GhostVisualComponent] Renderer '{renderer.name}' has no material slot {slotIndex}.", renderer);
                return;
            }

            materials[slotIndex] = material;
            renderer.sharedMaterials = materials;
        }

        private void Show()
        {
            if (_rootVisual)
                _rootVisual.SetActive(true);
        }

        public void Hide()
        {
            if (_rootVisual)
                _rootVisual.SetActive(false);
        }

        private void ClearRuntimeMaterials()
        {
            foreach (Material material in _runtimeMaterials.Values)
            {
                if (material)
                    Object.Destroy(material);
            }

            _runtimeMaterials.Clear();
        }
    }
}