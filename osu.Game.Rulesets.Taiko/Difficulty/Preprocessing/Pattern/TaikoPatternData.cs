// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Pattern
{
    public class TaikoPatternData
    {
        public TaikoRhythmicPattern NoteRhythm;

        public TaikoRhythmicPattern MonoRhythm;

        public TaikoRhythmicPattern? ColourChangeRhythm;

        public TaikoPatternData(TaikoDifficultyHitObject hitObject)
        {
            NoteRhythm = new TaikoRhythmicPattern(hitObject, hitObject.PreviousObjects, 2000, 8);

            MonoRhythm = new TaikoRhythmicPattern(
                hitObject,
                hitObject.PreviousObjects.Where(isMonoOf(hitObject)),
                2000, 4);

            if (hitObject.IsColourChange)
            {
                ColourChangeRhythm = new TaikoRhythmicPattern(
                    hitObject,
                    hitObject.PreviousObjects.Where(o => o.IsColourChange),
                    2000, 4);
            }
        }

        private static Func<TaikoDifficultyHitObject, bool> isMonoOf(TaikoDifficultyHitObject hitObject)
        {
            return (TaikoDifficultyHitObject other) =>
                (other.BaseObject as Hit)?.Type == (hitObject.BaseObject as Hit)?.Type;
        }
    }
}

