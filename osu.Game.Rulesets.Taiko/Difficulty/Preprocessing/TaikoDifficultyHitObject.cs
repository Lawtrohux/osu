// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Pattern;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Utils;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing
{
    public class TaikoDifficultyHitObject : DifficultyHitObject
    {
        private readonly IReadOnlyList<TaikoDifficultyHitObject>? monoDifficultyHitObjects;

        public IReadOnlyList<TaikoDifficultyHitObject> MonoStreak { get; private set; }

        public readonly int MonoIndex;

        public readonly int MonoStreakIndex;

        public TaikoDifficultyHitObject? PreviousColourChange => MonoStreak.First();

        public TaikoDifficultyHitObject? NextColourChange => MonoStreak.Last().Next(0) as TaikoDifficultyHitObject;

        public bool IsColourChange => PreviousColourChange == this;

        public TaikoRhythmicAlignmentObject Rhythm;

        public TaikoRhythmicAlignmentObject? Mono;

        public TaikoRhythmicAlignmentObject? ColourChange;

        public double EffectiveBPM;

        private TaikoDifficultyHitObject(HitObject hitObject, HitObject lastObject, double clockRate,
                                         List<DifficultyHitObject> objects,
                                         List<TaikoDifficultyHitObject> monoObjects,
                                         List<TaikoDifficultyHitObject> monoStreak,
                                         TaikoPatternFields patternFields,
                                         int index,
                                         ControlPointInfo controlPointInfo,
                                         double globalSliderVelocity)
            : base(hitObject, lastObject, clockRate, objects, index)
        {
            MonoStreakIndex = monoStreak.Count;
            monoStreak.Add(this);
            MonoStreak = monoStreak;

            MonoIndex = monoObjects.Count;
            monoObjects.Add(this);
            monoDifficultyHitObjects = monoObjects;

            Rhythm = patternFields.RhythmField.Add(this);

            if (IsColourChange)
                ColourChange = patternFields.ColourChangeField.Add(this);

            switch ((BaseObject as Hit)?.Type)
            {
                case HitType.Centre:
                    Mono = patternFields.CentreField.Add(this);
                    break;

                case HitType.Rim:
                    Mono = patternFields.RimField.Add(this);
                    break;
            }

            double normalisedStartTime = StartTime * clockRate;
            TimingControlPoint currentControlPoint = controlPointInfo.TimingPointAt(normalisedStartTime);
            double currentSliderVelocity = calculateSliderVelocity(controlPointInfo, globalSliderVelocity, normalisedStartTime, clockRate);
            EffectiveBPM = currentControlPoint.BPM * currentSliderVelocity;
        }

        public static List<DifficultyHitObject> FromHitObjects(
            IEnumerable<HitObject> hitObjects,
            double clockRate,
            ControlPointInfo controlPointInfo,
            double globalSliderVelocity)
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
                .Where(hitObject => hitObject is Hit)
                .ForEachPair((previous, current) =>
                {
                    var previousHit = (Hit)previous;
                    var currentHit = (Hit)current;

                    if (previousHit.Type != currentHit.Type)
                        monoStreak = new List<TaikoDifficultyHitObject>();

                    var difficultyHitObject = new TaikoDifficultyHitObject(
                        current, previous, clockRate,
                        difficultyHitObjects,
                        dhoByColour[currentHit.Type],
                        monoStreak,
                        patternFields,
                        difficultyHitObjects.Count,
                        controlPointInfo,
                        globalSliderVelocity);

                    difficultyHitObjects.Add(difficultyHitObject);
                });

            return difficultyHitObjects;
        }

        private static double calculateSliderVelocity(ControlPointInfo controlPointInfo, double globalSliderVelocity, double startTime, double clockRate)
        {
            var activeEffectControlPoint = controlPointInfo.EffectPointAt(startTime);
            return globalSliderVelocity * activeEffectControlPoint.ScrollSpeed * clockRate;
        }

        public TaikoDifficultyHitObject? PreviousMono(int backwardsIndex) => monoDifficultyHitObjects?.ElementAtOrDefault(MonoIndex - (backwardsIndex + 1));
    }
}
