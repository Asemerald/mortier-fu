using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private SO_AugmentProviderSettings _settings;
        
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

                int randIndex = Random.Range(0, augments.Count);
                var pulledAugment = augments[randIndex];
                
                // Remove the augment from its rarity list to prevent picking it up multiple times this batch.
                if (!_settings.AllowCopiesInBatch)
                {
                    int lastIndex = augments.Count - 1;
                    augments[randIndex] = augments[lastIndex];
                    augments.RemoveAt(lastIndex);
                }
                
                outAugments[i] = pulledAugment;
            }

            if (!_settings.AllowCopiesInBatch)
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
            var settingsRef = SystemManager.Config.AugmentProviderSettings;
            _settings = await AddressablesUtils.LazyLoadAsset(settingsRef);
            if (_settings == null) return;
            
            // Create the loot table
            var config = new LootTableConfig()
            {
                AllowDuplicates = false,
                RemoveOnPull = false
            };
            _rarityTable = new LootTable<E_AugmentRarity>(config);
            
            // Load all the rarity drop rates into the rarity loot table
            _rarityTable.PopulateLootBag(_settings.RarityDropRates);
            if(_settings.EnableDebug)
                Logs.Log($"Successfully populate the augment rarity loot table with {_rarityTable.TotalWeight} total weight.");
            
            await PopulateAugmentDictionary();
        }
        
        private async UniTask PopulateAugmentDictionary()
        {
            var handle = Addressables.LoadAssetsAsync<SO_AugmentLibrary>(k_augmentLibLabel);
            await handle;
            
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Logs.LogWarning($"Error occurred while loading Augment libs: {handle.OperationException.Message}");
                return;
            }

            _augmentsPerRarity = new Dictionary<E_AugmentRarity, List<SO_Augment>>();
            AugmentsPerRarity = new ReadOnlyDictionary<E_AugmentRarity, List<SO_Augment>>(_augmentsPerRarity);

            var libs = handle.Result;
            Addressables.Release(handle);
            
            foreach (var lib in libs)
            {
                foreach (var augment in lib.Augments)
                {
                    AddAugmentInDictionary(augment);
                }
                Logs.Log($"Successfully included augments from the following augment library: {lib.name}");
            }
        }
        
        private void AddAugmentInDictionary(SO_Augment augment)
        {
            var augmentRarity = augment.Rarity;
            
            // If it is the first augment of that rarity to be included, prepare an empty array.
            _augmentsPerRarity.TryAdd(augmentRarity, new List<SO_Augment>());

            // Then add this augment
            _augmentsPerRarity[augmentRarity].Add(augment);
        }

        public void Dispose()
        { }
        
        public bool IsInitialized { get; set; }
    }
}
