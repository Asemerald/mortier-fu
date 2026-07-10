using System;
using UnityEngine;

namespace MortierFu
{
    public sealed class RaceAugmentLayout
    {
        public readonly Transform Pivot;
        public readonly Vector3[] Points;
        public readonly bool ParentPointsToPivot;
        public readonly bool UseRotatorPrediction;
        public readonly float HeightOffset;

        public RaceAugmentLayout(Transform pivot, Vector3[] points, bool parentPointsToPivot, bool useRotatorPrediction, float heightOffset = 1f)
        {
            Pivot = pivot;
            Points = points;
            ParentPointsToPivot = parentPointsToPivot;
            UseRotatorPrediction = useRotatorPrediction;
            HeightOffset = heightOffset;
        }

        public bool IsValid(int expectedCount) => Points != null && Points.Length == expectedCount;

        public static RaceAugmentLayout FromLevelSystem(LevelSystem levelSystem, int augmentCount)
        {
            if (levelSystem == null || augmentCount <= 0)
                return new RaceAugmentLayout(null, Array.Empty<Vector3>(), false, false);

            var points = new Vector3[augmentCount];
            levelSystem.PopulateAugmentPoints(points);

            return new RaceAugmentLayout(levelSystem.GetAugmentPivot(), points, parentPointsToPivot: true, useRotatorPrediction: true);
        }
    }
}