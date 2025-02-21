// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators.Pattern
{
    public static class PatternEvaluator
    {
        private static double pNorm(double p, params double[] values) => Math.Pow(values.Sum(x => Math.Pow(x, p)), 1 / p);

        public static double EvaluateDifficultyOf(
            TaikoDifficultyHitObject hitObject,
            double hitWindowMs)
        {
            double rhythmMisalignment = hitObject.Pattern.Rhythm.CalculateMisalignment(hitWindowMs);
            rhythmMisalignment *= 0.66;

            double monoMisalignment = hitObject.Pattern.Mono?.CalculateMisalignment(hitWindowMs) ?? 0;

            double colourChangeMisalignment = hitObject.Pattern.ColourChange?.CalculateMisalignment(hitWindowMs) ?? 0;

            // Console.WriteLine($"Rhythm: {rhythmMisalignment}, Colour Change: {colourChangeMisalignment}, Mono: {monoMisalignment}");

            return pNorm(2, rhythmMisalignment, colourChangeMisalignment) + monoMisalignment;
        }
    }
}
