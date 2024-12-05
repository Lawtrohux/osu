using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public static class DeltaEvaluator
    {
        /// <summary>
        /// Evaluates the difficulty of a single TaikoDifficultyHitObject based on delta time and entropy.
        /// </summary>
        /// <param name="noteObject">The TaikoDifficultyHitObject to evaluate.</param>
        /// <param name="binSize">The bin size for grouping delta times during entropy calculation.</param>
        /// <returns>The difficulty value for the given hit object.</returns>
        public static double EvaluateDifficultyOf(TaikoDifficultyHitObject noteObject, double binSize)
        {
            if (noteObject.PreviousNote(1) == null) // Ensure there's a previous note for comparison.
                return 0.0;

            var previousNote = noteObject.PreviousNote(1);
            if (previousNote == null)
                return 0.0;

            double deltaTime = noteObject.DeltaTime;
            double previousDeltaTime = previousNote.DeltaTime;

            // Compare the current object's delta time with its predecessor to assess variation.
            var deltaTimes = new List<double> { previousDeltaTime, deltaTime };

            return calculateEntropy(deltaTimes, binSize);
        }

        /// <summary>
        /// Calculates the entropy of a dataset based on binning.
        /// </summary>
        /// <param name="values">The dataset to calculate entropy for.</param>
        /// <param name="binSize">The bin size for grouping values.</param>
        /// <returns>The entropy of the dataset.</returns>
        private static double calculateEntropy(IEnumerable<double> values, double binSize)
        {
            if (!values.Any())
                return 0.0;

            var binnedValues = values
                               .Select(v => Math.Round(v / binSize) * binSize)
                               .ToList();

            var groups = binnedValues
                         .GroupBy(v => v)
                         .Select(g => new { Value = g.Key, Count = g.Count() });

            int total = binnedValues.Count;
            double entropy = 0.0;

            foreach (var group in groups)
            {
                double probability = (double)group.Count / total;
                entropy -= probability * Math.Log(probability, 2);
            }

            return entropy;
        }
    }
}
