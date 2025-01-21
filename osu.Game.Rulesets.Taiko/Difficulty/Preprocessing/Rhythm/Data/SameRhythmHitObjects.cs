// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm.Data
{
    /// <summary>
    /// Represents a group of <see cref="TaikoDifficultyHitObject"/>s with no rhythm variation.
    /// </summary>
    public class SameRhythmHitObjects : SameRhythm<TaikoDifficultyHitObject>, IHasInterval
    {
        /// <summary>
        /// The first hit object in this group.
        /// </summary>
        public TaikoDifficultyHitObject FirstHitObject => Children[0];

        /// <summary>
        /// The previous group of <see cref="SameRhythmHitObjects"/>, if available.
        /// </summary>
        public SameRhythmHitObjects? Previous { get; }

        /// <summary>
        /// The start time of the first hit object in this group.
        /// </summary>
        public double StartTime => FirstHitObject.StartTime;

        /// <summary>
        /// The duration between the first and last hit objects in this group.
        /// </summary>
        public double Duration => Children[^1].StartTime - FirstHitObject.StartTime;

        /// <summary>
        /// The average interval between hit objects in this group, if applicable.
        /// </summary>
        public double? HitObjectInterval { get; private set; }

        /// <summary>
        /// The ratio of <see cref="HitObjectInterval"/> between this and the previous group.
        /// Defaults to 1 if undefined.
        /// </summary>
        public double HitObjectIntervalRatio { get; private set; } = 1;

        /// <summary>
        /// The interval between the start time of this group and the previous group.
        /// Defaults to positive infinity if no previous group exists.
        /// </summary>
        public double Interval { get; private set; } = double.PositiveInfinity;

        public SameRhythmHitObjects(SameRhythmHitObjects? previous, List<TaikoDifficultyHitObject> data, ref int index)
            : base(data, ref index, 5)
        {
            Previous = previous;

            assignToChildren();
            calculateIntervals();
        }

        /// <summary>
        /// Groups a list of <see cref="TaikoDifficultyHitObject"/>s into <see cref="SameRhythmHitObjects"/>.
        /// </summary>
        /// <param name="data">The list of hit objects to group.</param>
        public static List<SameRhythmHitObjects> GroupHitObjects(List<TaikoDifficultyHitObject> data)
        {
            var groups = new List<SameRhythmHitObjects>();

            for (int i = 0; i < data.Count;)
            {
                var previous = groups.Count > 0 ? groups[^1] : null;
                groups.Add(new SameRhythmHitObjects(previous, data, ref i));
            }

            return groups;
        }

        /// <summary>
        /// Assigns this group to its children and propagates the calculated hit object interval.
        /// </summary>
        private void assignToChildren()
        {
            foreach (var hitObject in Children)
            {
                hitObject.Rhythm.SameRhythmHitObjects = this;
                hitObject.HitObjectInterval = HitObjectInterval;
            }
        }

        /// <summary>
        /// Calculates the intervals for this group, including the average hit object interval
        /// and its ratio with the previous group's interval.
        /// </summary>
        private void calculateIntervals()
        {
            // Calculate the average interval between hit objects, or null if fewer than two exist.
            HitObjectInterval = Children.Count > 1
                ? (Children[^1].StartTime - FirstHitObject.StartTime) / (Children.Count - 1)
                : null;

            // Calculate the interval ratio with the previous group, if applicable.
            if (Previous?.HitObjectInterval != null && HitObjectInterval != null)
            {
                HitObjectIntervalRatio = HitObjectInterval.Value / Previous.HitObjectInterval.Value;
            }

            // Calculate the interval to the previous group.
            Interval = Previous != null ? StartTime - Previous.StartTime : double.PositiveInfinity;
        }
    }
}
