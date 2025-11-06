using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_BombshellSettings", menuName = "Mortier Fu/Settings/Bombshell")]
    public class SO_BombshellSettings : SO_SystemSettings
    {
        [Header("Parameters")]
        [Tooltip("Determine if the players can damage themselves with their own bombshells.")]
        public bool AllowSelfDamage = true;
        public float BombshellHeight = 8f;

        [Header("References")]
        public AssetReferenceGameObject BombshellPrefab;
    }
}