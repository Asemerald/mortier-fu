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
        public float AugmentShowcasePreConfirmationDelay = 2f;

        [Tooltip("Si true, tous les joueurs doivent confirmer avant de passer à la révélation / placement des augments.")]
        public bool RequireAllPlayersConfirmationBeforeAugmentReveal = true;

        [Header("Augment Race")]
        [Tooltip("Durée maximale pendant laquelle les joueurs peuvent courir pour récupérer un augment.")]
        public float AugmentRaceDuration = 20f;

        [Header("Augment Summary")]
        [Tooltip("Durée minimale du résumé des augments après la race, avant de passer au round.")]
        public float AugmentSummaryDuration = 4f;

        [Header("Round Start")]
        [Tooltip("Nombre affiché au départ du countdown. 3 = 3 / 2 / 1 / GO.")]
        public int RoundCountdownSeconds = 3;

        [Tooltip("Durée globale souhaitée de l'affichage 3 / 2 / 1 / GO.")]
        public float RoundCountdownTotalDuration = 4f;

        [Header("Round End")]
        [Tooltip("Durée du zoom / focus sur le gagnant du round avant le scoreboard.")]
        public float RoundWinnerFocusDuration = 3f;

        [Tooltip("Durée minimale du scoreboard. Il pourra durer plus longtemps si ses animations ne sont pas finies.")]
        public float ScoreboardMinimumDuration = 5f;

        [Header("End Game")]
        [Tooltip("Durée minimale de l'écran de fin de partie avant autorisation d'action joueur.")]
        public float EndGameMinimumDuration = 2f;

        [Header("Loading Mask Strategy")]
        [Tooltip("Si true, le scoreboard sert à cacher le chargement de la prochaine map race.")]
        public bool UseScoreboardAsRaceMapLoadCover = true;

        [Tooltip("Si true, le résumé d'augments sert à cacher le chargement de la prochaine map arena.")]
        public bool UseAugmentSummaryAsArenaMapLoadCover = true;

        [Tooltip("Ancien système : transitions vidéo. À terme, on le désactivera.")]
        public bool UseVideoTransitions = false;
    }
}