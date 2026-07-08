using System.Collections.Generic;
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

        private AsyncOperationHandle<SO_AugmentProviderSettings> _settingsHandle;
        public SO_AugmentProviderSettings Settings => _settingsHandle.Result;

        private AsyncOperationHandle<IList<SO_AugmentLibrary>> _augmentLibHandle;

        private readonly Dictionary<SO_Augment, float> _augmentChances = new();
        private readonly List<(E_AugmentRarity rarity, SO_Augment augment)> _removedAugments = new();
        private readonly List<E_AugmentRarity> _availableUnlockedRarities = new();

        private const string k_augmentLibLabel = "AugmentLib";

        // Sert à détecter les changements de round (recovery progressif) .
        private int _lastKnownRaceNumber = 1;

        public void PopulateAugmentsNonAlloc(SO_Augment[] outAugments, int raceNumber)
        {
            if (outAugments == null || outAugments.Length == 0)
                return;

            SyncRoundRecovery(raceNumber);

            var length = outAugments.Length;
            IReadOnlyList<E_AugmentRarity> normalRarities = _rarityTable.BatchPull(length);
            var useRaceUnlocks = ShouldUseRarityUnlocksByRace(raceNumber);

            _removedAugments.Clear();

            for (var i = 0; i < length; i++)
            {
                if (!TryResolveRarityForSlot(i, normalRarities, raceNumber, useRaceUnlocks, out var rarity) || !TryGetAugmentsForRarity(rarity, out var augments))
                {
                    outAugments[i] = null;
                    continue;
                }

                var randIndex = WeightedRandomIndex(augments);

                if (randIndex < 0)
                {
                    outAugments[i] = null;
                    continue;
                }

                var pulledAugment = augments[randIndex];

                if (!Settings.AllowCopiesInBatch)
                {
                    var lastIndex = augments.Count - 1;
                    augments[randIndex] = augments[lastIndex];
                    augments.RemoveAt(lastIndex);
                    _removedAugments.Add((rarity, pulledAugment));
                }

                outAugments[i] = pulledAugment;
            }

            RestoreRemovedAugments();

            if (Settings.EnableDebug && useRaceUnlocks)
                LogUnlockedRarities(raceNumber);
        }
        
        private void SyncRoundRecovery(int raceNumber)
        {
            if (raceNumber <= _lastKnownRaceNumber && !(raceNumber == 1 && _lastKnownRaceNumber == 1))
            {
                ResetChances();
                _lastKnownRaceNumber = raceNumber;
                return;
            }

            var roundsPassed = raceNumber - _lastKnownRaceNumber;

            for (var r = 0; r < roundsPassed; r++)
            {
                RecoverChances();
            }

            _lastKnownRaceNumber = raceNumber;
        }

        private bool ShouldUseRarityUnlocksByRace(int raceNumber)
        {
            if (raceNumber <= 0)
                return false;

            if (!Settings.UseRarityUnlocksByRace)
                return false;

            return Settings.RarityUnlocksByRace is { Count: > 0 };
        }

        private bool TryResolveRarityForSlot(int slotIndex, IReadOnlyList<E_AugmentRarity> normalRarities, int raceNumber, bool useRaceUnlocks, out E_AugmentRarity rarity)
        {
            if (!useRaceUnlocks)
                return TryResolveNormalRarityForSlot(slotIndex, normalRarities, out rarity);

            var normalRarity = normalRarities[slotIndex];

            if (IsRarityUnlockedForRace(normalRarity, raceNumber) &&
                TryGetAugmentsForRarity(normalRarity, out _))
            {
                rarity = normalRarity;
                return true;
            }

            if (TryGetRandomAvailableUnlockedRarity(raceNumber, out rarity))
                return true;

            if (Settings.FallbackToNormalRarityTableIfNoUnlockedRarity)
                return TryResolveNormalRarityForSlot(slotIndex, normalRarities, out rarity);

            rarity = default;
            Logs.LogWarning($"[AugmentProviderSystem] Pas de rareté disponible pour {raceNumber}.");
            return false;
        }

        private bool TryResolveNormalRarityForSlot(int slotIndex, IReadOnlyList<E_AugmentRarity> normalRarities, out E_AugmentRarity rarity)
        {
            rarity = normalRarities[slotIndex];

            if (TryGetAugmentsForRarity(rarity, out _))
                return true;

            rarity = E_AugmentRarity.Rare;
            return TryGetAugmentsForRarity(rarity, out _);
        }

        private bool TryGetRandomAvailableUnlockedRarity(int raceNumber, out E_AugmentRarity rarity)
        {
            _availableUnlockedRarities.Clear();

            for (var i = 0; i < Settings.RarityUnlocksByRace.Count; i++)
            {
                var unlock = Settings.RarityUnlocksByRace[i];

                if (!IsRarityUnlockedForRace(unlock.Rarity, raceNumber))
                    continue;

                if (!TryGetAugmentsForRarity(unlock.Rarity, out _))
                    continue;

                if (_availableUnlockedRarities.Contains(unlock.Rarity))
                    continue;

                _availableUnlockedRarities.Add(unlock.Rarity);
            }

            if (_availableUnlockedRarities.Count == 0)
            {
                rarity = default;
                return false;
            }

            rarity = _availableUnlockedRarities[Random.Range(0, _availableUnlockedRarities.Count)];
            return true;
        }

        private bool IsRarityUnlockedForRace(E_AugmentRarity rarity, int raceNumber)
        {
            if (!Settings.UseRarityUnlocksByRace)
                return true;

            if (Settings.RarityUnlocksByRace == null || Settings.RarityUnlocksByRace.Count == 0)
                return true;

            for (var i = 0; i < Settings.RarityUnlocksByRace.Count; i++)
            {
                var unlock = Settings.RarityUnlocksByRace[i];

                if (unlock.Rarity != rarity)
                    continue;

                var unlockFromRace = Mathf.Max(1, unlock.UnlockFromRace);
                return raceNumber >= unlockFromRace;
            }

            return false;
        }

        private bool TryGetAugmentsForRarity(E_AugmentRarity rarity, out List<SO_Augment> augments)
        {
            if (_augmentsPerRarity != null && _augmentsPerRarity.TryGetValue(rarity, out augments) && augments.Count > 0)
                return true;

            augments = null;
            return false;
        }

        private void RestoreRemovedAugments()
        {
            if (Settings.AllowCopiesInBatch)
                return;

            for (var i = 0; i < _removedAugments.Count; i++)
            {
                var removed = _removedAugments[i];

                if (_augmentsPerRarity.TryGetValue(removed.rarity, out List<SO_Augment> augments))
                    augments.Add(removed.augment);
            }

            _removedAugments.Clear();
        }

        public async UniTask OnInitialize()
        {
            _settingsHandle = await SystemManager.Config.AugmentProviderSettings.LazyLoadAssetRef();

            LootTableConfig config = new()
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
            if (Settings.EnableDebug)
                Logs.Log("Loading Augment Libraries...");

            _augmentLibHandle = Addressables.LoadAssetsAsync<SO_AugmentLibrary>(k_augmentLibLabel);
            await _augmentLibHandle;

            if (_augmentLibHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Logs.LogWarning($"Error: {_augmentLibHandle.OperationException.Message}");
                return;
            }

            _augmentsPerRarity = new Dictionary<E_AugmentRarity, List<SO_Augment>>();

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
            if (!augment)
                return;

            var augmentRarity = augment.Rarity;

            if (!_augmentsPerRarity.ContainsKey(augmentRarity))
                _augmentsPerRarity.Add(augmentRarity, new List<SO_Augment>());

            _augmentsPerRarity[augmentRarity].Add(augment);
        }

        private int WeightedRandomIndex(List<SO_Augment> augments)
        {
            if (augments == null || augments.Count == 0)
                return -1;

            var totalWeight = 0f;

            for (var i = 0; i < augments.Count; i++)
                totalWeight += GetAugmentWeight(augments[i]);

            if (totalWeight <= 0f)
            {
                Logs.LogWarning("[AugmentProviderSystem] All augment weights are zero. Falling back to uniform random.");
                return Random.Range(0, augments.Count);
            }

            var rand = Random.Range(0f, totalWeight);
            var current = 0f;

            for (var i = 0; i < augments.Count; i++)
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

            return _augmentChances.TryGetValue(augment, out var chance) ? Mathf.Max(0f, chance) : 1f;
        }

        public bool ApplyDamping(SO_Augment augment)
        {
            if (!augment)
            {
                Logs.LogWarning("[AugmentProviderSystem] Cannot apply damping to a null augment.");
                return false;
            }

            if (!_augmentChances.TryGetValue(augment, out var previousChance))
            {
                Logs.LogWarning($"[AugmentProviderSystem] Cannot apply damping to '{augment.name}' because it is not registered.");
                return false;
            }

            var damping = GetDampingForAugment(augment);
            var newChance = Mathf.Max(0f, previousChance * (1f - damping));

            _augmentChances[augment] = newChance;

            if (Settings.EnableDebug)
                Logs.Log($"[AugmentProviderSystem] Damping applied to '{augment.name}': " + $"{previousChance:0.###} -> {newChance:0.###} " + $"with damping {damping:0.###}.");

            return true;
        }
        
        private float GetDampingForAugment(SO_Augment augment)
        {
            var rarityDamping = Settings.RarityDropRateDamping;

            if (rarityDamping != null)
            {
                for (var i = 0; i < rarityDamping.Count; i++)
                {
                    if (rarityDamping[i].Rarity == augment.Rarity)
                        return Mathf.Clamp01(rarityDamping[i].DampingFactor);
                }
            }

            return Mathf.Clamp01(Settings.DropRateDamping);
        }
        
        private void RecoverChances()
        {
            var recovery = Mathf.Clamp01(Settings.DampingRecoveryRate);
            if (recovery <= 0f)
                return;

            var keys = new List<SO_Augment>(_augmentChances.Keys);
            foreach (var key in keys)
            {
                var current = _augmentChances[key];
                _augmentChances[key] = Mathf.Min(1f, current + recovery);
            }
        }
        
        public void ResetChances()
        {
            var keys = new List<SO_Augment>(_augmentChances.Keys);
            foreach (var key in keys)
                _augmentChances[key] = 1f;

            if (Settings.EnableDebug)
                Logs.Log("[AugmentProviderSystem] Augment chances reset to default.");
        }

        private void LogUnlockedRarities(int raceNumber)
        {
            _availableUnlockedRarities.Clear();

            for (var i = 0; i < Settings.RarityUnlocksByRace.Count; i++)
            {
                var unlock = Settings.RarityUnlocksByRace[i];

                if (IsRarityUnlockedForRace(unlock.Rarity, raceNumber))
                    _availableUnlockedRarities.Add(unlock.Rarity);
            }

            Logs.Log($"[AugmentProviderSystem] Race {raceNumber} unlocked rarities: " + $"{string.Join(", ", _availableUnlockedRarities)}.");
        }

        public void Dispose()
        {
            _rarityTable?.Dispose();
            _augmentsPerRarity?.Clear();
            _augmentChances.Clear();
            _removedAugments.Clear();
            _availableUnlockedRarities.Clear();

            if (_settingsHandle.IsValid())
                Addressables.Release(_settingsHandle);

            if (_augmentLibHandle.IsValid())
                Addressables.Release(_augmentLibHandle);
        }

        public bool IsInitialized { get; set; }
    }
}