// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators.Rhythm
{
    /// <summary>
    /// Stores amplitude points data by time. Getting amplitude at a specific time is defined as the sum of amplitudes
    /// within a range scaled by a normal distribution.
    /// </summary>
    public class TaikoRhythmicAlignmentField
    {
        public TaikoDifficultyHitObjectRhythm RhythmData { get; private set; }

        private double harmonicsCount;

        private double timeDecay;

        private double countDecay;

        /// <summary>
        /// Creates a new field to calculate rhythmic misalignment.
        /// </summary>
        /// <param name="rhythmData">The pattern associated with the note to calculate misalignment for.</param>
        /// <param name="harmonicsCount">The amount of harmonics to calculate.</param>
        /// <param name="timeDecay">How much to decay values per second.</param>
        /// <param name="countDecay">How much to decay values per event.</param>
        public TaikoRhythmicAlignmentField(
            TaikoDifficultyHitObjectRhythm rhythmData,
            double harmonicsCount,
            double timeDecay,
            double countDecay)
        {
            RhythmData = rhythmData;
            this.harmonicsCount = harmonicsCount;
            this.timeDecay = timeDecay;
            this.countDecay = countDecay;
        }

        public double CalculateMisalignment(double hitWindowMs)
        {
            if (!RhythmData.BaseInterval.HasValue) return 0;

            List<(double dt, double amplitude)> residue = (RhythmData.PreviousEventDeltaTimes ?? [])
                .Select(x => (dt: x, amplitude: 1d))
                .ToList();
            List<double> decayMultipliers = residue
                .Select((x, i) => Math.Pow(timeDecay, x.dt / 1000) * Math.Pow(i, countDecay))
                .ToList();

            double leniencyExponent = calculateLeniencyExponent(hitWindowMs / RhythmData.BaseInterval.Value);
            double totalMisalignment = 0;

            for (int harmonic = 1; harmonic <= harmonicsCount; harmonic++)
            {
                double alignmentInterval = RhythmData.BaseInterval.Value / harmonic;

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
            totalMisalignment += residue.Sum(x => x.amplitude * harmonicsCount);

            return totalMisalignment;
        }

        private double calculateAlignment(double dt, double alignmentInterval, double leniencyExponent)
        {
            double phase = (dt / alignmentInterval) * (Math.PI / 2);
            double cosComponent = Math.Pow(Math.Abs(Math.Cos(phase)), leniencyExponent);
            double sinComponent = Math.Pow(Math.Abs(Math.Sin(phase)), leniencyExponent);

            return Math.Max(cosComponent, sinComponent);
        }

        private double calculateLeniencyExponent(double leniency)
        {
            leniency = Math.Clamp(leniency, 0, 1);
            return Math.Log(0.5) / Math.Log(Math.Cos(Math.PI * leniency / 2));
        }
    }
}
