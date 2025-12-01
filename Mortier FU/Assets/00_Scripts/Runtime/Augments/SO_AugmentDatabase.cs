using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_AugmentDatabase", menuName = "Mortier Fu/Augments/New Database")]
    public class SO_AugmentDatabase : ScriptableObject
    {
        [Header("Durability")]
        public AGM_BigGuy.Params BigGuyParams;
        public AGM_HealthPack.Params HealthPackParams;
        
        [Header("Offensive")]
        public AGM_Ballista.Params BallistaParams;
        public AGM_Berserker.Params BerserkerParams;
        public AGM_BigBullets.Params BigBulletsParams;
        public AGM_Confidence.Params ConfidenceParams;
        public AGM_FastReload.Params FastReloadParams;
        public AGM_LuckyLuck.Params LuckyLuckParams;
        public AGM_MaximumVelocity.Params MaximumVelocityParams;
        public AGM_SharperBullets.Params SharperBulletsParams;
        public AGM_TsarBomba.Params TsarBombaParams;
        public AGM_Bouncy.Params BouncyParams;
        
        [Header("Strike")]
        public AGM_BigStrike.Params BigStrikeParams;
        public AGM_FastStrike.Params FastStrikeParams;
        public AGM_PerfectParry.Params PerfectParryParams;
    }

}
