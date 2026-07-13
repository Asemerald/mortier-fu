using System.Collections.Generic;
using UnityEngine;
namespace MortierFu
{
    [System.Serializable]
    public struct AugmentRarityRaceUnlock
    {
        public E_AugmentRarity Rarity;

        [Min(1)] public int UnlockFromRace;
    }

    [System.Serializable]
    public class PlayerCountRarityUnlocks
    {
        [Min(1)] public int PlayerCount;

        [Tooltip("Seuils de déblocage par rareté, pour ce nombre de joueurs.")]
        public List<AugmentRarityRaceUnlock> Unlocks = new();
    }

    [System.Serializable]
    public struct AugmentRarityDamping
    {
        public E_AugmentRarity Rarity;

        public float DampingFactor;
    }

    [CreateAssetMenu(fileName = "DA_AugmentProviderSettings", menuName = "Mortier Fu/Settings/Augment Provider")]
    public class SO_AugmentProviderSettings : SO_SystemSettings
    {
        [Header("Settings")]
        [Tooltip("Si activé, il peut y avoir plusieurs copies d'une augment dans un tas")]
        public bool AllowCopiesInBatch = false;

        [Tooltip("Réduit les chances d'une augment d'être pick, sert de sécurité si un damping arrive à 0")]
        public float DropRateDamping = 0.03f;
        
        [Range(0f, 1f)]
        public float DampingRecoveryRate = 0.02f;

        [Header("Drop Rates")] [Tooltip("Drop rate par rarity")]
        public List<LootTable<E_AugmentRarity>.LootTableEntry> RarityDropRates;

        [Header("Rarity Damping")]
        [Tooltip("Damping par rarity")]
        public List<AugmentRarityDamping> RarityDropRateDamping = new();

        [Tooltip("De combien l'augment peut revenir dans le pool")]
        public float DampingRecoveryRate = 0.02f;

        [Header("Rarity unlock par Race")] public bool UseRarityUnlocksByRace = true;

        [Tooltip("Une sous-liste par nombre de joueurs, chacune avec ses propres seuils de déblocage par rareté, Si doit vérifier les valeurs demander à Archi sur le GSheets")]
        public List<PlayerCountRarityUnlocks> RarityUnlocksByPlayerCount = new();

        [Tooltip("If enabled, falls back to the normal rarity table when no unlocked rarity is available.")]
        public bool FallbackToNormalRarityTableIfNoUnlockedRarity = false;
    }

    [System.Serializable]
    public class RarityRoundLock
    {
        public E_AugmentRarity Rarity;
        public int MinRound = 1; // Permet de lock une augment pour certains rounds
    }
}
