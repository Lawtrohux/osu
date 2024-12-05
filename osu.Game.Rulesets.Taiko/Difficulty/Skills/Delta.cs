// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    /// <summary>
    /// Calculates the reading coefficient of taiko difficulty.
    /// </summary>
    public class Delta : StrainDecaySkill
    {
        protected override double SkillMultiplier => 1.0;
        protected override double StrainDecayBase => 0.4;

        private const double bin_size = 0.1;

        private double currentStrain;

        public Delta(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            if (current is not TaikoDifficultyHitObject taikoDifficultyObject)
                return 0.0;

            // Drum Rolls and Swells are exempt.
            if (taikoDifficultyObject.BaseObject is not Hit)
                return 0.0;

            currentStrain *= StrainDecayBase;
            currentStrain += DeltaEvaluator.EvaluateDifficultyOf(taikoDifficultyObject, bin_size) * SkillMultiplier;

            return currentStrain;
        }
    }
}
