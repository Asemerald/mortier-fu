using UnityEngine;

namespace MortierFu.Shared
{
    public static class ColorUtils
    {
        public static Color RandomizedHue()
        {
            return Color.HSVToRGB(Random.Range(0.0f, 1.0f), 1.0f, 1.0f);
        }
    }
}