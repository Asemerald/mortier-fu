using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_StormSettings", menuName = "Mortier Fu/Actors/Storm Settings")]
    public class SO_StormSettings : ScriptableObject
    {
        [Header("Settings")]
        public float MinRadius = 5;
        public float MaxRadius = 20;
        public float Speed = 0.6f;
        [Tooltip("The storm will damage all the players inside of it every 1f / TicksPerSeconds. Meaning that a 0.5 tick result in damage every 2 seconds.")]
        public float TicksPerSecond = 0.5f;
        public int DamageAmount;

        [Header("References")]
        public AssetReferenceGameObject StormPrefab;
    }
}
