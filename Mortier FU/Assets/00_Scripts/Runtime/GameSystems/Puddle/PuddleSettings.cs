using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_PuddleSettings", menuName = "Mortier Fu/Settings/Puddle")]
    public class SO_PuddleSettings : SO_SystemSettings
    {
        [Header("Parameters")]

        [Header("References")]
        public AssetReferenceGameObject PuddlePrefab;
    }
}