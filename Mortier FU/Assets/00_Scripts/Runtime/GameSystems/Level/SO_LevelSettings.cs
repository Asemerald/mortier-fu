using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_LevelSettings", menuName = "Mortier Fu/Settings/Level")]
    public class SO_LevelSettings : SO_SystemSettings
    {
        [Header("References")]
        public AssetReference AugmentMapScene;
    }
}
