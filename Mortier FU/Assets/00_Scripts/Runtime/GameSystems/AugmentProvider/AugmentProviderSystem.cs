using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MortierFu
{
    public class AugmentProviderSystem : IGameSystem
    {
        /// <summary>
        /// Contains all the rarity and their drop rate, can be increased with new relative rarity by calling PopulateLootBag.
        /// </summary>
        private LootTable<E_AugmentRarity> _rarityTable;

        private Dictionary<E_AugmentRarity, List<SO_Augment>> _augmentsPerRarity;
        public ReadOnlyDictionary<E_AugmentRarity, List<SO_Augment>> AugmentsPerRarity;

        private AsyncOperationHandle<SO_AugmentProviderSettings> _settingsHandle;
        public SO_AugmentProviderSettings Settings => _settingsHandle.Result;

        private AsyncOperationHandle<IList<SO_AugmentLibrary>> _augmentLibHandle;

        private Dictionary<SO_Augment, float> _augmentChances = new();

        private const string k_augmentLibLabel = "AugmentLib";

        public void PopulateAugmentsNonAlloc(SO_Augment[] outAugments)
        {
            int length = outAugments.Length;
            var rarities = _rarityTable.BatchPull(length);

            for (int i = 0; i < length; i++)
            {
                E_AugmentRarity rarity = rarities[i];
                if (!_augmentsPerRarity.TryGetValue(rarity, out var augments))
                {
                    Logs.LogError($"No augment found of rarity {rarity} !");
                    outAugments[i] = null;
                    continue;
                }

                // TODO: SHOULD BE REMOVED WHEN SUFFICIENT AMOUNT OF AUGMENTS
                // Hard fix for empty augment lists
                if (augments.Count == 0)
                {
                    rarity = E_AugmentRarity.Rare;
                    if (!_augmentsPerRarity.TryGetValue(rarity, out augments))
                    {
                        Logs.LogError($"No augment found of rarity {rarity} !");
                        outAugments[i] = null;
                        continue;
                    }
                }
                
                int randIndex = WeightedRandomIndex(augments);
                var pulledAugment = augments[randIndex];

                // Remove the augment from its rarity list to prevent picking it up multiple times this batch.
                if (!Settings.AllowCopiesInBatch)
                {
                    int lastIndex = augments.Count - 1;
                    augments[randIndex] = augments[lastIndex];
                    augments.RemoveAt(lastIndex);
                }

                outAugments[i] = pulledAugment;
            }

            if (!Settings.AllowCopiesInBatch)
            {
                // Restore all pulled augments to their list
                foreach (var augment in outAugments)
                {
                    if (augment == null) continue;
                    AddAugmentInDictionary(augment);
                }
            }
        }

        public async UniTask OnInitialize()
        {
            // Load the system settings
            _settingsHandle = await SystemManager.Config.AugmentProviderSettings.LazyLoadAssetRef();

            // Create the loot table
            var config = new LootTableConfig()
            {
                AllowDuplicates = false,
                RemoveOnPull = false
            };
            _rarityTable = new LootTable<E_AugmentRarity>(config);

            // Load all the rarity drop rates into the rarity loot table
            _rarityTable.PopulateLootBag(Settings.RarityDropRates);
            if (Settings.EnableDebug)
                Logs.Log(
                    $"Successfully populate the augment rarity loot table with {_rarityTable.TotalWeight} total weight.");

            await PopulateAugmentDictionary();
        }

        private async UniTask PopulateAugmentDictionary()
        {
            if (Settings.EnableDebug)
                Logs.Log("Loading Augment Libraries...");

            _augmentLibHandle = Addressables.LoadAssetsAsync<SO_AugmentLibrary>(k_augmentLibLabel);
            await _augmentLibHandle;

            if (_augmentLibHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Logs.LogWarning(
                    $"Error occurred while loading Augment libs: {_augmentLibHandle.OperationException.Message}");
                return;
            }

            _augmentsPerRarity = new Dictionary<E_AugmentRarity, List<SO_Augment>>();
            AugmentsPerRarity = new ReadOnlyDictionary<E_AugmentRarity, List<SO_Augment>>(_augmentsPerRarity);

            foreach (var lib in _augmentLibHandle.Result)
            {
                foreach (var augment in lib.Augments)
                {
                    AddAugmentInDictionary(augment);
                    _augmentChances[augment] = 1f;
                }

                if (Settings.EnableDebug)
                    Logs.Log($"Successfully included augments from the following augment library: {lib.name}");
            }
        }

        private void AddAugmentInDictionary(SO_Augment augment)
        {
            var augmentRarity = augment.Rarity;

            if (!_augmentsPerRarity.ContainsKey(augmentRarity))
                _augmentsPerRarity.Add(augmentRarity, new List<SO_Augment>());

            // Then add this augment
            _augmentsPerRarity[augmentRarity].Add(augment);
        }

        private int WeightedRandomIndex(List<SO_Augment> augments)
        {
            float totalWeight = 0f;
            for (int i = 0; i < augments.Count; i++)
                totalWeight += _augmentChances.ContainsKey(augments[i]) ? _augmentChances[augments[i]] : 1f;

            float rand = Random.Range(0f, totalWeight);
            float current = 0f;

            for (int i = 0; i < augments.Count; i++)
            {
                float weight = _augmentChances.ContainsKey(augments[i]) ? _augmentChances[augments[i]] : 1f;
                current += weight;
                if (rand <= current)
                    return i;
            }

            return augments.Count - 1;
        }

        public void ApplyDamping(SO_Augment augment)
        {
            if (!_augmentChances.ContainsKey(augment)) return;
            
            _augmentChances[augment] *= Settings.DampingFactor;
            Debug.Log($"[Augment Damping] {augment.name} new chance: {_augmentChances[augment]}");
        }
        
        public void Dispose()
        {
            _rarityTable.Dispose();
            _augmentsPerRarity.Clear();

            Addressables.Release(_settingsHandle);

            if (_augmentLibHandle.IsValid())
                Addressables.Release(_augmentLibHandle);
        }

        public bool IsInitialized { get; set; }
    }
}