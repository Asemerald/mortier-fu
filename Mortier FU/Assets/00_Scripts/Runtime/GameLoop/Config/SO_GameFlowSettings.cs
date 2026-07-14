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

        [Header("Augment Race")]
        [Header("Augment Race Start")]
        [Tooltip("Délai avant de commencer le showcase des augments.")]
        public float AugmentStartShowcaseDelay = 0.3f;
        
        [Tooltip("Délai après la disparition de la confirmation avant d'afficher le RACE.")]
        public float AugmentRaceStartDelayAfterConfirmation = 0.1f;

        [Tooltip("Durée maximale pendant laquelle les joueurs peuvent récupérer une augment.")]
        public float AugmentRaceDuration = 20f;
        
        [Header("Augment Summary")]
        [Tooltip("Durée de l'augment summary après la race avant de passer au round.")]
        public float AugmentSummaryDuration = 4f;

        [Header("Round Start")]
        [Tooltip("Durée avant de rendre les inputs utilisables.")]
        public float RoundStartCountdown = 5f;
        
        [Header("Round End")]
        [Tooltip("Durée minimale du scoreboard. Il pourra durer plus longtemps si ses animations ne sont pas finies.")]
        public float ScoreboardMinimumDuration = 3f;
        
        [Tooltip("Durée du zoom sur le joueur.")]
        public float CameraZoomOnWinnerDuration = 1f;

        [Tooltip("Durée avant de faire la transition d'ouverture sur la race.")]
        public float RacePreloadDelay = 0.5f;
        
        [Tooltip("Valeur ajoutée à CameraZoomOnWinnerDuration avant de montrer le scoreboard.")]
        public float ShowScoreboardDelayFactor = 0.5f;
        
        [Tooltip("Durée de chaque transition.")]
        public float TransitionDuration = 0.3f;

        [Header("Race Modes")]
        public SO_RaceModeDefinition DefaultRaceModeDefinition;
    }
}