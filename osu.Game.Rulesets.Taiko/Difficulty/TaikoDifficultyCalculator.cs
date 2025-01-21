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
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Reading;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm.Data;
using osu.Game.Rulesets.Taiko.Difficulty.Skills;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Rulesets.Taiko.Scoring;

namespace osu.Game.Rulesets.Taiko.Difficulty
{
    public class TaikoDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 0.084375;
        private const double rhythm_skill_multiplier = 0.65 * difficulty_multiplier;
        private const double reading_skill_multiplier = 0.1 * difficulty_multiplier;
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
            var bpmLoader = new EffectiveBPMPreprocessor(beatmap, noteObjects);

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
                    difficultyHitObjects.Count
                ));
            }

            var groupedHitObjects = SameRhythmHitObjects.GroupHitObjects(noteObjects);

            TaikoColourDifficultyPreprocessor.ProcessAndAssign(difficultyHitObjects);
            SamePatterns.GroupPatterns(groupedHitObjects);
            bpmLoader.ProcessEffectiveBPM(beatmap.ControlPointInfo, clockRate);

            return difficultyHitObjects;
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new TaikoDifficultyAttributes { Mods = mods };

            bool isRelax = mods.Any(h => h is TaikoModRelax);

            var rhythm = skills.OfType<Rhythm>().Single();
            var reading = skills.OfType<Reading>().Single();
            var colour = skills.OfType<Colour>().Single();
            var stamina = skills.OfType<Stamina>().Single(s => !s.SingleColourStamina);
            var singleColourStamina = skills.OfType<Stamina>().Single(s => s.SingleColourStamina);

            double rhythmRating = rhythm.DifficultyValue() * rhythm_skill_multiplier;
            double readingRating = reading.DifficultyValue() * reading_skill_multiplier;
            double colourRating = colour.DifficultyValue() * colour_skill_multiplier;
            double staminaRating = stamina.DifficultyValue() * stamina_skill_multiplier;
            double monoStaminaRating = singleColourStamina.DifficultyValue() * stamina_skill_multiplier;
            double monoStaminaFactor = staminaRating == 0 ? 1 : Math.Pow(monoStaminaRating / staminaRating, 5);

            double colourDifficultStrains = colour.CountTopWeightedStrains();
            double rhythmDifficultStrains = rhythm.CountTopWeightedStrains();
            double staminaDifficultStrains = stamina.CountTopWeightedStrains();

            patternMultiplier = Math.Pow(staminaRating * colourRating, 0.10);
            strainLengthBonus = 1
                                + Math.Min(Math.Max((staminaDifficultStrains - 1000) / 3700, 0), 0.15)
                                + Math.Min(Math.Max((staminaRating - 7.0) / 1.0, 0), 0.05);

            var peaks = new List<double>();
            var rhythmPeaks = rhythm.GetCurrentStrainPeaks().ToList();
            var readingPeaks = reading.GetCurrentStrainPeaks().ToList();
            var colourPeaks = colour.GetCurrentStrainPeaks().ToList();
            var staminaPeaks = stamina.GetCurrentStrainPeaks().ToList();

            for (int i = 0; i < colourPeaks.Count; i++)
            {
                double rhythmPeak = rhythmPeaks[i] * rhythm_skill_multiplier * patternMultiplier;
                double readingPeak = readingPeaks[i] * reading_skill_multiplier;
                double colourPeak = isRelax ? 0 : colourPeaks[i] * colour_skill_multiplier;
                double staminaPeak = staminaPeaks[i] * stamina_skill_multiplier * strainLengthBonus;

                if (isConvert || isRelax)
                    staminaPeak /= 1.5;

                double peak = DifficultyCalculationUtils.Norm(2, DifficultyCalculationUtils.Norm(1.5, colourPeak, staminaPeak), rhythmPeak, readingPeak);

                if (peak > 0)
                    peaks.Add(peak);
            }

            double difficulty = 0;
            double weight = 1;

            foreach (double strain in peaks.OrderByDescending(p => p))
            {
                difficulty += strain * weight;
                weight *= 0.9;
            }

            double starRating = rescale(difficulty * 1.4);

            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            return new TaikoDifficultyAttributes
            {
                StarRating = starRating,
                Mods = mods,
                RhythmDifficulty = rhythmRating,
                ReadingDifficulty = readingRating,
                ColourDifficulty = colourRating,
                StaminaDifficulty = staminaRating,
                MonoStaminaFactor = monoStaminaFactor,
                RhythmTopStrains = rhythmDifficultStrains,
                ColourTopStrains = colourDifficultStrains,
                StaminaTopStrains = staminaDifficultStrains,
                GreatHitWindow = hitWindows.WindowFor(HitResult.Great) / clockRate,
                OkHitWindow = hitWindows.WindowFor(HitResult.Ok) / clockRate,
                MaxCombo = beatmap.GetMaxCombo(),
            };
        }

        private double rescale(double sr) => sr < 0 ? sr : 10.43 * Math.Log(sr / 8 + 1);
    }
}
