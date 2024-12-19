// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Scoring;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    /// <summary>
    /// Calculates the rhythm coefficient of taiko difficulty.
    /// </summary>
    public class Rhythm : StrainDecaySkill
    {
        protected override double SkillMultiplier => 16.0;
        protected override double StrainDecayBase => 0.0;

        private readonly double greatHitWindow;

        public Rhythm(IBeatmap beatmap, Mod[] mods, double clockRate)
            : base(mods)
        {
            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);
            greatHitWindow = hitWindows.WindowFor(HitResult.Great) / clockRate;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            return RhythmEvaluator.EvaluateDifficultyOf((TaikoDifficultyHitObject)current, greatHitWindow);
        }
    }
}
