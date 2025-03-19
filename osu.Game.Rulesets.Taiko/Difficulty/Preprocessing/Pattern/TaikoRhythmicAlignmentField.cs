// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Pattern
{
    /// <summary>
    /// Stores collection of events to calculate rhythmic misalignment.
    /// </summary>
    public class TaikoRhythmicAlignmentField
    {
        private readonly List<TaikoRhythmicAlignmentObject> events = [];

        public ReadOnlyCollection<TaikoRhythmicAlignmentObject> Events => events.AsReadOnly();

        private readonly List<TaikoRhythmicAlignmentObject> slowdownEvents = [];

        public ReadOnlyCollection<TaikoRhythmicAlignmentObject> SlowdownEvents => slowdownEvents.AsReadOnly();

        public readonly double HarmonicsCount;

        public readonly double TimeDecay;

        public readonly double CycleDecay;

        public readonly int MaxPreviousEvents;

        /// <summary>
        /// Creates a new field to calculate rhythmic misalignment.
        /// </summary>
        /// <param name="harmonicsCount">The amount of harmonics to calculate.</param>
        /// <param name="timeDecay">How much to decay values per second.</param>
        /// <param name="cycleDecay">How much to decay values per event.</param>
        /// <param name="maxPreviousEvents">The maximum amount of previous events to consider when calculating misalignment.</param>
        public TaikoRhythmicAlignmentField(
            double harmonicsCount,
            double timeDecay,
            double cycleDecay,
            int maxPreviousEvents = 8)
        {
            HarmonicsCount = harmonicsCount;
            TimeDecay = timeDecay;
            CycleDecay = cycleDecay;
            MaxPreviousEvents = maxPreviousEvents;
        }

        public TaikoRhythmicAlignmentObject Add(TaikoDifficultyHitObject hitObject)
        {
            var alignmentObject = new TaikoRhythmicAlignmentObject(this, hitObject, events, slowdownEvents);
            events.Add(alignmentObject);
            return alignmentObject;
        }
    }
}
