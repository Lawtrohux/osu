// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing
{
    /// <summary>
    /// Represents a single hit object in taiko difficulty calculation.
    /// </summary>
    public class TaikoDifficultyHitObject : DifficultyHitObject
    {
        /// <summary>
        /// The rhythm required to hit this hit object.
        /// </summary>
        public readonly TaikoDifficultyHitObjectRhythm Rhythm;

        /// <summary>
        /// The hit type of this hit object.
        /// </summary>
        public readonly HitType? HitType;

        /// <summary>
        /// The index of the object in the beatmap.
        /// </summary>
        public readonly int ObjectIndex;

        /// <summary>
        /// Whether the object should carry a penalty due to being hittable using special techniques
        /// making it easier to do so.
        /// </summary>
        public bool StaminaCheese;

        /// <summary>
        /// Effective BPM of the object, required for reading difficulty calculation
        /// </summary>
        public double EffectiveBPM;

        /// <summary>
        /// Creates a new difficulty hit object.
        /// </summary>
        /// <param name="hitObject">The gameplay <see cref="HitObject"/> associated with this difficulty object.</param>
        /// <param name="lastObject">The gameplay <see cref="HitObject"/> preceding <paramref name="hitObject"/>.</param>
        /// <param name="lastLastObject">The gameplay <see cref="HitObject"/> preceding <paramref name="lastObject"/>.</param>
        /// <param name="clockRate">The rate of the gameplay clock. Modified by speed-changing mods.</param>
        /// <param name="objectIndex">The index of the object in the beatmap.</param>
        public TaikoDifficultyHitObject(HitObject hitObject, HitObject lastObject, HitObject lastLastObject, double clockRate, int objectIndex)
            : base(hitObject, lastObject, clockRate)
        {
            var currentHit = hitObject as Hit;

            Rhythm = createRhythmChange(lastObject, lastLastObject, clockRate);
            HitType = currentHit?.Type;

            ObjectIndex = objectIndex;
        }

        /// <summary>
        /// Returns a <see cref="TaikoDifficultyHitObjectRhythm"/> representing the rhythm change of the current object.
        /// </summary>
        /// <param name="lastObject">The gameplay <see cref="HitObject"/> preceding this one.</param>
        /// <param name="lastLastObject">The gameplay <see cref="HitObject"/> preceding <paramref name="lastObject"/>.</param>
        /// <param name="clockRate">The rate of the gameplay clock.</param>
        private TaikoDifficultyHitObjectRhythm createRhythmChange(HitObject lastObject, HitObject lastLastObject, double clockRate)
        {
            double prevLength = (lastObject.StartTime - lastLastObject.StartTime) / clockRate;
            double ratio = DeltaTime / prevLength;

            return new TaikoDifficultyHitObjectRhythm(ratio);
        }
    }
}
