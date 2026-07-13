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
        
        private int _lastKnownRaceNumber = 1;

        public void PopulateAugmentsNonAlloc(SO_Augment[] outAugments, int raceNumber, int playerCount)
        {
            int roundsPassed = round - _currentRound;

            SyncRoundRecovery(raceNumber);

            var length = outAugments.Length;
            IReadOnlyList<E_AugmentRarity> normalRarities = _rarityTable.BatchPull(length);
            var useRaceUnlocks = ShouldUseRarityUnlocksByRace(raceNumber);

            _currentRound = round;
        }

        public void PopulateAugmentsNonAlloc(SO_Augment[] outAugments)
        {
            int length = outAugments.Length;
            var rarities = _rarityTable.BatchPull(length);
            var removedAugments = new List<(E_AugmentRarity rarity, SO_Augment augment)>();

            for (int i = 0; i < length; i++)
            {
                if (!TryResolveRarityForSlot(i, normalRarities, raceNumber, playerCount, useRaceUnlocks, out var rarity) || !TryGetAugmentsForRarity(rarity, out var augments))
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

            RestoreRemovedAugments();

            if (Settings.EnableDebug && useRaceUnlocks)
                LogUnlockedRarities(raceNumber, playerCount);
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

            return Settings.RarityUnlocksByPlayerCount is { Count: > 0 };
        }

        private bool TryResolveRarityForSlot(int slotIndex, IReadOnlyList<E_AugmentRarity> normalRarities, int raceNumber, int playerCount, bool useRaceUnlocks, out E_AugmentRarity rarity)
        {
            if (!useRaceUnlocks)
                return TryResolveNormalRarityForSlot(slotIndex, normalRarities, out rarity);

            var normalRarity = normalRarities[slotIndex];

            if (IsRarityUnlockedForRace(normalRarity, raceNumber, playerCount) &&
                TryGetAugmentsForRarity(normalRarity, out _))
            {
                foreach ((E_AugmentRarity rarity, SO_Augment augment) in removedAugments)
                {
                    if (_augmentsPerRarity.TryGetValue(rarity, out var augmentsList))
                    {
                        augmentsList.Add(augment);
                    }
                }
            }

            if (TryGetRandomAvailableUnlockedRarity(raceNumber, playerCount, out rarity))
                return true;

            if (Settings.FallbackToNormalRarityTableIfNoUnlockedRarity)
                return TryResolveNormalRarityForSlot(slotIndex, normalRarities, out rarity);

            rarity = default;
            Logs.LogWarning($"[AugmentProviderSystem] Pas de rareté disponible pour la race {raceNumber} ({playerCount} joueurs).");
            return false;
        }
        
        private E_AugmentRarity ResolveAllowedRarity(E_AugmentRarity pulledRarity)
        {
            if (!IsRarityLocked(pulledRarity))
                return pulledRarity;

            if (TryGetAugmentsForRarity(rarity, out _))
                return true;

            rarity = E_AugmentRarity.Rare;
            return TryGetAugmentsForRarity(rarity, out _);
        }
        
        private List<AugmentRarityRaceUnlock> GetUnlocksForPlayerCount(int playerCount)
        {
            var groups = Settings.RarityUnlocksByPlayerCount;

            if (groups == null)
                return null;

            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i].PlayerCount == playerCount)
                    return groups[i].Unlocks;
            }

            return null;
        }

        private bool TryGetRandomAvailableUnlockedRarity(int raceNumber, int playerCount, out E_AugmentRarity rarity)
        {
            _availableUnlockedRarities.Clear();

            var unlocks = GetUnlocksForPlayerCount(playerCount);

            if (unlocks != null)
            {
                for (var i = 0; i < unlocks.Count; i++)
                {
                    var unlock = unlocks[i];

                    if (!IsRarityUnlockedForRace(unlock.Rarity, raceNumber, playerCount))
                        continue;

                    if (!TryGetAugmentsForRarity(unlock.Rarity, out _))
                        continue;

                    if (_availableUnlockedRarities.Contains(unlock.Rarity))
                        continue;

                    _availableUnlockedRarities.Add(unlock.Rarity);
                }
            }
            return fallback;
        }
        
        private bool IsRarityUnlockedForRace(E_AugmentRarity rarity, int raceNumber, int playerCount)
        {
            if (!Settings.UseRarityUnlocksByRace)
                return true;

            var unlocks = GetUnlocksForPlayerCount(playerCount);

            if (unlocks == null || unlocks.Count == 0)
                return true;

            for (var i = 0; i < unlocks.Count; i++)
            {
                var unlock = unlocks[i];

                if (unlock.Rarity != rarity)
                    continue;

                var unlockFromRace = Mathf.Max(1, unlock.UnlockFromRace);
                return raceNumber >= unlockFromRace;
            }

            return false;
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

            var damping = GetDampingForAugment(augment);
            var newChance = Mathf.Max(0f, previousChance * (1f - damping));

            _augmentChances[augment] = newChance;
            
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
                Logs.Log("[AugmentProviderSystem] Augment chances reset to default");
        }

        private void LogUnlockedRarities(int raceNumber, int playerCount)
        {
            _availableUnlockedRarities.Clear();

            var unlocks = GetUnlocksForPlayerCount(playerCount);

            if (unlocks != null)
            {
                for (var i = 0; i < unlocks.Count; i++)
                {
                    var unlock = unlocks[i];

                    if (IsRarityUnlockedForRace(unlock.Rarity, raceNumber, playerCount))
                        _availableUnlockedRarities.Add(unlock.Rarity);
                }
            }

            Logs.Log($"[AugmentProviderSystem] Race {raceNumber} ({playerCount} players) unlocked rarities: {string.Join(", ", _availableUnlockedRarities)}.");
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