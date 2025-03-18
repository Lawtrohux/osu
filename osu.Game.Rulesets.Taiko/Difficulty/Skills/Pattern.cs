// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.


using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators.Pattern;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Scoring;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    public class Pattern : StrainDecaySkill
    {
        protected override double SkillMultiplier => 0.37;

        protected override double StrainDecayBase => 0.4;

        private double greatHitWindowMs;

        private double targetHitWindow;

        public Pattern(IBeatmap beatmap, Mod[] mods, double clockRate) : base(mods)
        {
            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);
            greatHitWindowMs = hitWindows.WindowFor(HitResult.Great) / clockRate;
            targetHitWindow = hitWindows.WindowFor(HitResult.Ok) / clockRate / 2;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            return PatternEvaluator.EvaluateDifficultyOf((TaikoDifficultyHitObject)current, targetHitWindow);
        }
    }
}
