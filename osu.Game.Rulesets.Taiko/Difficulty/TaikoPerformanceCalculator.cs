// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Scoring;
using osu.Game.Scoring;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Taiko.Difficulty
{
    public class TaikoPerformanceCalculator : PerformanceCalculator
    {
        private int countGreat;
        private int countOk;
        private int countMeh;
        private int countMiss;
        private double? estimatedUnstableRate;

        private double clockRate;
        private double greatHitWindow;

        private double effectiveMissCount;
        private double lengthBonus;

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

            clockRate = ModUtils.CalculateRateWithMods(score.Mods);

            var difficulty = score.BeatmapInfo!.Difficulty.Clone();
            score.Mods.OfType<IApplicableToDifficulty>().ForEach(m => m.ApplyToDifficulty(difficulty));

            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(difficulty.OverallDifficulty);
            greatHitWindow = hitWindows.WindowFor(HitResult.Great) / clockRate;

            estimatedUnstableRate = computeDeviationUpperBound() * 10;

            if (totalSuccessfulHits > 0)
                effectiveMissCount = Math.Max(1.0, 1000.0 / totalSuccessfulHits) * countMiss;

            lengthBonus = 1 + 0.1 * Math.Min(1.0, totalHits / 1500.0);

            bool isConvert = score.BeatmapInfo!.Ruleset.OnlineID != 1;

            double multiplier = 1.13;

            if (score.Mods.Any(m => m is ModHidden) && !isConvert)
                multiplier *= 1.075;

            if (score.Mods.Any(m => m is ModEasy))
                multiplier *= 0.950;

            double difficultyValue = computeDifficultyValue(score, taikoAttributes, out double mechanicalValue, out double rhythmValue, out double readingValue);
            double accuracyValue = computeAccuracyValue(score, taikoAttributes, isConvert);

            double totalValue =
                Math.Pow(
                    Math.Pow(difficultyValue, 1.1) +
                    Math.Pow(accuracyValue, 1.1), 1.0 / 1.1
                ) * multiplier;

            return new TaikoPerformanceAttributes
            {
                MechanicalDifficulty = mechanicalValue,
                RhythmDifficulty = rhythmValue,
                ReadingDifficulty = readingValue,
                Accuracy = accuracyValue,
                EffectiveMissCount = effectiveMissCount,
                EstimatedUnstableRate = estimatedUnstableRate,
                Total = totalValue
            };
        }

        private double computeDifficultyValue(ScoreInfo score, TaikoDifficultyAttributes attributes, out double mechanicalValue, out double rhythmValue, out double readingValue)
        {
            double baseDifficulty = 5 * Math.Max(1.0, attributes.StarRating / 0.110) - 4.0;
            double difficultyValue = Math.Min(Math.Pow(baseDifficulty, 3) / 69052.51, Math.Pow(baseDifficulty, 2.25) / 1250.0);

            difficultyValue *= 1 + 0.10 * Math.Max(0, attributes.StarRating - 10);

            double lengthBonus = 1 + 0.1 * Math.Min(1.0, totalHits / 1500.0);
            difficultyValue *= lengthBonus;

            difficultyValue *= Math.Pow(0.986, effectiveMissCount);

            if (score.Mods.Any(m => m is ModEasy))
                difficultyValue *= 0.90;

            if (score.Mods.Any(m => m is ModHidden))
                difficultyValue *= 1.025;

            if (score.Mods.Any(m => m is ModFlashlight<TaikoHitObject>))
                difficultyValue *= Math.Max(1, 1.050 - Math.Min(attributes.MonoStaminaFactor / 50, 1) * lengthBonus);

            if (estimatedUnstableRate == null)
                difficultyValue = 0;

            // Scale accuracy more harshly on nearly-completely mono (single coloured) speed maps.
            double accScalingExponent = 2 + attributes.MonoStaminaFactor;
            double accScalingShift = 500 - 100 * (attributes.MonoStaminaFactor * 3);

            difficultyValue *= Math.Pow(DifficultyCalculationUtils.Erf(accScalingShift / (Math.Sqrt(2) * estimatedUnstableRate.Value)), accScalingExponent);

            mechanicalValue = difficultyValue * (attributes.StaminaDifficulty + attributes.ColourDifficulty) / attributes.StarRating;
            rhythmValue = difficultyValue * attributes.RhythmDifficulty / attributes.StarRating;
            readingValue = difficultyValue * attributes.ReadingDifficulty / attributes.StarRating;

            return difficultyValue;
        }

        private double computeAccuracyValue(ScoreInfo score, TaikoDifficultyAttributes attributes, bool isConvert)
        {
            if (greatHitWindow <= 0 || estimatedUnstableRate == null)
                return 0;

            double accuracyValue = Math.Pow(70 / estimatedUnstableRate.Value, 1.1) * Math.Pow(attributes.StarRating, 0.4) * 100.0;

            double accLengthBonus = Math.Min(1.15, Math.Pow(totalHits / 1500.0, 0.3));

            if (score.Mods.Any(m => m is ModFlashlight<TaikoHitObject>) && score.Mods.Any(m => m is ModHidden) && !isConvert)
                accuracyValue *= Math.Max(1.0, 1.05 * accLengthBonus);

            return accuracyValue;
        }

        private double? computeDeviationUpperBound()
        {
            if (countGreat == 0 || greatHitWindow <= 0)
                return null;

            const double z = 2.32634787404;
            double n = totalHits;
            double p = countGreat / n;

            double pLowerBound = (n * p + z * z / 2) / (n + z * z) - z / (n + z * z) * Math.Sqrt(n * p * (1 - p) + z * z / 4);

            return greatHitWindow / (Math.Sqrt(2) * DifficultyCalculationUtils.ErfInv(pLowerBound));
        }

        private int totalHits => countGreat + countOk + countMeh + countMiss;
        private int totalSuccessfulHits => countGreat + countOk + countMeh;
    }
}
