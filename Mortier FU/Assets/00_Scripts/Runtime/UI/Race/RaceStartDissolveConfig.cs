using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "RaceStartDissolveConfig", menuName = "MortierFu/RaceStartDissolveConfig")]
    public class RaceStartDissolveConfig : ScriptableObject
    {
        public Vector2 dissolveCurrentOne;
        public Vector2 dissolveTargetOne;
        public Vector2 dissolveCurrentSecond;
        public Vector2 dissolveTargetSecond;
        public float durationLerp;
    }
}