using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing
{
    public static class EntropyCalculator
    {
        public static double CalculateEntropy(IEnumerable<double>? values, double binSize)
        {
            if (values == null || !values.Any())
                return 0.0;

            var binnedValues = values
                               .Select(v => Math.Round(v / binSize) * binSize)
                               .ToList();

            var groups = binnedValues
                         .GroupBy(v => v)
                         .Select(g => new { Value = g.Key, Count = g.Count() })
                         .ToList();

            int total = binnedValues.Count;
            double entropy = 0.0;

            foreach (var group in groups)
            {
                double probability = (double)group.Count / total;
                entropy -= probability * Math.Log(probability, 2);
            }

            return entropy;
        }

        public static double CalculateCoefficientOfVariation(IEnumerable<double>? values)
        {
            if (values == null || !values.Any())
                return 0.0;

            double mean = values.Average();
            if (mean == 0.0)
                return 0.0;

            double variance = CalculateVariance(values, mean);
            double standardDeviation = Math.Sqrt(variance);

            return standardDeviation / mean;
        }

        public static double CalculateAverage(IEnumerable<double> values)
        {
            if (!values.Any())
                return 0.0;

            return values.Average();
        }

        public static double CalculateVariance(IEnumerable<double> values, double mean)
        {
            if (!values.Any())
                return 0.0;

            return values.Select(v => Math.Pow(v - mean, 2)).Average();
        }
    }
}
