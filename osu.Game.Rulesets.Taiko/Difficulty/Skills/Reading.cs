using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills 
{
    public class Reading : Skill 
    {
        private double squaredSvBonusSum = 0;
        private uint calculatedObjects = 0;

        public Reading(Mod[] mods) : base(mods)
        {
        }

        protected override void Process(DifficultyHitObject hitObject) 
        {
            TaikoDifficultyHitObject taikoDifficultyHitObject = (TaikoDifficultyHitObject)hitObject;

            squaredSvBonusSum += svBonus(taikoDifficultyHitObject.EffectiveBPM);
            ++calculatedObjects;
        }

        /// <summary>
        /// Returns the calculated difficulty value representing all <see cref="DifficultyHitObject"/>s that have been processed up to this point.
        /// </summary>
        public override double DifficultyValue()
        {
            return Math.Sqrt(this.squaredSvBonusSum / this.calculatedObjects);
        }


        // TODO: Probably wanna move these somewhere else
        // No idea what sc and centre mean so I'm just copying them here
        private const double highSvLowerBound = 240;
        private const double highSvUpperBound = 320;
        private const double scHigh = 4 * 180 / (highSvUpperBound - highSvLowerBound);
        private const double centreHigh = (highSvUpperBound - highSvLowerBound) / 360;
        private const double lowSvLowerBound = 0;
        private const double lowSvUpperBound = 90;
        private const double scLow = 4 * 180 / (lowSvUpperBound - lowSvLowerBound);
        private const double centreLow = (lowSvUpperBound - lowSvLowerBound) / 360;
        private const double svBonusExponentBase = Math.E;
        private const double highSvMulti = 0.2;
        private const double lowSvMulti = 0.2;
        
        private double svBonus(double sv) {
            double highSvBonus = 1 / Math.Pow(svBonusExponentBase, -(sv - centreHigh) * scHigh);
            double lowSvBonus = 1 / Math.Pow(svBonusExponentBase, -(sv - centreLow) * scLow);
            
            return 1 + highSvMulti * highSvBonus + lowSvMulti * lowSvBonus;
        }
    }
}