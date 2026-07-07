using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_LevelSettings", menuName = "Mortier Fu/Settings/Level")]
    public class SO_LevelSettings : SO_SystemSettings
    {
        [Header("Map Randomization")]
        [Tooltip("Nombre avant qu'une map ne soit remise dans le pool.")]
        public int NbOfUnavailabilityRounds = 3;
    }
}