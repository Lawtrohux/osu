// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm.Data
{
    /// <summary>
    /// Represents <see cref="SameRhythmHitObjects"/> grouped by their <see cref="SameRhythmHitObjects.StartTime"/>'s interval.
    /// </summary>
    public class SamePatterns : SameRhythm<SameRhythmHitObjects>
    {
        /// <summary>
        /// The previous pattern in the sequence.
        /// </summary>
        public SamePatterns? Previous { get; }

        /// <summary>
        /// The interval between children <see cref="SameRhythmHitObjects"/> within this group.
        /// Defaults to the first child's interval if there is only one child.
        /// </summary>
        public double ChildrenInterval => Children.Count > 1 ? Children[1].Interval : Children[0].Interval;

        /// <summary>
        /// The ratio of <see cref="ChildrenInterval"/> between this and the previous pattern.
        /// Returns 1 if there is no previous pattern.
        /// </summary>
        public double IntervalRatio => Previous != null ? ChildrenInterval / Previous.ChildrenInterval : 1.0;

        /// <summary>
        /// The first hit object in this pattern.
        /// </summary>
        public TaikoDifficultyHitObject FirstHitObject => Children[0].FirstHitObject;

        /// <summary>
        /// All hit objects across all rhythm groups in this pattern.
        /// </summary>
        public IEnumerable<TaikoDifficultyHitObject> AllHitObjects => Children.SelectMany(child => child.Children);

        /// <summary>
        /// Constructs a new <see cref="SamePatterns"/> group.
        /// </summary>
        /// <param name="previous">The previous pattern in the sequence.</param>
        /// <param name="data">The data to group.</param>
        /// <param name="index">The current index in the data.</param>
        private SamePatterns(SamePatterns? previous, List<SameRhythmHitObjects> data, ref int index)
            : base(data, ref index, 5)
        {
            Previous = previous;

            foreach (var hitObject in AllHitObjects)
                hitObject.Rhythm.SamePatterns = this;
        }

        /// <summary>
        /// Groups the provided rhythm data into patterns.
        /// </summary>
        /// <param name="data">The rhythm data to group.</param>
        public static void GroupPatterns(List<SameRhythmHitObjects> data)
        {
            var samePatterns = new List<SamePatterns>();

            // Index incrementation is handled by the base constructor.
            for (int i = 0; i < data.Count;)
            {
                var previous = samePatterns.LastOrDefault();
                samePatterns.Add(new SamePatterns(previous, data, ref i));
            }
        }
    }
}
