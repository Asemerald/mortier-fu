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
        private LootTable<E_AugmentRarity> _rarityTable;

        private Dictionary<E_AugmentRarity, List<SO_Augment>> _augmentsPerRarity;
        public ReadOnlyDictionary<E_AugmentRarity, List<SO_Augment>> AugmentsPerRarity;

        private AsyncOperationHandle<SO_AugmentProviderSettings> _settingsHandle;
        public SO_AugmentProviderSettings Settings => _settingsHandle.Result;

        private AsyncOperationHandle<IList<SO_AugmentLibrary>> _augmentLibHandle;

        private readonly Dictionary<SO_Augment, float> _augmentChances = new();

        private const string k_augmentLibLabel = "AugmentLib";

        // TODO: PLACEHOLDER - brancher sur le vrai système de progression de round (event, OnRoundStart, etc.)
        private int _currentRound = 1;

        public void SetCurrentRound(int round)
        {
            int roundsPassed = round - _currentRound;

            for (int r = 0; r < roundsPassed; r++)
            {
                RecoverChances();
            }

            _currentRound = round;
        }

        public void PopulateAugmentsNonAlloc(SO_Augment[] outAugments)
        {
            int length = outAugments.Length;
            var rarities = _rarityTable.BatchPull(length);
            var removedAugments = new List<(E_AugmentRarity rarity, SO_Augment augment)>();

            for (int i = 0; i < length; i++)
            {
                E_AugmentRarity rarity = ResolveAllowedRarity(rarities[i]);

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
        
        private E_AugmentRarity ResolveAllowedRarity(E_AugmentRarity pulledRarity)
        {
            if (!IsRarityLocked(pulledRarity))
                return pulledRarity;

            E_AugmentRarity fallback = pulledRarity;
            while (IsRarityLocked(fallback) && fallback > E_AugmentRarity.Common)
            {
                fallback--;
            }
            return fallback;
        }

        private bool IsRarityLocked(E_AugmentRarity rarity)
        {
            var lockEntry = Settings.RarityRoundLocks?.Find(l => l.Rarity == rarity);
            return lockEntry != null && _currentRound < lockEntry.MinRound;
        }

        public async UniTask OnInitialize()
        {
            _settingsHandle = await SystemManager.Config.AugmentProviderSettings.LazyLoadAssetRef();

          
            LootTableConfig config = new LootTableConfig()
            {
                AllowDuplicates = false,
                RemoveOnPull = false
            };
            _rarityTable = new LootTable<E_AugmentRarity>(config);
            _rarityTable.PopulateLootBag(Settings.RarityDropRates);
            
            await PopulateAugmentDictionary();
        }

        private async UniTask PopulateAugmentDictionary()
        {
            if (Settings.EnableDebug) ;

            _augmentLibHandle = Addressables.LoadAssetsAsync<SO_AugmentLibrary>(k_augmentLibLabel);
            await _augmentLibHandle;

            
            _augmentsPerRarity = new Dictionary<E_AugmentRarity, List<SO_Augment>>();
            AugmentsPerRarity = new ReadOnlyDictionary<E_AugmentRarity, List<SO_Augment>>(_augmentsPerRarity);

            foreach (SO_AugmentLibrary lib in _augmentLibHandle.Result)
            {
                foreach (SO_Augment augment in lib.Augments)
                {
                    AddAugmentInDictionary(augment);
                    _augmentChances[augment] = 1f;
                }
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
                Logs.LogWarning("[AugmentProviderSystem] Le poid est à 0");
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
            }

            if (!_augmentChances.TryGetValue(augment, out float previousChance))
            {
            }

            float damping = GetDampingForAugment(augment);
            float newChance = Mathf.Max(0f, previousChance * (1f - damping));

            _augmentChances[augment] = newChance;
            
            return true;
        }
        
        private float GetDampingForAugment(SO_Augment augment)
        {
            var rarityEntry = Settings.RarityDropRateDamping?.Find(e => e.Rarity == augment.Rarity);
            return Mathf.Clamp01(rarityEntry != null ? rarityEntry.DampingFactor : Settings.DropRateDamping);
        }
        
        private void RecoverChances()
        {
            float recovery = Mathf.Clamp01(Settings.DampingRecoveryRate);
            if (recovery <= 0f)
                return;

            var keys = new List<SO_Augment>(_augmentChances.Keys);
            foreach (var key in keys)
            {
                float current = _augmentChances[key];
                _augmentChances[key] = Mathf.Min(1f, current + recovery);
            }
        }
        
        public void ResetChances()
        {
            var keys = new List<SO_Augment>(_augmentChances.Keys);
            foreach (var key in keys)
                _augmentChances[key] = 1f;
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