using UnityEngine.AddressableAssets;
using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_CameraSettings", menuName = "Mortier Fu/Settings/Camera")]
    public class SO_CameraSettings : SO_SystemSettings
    {
        [Header("Parameters")]
        
        [Header("References")]
        public AssetReferenceGameObject CameraPrefab;
    }
}