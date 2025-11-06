using UnityEngine;

namespace MortierFu
{
    public abstract class SO_SystemSettings : ScriptableObject
    {
        [Header("Debugging")]
        public bool EnableDebug = true;
    }
}