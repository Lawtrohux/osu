// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm
{
    public class TaikoDifficultyHitObjectRhythm
    {
        public List<double> PreviousEventDeltaTimes { get; private set; }

        public double? BaseInterval { get; private set; }

        public TaikoDifficultyHitObjectRhythm(
            TaikoDifficultyHitObject hitObject,
            double maxWindowMs,
            int maxObjects)
        {
            BaseInterval = hitObject.DeltaTime;
            PreviousEventDeltaTimes = new List<double>();

            var current = hitObject.Previous(0);
            while (PreviousEventDeltaTimes.Count < maxObjects &&
                PreviousEventDeltaTimes.LastOrDefault(0) < maxWindowMs &&
                current != null)
            {
                Debug.Assert(hitObject.StartTime > current.StartTime);
                PreviousEventDeltaTimes.Add(hitObject.StartTime - current.StartTime);
                current = current.Previous(0);
            }
        }
    }
}
