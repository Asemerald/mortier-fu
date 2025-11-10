using UnityEngine;
using NaughtyAttributes;

namespace MortierFu.Shared
{
    [System.Serializable]
    public struct MinMaxRange
    {
        [BoxGroup("Range Values")]
        public float Min;
        [BoxGroup("Range Values")]
        public float Max;

        public MinMaxRange(float min, float max)
        {
            Min = min;
            Max = max;
        }
            
        public float GetRandomValue()
        {
            return Random.Range(Min, Max);
        }
    }
}