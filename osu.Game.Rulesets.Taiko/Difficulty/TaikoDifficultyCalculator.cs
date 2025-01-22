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
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm;
using osu.Game.Rulesets.Taiko.Difficulty.Skills;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Rulesets.Taiko.Scoring;
using static osu.Game.Rulesets.Difficulty.Utils.DifficultyCalculationUtils;

namespace osu.Game.Rulesets.Taiko.Difficulty
{
    public class TaikoDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 0.084375;
        private const double rhythm_skill_multiplier = 0.65 * difficulty_multiplier;
        private const double reading_skill_multiplier = 0.100 * difficulty_multiplier;
        private const double colour_skill_multiplier = 0.375 * difficulty_multiplier;
        private const double stamina_skill_multiplier = 0.445 * difficulty_multiplier;

        private double strainLengthBonus;
        private double patternMultiplier;

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
                new Rhythm(mods, hitWindows.WindowFor(HitResult.Great) / clockRate),
                new Reading(mods),
                new Colour(mods),
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
            var difficultyHitObjects = new List<DifficultyHitObject>();
            var centreObjects = new List<TaikoDifficultyHitObject>();
            var rimObjects = new List<TaikoDifficultyHitObject>();
            var noteObjects = new List<TaikoDifficultyHitObject>();

            // Generate TaikoDifficultyHitObjects from the beatmap's hit objects.
            for (int i = 2; i < beatmap.HitObjects.Count; i++)
            {
                difficultyHitObjects.Add(new TaikoDifficultyHitObject(
                    beatmap.HitObjects[i],
                    beatmap.HitObjects[i - 1],
                    beatmap.HitObjects[i - 2],
                    clockRate,
                    difficultyHitObjects,
                    centreObjects,
                    rimObjects,
                    noteObjects,
                    difficultyHitObjects.Count,
                    beatmap.ControlPointInfo,
                    beatmap.Difficulty.SliderMultiplier
                ));
            }

            TaikoColourDifficultyPreprocessor.ProcessAndAssign(difficultyHitObjects);
            TaikoRhythmDifficultyPreprocessor.ProcessAndAssign(noteObjects);

            return difficultyHitObjects;
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new TaikoDifficultyAttributes { Mods = mods };

            bool isRelax = mods.Any(h => h is TaikoModRelax);

            var rhythm = skills.OfType<Rhythm>().First();
            var reading = skills.OfType<Reading>().First();
            var colour = skills.OfType<Colour>().First();
            var stamina = skills.OfType<Stamina>().First(s => !s.SingleColourStamina);
            var singleColourStamina = skills.OfType<Stamina>().Last(s => s.SingleColourStamina);

            double colourDifficulty = colour.DifficultyValue() * colour_skill_multiplier;
            double staminaDifficulty = stamina.DifficultyValue() * stamina_skill_multiplier;
            double monoStaminaDifficulty = Math.Pow(singleColourStamina.DifficultyValue() * stamina_skill_multiplier / staminaDifficulty, 5);

            // As we don't have pattern integration in osu!taiko, we apply the other two skills relative to rhythm.
            patternMultiplier = Math.Pow(staminaDifficulty * colourDifficulty, 0.10);

            strainLengthBonus = 1
                                + Math.Min(Math.Max((stamina.CountTopWeightedStrains() - 1000) / 3700, 0), 0.15)
                                + Math.Min(Math.Max((staminaDifficulty - 7.0) / 1.0, 0), 0.05);

            // Compute strain peaks
            var strainPeaks = computeStrainPeaks(rhythm, reading, colour, stamina, isRelax);

            double rhythmRating = strainPeaks.rhythmPeaks.Sum();
            double readingRating = strainPeaks.readingPeaks.Sum();
            double colourRating = strainPeaks.colourPeaks.Sum();
            double staminaRating = strainPeaks.staminaPeaks.Sum();

            double totalDifficulty = rhythmRating + readingRating + colourRating + staminaRating;
            double starRating = rescale(combinedDifficultyValue(strainPeaks) * 1.4);

            if (totalDifficulty > 0)
            {
                rhythmRating = rhythmRating / totalDifficulty * starRating;
                readingRating = readingRating / totalDifficulty * starRating;
                colourRating = colourRating / totalDifficulty * starRating;
                staminaRating = staminaRating / totalDifficulty * starRating;
            }

            // Initialize hit windows for difficulty attributes
            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            // Return difficulty attributes
            TaikoDifficultyAttributes attributes = new TaikoDifficultyAttributes
            {
                StarRating = starRating,
                Mods = mods,
                RhythmDifficulty = rhythmRating,
                ReadingDifficulty = readingRating,
                ColourDifficulty = colourRating,
                StaminaDifficulty = staminaRating,
                MonoStaminaFactor = monoStaminaDifficulty,
                RhythmTopStrains = rhythm.CountTopWeightedStrains(),
                ColourTopStrains = colour.CountTopWeightedStrains(),
                StaminaTopStrains = stamina.CountTopWeightedStrains(),
                GreatHitWindow = hitWindows.WindowFor(HitResult.Great) / clockRate,
                OkHitWindow = hitWindows.WindowFor(HitResult.Ok) / clockRate,
                MaxCombo = beatmap.GetMaxCombo(),
            };

            return attributes;
        }

        /// <summary>
        /// Computes strain peaks for each skill.
        /// </summary>
        private (List<double> rhythmPeaks, List<double> readingPeaks, List<double> colourPeaks, List<double> staminaPeaks) computeStrainPeaks(
            Rhythm rhythm, Reading reading, Colour colour, Stamina stamina, bool isRelax)
        {
            // Common logic for computing peaks
            List<double> computeSkillPeaks(IEnumerable<double> peaks, double multiplier, Func<double, double>? adjust = null)
            {
                var adjustedPeaks = adjust != null ? peaks.Select(adjust) : peaks;
                return adjustedPeaks.Select(p => p * multiplier).ToList();
            }

            return (
                computeSkillPeaks(rhythm.GetCurrentStrainPeaks(), rhythm_skill_multiplier, p => p * patternMultiplier),
                computeSkillPeaks(reading.GetCurrentStrainPeaks(), reading_skill_multiplier),
                computeSkillPeaks(colour.GetCurrentStrainPeaks(), colour_skill_multiplier, p => isRelax ? 0 : p),
                computeSkillPeaks(stamina.GetCurrentStrainPeaks(), stamina_skill_multiplier, p => p * strainLengthBonus / (isConvert || isRelax ? 1.5 : 1.0))
            );
        }

        /// <summary>
        /// Combines strain peaks into a weighted difficulty value.
        /// </summary>
        private double combinedDifficultyValue((List<double> rhythmPeaks, List<double> readingPeaks, List<double> colourPeaks, List<double> staminaPeaks) strainPeaks)
        {
            var peaks = new List<double>();

            for (int i = 0; i < strainPeaks.colourPeaks.Count; i++)
            {
                double rhythmPeak = strainPeaks.rhythmPeaks[i];
                double readingPeak = strainPeaks.readingPeaks[i];
                double colourPeak = strainPeaks.colourPeaks[i];
                double staminaPeak = strainPeaks.staminaPeaks[i];

                double peak = Norm(2, Norm(1.5, colourPeak, staminaPeak), rhythmPeak, readingPeak);
                if (peak > 0)
                    peaks.Add(peak);
            }

            return calculateWeightedDifficulty(peaks);
        }

        /// <summary>
        /// Calculates the weighted difficulty value from peaks.
        /// </summary>
        private double calculateWeightedDifficulty(List<double> peaks)
        {
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
