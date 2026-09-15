using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class MainMenuPlayerRandomizer : MonoBehaviour
    {
        [Header("Skins / Hats")]
        [SerializeField] private SkinnedMeshRenderer _customSkinMeshRenderer;
        [SerializeField] private Mesh[] _availableSkins;
        [SerializeField] private SkinnedMeshRenderer _crownMeshRenderer;
        [SerializeField] private Material _customizationMaterial;

        [Header("Player")]
        [SerializeField] private SkinnedMeshRenderer _bodySkinnedMeshRenderer;
        [SerializeField] private SkinnedMeshRenderer _tailSkinnedMeshRenderer;

        [Header("Colors")]
        [SerializeField] private Material[] _playerMaterials;
        [SerializeField] private Material[] _outlineMaterials;

        [Header("Face")]
        [SerializeField] private int _faceColumnCount = 4;
        [SerializeField] private int _faceRowCount = 4;
        [SerializeField] private string _columnPropertyName = "_Column";
        [SerializeField] private string _rowPropertyName = "_Row";

        private Material _faceMaterialInstance;
        private Material _customMaterialInstance;

        private static readonly int ShaderPropColor = Shader.PropertyToID("_PlayerColor");

        private void Awake() => EnsureInitialized();

        private void OnEnable() => Randomize();

        private void OnDestroy()
        {
            if (_faceMaterialInstance)
            {
                Destroy(_faceMaterialInstance);
                _faceMaterialInstance = null;
            }

            if (!_customMaterialInstance) return;
            Destroy(_customMaterialInstance);
            _customMaterialInstance = null;
        }

        private void Randomize()
        {
            if (!CanRandomize())
                return;

            int colorIndex = Random.Range(0, _playerMaterials.Length);
            int skinIndex = Random.Range(0, _availableSkins.Length);

            int faceColumn = Random.Range(1, _faceColumnCount + 1);
            int faceRow = Random.Range(1, _faceRowCount + 1);

            ApplyColor(colorIndex);
            ApplySkin(skinIndex);
            ApplyFace(faceColumn, faceRow);
        }

        private void ApplySkin(int skinIndex)
        {
            if (!_customSkinMeshRenderer || _availableSkins == null || _availableSkins.Length == 0)
                return;

            _customSkinMeshRenderer.sharedMesh = _availableSkins[skinIndex];
        }

        private void ApplyFace(int column, int row)
        {
            if (!_bodySkinnedMeshRenderer)
                return;

            _faceMaterialInstance ??= _bodySkinnedMeshRenderer.materials[2];

            if (!_faceMaterialInstance)
            {
                Logs.LogWarning("[MainMenuPlayerRandomizer] Face material is null.", this);
                return;
            }

            if (_faceMaterialInstance.HasProperty(_columnPropertyName))
                _faceMaterialInstance.SetFloat(_columnPropertyName, column);

            if (_faceMaterialInstance.HasProperty(_rowPropertyName))
                _faceMaterialInstance.SetFloat(_rowPropertyName, row);
        }

        private void ApplyColor(int colorIndex)
        {
            _customMaterialInstance.SetInt(ShaderPropColor, colorIndex);

            ApplyBodyColor(colorIndex);
            ApplyTailColor(colorIndex);
            ApplyCustomColor(colorIndex);
            ApplyCrownColor(colorIndex);
        }

        private void ApplyBodyColor(int colorIndex)
        {
            if (!_bodySkinnedMeshRenderer)
                return;

            var materials = _bodySkinnedMeshRenderer.materials;

            if (materials.Length > 0)
                materials[0] = _outlineMaterials[colorIndex];

            if (materials.Length > 1)
                materials[1] = _playerMaterials[colorIndex];

            _bodySkinnedMeshRenderer.materials = materials;
        }

        private void ApplyTailColor(int colorIndex)
        {
            if (!_tailSkinnedMeshRenderer)
                return;

            var materials = _tailSkinnedMeshRenderer.materials;

            if (materials.Length > 0)
                materials[0] = _playerMaterials[colorIndex];

            if (materials.Length > 1)
                materials[1] = _outlineMaterials[colorIndex];

            _tailSkinnedMeshRenderer.materials = materials;
        }

        private void ApplyCustomColor(int colorIndex)
        {
            if (!_customSkinMeshRenderer)
                return;

            var materials = _customSkinMeshRenderer.materials;

            if (materials.Length > 0)
                materials[0] = _customMaterialInstance;

            if (materials.Length > 1)
                materials[1] = _outlineMaterials[colorIndex];

            _customSkinMeshRenderer.materials = materials;
        }

        private void ApplyCrownColor(int colorIndex)
        {
            if (!_crownMeshRenderer)
                return;

            var materials = _crownMeshRenderer.materials;

            if (materials.Length > 0)
                materials[0] = _customMaterialInstance;

            if (materials.Length > 1)
                materials[1] = _outlineMaterials[colorIndex];

            _crownMeshRenderer.materials = materials;
        }

        private void EnsureInitialized()
        {
            if (_customMaterialInstance)
                return;

            if (!_customizationMaterial)
            {
                Logs.LogWarning("[MainMenuPlayerRandomizer] Customization material is missing.", this);
                return;
            }

            _customMaterialInstance = new Material(_customizationMaterial);
        }

        private bool CanRandomize()
        {
            if (!_customMaterialInstance)
                return false;

            if (_availableSkins == null || _availableSkins.Length == 0)
                return false;

            if (_playerMaterials == null || _outlineMaterials == null)
                return false;

            if (_playerMaterials.Length != 0 && _playerMaterials.Length == _outlineMaterials.Length) return true;
            Logs.LogWarning("[MainMenuPlayerRandomizer] Player and outline material arrays must have the same size.", this);
            return false;

        }
    }
}