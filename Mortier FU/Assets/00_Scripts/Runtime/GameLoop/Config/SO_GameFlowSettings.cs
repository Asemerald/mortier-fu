using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(
        fileName = "DA_GameFlowSettings",
        menuName = "Mortier Fu/Settings/Game Flow"
    )]
    public sealed class SO_GameFlowSettings : ScriptableObject
    {
        [Header("Augment Showcase / Confirmation")]
        [Tooltip("Délai après l'apparition des cartes avant d'afficher la confirmation des joueurs.")]
        public float AugmentShowcasePreConfirmationDelay = 0f;

        [Tooltip("Si true, tous les joueurs doivent confirmer avant de passer à la révélation / placement des augments.")]
        public bool RequireAllPlayersConfirmationBeforeAugmentReveal = true;

        [Header("Augment Race")]
        [Tooltip("Durée maximale pendant laquelle les joueurs peuvent courir pour récupérer un augment.")]
        public float AugmentRaceDuration = 20f;
        
        [Header("Augment Race Start")]
        [Tooltip("Délai avant de commencer le showcase des augments.")]
        public float AugmentStartShowcaseDelay = 0.3f;
        
        [Tooltip("Délai après la disparition de la confirmation avant d'afficher le RACE.")]
        public float AugmentRaceStartDelayAfterConfirmation = 0.1f;

        [Header("Augment Summary")]
        [Tooltip("Durée minimale du résumé des augments après la race, avant de passer au round.")]
        public float AugmentSummaryDuration = 4f;

        [Header("Round End")]
        [Tooltip("Durée minimale du scoreboard. Il pourra durer plus longtemps si ses animations ne sont pas finies.")]
        public float ScoreboardMinimumDuration = 3f;

        [Header("End Game")]
        [Tooltip("Durée minimale de l'écran de fin de partie avant autorisation d'action joueur.")]
        public float EndGameMinimumDuration = 2f;
        
        [Header("Previous Winner Race Size")]
        [Tooltip("Si true, le gagnant du round aura une taille différente pendant la race.")]
        public bool EnablePreviousRoundWinnerRaceGiant = true;

        [Min(0.1f)]
        [Tooltip("La taille pendant la race du gagnant du round précédent.")]
        public float PreviousRoundWinnerRaceTargetSize = 3.5f;

        [Header("Loading Mask Strategy")]
        [Tooltip("Si true, le scoreboard sert à cacher le chargement de la prochaine map race.")]
        public bool UseScoreboardAsRaceMapLoadCover = true;

        [Tooltip("Si true, le résumé d'augments sert à cacher le chargement de la prochaine map arena.")]
        public bool UseAugmentSummaryAsArenaMapLoadCover = true;

        [Tooltip("Ancien système, à terme, on le désactivera.")]
        public bool UseVideoTransitions;
    }
}