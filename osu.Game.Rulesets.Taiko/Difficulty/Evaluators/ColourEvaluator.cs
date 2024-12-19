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
        /// Calculates a consistency penalty based on the number of consecutive consistent intervals,
        /// considering the delta time between each colour sequence.
        /// </summary>
        /// <param name="hitObject">The current hitObject to consider.</param>
        /// <param name="threshold"> The allowable margin of error for determining whether intervals are consistent.</param>
        private static double consistentIntervalPenalty(TaikoDifficultyHitObject hitObject, double threshold = 0.01)
        {
            int consistentIntervalCount = 0;
            double totalDeltaTime = 0.0;

            TaikoDifficultyHitObject current = hitObject;

            while (current.Previous(1) is TaikoDifficultyHitObject previousHitObject)
            {
                double currentInterval = current.DeltaTime;
                double previousInterval = previousHitObject.DeltaTime;

                // If there's no valid hit object before the previous one, break the loop.
                if (previousHitObject.Previous(1) is not TaikoDifficultyHitObject)
                    break;

                // A Consistent Interval is defined as the percentage difference between the two intervals with the margin of error.
                if (Math.Abs(1 - currentInterval / previousInterval) <= threshold)
                {
                    consistentIntervalCount++;
                    totalDeltaTime += currentInterval;
                }

                current = previousHitObject;
            }

            // The penalty decreases as totalDeltaTime increases relative to consistentCount.
            double deltaPenalty = 1 - totalDeltaTime / (consistentIntervalCount + 1) * 0.0018;

            return 1.0 - (1 - deltaPenalty);
        }

        /// <summary>
        /// Evaluate the difficulty of the first hitobject within a colour streak.
        /// </summary>
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

            double consistencyPenalty = consistentIntervalPenalty(taikoObject);
            difficulty *= consistencyPenalty;

            return difficulty;
        }
    }
}
