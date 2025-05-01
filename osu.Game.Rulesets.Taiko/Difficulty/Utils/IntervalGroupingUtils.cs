// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Utils;

namespace osu.Game.Rulesets.Taiko.Difficulty.Utils
{
    public static class IntervalGroupingUtils
    {
        /// <summary>
        /// Splits a sequence of objects into groups where successive intervals are within tolerance.
        /// Single-element groups are merged back into the previous group to avoid isolated hits.
        /// </summary>
        public static List<List<T>> GroupByInterval<T>(IReadOnlyList<T> objects)
            where T : IHasInterval
        {
            var groups = new List<List<T>>();
            int i = 0;

            while (i < objects.Count)
            {
                var group = createNextGroup(objects, ref i);

                // Merge single-element groups into previous group to avoid isolated hits
                groups.Add(group);
            }

            return groups;
        }

        /// <summary>
        /// Builds the next group starting at <paramref name="startIndex"/>, advancing the index inline.
        /// </summary>
        private static List<T> createNextGroup<T>(IReadOnlyList<T> objects, ref int startIndex, double marginOfError = 5.0)
            where T : IHasInterval
        {
            var groupedObjects = new List<T> { objects[startIndex++] };

            // Include the second object (specifically in a doublet) if it matches the first within tolerance
            if (startIndex < objects.Count &&
                Precision.AlmostEquals(groupedObjects[0].Interval, objects[startIndex].Interval, marginOfError))
            {
                groupedObjects.Add(objects[startIndex++]);
                return groupedObjects;
            }

            // Continue grouping while intervals stay within tolerance, or allow
            // one slight increase if the entire group so far has been uniform.
            while (startIndex < objects.Count)
            {
                double previousInterval = groupedObjects[^1].Interval;
                double currentInterval = objects[startIndex].Interval;

                if (Precision.AlmostEquals(previousInterval, currentInterval, marginOfError))
                {
                    groupedObjects.Add(objects[startIndex++]);
                    continue;
                }

                // If all intervals in the group match 'previousInterval', allow
                // one slight increase that's still within marginOfError.
                if (hasUniformInterval(groupedObjects, previousInterval, marginOfError)
                    && currentInterval <= previousInterval + marginOfError)
                {
                    groupedObjects.Add(objects[startIndex++]);
                    continue;
                }

                break;
            }

            return groupedObjects;
        }

        /// <summary>
        /// Returns true if every item's Interval in <paramref name="currentGroup"/> is
        /// within <paramref name="tolerance"/> of <paramref name="previousInterval"/>.
        /// </summary>
        private static bool hasUniformInterval<T>(List<T> currentGroup, double previousInterval, double tolerance)
            where T : IHasInterval
        {
            return currentGroup.All(current => Precision.AlmostEquals(current.Interval, previousInterval, tolerance));
        }
    }
}
