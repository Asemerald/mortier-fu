using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_GameConfig", menuName = "Mortier Fu/Game Config")]
    public class SO_GameConfig : ScriptableObject
    {
        [Header("Core FMOD Banks")] public AssetReference[] fmodBanks;

        [Header("Optional global assets")] 
        public AssetReferenceT<SO_LevelSettings> LevelSettings;
        public AssetReferenceT<SO_BombshellSettings> BombshellSettings;
        public AssetReferenceT<SO_AugmentProviderSettings> AugmentProviderSettings;
        public AssetReferenceT<SO_AugmentSelectionSettings> AugmentSelectionSettings;
        public AssetReferenceT<SO_StormSettings> StormSettings;
        public SO_AugmentDatabase AugmentDatabase;
        public List<AssetReference> globalPrefabs = new List<AssetReference>();
        public List<Texture> globalTextures = new List<Texture>();
    }   
}