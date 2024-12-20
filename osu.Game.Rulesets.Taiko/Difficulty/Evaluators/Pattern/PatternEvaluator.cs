// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Pattern;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators.Pattern
{
    public static class PatternEvaluator
    {
        private static double pNorm(double p, params double[] values) => Math.Pow(values.Sum(x => Math.Pow(x, p)), 1 / p);

        public static double EvaluateDifficultyOf(
            TaikoDifficultyHitObject hitObject,
            double hitWindowMs)
        {
            double rhythmMisalignment =
                new TaikoRhythmicAlignmentField(hitObject.PatternData.NoteRhythm, 4, 0.7071, 0.7071)
                .CalculateMisalignment(hitWindowMs);
            rhythmMisalignment *= 0.66;

            double monoMisalignment =
                new TaikoRhythmicAlignmentField(hitObject.PatternData.MonoRhythm, 4, 0.5, 0.5)
                .CalculateMisalignment(hitWindowMs);

            double colourChangeMisalignment = 0;
            if (hitObject.PatternData.ColourChangeRhythm != null)
            {
                colourChangeMisalignment =
                    new TaikoRhythmicAlignmentField(hitObject.PatternData.ColourChangeRhythm, 4, 0.5, 0.7071)
                    .CalculateMisalignment(hitWindowMs);
            }

            // System.Console.WriteLine($"{hitObject.StartTime},{rhythmMisalignment},{monoMisalignment},{colourChangeMisalignment}");

            return pNorm(2, rhythmMisalignment, colourChangeMisalignment) + monoMisalignment;
        }
    }
}
