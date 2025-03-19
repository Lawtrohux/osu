// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Skills;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Rulesets.Taiko.Scoring;

namespace osu.Game.Rulesets.Taiko.Difficulty
{
    public class TaikoDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 0.067;
        private const double stamina_skill_multiplier = 0.5 * difficulty_multiplier;
        private const double pattern_skill_multiplier = 0.5 * difficulty_multiplier;
        private const double reading_skill_multiplier = 0.1 * difficulty_multiplier;

        private double strainLengthBonus;

        private bool isConvert;

        public override int Version => 20241007;

        public TaikoDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            isConvert = beatmap.BeatmapInfo.Ruleset.OnlineID == 0;

            return new Skill[]
            {
                new Pattern(beatmap, mods, clockRate),
                new Reading(mods),
                new Stamina(mods, false, isConvert),
                new Stamina(mods, true, isConvert)
            };
        }

        protected override Mod[] DifficultyAdjustmentMods => new Mod[]
        {
            new TaikoModDoubleTime(),
            new TaikoModHalfTime(),
            new TaikoModEasy(),
            new TaikoModHardRock(),
        };

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, double clockRate)
        {
            List<DifficultyHitObject> difficultyHitObjects = TaikoDifficultyHitObject.FromHitObjects(
                beatmap.HitObjects,
                clockRate,
                beatmap.ControlPointInfo,
                beatmap.Difficulty.SliderMultiplier);
            return difficultyHitObjects;
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new TaikoDifficultyAttributes { Mods = mods };

            var pattern = skills.OfType<Pattern>().Single();
            var stamina = skills.OfType<Stamina>().Single(s => !s.SingleColourStamina);
            var singleColourStamina = skills.OfType<Stamina>().Single(s => s.SingleColourStamina);
            var reading = skills.OfType<Reading>().Single();

            bool isRelax = mods.Any(h => h is TaikoModRelax);

            double patternSkill = pattern.DifficultyValue() * pattern_skill_multiplier;
            double readingSkill = reading.DifficultyValue() * reading_skill_multiplier;

            double staminaSkill = stamina.DifficultyValue() * stamina_skill_multiplier;
            double monoStaminaSkill = singleColourStamina.DifficultyValue() * stamina_skill_multiplier;
            double monoStaminaFactor = staminaSkill == 0 ? 1 : Math.Pow(monoStaminaSkill / staminaSkill, 5);

            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            double staminaDifficultStrains = stamina.CountTopWeightedStrains();
            double patternDifficultStrains = pattern.CountTopWeightedStrains();

            strainLengthBonus = 1
                                + Math.Min(Math.Max((staminaDifficultStrains - 1000) / 3700, 0), 0.15)
                                + Math.Min(Math.Max((staminaSkill - 7.0) / 1.0, 0), 0.05);

            double combinedRating = combinedDifficultyValue(pattern, reading, stamina, isRelax, isConvert);
            double starRating = rescale(combinedRating * 1.4);

            // Calculate proportional contribution of each skill to the combinedRating.
            double skillRating = starRating / (patternSkill + readingSkill + staminaSkill);

            double patternDifficulty = patternSkill * skillRating;
            double readingDifficulty = readingSkill * skillRating;
            double staminaRating = staminaSkill * skillRating;

            TaikoDifficultyAttributes attributes = new TaikoDifficultyAttributes
            {
                StarRating = starRating,
                Mods = mods,
                ReadingDifficulty = readingDifficulty,
                StaminaDifficulty = staminaRating,
                MonoStaminaFactor = monoStaminaFactor,
                PatternDifficulty = patternDifficulty,
                StaminaTopStrains = staminaDifficultStrains,
                PatternTopStrains = patternDifficultStrains,
                MaxCombo = beatmap.GetMaxCombo(),
            };

            return attributes;
        }

        /// <summary>
        /// Returns the combined star rating of the beatmap, calculated using peak strains from all sections of the map.
        /// </summary>
        /// <remarks>
        /// For each section, the peak strains of all separate skills are combined into a single peak strain for the section.
        /// The resulting partial rating of the beatmap is a weighted sum of the combined peaks (higher peaks are weighted more).
        /// </remarks>
        private double combinedDifficultyValue(Pattern pattern, Reading reading, Stamina stamina, bool isRelax, bool isConvert)
        {
            List<double> peaks = new List<double>();

            var readingPeaks = reading.GetCurrentStrainPeaks().ToList();
            var staminaPeaks = stamina.GetCurrentStrainPeaks().ToList();
            var patternPeaks = pattern.GetCurrentStrainPeaks().ToList();

            for (int i = 0; i < staminaPeaks.Count; i++)
            {
                double patternPeak = patternPeaks[i] * pattern_skill_multiplier;
                double staminaPeak = staminaPeaks[i] * stamina_skill_multiplier * strainLengthBonus;
                double readingPeak = readingPeaks[i] * reading_skill_multiplier;

                // Available finger count is increased by 150%, thus we adjust accordingly.
                staminaPeak /= isConvert || isRelax ? 1.5 : 1.0;

                double peak = DifficultyCalculationUtils.Norm(2, patternPeak, staminaPeak, readingPeak);

                // Sections with 0 strain are excluded to avoid worst-case time complexity of the following sort (e.g. /b/2351871).
                // These sections will not contribute to the difficulty.
                if (peak > 0)
                    peaks.Add(peak);
            }

            double difficulty = 0;
            double weight = 1;

            foreach (double strain in peaks.OrderDescending())
            {
                difficulty += strain * weight;
                weight *= 0.9;
            }

            return difficulty;
        }

        /// <summary>
        /// Applies a final re-scaling of the star rating.
        /// </summary>
        /// <param name="sr">The raw star rating value before re-scaling.</param>
        private static double rescale(double sr)
        {
            if (sr < 0)
                return sr;

            return 10.43 * Math.Log(sr / 8 + 1);
        }
    }
}
