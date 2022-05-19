// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Taiko.Difficulty
{
    public class TaikoPerformanceCalculator : PerformanceCalculator
    {
        private int countGreat;
        private int countOk;
        private int countMeh;
        private int countMiss;

        public TaikoPerformanceCalculator()
            : base(new TaikoRuleset())
        {
        }

        protected override PerformanceAttributes CreatePerformanceAttributes(ScoreInfo score, DifficultyAttributes attributes)
        {
            var taikoAttributes = (TaikoDifficultyAttributes)attributes;

            countGreat = score.Statistics.GetValueOrDefault(HitResult.Great);
            countOk = score.Statistics.GetValueOrDefault(HitResult.Ok);
            countMeh = score.Statistics.GetValueOrDefault(HitResult.Meh);
            countMiss = score.Statistics.GetValueOrDefault(HitResult.Miss);

            double multiplier = 1.1; // This is being adjusted to keep the final pp value scaled around what it used to be when changing things

            if (score.Mods.Any(m => m is ModNoFail))
                multiplier *= 0.90;

            if (score.Mods.Any(m => m is ModHidden))
                multiplier *= 1.10;

            double totalPerformanceValue = computeTotalPerformanceValue(score, taikoAttributes);
            double totalDifficultyValue = computeTotalDifficultyValue(score, taikoAttributes);

            double staminaValue = computeStaminaValue(score, taikoAttributes);
            double rhythmValue = computeRhythmValue(score, taikoAttributes);
            double colourValue = computeColourValue(score, taikoAttributes);

            double accuracyValue = computeAccuracyValue(score, taikoAttributes);
            double totalValue =
                Math.Pow(
                    Math.Pow(totalPerformanceValue, 1.1) +
                    Math.Pow(accuracyValue, 1.1), 1.0 / 1.1
                ) * multiplier;

            return new TaikoPerformanceAttributes
            {
                TotalPerformance = totalPerformanceValue,
                TotalDifficulty = totalDifficultyValue,
                Stamina = staminaValue,
                Rhythm = rhythmValue,
                Colour = colourValue,
                Accuracy = accuracyValue,
                Total = totalValue
            };
        }

        private double computeTotalDifficultyValue(ScoreInfo score, TaikoDifficultyAttributes taikoAttributes)
        {
            double computedRating = 1.4 * TaikoDifficultyCalculator.Norm(1.5, computeStaminaValue(score, taikoAttributes), computeRhythmValue(score, taikoAttributes), computeColourValue(score, taikoAttributes));
            double difficultyValue = computedRating + taikoAttributes.StrainDifficulty;
            difficultyValue = TaikoDifficultyCalculator.Rescale(difficultyValue);
            return difficultyValue;
        }

        private double computeTotalPerformanceValue(ScoreInfo score, TaikoDifficultyAttributes taikoAttributes)
        {
            double totalPerformance = Math.Pow(5 * Math.Max(1.0, computeTotalDifficultyValue(score, taikoAttributes) / 0.175) - 4.0, 2.25) / 450.0;

            double lengthBonus = 1 + 0.1 * Math.Min(1.0, totalHits / 1500.0);
            totalPerformance *= lengthBonus;

            totalPerformance *= Math.Pow(0.985, countMiss);

            return totalPerformance * score.Accuracy;
        }

        private double computeStaminaValue(ScoreInfo score, TaikoDifficultyAttributes attributes)
        {
            double staminaValue = attributes.StaminaDifficulty;
            return staminaValue;
        }

        private double computeRhythmValue(ScoreInfo score, TaikoDifficultyAttributes attributes)
        {
            double rhythmValue = attributes.RhythmDifficulty;
            return rhythmValue;
        }

        private double computeColourValue(ScoreInfo score, TaikoDifficultyAttributes attributes)
        {
            double colourValue = attributes.ColourDifficulty;
            return colourValue;
        }

        private double computeAccuracyValue(ScoreInfo score, TaikoDifficultyAttributes attributes)
        {
            if (attributes.GreatHitWindow <= 0)
                return 0;

            double accValue = Math.Pow(150.0 / attributes.GreatHitWindow, 1.1) * Math.Pow(score.Accuracy, 15) * 22.0;

            // Bonus for many objects - it's harder to keep good accuracy up for longer
            return accValue * Math.Min(1.15, Math.Pow(totalHits / 1500.0, 0.3));
        }

        private int totalHits => countGreat + countOk + countMeh + countMiss;
    }
}
