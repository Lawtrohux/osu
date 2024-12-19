// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators.Rhythm
{
    public static class RhythmEvaluator
    {
        public static double EvaluateDifficultyOf(
            TaikoDifficultyHitObject hitObject,
            double hitWindowMs)
        {
            var alignmentField = new TaikoRhythmicAlignmentField(hitObject.Rhythm, 4, 0.7071, 0.7071);
            return alignmentField?.CalculateMisalignment(hitWindowMs) ?? 0;
        }
    }
}
