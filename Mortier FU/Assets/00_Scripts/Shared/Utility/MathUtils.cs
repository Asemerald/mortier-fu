using System;

namespace MortierFu.Shared
{
    public static class MathUtils
    {
        public static float QuadraticEquation(float a, float b, float c, float sign)
        {
            return (-b + sign * MathF.Sqrt(b * b - 4 * a * c)) / (2 * a);
        }
    }
}