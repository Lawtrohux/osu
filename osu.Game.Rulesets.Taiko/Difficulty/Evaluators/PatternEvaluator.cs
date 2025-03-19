// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public static class PatternEvaluator
    {
        private static double pNorm(double p, params double[] values) => Math.Pow(values.Sum(x => Math.Pow(x, p)), 1 / p);

        private static double monoEffectiveHitWindow(TaikoDifficultyHitObject hitObject, double hitWindowMs)
        {
            double? previousColourChange = hitObject.PreviousColourChange?.StartTime;
            double? nextColourChange = hitObject.NextColourChange?.StartTime;

            double window = hitWindowMs;

            if (previousColourChange is not null && nextColourChange is not null)
            {
                window = (nextColourChange.Value - previousColourChange.Value) / 2;
            }

            return double.Clamp(window, 1, hitWindowMs);
        }

        public static double EvaluateDifficultyOf(
            TaikoDifficultyHitObject hitObject,
            double hitWindowMs)
        {
            double rhythmMisalignment = hitObject.Pattern.Rhythm.CalculateMisalignment(hitWindowMs);
            rhythmMisalignment *= 2;

            double monoMisalignment = hitObject.Pattern.Mono?.CalculateMisalignment(monoEffectiveHitWindow(hitObject, hitWindowMs)) ?? 0;

            double colourChangeMisalignment = hitObject.Pattern.ColourChange?.CalculateMisalignment(hitWindowMs) ?? 0;

            // Console.WriteLine($"Rhythm: {rhythmMisalignment}, Colour Change: {colourChangeMisalignment}, Mono: {monoMisalignment}");

            return rhythmMisalignment + pNorm(2, monoMisalignment, colourChangeMisalignment);
        }
    }
}
