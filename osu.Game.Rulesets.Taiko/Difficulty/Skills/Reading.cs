// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    public class Reading : StrainDecaySkill
    {
        protected override double SkillMultiplier => 1;
        protected override double StrainDecayBase => 0.1;

        private const double highSvMultiplier = 1;
        private const double lowSvMultiplier = 1.2;

        public Reading(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            TaikoDifficultyHitObject taikoDifficultyHitObject = (TaikoDifficultyHitObject)current;

            double objectStrain = svBonus(taikoDifficultyHitObject);

            objectStrain *= highSvMultiplier * lowSvMultiplier;

            return objectStrain;
        }

        private double svBonus(TaikoDifficultyHitObject current)
        {
            // High SV Variables
            const double highSvUpperBound = 640;
            const double highSvLowerBound = 480;
            const double highSvCenter = (highSvUpperBound + highSvLowerBound) / 2;
            const double highSvWidth = highSvUpperBound - highSvLowerBound;

            // Low SV Variables
            const double lowSvDeltaTimeCenter = 200;
            const double lowSvDeltaTimeWidth = 300;
            // Maximum center for low sv (for high density)
            const double lowSvCenterUpperBound = 200;
            // Minimum center for low sv (for low density)
            const double lowSvCenterLowerBound = 100;
            const double lowSvWidth = 160;

            // Calculate low sv center, considering density
            double lowSvCenter = lowSvCenterUpperBound - (lowSvCenterUpperBound - lowSvCenterLowerBound) * sigmoid(current.DeltaTime, lowSvDeltaTimeCenter, lowSvDeltaTimeWidth);

            double highSvBonus = sigmoid(current.EffectiveBPM, highSvCenter, highSvWidth);
            double lowSvBonus = 1 - sigmoid(current.EffectiveBPM, lowSvCenter, lowSvWidth);

            return highSvBonus + lowSvBonus;
        }

        private double sigmoid(double value, double center, double width)
        {
            width /= 10;
            return 1 / (1 + Math.Exp(-(value - center) / width));
        }
    }
}
