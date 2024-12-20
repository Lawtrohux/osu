// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Pattern
{
    public class TaikoRhythmicPattern
    {
        public List<double>? PreviousEventDeltaTimes { get; private set; }

        public double? BaseInterval { get; private set; }

        public TaikoRhythmicPattern(
            TaikoDifficultyHitObject hitObject,
            IEnumerable<TaikoDifficultyHitObject> previousObjects,
            double maxWindowMs,
            int maxObjects)
        {
            var previousObjectsEnumerator = previousObjects.GetEnumerator();
            if (!previousObjectsEnumerator.MoveNext()) return;

            BaseInterval = hitObject.StartTime - previousObjectsEnumerator.Current.StartTime;

            PreviousEventDeltaTimes = previousObjects
                .Take(maxObjects)
                .Select(o => Math.Abs(hitObject.StartTime - o.StartTime))
                .Where(dt => dt < maxWindowMs)
                .ToList();
        }
    }
}
