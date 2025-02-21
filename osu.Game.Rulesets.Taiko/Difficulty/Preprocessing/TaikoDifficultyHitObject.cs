// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Pattern;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Utils;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing
{
    /// <summary>
    /// Represents a single hit object in taiko difficulty calculation.
    /// </summary>
    public class TaikoDifficultyHitObject : DifficultyHitObject
    {
        /// <summary>
        /// The list of all <see cref="TaikoDifficultyHitObject"/> of the same colour as this <see cref="TaikoDifficultyHitObject"/> in the beatmap.
        /// </summary>
        private readonly IReadOnlyList<TaikoDifficultyHitObject>? monoDifficultyHitObjects;

        /// <summary>
        /// The list consecutive <see cref="TaikoDifficultyHitObject"/> of the same colour as this <see cref="TaikoDifficultyHitObject"/>.
        /// </summary>
        public IReadOnlyList<TaikoDifficultyHitObject> MonoStreak { get; private set; }

        /// <summary>
        /// The index of this <see cref="TaikoDifficultyHitObject"/> in <see cref="monoDifficultyHitObjects"/>.
        /// </summary>
        public readonly int MonoIndex;

        public readonly int MonoStreakIndex;

        public TaikoDifficultyHitObject? PreviousColourChange => MonoStreak.First();

        public TaikoDifficultyHitObject? NextColourChange => MonoStreak.Last();

        public bool IsColourChange => PreviousColourChange == this;

        public TaikoDifficultyHitObjectPattern Pattern;

        /// <summary>
        /// Creates a new difficulty hit object.
        /// </summary>
        /// <param name="hitObject">The gameplay <see cref="HitObject"/> associated with this difficulty object.</param>
        /// <param name="lastObject">The gameplay <see cref="HitObject"/> preceding <paramref name="hitObject"/>.</param>
        /// <param name="clockRate">The rate of the gameplay clock. Modified by speed-changing mods.</param>
        /// <param name="objects">The list of all <see cref="DifficultyHitObject"/>s in the current beatmap.</param>
        /// <param name="monoObjects">The list of all <see cref="TaikoDifficultyHitObject"/>s of the same colour as this <see cref="TaikoDifficultyHitObject"/> in the beatmap.</param>
        /// <param name="monoStreak">The list of previous consecutive <see cref="TaikoDifficultyHitObject"/>s of the same colour as this <see cref="TaikoDifficultyHitObject"/>.</param>
        /// <param name="index">The position of this <see cref="DifficultyHitObject"/> in the <paramref name="objects"/> list.</param>
        private TaikoDifficultyHitObject(HitObject hitObject, HitObject lastObject, double clockRate,
                                        List<DifficultyHitObject> objects,
                                        List<TaikoDifficultyHitObject> monoObjects,
                                        List<TaikoDifficultyHitObject> monoStreak,
                                        TaikoPatternFields patternFields,
                                        int index)
            : base(hitObject, lastObject, clockRate, objects, index)
        {
            MonoStreakIndex = monoStreak.Count;
            monoStreak.Add(this);
            MonoStreak = monoStreak;

            MonoIndex = monoObjects.Count;
            monoObjects.Add(this);
            monoDifficultyHitObjects = monoObjects;

            Pattern = new TaikoDifficultyHitObjectPattern(this, patternFields);
        }

        public static List<DifficultyHitObject> FromHitObjects(IEnumerable<HitObject> hitObjects, double clockRate)
        {
            List<DifficultyHitObject> difficultyHitObjects = new List<DifficultyHitObject>();
            var dhoByColour = new Dictionary<HitType, List<TaikoDifficultyHitObject>>
            {
                { HitType.Centre, new List<TaikoDifficultyHitObject>() },
                { HitType.Rim, new List<TaikoDifficultyHitObject>() }
            };
            List<TaikoDifficultyHitObject> monoStreak = new List<TaikoDifficultyHitObject>();

            TaikoPatternFields patternFields = new TaikoPatternFields();

            hitObjects
                // Do not consider non-note objects (spinners & sliders) for now
                .Where(hitObject => hitObject is Hit)
                .ForEachPair((previous, current) =>
                {
                    var previousHit = (Hit)previous;
                    var currentHit = (Hit)current;

                    if (previousHit.Type != currentHit.Type)
                    {
                        monoStreak = new List<TaikoDifficultyHitObject>();
                    }

                    var difficultyHitObject = new TaikoDifficultyHitObject(
                        current, previous, clockRate,
                        difficultyHitObjects,
                        dhoByColour[currentHit.Type],
                        monoStreak,
                        patternFields,
                        difficultyHitObjects.Count);

                    difficultyHitObjects.Add(difficultyHitObject);
                });

            return difficultyHitObjects;
        }

        public TaikoDifficultyHitObject? PreviousMono(int backwardsIndex) => monoDifficultyHitObjects?.ElementAtOrDefault(MonoIndex - (backwardsIndex + 1));
    }
}
