// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Objects;
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

            double difficultyValue = computeDifficultyValue(score, taikoAttributes);
            double accuracyValue = computeAccuracyValue(score, taikoAttributes);
            double readingValue = computeReadingValue(score, taikoAttributes);
            double totalValue =
                Math.Pow(
                    Math.Pow(difficultyValue, 1.1) +
                    Math.Pow(accuracyValue, 1.0) +
                    Math.Pow(readingValue, 1.0), 1.0 / 1.1
                );

            return new TaikoPerformanceAttributes
            {
                Difficulty = difficultyValue,
                Accuracy = accuracyValue,
                Reading = readingValue,
                Total = totalValue
            };
        }

        private double computeDifficultyValue(ScoreInfo score, TaikoDifficultyAttributes attributes)
        {
            double difficultyValue = Math.Pow(5.0 * Math.Max(1.0, attributes.StarRating / 0.170) - 4.0, 2.25) / 425.0;

            double lengthBonus = 1 + 0.1 * Math.Min(1.0, totalHits / 1500.0);
            difficultyValue *= lengthBonus;

            difficultyValue *= Math.Pow(0.975, countMiss);

            if (score.Mods.Any(m => m is ModHidden))
                difficultyValue *= 1.125;

            return difficultyValue * score.Accuracy;
        }

        private double computeAccuracyValue(ScoreInfo score, TaikoDifficultyAttributes attributes)
        {
            if (attributes.GreatHitWindow <= 0)
                return 0;

            double accuracyValue = Math.Pow(150 / attributes.GreatHitWindow, 1.1) * Math.Pow(score.Accuracy, 15) * Math.Max(1.0, attributes.StarRating / 0.170);

            double accuracylengthBonus = Math.Min(1.15, Math.Pow(totalHits / 1500.0, 0.3));
            accuracyValue *= accuracylengthBonus;

            if (score.Mods.Any(m => m is ModHidden))
                accuracyValue *= 1.075;

            return accuracyValue;
        }

        private double computeReadingValue(ScoreInfo score, TaikoDifficultyAttributes attributes)
        {
            if (!score.Mods.Any(m => m is ModFlashlight<TaikoHitObject>))
                return 0.0;

            double readingValue = Math.Max(1.5, Math.Pow(score.Accuracy, 15) * (4 * Math.Max(1.0, attributes.StarRating / 0.170)));

            double lengthBonus = 1 + 0.1 * Math.Min(1.0, totalHits / 1500.0);
            readingValue *= lengthBonus;

            if (score.Mods.Any(m => m is ModFlashlight<TaikoHitObject> && score.Mods.Any(m => m is ModHidden)))
                readingValue *= 1.5;

            return readingValue;
        }

        private int totalHits => countGreat + countOk + countMeh + countMiss;
    }
}
