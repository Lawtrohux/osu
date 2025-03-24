// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Utils;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Pattern
{
    public class TaikoRhythmicAlignmentObject
    {
        public readonly TaikoRhythmicAlignmentField Field;

        public readonly TaikoDifficultyHitObject HitObject;

        public int Index { get; private set; }

        public int? SlowdownIndex { get; private set; }

        public bool IsSlowDown { get; private set; }

        public TaikoRhythmicAlignmentObject(
            TaikoRhythmicAlignmentField field,
            TaikoDifficultyHitObject hitObject,
            List<TaikoRhythmicAlignmentObject> events,
            List<TaikoRhythmicAlignmentObject> slowdownEvents)
        {
            Field = field;
            HitObject = hitObject;
            Index = events.Count;

            var previousDeltaTimes = GetPreviousObjects(true)
                                     .SelectPair((x, y) => x.HitObject.StartTime - y.HitObject.StartTime)
                                     .Take(2)
                                     .ToList();

            IsSlowDown = previousDeltaTimes.Count == 2 && previousDeltaTimes[0] + 2 > previousDeltaTimes[1];

            if (!IsSlowDown) return;

            SlowdownIndex = slowdownEvents.Count;
            slowdownEvents.Add(this);
        }

        private TaikoRhythmicAlignmentObject? previous(int backwardsIndex, int? currentIndex, IReadOnlyCollection<TaikoRhythmicAlignmentObject> events)
        {
            return currentIndex == null ? null : events.ElementAtOrDefault(currentIndex.Value - (backwardsIndex + 1));
        }

        public TaikoRhythmicAlignmentObject? Previous(int backwardsIndex) => previous(backwardsIndex, Index, Field.Events);

        public TaikoRhythmicAlignmentObject? PreviousSlowdown(int backwardsIndex) => previous(backwardsIndex, SlowdownIndex, Field.SlowdownEvents);

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
    }
}
