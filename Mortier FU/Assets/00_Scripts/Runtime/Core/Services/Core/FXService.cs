using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using PrimeTween;
using UnityEngine;
using Object = UnityEngine.Object;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace MortierFu
{
    public sealed class FXService : IGameService
    {
        private FXSceneLibrary _library;

        public bool IsInitialized { get; set; }

        public UniTask OnInitialize()
        {
            Logs.Log("[FXService] Initialized.");
            return UniTask.CompletedTask;
        }

        public void RegisterLibrary(FXSceneLibrary library)
        {
            if (library == null)
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

        public void PlayBombshellPreview(Vector3 position, float travelTime, float range)
        {
            if (!TryGetLibrary(out var library))
                return;

            if (library.BombshellPreview == null)
            {
                Logs.LogWarning("[FXService] Bombshell preview prefab is missing.");
                return;
            }

            var preview = Object.Instantiate(
                library.BombshellPreview,
                position + new Vector3(0f, 0.1f, 0f),
                Quaternion.identity
            );

            preview.transform.localScale = Vector3.zero;

            Tween.Scale(
                preview.transform,
                Vector3.one * (range * 2f),
                duration: travelTime * 0.9f,
                ease: Ease.OutQuad
            );

            var main = preview.main;

            if (travelTime > 0f)
            {
                main.simulationSpeed = 1f / travelTime;
                Object.Destroy(preview.gameObject, travelTime + 1f);
            }
            else
            {
                Object.Destroy(preview.gameObject, main.duration + 1f);
            }
        }

        public void PlayBombshellExplosion(Vector3 position, float range, int playerIndex)
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

            if (colors[playerIndex] == null)
            {
                Logs.LogWarning($"[FXService] Bombshell explosion prefab for Player {playerIndex + 1} is missing.");
                return;
            }

            var ps = Object.Instantiate(
                colors[playerIndex],
                position,
                Quaternion.identity
            );

            ps.transform.localScale = Vector3.one * (range * 0.5f);

            var main = ps.main;
            Object.Destroy(ps.gameObject, main.duration + main.startLifetime.constantMax + 1f);
        }

        public void PlayDashFX(Transform strikePoint, float size)
        {
            if (strikePoint == null)
                return;

            if (!TryGetLibrary(out var library))
                return;

            if (library.Dash == null)
            {
                Logs.LogWarning("[FXService] Dash prefab is missing.");
                return;
            }

            var strikeFX = Object.Instantiate(library.Dash, strikePoint);
            strikeFX.transform.localScale = Vector3.one * size;

            var main = strikeFX.main;
            Object.Destroy(strikeFX.gameObject, main.duration + main.startLifetime.constantMax + 1f);
        }

        public void PlayWaterExplosionFX(Vector3 hitPoint)
        {
            if (!TryGetLibrary(out var library))
                return;

            if (library.BombshellWaterExplosion == null)
            {
                Logs.LogWarning("[FXService] Water explosion prefab is missing.");
                return;
            }

            var ps = Object.Instantiate(
                library.BombshellWaterExplosion,
                hitPoint,
                library.BombshellWaterExplosion.transform.rotation
            );

            var main = ps.main;
            Object.Destroy(ps.gameObject, main.duration + main.startLifetime.constantMax + 1f);
        }

        private bool TryGetLibrary(out FXSceneLibrary library)
        {
            library = _library;

            if (library != null)
                return true;

            Logs.LogWarning("[FXService] No FXSceneLibrary registered in the current scene.");
            return false;
        }

        public void Dispose()
        {
            _library = null;
            Logs.Log("[FXService] Disposed.");
        }
    }
}