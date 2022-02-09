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

        private const double highSvLowerBound = 240;
        private const double highSvUpperBound = 320;

        private const double centreHigh = (highSvUpperBound - highSvLowerBound) / 360;
        private const double centreLow = (lowSvUpperBound - lowSvLowerBound) / 360;
        // Centre is the bpm corresponding to 0, so we need to subtract it from the bpm

        private const double scHigh = 4 * 180 / (highSvUpperBound - highSvLowerBound);
        private const double scLow = 4 * 180 / (lowSvUpperBound - lowSvLowerBound);
        // SC is used to normalize the domain so the value of the final function visibly changes in the range defined by [bpmL,bpmR]

        private const double lowSvLowerBound = 0;
        private const double lowSvUpperBound = 90;

        private const double svBonusExponentBase = Math.E;
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
            double highSvBonus = 1 / Math.Pow(svBonusExponentBase, -(sv - centreHigh) * scHigh);
            double lowSvBonus = 1 / Math.Pow(svBonusExponentBase, -(sv - centreLow) * scLow);

            return 1 + highSvMultiplier * highSvBonus + lowSvMultiplier * lowSvBonus;
        }
    }
}