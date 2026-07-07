using System;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class PlayerCustomizationVisual : MonoBehaviour
    {
        [Header("Skins / Hats")]
        [SerializeField] private GameObject[] _availableSkins;
        [SerializeField] private GameObject[] _availableSkinOutlines;
        [SerializeField] private GameObject _crownInstance;

        [Header("Face")]
        [SerializeField] private SkinnedMeshRenderer _faceMeshRenderer;
        [SerializeField] private string _columnPropertyName = "_Column";
        [SerializeField] private string _rowPropertyName = "_Row";

        [Header("Debug")]
        [SerializeField] private bool _showDebugLogs;

        private Material _faceMaterialInstance;

        public int SkinCount => _availableSkins?.Length ?? 0;

        private GameModeBase _gameModeBase;

        private void Awake()
        {
            UpdateVisualsAfterRound(false);
            _gameModeBase = GameService.CurrentGameMode as GameModeBase;
        }

        private void OnEnable()
        {
            _gameModeBase ??= GameService.CurrentGameMode as GameModeBase;
            if (_gameModeBase == null)
            {
                Logs.LogWarning("No GameModeBase found; skipping visuals reset subscription.", this);
                return;
            }

            _gameModeBase.OnGameStarted += ResetVisualsOnGameStart;
        }

        private void OnDisable()
        {
            if (_gameModeBase == null)
                return;

            _gameModeBase.OnGameStarted -= ResetVisualsOnGameStart;
        }

        private void OnDestroy()
        {
            if (!_faceMaterialInstance) return;
            
            Destroy(_faceMaterialInstance);
            _faceMaterialInstance = null;
        }

        public void Apply(PlayerCustomizationData customization)
        {
            if (customization is null)
                return;

            Apply(
                customization.SkinIndex,
                customization.FaceColumn,
                customization.FaceRow
            );
        }

        public void Apply(int skinIndex, int faceColumn, int faceRow)
        {
            ApplySkin(skinIndex);
            ApplyFace(faceColumn, faceRow);
        }

        private void ApplySkin(int skinIndex)
        {
            if (_availableSkins == null || _availableSkins.Length == 0)
            {
                DebugLog("[PlayerCustomizationVisual] No skins assigned.");
                return;
            }

            for (int i = 0; i < _availableSkins.Length; i++)
            {
                if (_availableSkins[i])
                    _availableSkins[i].SetActive(false);
            }

            if (_availableSkinOutlines != null)
            {
                for (int i = 0; i < _availableSkinOutlines.Length; i++)
                {
                    if (_availableSkinOutlines[i])
                        _availableSkinOutlines[i].SetActive(false);
                }
            }

            if (skinIndex < 0 || skinIndex >= _availableSkins.Length)
            {
                Logs.LogWarning($"[PlayerCustomizationVisual] Skin index {skinIndex} is out of range.", this);
                return;
            }

            if (_availableSkins[skinIndex])
                _availableSkins[skinIndex].SetActive(true);

            if (_availableSkinOutlines != null &&
                skinIndex >= 0 &&
                skinIndex < _availableSkinOutlines.Length &&
                _availableSkinOutlines[skinIndex])
            {
                _availableSkinOutlines[skinIndex].SetActive(true);
            }

            DebugLog($"[PlayerCustomizationVisual] Applied skin {skinIndex}.");
        }

        private void ApplyFace(int column, int row)
        {
            if (!_faceMeshRenderer)
            {
                DebugLog("[PlayerCustomizationVisual] No face mesh renderer assigned.");
                return;
            }

            _faceMaterialInstance ??= _faceMeshRenderer.material;

            if (!_faceMaterialInstance)
            {
                Logs.LogWarning("[PlayerCustomizationVisual] Face material is null.", this);
                return;
            }

            if (_faceMaterialInstance.HasProperty(_columnPropertyName))
                _faceMaterialInstance.SetFloat(_columnPropertyName, column);
            else
                Logs.LogWarning($"[PlayerCustomizationVisual] Material has no property '{_columnPropertyName}'.", this);

            if (_faceMaterialInstance.HasProperty(_rowPropertyName))
                _faceMaterialInstance.SetFloat(_rowPropertyName, row);
            else
                Logs.LogWarning($"[PlayerCustomizationVisual] Material has no property '{_rowPropertyName}'.", this);

            DebugLog($"[PlayerCustomizationVisual] Applied face Column={column}, Row={row}.");
        }

        private void DebugLog(string message)
        {
            if (!_showDebugLogs)
                return;

            Logs.Log(message, this);
        }

        private void ResetVisualsOnGameStart() => UpdateVisualsAfterRound(false);
        
        public void UpdateVisualsAfterRound(bool isWinningGame)
        {
            // In the future, add VFX or other win feedback here.
            _crownInstance?.SetActive(isWinningGame);
        }
    }
}