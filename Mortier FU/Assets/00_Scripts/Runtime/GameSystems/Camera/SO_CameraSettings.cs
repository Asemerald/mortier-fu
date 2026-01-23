using UnityEngine.AddressableAssets;
using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_CameraSettings", menuName = "Mortier Fu/Settings/Camera")]
    public class SO_CameraSettings : SO_SystemSettings
    {
        [Header("Parameters")] [Header("Zoom settings")]
        public float MinOrthoSize = 15f;

        public float MaxOrthoSize = 25f;
        public float MinPlayersExtent = 5f;
        public float MaxPlayersExtent = 25f;
        
        [Header("Arena Fallback")]
        public float ArenaStopFollowExtent = 30f;
        public float ArenaResumeFollowExtent = 22f; 

        [Header("Smoothing")] public float PositionLerpSpeed = 5f;
        public float ZoomLerpSpeed = 5f;

        [Header("Default Position")] public Vector3 RacePosition = new (0, 10, -10);
        public Vector3 MapPosition = new (0, 10, -10);
        public float DefaultOrtho = 20f;

        [Header("References")] public AssetReferenceGameObject CameraPrefab;
    }
}