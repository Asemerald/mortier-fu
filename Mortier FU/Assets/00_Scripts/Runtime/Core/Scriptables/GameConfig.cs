using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "GameConfig", menuName = "MortierFu/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Core FMOD Banks")] public AssetReference[] fmodBanks;

    [Header("Optional global assets")]
    public AssetReference AugmentPickupPrefab;
    public AssetReference BombshellPrefab;
    public List<AssetReference> globalPrefabs = new List<AssetReference>();
    public List<Texture> globalTextures = new List<Texture>();
}