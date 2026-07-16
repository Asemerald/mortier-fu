using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MortierFu
{
    public sealed class FXService : IGameService
    {
        private const float k_surfaceFxOffset = 0.03f;

        private FXSceneLibrary _library;

        /// <summary>
        /// Will be used to stored fx on player and not fx spawn by player like BombShellPreview.
        /// A good exemple is like stun fx
        /// </summary>
        private readonly List<GameObject>[] _stockingPlayersFxs = new List<GameObject>[4];

        public bool IsInitialized { get; set; }

        public UniTask OnInitialize()
        {
            Logs.Log("[FXService] Initialized.");

            InitializeFxStock();
            
            return UniTask.CompletedTask;
        }

        public void RegisterLibrary(FXSceneLibrary library)
        {
            if (!library)
            {
                Logs.LogWarning("[FXService] Tried to register a null FXSceneLibrary.");
                return;
            }

            _library = library;

            Logs.Log($"[FXService] Registered FX library from scene object '{library.name}'.");
        }

        public void UnregisterLibrary(FXSceneLibrary library)
        {
            if (_library != library)
                return;

            _library = null;

            Logs.Log("[FXService] Unregistered FX library.");
        }

        public void PlayBombshellPreview(Vector3 position, Vector3 normal, float travelTime, float range)
        {
            if (!TryGetLibrary(out var library))
                return;

            if (!library.BombshellPreview)
            {
                Logs.LogWarning("[FXService] Bombshell preview prefab is missing.");
                return;
            }

            Vector3 surfaceNormal = GetSafeNormal(normal);
            Vector3 spawnPosition = position + surfaceNormal * k_surfaceFxOffset;
            Quaternion spawnRotation = Quaternion.identity;
            
            // 90 sur X pour le decal
            spawnRotation *= Quaternion.Euler(90f, 0f, 0f);

            Transform preview = Object.Instantiate(library.BombshellPreview, spawnPosition, spawnRotation).transform;
            
            var decalProjector = preview.GetComponent<DecalProjector>();

            // Taille initiale
            Vector3 size = decalProjector.size;
            size.x = 0.001f;
            size.y = 0.001f;
            size.z = 10f;
            decalProjector.size = size;

            float safeTravelTime = Mathf.Max(0.01f, travelTime);

            Tween.Custom(
                0f,
                range * 2f,
                duration: Mathf.Max(0.01f, safeTravelTime * 0.9f),
                onValueChange: v =>
                {
                    Vector3 newSize = decalProjector.size;
                    newSize.x = v;
                    newSize.y = v;
                    decalProjector.size = newSize;
                },
                ease: Ease.OutQuad
            );

            Object.Destroy(preview.gameObject, safeTravelTime);
        }

        public void PlayBombshellExplosion(Vector3 position, float range, int playerIndex)
        {
            PlayBombshellExplosion(position, Vector3.up, range, playerIndex);
        }

        private void PlayBombshellExplosion(Vector3 position, Vector3 normal, float range, int playerIndex)
        {
            if (!TryGetLibrary(out var library))
                return;

            var colors = library.BombshellExplosionColors;

            if (colors == null || colors.Length == 0)
            {
                Logs.LogWarning("[FXService] Bombshell explosion color prefabs are missing.");
                return;
            }

            if (playerIndex < 0 || playerIndex >= colors.Length)
            {
                Logs.LogError($"[FXService] Player index {playerIndex} is out of range for bombshell explosion colors.");
                return;
            }

            ParticleSystem explosionPrefab = colors[playerIndex];

            if (!explosionPrefab)
            {
                Logs.LogWarning($"[FXService] Bombshell explosion prefab for Player {playerIndex + 1} is missing.");
                return;
            }

            Vector3 surfaceNormal = GetSafeNormal(normal);
            Vector3 spawnPosition = position + surfaceNormal * k_surfaceFxOffset;
            Quaternion spawnRotation = GetSurfaceAlignedRotation(explosionPrefab, surfaceNormal);

            ParticleSystem ps = Object.Instantiate(
                explosionPrefab,
                spawnPosition,
                spawnRotation
            );

            ps.transform.localScale = Vector3.one * (range * 0.5f);

            ParticleSystem.MainModule main = ps.main;
            Object.Destroy(ps.gameObject, main.duration + main.startLifetime.constantMax + 1f);
        }

        public void PlayDashFX(Transform strikePoint, float size)
        {
            if (!strikePoint)
                return;

            if (!TryGetLibrary(out var library))
                return;

            if (!library.Dash)
            {
                Logs.LogWarning("[FXService] Dash prefab is missing.");
                return;
            }

            ParticleSystem strikeFX = Object.Instantiate(library.Dash, strikePoint);
            strikeFX.transform.localScale = Vector3.one * size;

            ParticleSystem.MainModule main = strikeFX.main;
            Object.Destroy(strikeFX.gameObject, main.duration + main.startLifetime.constantMax + 1f);
        }

        public void PlayStunFX(PlayerCharacter playerCharacter)
        {
            if (!playerCharacter)
                return;

            if (!TryGetLibrary(out FXSceneLibrary library))
                return;

            if (!library.Stun)
            {
                Logs.LogWarning("[FXService] Stun prefab is missing.");
                return;
            }

            ParticleSystem stunFX = Object.Instantiate(library.Stun, playerCharacter.gameObject.transform);
            stunFX.transform.localPosition = new Vector3(0f, 3f, 0f);
            stunFX.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            
            AddElementToFxStock(playerCharacter.Owner.PlayerIndex, stunFX.gameObject);

            ParticleSystem.MainModule main = stunFX.main;
            Object.Destroy(stunFX.gameObject, main.duration + main.startLifetime.constantMax + 1f);
        }

        public void PlayWaterExplosionFX(Vector3 hitPoint)
        {
            PlayWaterExplosionFX(hitPoint, Vector3.up);
        }

        private void PlayWaterExplosionFX(Vector3 hitPoint, Vector3 normal)
        {
            if (!TryGetLibrary(out var library))
                return;

            if (!library.BombshellWaterExplosion)
            {
                Logs.LogWarning("[FXService] Water explosion prefab is missing.");
                return;
            }

            Vector3 surfaceNormal = GetSafeNormal(normal);
            Vector3 spawnPosition = hitPoint + surfaceNormal * k_surfaceFxOffset;
            Quaternion spawnRotation = GetSurfaceAlignedRotation(library.BombshellWaterExplosion, surfaceNormal);

            ParticleSystem ps = Object.Instantiate(
                library.BombshellWaterExplosion,
                spawnPosition,
                spawnRotation
            );

            ParticleSystem.MainModule main = ps.main;
            Object.Destroy(ps.gameObject, main.duration + main.startLifetime.constantMax + 1f);
        }

        private bool TryGetLibrary(out FXSceneLibrary library)
        {
            library = _library;

            if (library)
                return true;

            Logs.LogWarning("[FXService] No FXSceneLibrary registered in the current scene.");
            return false;
        }

        private static Vector3 GetSafeNormal(Vector3 normal)
        {
            return normal.sqrMagnitude > 0.0001f
                ? normal.normalized
                : Vector3.up;
        }

        private static Quaternion GetSurfaceAlignedRotation(Transform prefabTransform, Vector3 normal)
        {
            Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, normal);
            return surfaceRotation * prefabTransform.rotation;
        }

        private static Quaternion GetSurfaceAlignedRotation(ParticleSystem prefab, Vector3 normal)
        {
            return GetSurfaceAlignedRotation(prefab.transform, normal);
        }

        public void Dispose()
        {
            _library = null;
            Logs.Log("[FXService] Disposed.");
        }

        private void AddElementToFxStock(int index, GameObject fx)
        {
            CheckForFxNull(index);
            _stockingPlayersFxs[index].Add(fx);
        }
        
        private void CheckForFxNull(int index)
        {
            _stockingPlayersFxs[index].RemoveAll(s => !s);
        }

        private void InitializeFxStock()
        {
            int count =  _stockingPlayersFxs.Length;
            for (int i = 0; i < count; i++)
                _stockingPlayersFxs[i] = new List<GameObject>();
        }
        
        public void Reset(PlayerCharacter playerCharacter)
        {
            int count = _stockingPlayersFxs[playerCharacter.Owner.PlayerIndex].Count;
            int index = playerCharacter.Owner.PlayerIndex;
            
            for (int i = 0; i < count; i++)
                Object.Destroy(_stockingPlayersFxs[index][i]);
            
            _stockingPlayersFxs[index].Clear();
        }
    }
}