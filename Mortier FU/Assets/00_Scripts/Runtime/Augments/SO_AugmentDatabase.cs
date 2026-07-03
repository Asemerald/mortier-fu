using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_AugmentDatabase", menuName = "Mortier Fu/Augments/New Database")]
    public class SO_AugmentDatabase : ScriptableObject
    {
        [Header("Durability")]
        public AGM_RockSolid.Params RockSolidParams;
        public AGM_Toughness.Params ToughnessParams;
        public AGM_Vampire.Params  VampireParams;
        public AGM_WayFaster.Params WayFasterParams;
        
        [Header("Offensive")]
        public AGM_Ballista.Params BallistaParams;
        public AGM_Ascension.Params AscensionParams;
        public AGM_BigBullets.Params BigBulletsParams;
        public AGM_Bouncy.Params BouncyParams;
        public AGM_ChaoticBounce.Params ChaoticBounceParams;
        public AGM_BouncySnowball.Params BouncySnowballParams;
        public AGM_Confidence.Params ConfidenceParams;
        public AGM_ExtentedRange.Params ExtentedRangeParams;
        public AGM_FastReload.Params FastReloadParams;
        public AGM_GigaSharper.Params GigaSharperParams;
        public AGM_Gunslinger.Params GunslingerParams;
        public AGM_Impact.Params ImpactParams;
        public AGM_MaximumVelocity.Params MaximumVelocityParams;
        public AGM_Overheating.Params OverheatingParams;
        public AGM_RealSniper.Params RealSniperParams;
        public AGM_SharperBullets.Params SharperBulletsParams;
        public AGM_TakeTheTempo.Params TakeTheTempoParams;
        public AGM_TsarBomba.Params TsarBombaParams;
        
        [Header("Dash/Strike")]
        public AGM_BigStrike.Params BigStrikeParams;
        public AGM_Bully.Params BullyParams;
        public AGM_DoubleDash.Params DoubleDashParams;
        public AGM_FastDash.Params FastDashParams;
        public AGM_PerfectPush.Params PerfectPushParams;
        public AGM_Traveler.Params TravelerParams;
    }

}
