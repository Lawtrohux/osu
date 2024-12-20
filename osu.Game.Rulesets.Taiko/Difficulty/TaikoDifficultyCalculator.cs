// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
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
        private const double difficulty_multiplier = 0.0775;
        private const double stamina_skill_multiplier = 0.5 * difficulty_multiplier;
        private const double pattern_skill_multiplier = 0.5 * difficulty_multiplier;

        public override int Version => 20241007;

        public TaikoDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            return new Skill[]
            {
                new Pattern(beatmap, mods, clockRate),
                new Stamina(mods, false),
                new Stamina(mods, true)
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
            List<DifficultyHitObject> difficultyHitObjects = TaikoDifficultyHitObject.FromHitObjects(beatmap.HitObjects, clockRate);
            return difficultyHitObjects;
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new TaikoDifficultyAttributes { Mods = mods };

            Pattern pattern = (Pattern)skills.First(x => x is Pattern);
            Stamina stamina = (Stamina)skills.First(x => x is Stamina);
            Stamina singleColourStamina = (Stamina)skills.Last(x => x is Stamina);

            double patternRating = pattern.DifficultyValue() * pattern_skill_multiplier;
            double staminaRating = stamina.DifficultyValue() * stamina_skill_multiplier;
            double monoStaminaRating = singleColourStamina.DifficultyValue() * stamina_skill_multiplier;
            double monoStaminaFactor = staminaRating == 0 ? 1 : Math.Pow(monoStaminaRating / staminaRating, 5);

            double combinedRating = combinedDifficultyValue(pattern, stamina);
            double starRating = rescale(combinedRating * 1.4);

            // TODO: This is temporary measure as we don't detect abuse of multiple-input playstyles of converts within the current system.
            if (beatmap.BeatmapInfo.Ruleset.OnlineID == 0)
            {
                starRating *= 0.925;
            }

            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            TaikoDifficultyAttributes attributes = new TaikoDifficultyAttributes
            {
                StarRating = starRating,
                Mods = mods,
                StaminaDifficulty = staminaRating,
                MonoStaminaFactor = monoStaminaFactor,
                PatternDifficulty = patternRating,
                PeakDifficulty = combinedRating,
                GreatHitWindow = hitWindows.WindowFor(HitResult.Great) / clockRate,
                OkHitWindow = hitWindows.WindowFor(HitResult.Ok) / clockRate,
                MaxCombo = beatmap.GetMaxCombo(),
            };

            return attributes;
        }

        /// <summary>
        /// Applies a final re-scaling of the star rating.
        /// </summary>
        /// <param name="sr">The raw star rating value before re-scaling.</param>
        private double rescale(double sr)
        {
            if (sr < 0) return sr;

            return 10.43 * Math.Log(sr / 8 + 1);
        }

        /// <summary>
        /// Returns the combined star rating of the beatmap, calculated using peak strains from all sections of the map.
        /// </summary>
        /// <remarks>
        /// For each section, the peak strains of all separate skills are combined into a single peak strain for the section.
        /// The resulting partial rating of the beatmap is a weighted sum of the combined peaks (higher peaks are weighted more).
        /// </remarks>
        private double combinedDifficultyValue(Pattern patttern, Stamina stamina)
        {
            List<double> peaks = new List<double>();

            var staminaPeaks = stamina.GetCurrentStrainPeaks().ToList();
            var patternPeaks = patttern.GetCurrentStrainPeaks().ToList();

            for (int i = 0; i < staminaPeaks.Count; i++)
            {
                double patternPeak = patternPeaks[i] * pattern_skill_multiplier;
                double staminaPeak = staminaPeaks[i] * stamina_skill_multiplier;

                double peak = norm(2, patternPeak, staminaPeak);

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
        /// Returns the <i>p</i>-norm of an <i>n</i>-dimensional vector.
        /// </summary>
        /// <param name="p">The value of <i>p</i> to calculate the norm for.</param>
        /// <param name="values">The coefficients of the vector.</param>
        private double norm(double p, params double[] values) => Math.Pow(values.Sum(x => Math.Pow(x, p)), 1 / p);
    }
}
