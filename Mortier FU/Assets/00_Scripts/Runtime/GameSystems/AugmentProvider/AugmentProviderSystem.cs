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
        
        private AsyncOperationHandle<SO_AugmentProviderSettings> _settingsHandle;
        
        private const string k_augmentLibLabel = "AugmentLib";        
        
        public SO_AugmentProviderSettings Settings => _settingsHandle.Result;
        
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
            _settingsHandle = SystemManager.Config.AugmentProviderSettings.LoadAssetAsync();
            await _settingsHandle;

            if (_settingsHandle.Status != AsyncOperationStatus.Succeeded)
            {
                if (Settings.EnableDebug)
                {
                    Logs.LogError("[AugmentSelectionSystem]: Failed while loading settings with Addressables. Error: " + _settingsHandle.OperationException.Message);
                }
                return;
            }

            // Create the loot table
            var config = new LootTableConfig()
            {
                AllowDuplicates = false,
                RemoveOnPull = false
            };
            _rarityTable = new LootTable<E_AugmentRarity>(config);
            
            // Load all the rarity drop rates into the rarity loot table
            _rarityTable.PopulateLootBag(Settings.RarityDropRates);
            if(Settings.EnableDebug)
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
            foreach (var lib in libs)
            {
                foreach (var augment in lib.Augments)
                {
                    AddAugmentInDictionary(augment);
                }
                Logs.Log($"Successfully included augments from the following augment library: {lib.name}");
            }
            
            Addressables.Release(handle);
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
        {
            Addressables.Release(_settingsHandle);
        }
        
        public bool IsInitialized { get; set; }
    }
}
