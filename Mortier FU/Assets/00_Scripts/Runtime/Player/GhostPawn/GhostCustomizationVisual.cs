using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class GhostCustomizationVisual : MonoBehaviour
    {
        [Header("Skin / Hat")]
        [SerializeField] private SkinnedMeshRenderer _customSkinMeshRenderer;
        [SerializeField] private Mesh[] _availableSkins;

        [Header("Face")]
        [SerializeField] private SkinnedMeshRenderer _faceRenderer;
        [SerializeField] private int _faceMaterialSlotIndex = 0;
        [SerializeField] private string _columnPropertyName = "_Column";
        [SerializeField] private string _rowPropertyName = "_Row";

        [Header("Optional Player Color")]
        [SerializeField] private bool _applyPlayerColorProperty;
        [SerializeField] private Renderer _playerColorRenderer;
        [SerializeField] private int _playerColorMaterialSlotIndex = 1;
        [SerializeField] private string _playerColorPropertyName = "_PlayerColor";

        private Material _faceMaterialInstance;
        private Material _playerColorMaterialInstance;

        private static readonly int k_defaultColumnProperty = Shader.PropertyToID("_Column");
        private static readonly int k_defaultRowProperty = Shader.PropertyToID("_Row");
        private static readonly int k_defaultPlayerColorProperty = Shader.PropertyToID("_PlayerColor");

        public void Apply(PlayerCustomizationData customization, int playerIndex)
        {
            if (customization == null)
                return;

            ApplySkin(customization.SkinIndex);
            ApplyFace(customization.FaceColumn, customization.FaceRow);

            if (_applyPlayerColorProperty)
                ApplyPlayerColor(playerIndex);
        }

        private void ApplySkin(int skinIndex)
        {
            if (!_customSkinMeshRenderer || _availableSkins == null || _availableSkins.Length == 0)
                return;

            if (skinIndex < 0 || skinIndex >= _availableSkins.Length)
            {
                Logs.LogWarning($"[GhostCustomizationVisual] Skin index {skinIndex} is out of range.", this);
                return;
            }

            _customSkinMeshRenderer.sharedMesh = _availableSkins[skinIndex];
        }

        private void ApplyFace(int column, int row)
        {
            Material faceMaterial = GetOrCreateFaceMaterial();

            if (!faceMaterial)
                return;

            int columnProperty = string.IsNullOrWhiteSpace(_columnPropertyName) ? k_defaultColumnProperty : Shader.PropertyToID(_columnPropertyName);

            int rowProperty = string.IsNullOrWhiteSpace(_rowPropertyName) ? k_defaultRowProperty : Shader.PropertyToID(_rowPropertyName);

            if (faceMaterial.HasProperty(columnProperty))
                faceMaterial.SetFloat(columnProperty, column);
            else
                Logs.LogWarning($"[GhostCustomizationVisual] Face material has no property '{_columnPropertyName}'.", this);

            if (faceMaterial.HasProperty(rowProperty))
                faceMaterial.SetFloat(rowProperty, row);
            else
                Logs.LogWarning($"[GhostCustomizationVisual] Face material has no property '{_rowPropertyName}'.", this);
        }

        private void ApplyPlayerColor(int playerIndex)
        {
            Material playerColorMaterial = GetOrCreatePlayerColorMaterial();

            if (!playerColorMaterial)
                return;

            int colorProperty = string.IsNullOrWhiteSpace(_playerColorPropertyName) ? k_defaultPlayerColorProperty : Shader.PropertyToID(_playerColorPropertyName);

            if (playerColorMaterial.HasProperty(colorProperty))
                playerColorMaterial.SetInt(colorProperty, playerIndex);
        }

        private Material GetOrCreateFaceMaterial()
        {
            if (_faceMaterialInstance)
                return _faceMaterialInstance;

            _faceMaterialInstance = CreateMaterialInstanceAtSlot(_faceRenderer, _faceMaterialSlotIndex, "Face");
            return _faceMaterialInstance;
        }

        private Material GetOrCreatePlayerColorMaterial()
        {
            if (_playerColorMaterialInstance)
                return _playerColorMaterialInstance;

            _playerColorMaterialInstance = CreateMaterialInstanceAtSlot(_playerColorRenderer, _playerColorMaterialSlotIndex, "PlayerColor");
            return _playerColorMaterialInstance;
        }

        private Material CreateMaterialInstanceAtSlot(Renderer targetRenderer, int slotIndex, string debugName)
        {
            if (!targetRenderer)
            {
                Logs.LogWarning($"[GhostCustomizationVisual] Missing {debugName} renderer.", this);
                return null;
            }

            Material[] materials = targetRenderer.sharedMaterials;

            if (slotIndex < 0 || slotIndex >= materials.Length)
            {
                Logs.LogWarning($"[GhostCustomizationVisual] Renderer '{targetRenderer.name}' has no material slot {slotIndex} for {debugName}.", this);

                return null;
            }

            Material sourceMaterial = materials[slotIndex];

            if (!sourceMaterial)
            {
                Logs.LogWarning($"[GhostCustomizationVisual] Material slot {slotIndex} is empty on '{targetRenderer.name}'.", this);
                return null;
            }

            Material instance = new(sourceMaterial);
            materials[slotIndex] = instance;
            targetRenderer.sharedMaterials = materials;

            return instance;
        }

        private void OnDestroy()
        {
            if (_faceMaterialInstance)
                Destroy(_faceMaterialInstance);

            if (_playerColorMaterialInstance)
                Destroy(_playerColorMaterialInstance);
        }
    }
}