using System.Collections.Generic;
using MortierFu;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "GameConfig", menuName = "MortierFu/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Core FMOD Banks")] public AssetReference[] fmodBanks;

    [Header("Optional global assets")]
    public AssetReference AugmentPickupPrefab;
    public AssetReferenceT<SO_BombshellSettings> BombshellSettings;
    public List<AssetReference> globalPrefabs = new List<AssetReference>();
    public List<Texture> globalTextures = new List<Texture>();
}