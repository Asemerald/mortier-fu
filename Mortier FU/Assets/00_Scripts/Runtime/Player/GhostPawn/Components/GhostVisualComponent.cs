using System;
using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    [Serializable]
    public struct GhostAspectMaterials
    {
        public Material Material;
        public SkinnedMeshRenderer[] GhostMeshes;
        public GameObject SpawnVFXPrefab;
    }
    
    public sealed class GhostVisualComponent : GhostPawnComponent
    {
        private readonly GameObject _rootVisual;
        public GhostAspectMaterials AspectMaterials { get; private set; }
        public void SetAspectMaterials()
        {
            AspectMaterials = Pawn.AspectMaterials[Owner.PlayerIndex];
            
            SetGhostMaterial();
        }
        

        public GhostVisualComponent(PlayerGhostPawn pawn, GameObject rootVisual) : base(pawn)
        {
            _rootVisual = rootVisual;
        }

        public override void Initialize()
        {
            Hide();
            SetAspectMaterials();
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
        
        public void SetGhostMaterial()
        {
            foreach (var mesh in AspectMaterials.GhostMeshes)
            {
                mesh.material = AspectMaterials.Material;
            }
        }

        public void PlaySpawnFeedback() => Show();

        public override void Reset() => Hide();
    }
}