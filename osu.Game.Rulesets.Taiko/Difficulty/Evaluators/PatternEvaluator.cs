// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Pattern;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public static class PatternEvaluator
    {
        private static double pNorm(double p, params double[] values) => Math.Pow(values.Sum(x => Math.Pow(x, p)), 1 / p);

        private static double calculateAlignment(double dt, double alignmentInterval, double leniencyExponent)
        {
            double phase = (dt / alignmentInterval) * (Math.PI / 2);
            double cosComponent = Math.Pow(Math.Abs(Math.Cos(phase)), leniencyExponent);
            double sinComponent = Math.Pow(Math.Abs(Math.Sin(phase)), leniencyExponent);

            return Math.Max(cosComponent, sinComponent);
        }

        private static double calculateLeniencyExponent(double leniency)
        {
            leniency = Math.Clamp(leniency, 0, 1);
            return Math.Log(0.5) / Math.Log(Math.Cos(Math.PI * leniency / 2));
        }

        public static double CalculateMisalignment(TaikoRhythmicAlignmentObject alignmentObject, double hitWindowMs)
        {
            var field = alignmentObject.Field;
            var hitObject = alignmentObject.HitObject;

            IEnumerable<TaikoRhythmicAlignmentObject> previousObjects =
                alignmentObject.IsSlowDown ? alignmentObject.GetPreviousSlowdownObjects() : alignmentObject.GetPreviousObjects();
            List<(double dt, double amplitude)> residue = previousObjects
                                                          .Take(field.MaxPreviousEvents)
                                                          .Select(x => (dt: hitObject.StartTime - x.HitObject.StartTime, amplitude: 1d))
                                                          .ToList();

            if (residue.Count == 0) return 0;

            double baseInterval = residue[0].dt;

            List<double> decayMultipliers = residue
                                            .Select((x, i) =>
                                                Math.Pow(field.TimeDecay, x.dt / 1000) *
                                                Math.Pow(field.CycleDecay, x.dt / baseInterval))
                                            .ToList();

            double leniencyExponent = calculateLeniencyExponent(hitWindowMs / baseInterval);
            double totalMisalignment = 0;

            for (int harmonic = 1; harmonic <= field.HarmonicsCount; harmonic++)
            {
                double alignmentInterval = baseInterval / harmonic;

                for (int i = 0; i < residue.Count; i++)
                {
                    double dt = residue[i].dt;
                    double alignment = calculateAlignment(dt, alignmentInterval, leniencyExponent);
                    double scaledAlignment = residue[i].amplitude * alignment;
                    totalMisalignment += scaledAlignment * (harmonic - 1) * decayMultipliers[i];
                    residue[i] = (dt, amplitude: residue[i].amplitude - scaledAlignment);
                }
            }

            // This is to avoid missing residues that aren't catched by any harmonic
            totalMisalignment += residue
                                 .Select((x, i) => x.amplitude * decayMultipliers[i])
                                 .Sum((x) => x * field.HarmonicsCount);

            return totalMisalignment;
        }

        private static double monoEffectiveHitWindow(TaikoDifficultyHitObject hitObject, double hitWindowMs)
        {
            double? previousColourChange = hitObject.PreviousColourChange?.StartTime;
            double? nextColourChange = hitObject.NextColourChange?.StartTime;

            double window = hitWindowMs;

            if (previousColourChange is not null && nextColourChange is not null)
            {
                window = (nextColourChange.Value - previousColourChange.Value) / 2;
            }

            return double.Clamp(window, 1, hitWindowMs);
        }

        /// <summary>
        /// Calculates a consistency penalty based on the number of consecutive consistent intervals,
        /// considering the delta time between each colour sequence.
        /// </summary>
        /// <param name="hitObject">The current hitObject to consider.</param>
        /// <param name="threshold"> The allowable margin of error for determining whether ratios are consistent.</param>
        /// <param name="maxObjectsToCheck">The maximum objects to check per count of consistent ratio.</param>
        private static double consistentRatioPenalty(TaikoDifficultyHitObject hitObject, double threshold = 0.01, int maxObjectsToCheck = 64)
        {
            int consistentRatioCount = 0;
            double totalRatioCount = 0.0;

            TaikoDifficultyHitObject current = hitObject;

            for (int i = 0; i < maxObjectsToCheck; i++)
            {
                // Break if there is no valid previous object
                if (current.Index <= 1)
                    break;

                var previousHitObject = (TaikoDifficultyHitObject)current.Previous(1);

                double currentRatio = current.RhythmData.Ratio;
                double previousRatio = previousHitObject.RhythmData.Ratio;

                // A consistent interval is defined as the percentage difference between the two rhythmic ratios with the margin of error.
                if (Math.Abs(1 - currentRatio / previousRatio) <= threshold)
                {
                    consistentRatioCount++;
                    totalRatioCount += currentRatio;
                    break;
                }

                // Move to the previous object
                current = previousHitObject;
            }

            // Ensure no division by zero
            double ratioPenalty = 1 - totalRatioCount / (consistentRatioCount + 1) * 0.80;

            return ratioPenalty;
        }

        public static double EvaluateDifficultyOf(TaikoDifficultyHitObject hitObject, double okHitWindowMs)
        {
            double monoMisalignment = 0;

            var taikoObject = hitObject;
            double difficulty = 0.0d;

            if (hitObject.Pattern.Mono is not null)
            {
                monoMisalignment = CalculateMisalignment(hitObject.Pattern.Mono, monoEffectiveHitWindow(hitObject, okHitWindowMs));
            }

            double colourChangeMisalignment = 0;

            if (hitObject.Pattern.ColourChange is not null)
            {
                colourChangeMisalignment = CalculateMisalignment(hitObject.Pattern.ColourChange, monoEffectiveHitWindow(hitObject, okHitWindowMs));
            }

            difficulty += pNorm(2, monoMisalignment, colourChangeMisalignment);

            double consistencyPenalty = consistentRatioPenalty(taikoObject);
            difficulty *= consistencyPenalty;

            return difficulty;
        }
    }
}
