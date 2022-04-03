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
        protected override double StrainDecayBase => 0.4;
        private const double highSvMultiplier = 1;
        private const double lowSvMultiplier = 1;

        public Reading(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            TaikoDifficultyHitObject taikoDifficultyHitObject = (TaikoDifficultyHitObject)current;
            return svBonus(taikoDifficultyHitObject);
        }

        private double svBonus(TaikoDifficultyHitObject current)
        {
            const double highSvUpperBound = 640;
            const double highSvLowerBound = 480;
            const double highSvCenter = (highSvUpperBound + highSvLowerBound) / 2;
            const double highSvWidth = highSvUpperBound - highSvLowerBound;

            // Center and width of delta time range for low sv calculation. We use delta time to determine density. The
            // lower the delta time (higher density), the higher the low sv bonus center. i.e. higher density = low sv 
            // bonus starts at higher sv
            const double lowSvDeltaTimeCenter = 200;
            const double lowSvDeltaTimeWidth = 300;
            // Maximum center for low sv (for high density)
            const double lowSvCenterUpperBound = 200;
            // Minimum center for low sv (for low density)
            const double lowSvCenterLowerBound = 100;

            const double lowSvWidth = 160;
            // Calculate low sv center, considering density
            double lowSvCenter = lowSvCenterUpperBound - (lowSvCenterUpperBound - lowSvCenterLowerBound) * this.sigmoid(current.DeltaTime, lowSvDeltaTimeCenter, lowSvDeltaTimeWidth);

            double highSvBonus = this.sigmoid(current.EffectiveBPM, highSvCenter, highSvWidth);
            double lowSvBonus = 1 - this.sigmoid(current.EffectiveBPM, lowSvCenter, lowSvWidth);

            return highSvMultiplier * highSvBonus + lowSvMultiplier * lowSvBonus;
        }

        private double sigmoid(double value, double center, double width)
        {
            width /= 10;
            return 1 / (1 + Math.Exp(-(value - center) / width));
        }
    }
}
