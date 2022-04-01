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
        private const double highSvMultiplier = 0.2;
        private const double lowSvMultiplier = 0.2;

        public Reading(Mod[] mods) : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            TaikoDifficultyHitObject taikoDifficultyHitObject = (TaikoDifficultyHitObject)current;
            return svBonus(taikoDifficultyHitObject.EffectiveBPM);
        }

        private double svBonus(double sv)
        {
            const double highSvUpperBound = 320;
            const double highSvLowerBound = 240;
            const double highSvCenter = (highSvUpperBound + highSvLowerBound) / 2;
            const double highSvWidth = highSvUpperBound - highSvLowerBound;

            const double lowSvUpperBound = 90;
            const double lowSvLowerBound = 0;
            const double lowSvCenter = (lowSvUpperBound + lowSvLowerBound) / 2;
            const double lowSvWidth = lowSvUpperBound - lowSvLowerBound;

            double highSvBonus = this.sigmoid(sv, highSvCenter, highSvWidth);
            double lowSvBonus = 1 - this.sigmoid(sv, lowSvCenter, lowSvWidth);

            return 1 + highSvMultiplier * highSvBonus + lowSvMultiplier * lowSvBonus;
        }

        private double sigmoid(double value, double center, double width)
        {
            width /= 10;
            return 1 / Math.Exp(-(value - center) / width);
        }
    }
}