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
        public SO_AugmentProviderSettings Settings => _settingsHandle.Result;

        private AsyncOperationHandle<IList<SO_AugmentLibrary>> _augmentLibHandle;

        private readonly Dictionary<SO_Augment, float> _augmentChances = new();

        private const string k_augmentLibLabel = "AugmentLib";

        public void PopulateAugmentsNonAlloc(SO_Augment[] outAugments)
        {
            int length = outAugments.Length;
            var rarities = _rarityTable.BatchPull(length);
            var removedAugments = new List<(E_AugmentRarity rarity, SO_Augment augment)>();

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

                if (randIndex < 0)
                {
                    outAugments[i] = null;
                    continue;
                }

                SO_Augment pulledAugment = augments[randIndex];

                if (!Settings.AllowCopiesInBatch)
                {
                    int lastIndex = augments.Count - 1;
                    augments[randIndex] = augments[lastIndex];
                    augments.RemoveAt(lastIndex);
                    removedAugments.Add((rarity, pulledAugment));
                }

                outAugments[i] = pulledAugment;
            }

            if (!Settings.AllowCopiesInBatch)
            {
                foreach ((E_AugmentRarity rarity, SO_Augment augment) in removedAugments)
                {
                    if (_augmentsPerRarity.TryGetValue(rarity, out var augmentsList))
                    {
                        augmentsList.Add(augment);
                    }
                }
            }
        }

        public async UniTask OnInitialize()
        {
            // Load the system settings
            _settingsHandle = await SystemManager.Config.AugmentProviderSettings.LazyLoadAssetRef();

            // Create the loot table
            LootTableConfig config = new LootTableConfig()
            {
                AllowDuplicates = false,
                RemoveOnPull = false
            };
            _rarityTable = new LootTable<E_AugmentRarity>(config);

            // Load all the rarity drop rates into the rarity loot table
            _rarityTable.PopulateLootBag(Settings.RarityDropRates);
            if (Settings.EnableDebug)
                Logs.Log($"Successfully populate the augment rarity loot table with {_rarityTable.TotalWeight} total weight.");

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
                Logs.LogWarning($"Error occurred while loading Augment libs: {_augmentLibHandle.OperationException.Message}");
                return;
            }

            _augmentsPerRarity = new Dictionary<E_AugmentRarity, List<SO_Augment>>();
            AugmentsPerRarity = new ReadOnlyDictionary<E_AugmentRarity, List<SO_Augment>>(_augmentsPerRarity);

            foreach (SO_AugmentLibrary lib in _augmentLibHandle.Result)
            {
                foreach (SO_Augment augment in lib.Augments)
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
            E_AugmentRarity augmentRarity = augment.Rarity;

            if (!_augmentsPerRarity.ContainsKey(augmentRarity))
                _augmentsPerRarity.Add(augmentRarity, new List<SO_Augment>());

            // Then add this augment
            _augmentsPerRarity[augmentRarity].Add(augment);
        }

        private int WeightedRandomIndex(List<SO_Augment> augments)
        {
            if (augments is null || augments.Count == 0)
                return -1;

            float totalWeight = 0f;

            for (int i = 0; i < augments.Count; i++)
            {
                totalWeight += GetAugmentWeight(augments[i]);
            }

            if (totalWeight <= 0f)
            {
                Logs.LogWarning("[AugmentProviderSystem] All augment weights are zero. Falling back to uniform random.");
                return Random.Range(0, augments.Count);
            }

            float rand = Random.Range(0f, totalWeight);
            float current = 0f;

            for (int i = 0; i < augments.Count; i++)
            {
                current += GetAugmentWeight(augments[i]);

                if (rand <= current)
                    return i;
            }

            return augments.Count - 1;
        }

        private float GetAugmentWeight(SO_Augment augment)
        {
            if (!augment)
                return 0f;

            return _augmentChances.TryGetValue(augment, out float chance) ? Mathf.Max(0f, chance) : 1f;
        }

        public bool ApplyDamping(SO_Augment augment)
        {
            if (!augment)
            {
                Logs.LogWarning("[AugmentProviderSystem] Cannot apply damping to a null augment.");
                return false;
            }

            if (!_augmentChances.TryGetValue(augment, out float previousChance))
            {
                Logs.LogWarning($"[AugmentProviderSystem] Cannot apply damping to '{augment.name}' because it is not registered.");
                return false;
            }

            float damping = Mathf.Clamp01(Settings.DropRateDamping);
            float newChance = Mathf.Max(0f, previousChance * (1f - damping));

            _augmentChances[augment] = newChance;

            if (Settings.EnableDebug)
                Logs.Log($"[AugmentProviderSystem] Damping applied to '{augment.name}': " + $"{previousChance:0.###} -> {newChance:0.###} " + $"with damping {damping:0.###}.");

            return true;
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