// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour.Data;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public class ColourEvaluator
    {
        /// <summary>
        /// Evaluate the difficulty of the first note of a <see cref="MonoStreak"/>.
        /// </summary>
        public static double EvaluateDifficultyOf(MonoStreak monoStreak)
        {
            return DifficultyCalculationUtils.Logistic(exponent: Math.E * monoStreak.Index - 2 * Math.E) * EvaluateDifficultyOf(monoStreak.Parent) * 0.5;
        }

        /// <summary>
        /// Evaluate the difficulty of the first note of a <see cref="AlternatingMonoPattern"/>.
        /// </summary>
        public static double EvaluateDifficultyOf(AlternatingMonoPattern alternatingMonoPattern)
        {
            return DifficultyCalculationUtils.Logistic(exponent: Math.E * alternatingMonoPattern.Index - 2 * Math.E) * EvaluateDifficultyOf(alternatingMonoPattern.Parent);
        }

        /// <summary>
        /// Evaluate the difficulty of the first note of a <see cref="RepeatingHitPatterns"/>.
        /// </summary>
        public static double EvaluateDifficultyOf(RepeatingHitPatterns repeatingHitPattern)
        {
            return 2 * (1 - DifficultyCalculationUtils.Logistic(exponent: Math.E * repeatingHitPattern.RepetitionInterval - 2 * Math.E));
        }

        /// <summary>
        /// Determines if the pattern of hit object intervals is consistent based on a given threshold.
        /// </summary>
        private static bool isConsistentPattern(TaikoDifficultyHitObject hitObject, double threshold = 0.1)
        {
            if (hitObject.Previous(1) is TaikoDifficultyHitObject previousHitObject)
            {
                double currentInterval = hitObject.StartTime - previousHitObject.StartTime;

                if (previousHitObject.Previous(1) is TaikoDifficultyHitObject twoBackHitObject)
                {
                    double previousInterval = previousHitObject.StartTime - twoBackHitObject.StartTime;

                    double ratio = currentInterval / previousInterval;
                    return Math.Abs(1 - ratio) <= threshold;
                }
            }

            return false;
        }

        public static double EvaluateDifficultyOf(DifficultyHitObject hitObject)
        {
            TaikoDifficultyHitObjectColour colour = ((TaikoDifficultyHitObject)hitObject).Colour;
            var taikoObject = (TaikoDifficultyHitObject)hitObject;
            double difficulty = 0.0d;

            if (colour.MonoStreak?.FirstHitObject == hitObject) // Difficulty for MonoStreak
                difficulty += EvaluateDifficultyOf(colour.MonoStreak);

            if (colour.AlternatingMonoPattern?.FirstHitObject == hitObject) // Difficulty for AlternatingMonoPattern
                difficulty += EvaluateDifficultyOf(colour.AlternatingMonoPattern);

            if (colour.RepeatingHitPattern?.FirstHitObject == hitObject) // Difficulty for RepeatingHitPattern
                difficulty += EvaluateDifficultyOf(colour.RepeatingHitPattern);

            if (isConsistentPattern(taikoObject))
            {
                difficulty *= 0.9;
            }

            return difficulty;
        }
    }
}
