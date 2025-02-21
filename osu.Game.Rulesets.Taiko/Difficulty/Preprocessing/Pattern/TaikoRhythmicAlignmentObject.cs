// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Utils;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Pattern
{
    public class TaikoRhythmicAlignmentObject
    {
        private TaikoRhythmicAlignmentField field;

        private TaikoDifficultyHitObject hitObject;

        public int Index { get; private set; }

        public int? SlowdownIndex { get; private set; }

        private bool isSlowDown;

        public TaikoRhythmicAlignmentObject(
            TaikoRhythmicAlignmentField field,
            TaikoDifficultyHitObject hitObject,
            List<TaikoRhythmicAlignmentObject> events,
            List<TaikoRhythmicAlignmentObject> slowdownEvents)
        {
            this.field = field;
            this.hitObject = hitObject;
            Index = events.Count;

            var previousDeltaTimes = GetPreviousObjects(true)
                .SelectPair((x, y) => x.hitObject.StartTime - y.hitObject.StartTime)
                .Take(2)
                .ToList();

            isSlowDown = previousDeltaTimes.Count == 2 && previousDeltaTimes[0] > previousDeltaTimes[1];
            if (isSlowDown)
            {
                SlowdownIndex = slowdownEvents.Count;
                slowdownEvents.Add(this);
            }
        }

        private TaikoRhythmicAlignmentObject? previous(int backwardsIndex, int? currentIndex, IReadOnlyCollection<TaikoRhythmicAlignmentObject> events)
        {
            if (currentIndex == null) return null;
            return events.ElementAtOrDefault(currentIndex.Value - (backwardsIndex + 1));
        }

        public TaikoRhythmicAlignmentObject? Previous(int backwardsIndex) => previous(backwardsIndex, Index, field.Events);

        public TaikoRhythmicAlignmentObject? PreviousSlowdown(int backwardsIndex) => previous(backwardsIndex, SlowdownIndex, field.SlowdownEvents);

        public IEnumerable<TaikoRhythmicAlignmentObject> GetPreviousObjects(bool includeSelf = false)
        {
            if (includeSelf) yield return this;

            for (int i = 0; true; i++)
            {
                var previous = Previous(i);
                if (previous == null) break;
                yield return previous;
            }
        }

        public IEnumerable<TaikoRhythmicAlignmentObject> GetPreviousSlowdownObjects(bool includeSelf = false)
        {
            if (includeSelf) yield return this;

            for (int i = 0; true; i++)
            {
                var previousSlowdown = PreviousSlowdown(i);
                if (previousSlowdown == null) break;
                yield return previousSlowdown;
            }
        }


        public double CalculateMisalignment(double hitWindowMs)
        {
            IEnumerable<TaikoRhythmicAlignmentObject> previousObjects =
                isSlowDown ? GetPreviousSlowdownObjects() : GetPreviousObjects();
            List<(double dt, double amplitude)> residue = previousObjects
                .Take(field.MaxPreviousEvents)
                .Select(x => (dt: hitObject.StartTime - x.hitObject.StartTime, amplitude: 1d))
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
