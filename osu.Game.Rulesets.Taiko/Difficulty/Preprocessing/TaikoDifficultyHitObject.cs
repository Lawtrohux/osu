// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Pattern;
using osu.Game.Rulesets.Taiko.Difficulty.Utils;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing
{
    /// <summary>
    /// Represents a single hit object in taiko difficulty calculation.
    /// </summary>
    public class TaikoDifficultyHitObject : DifficultyHitObject, IHasInterval
    {
        private readonly IReadOnlyList<TaikoDifficultyHitObject>? monoDifficultyHitObjects;
        private readonly IReadOnlyList<TaikoDifficultyHitObject> noteDifficultyHitObjects;

        public readonly int MonoIndex;
        public readonly int NoteIndex;

        public readonly TaikoRhythmData RhythmData;
        public readonly TaikoColourData ColourData;

        public readonly TaikoDifficultyHitObjectPattern Pattern;

        /// <summary>
        /// The list of consecutive same-colour objects (mono streak) this hit object belongs to.
        /// </summary>
        public IReadOnlyList<TaikoDifficultyHitObject> MonoStreak { get; private set; }

        public readonly int MonoStreakIndex;

        public TaikoDifficultyHitObject? PreviousColourChange => MonoStreak.First();
        public TaikoDifficultyHitObject? NextColourChange => MonoStreak.Last().NextNote(0);
        public bool IsColourChange => PreviousColourChange == this;

        public double EffectiveBPM;
        public double Interval => DeltaTime;

        public TaikoDifficultyHitObject(HitObject hitObject, HitObject lastObject, double clockRate,
                                        List<DifficultyHitObject> objects,
                                        List<TaikoDifficultyHitObject> centreHitObjects,
                                        List<TaikoDifficultyHitObject> rimHitObjects,
                                        List<TaikoDifficultyHitObject> noteObjects,
                                        List<TaikoDifficultyHitObject> monoStreak,
                                        TaikoPatternFields patternFields,
                                        int index,
                                        ControlPointInfo controlPointInfo,
                                        double globalSliderVelocity)
            : base(hitObject, lastObject, clockRate, objects, index)
        {
            noteDifficultyHitObjects = noteObjects;
            ColourData = new TaikoColourData();
            RhythmData = new TaikoRhythmData(this);

            if (hitObject is Hit hit)
            {
                switch (hit.Type)
                {
                    case HitType.Centre:
                        MonoIndex = centreHitObjects.Count;
                        centreHitObjects.Add(this);
                        monoDifficultyHitObjects = centreHitObjects;
                        break;

                    case HitType.Rim:
                        MonoIndex = rimHitObjects.Count;
                        rimHitObjects.Add(this);
                        monoDifficultyHitObjects = rimHitObjects;
                        break;
                }

                NoteIndex = noteObjects.Count;
                noteObjects.Add(this);
            }

            // Pattern system setup
            MonoStreakIndex = monoStreak.Count;
            monoStreak.Add(this);
            MonoStreak = monoStreak;

            Pattern = new TaikoDifficultyHitObjectPattern(this, patternFields);

            double normalisedStartTime = StartTime * clockRate;
            TimingControlPoint currentControlPoint = controlPointInfo.TimingPointAt(normalisedStartTime);
            double currentSliderVelocity = calculateSliderVelocity(controlPointInfo, globalSliderVelocity, normalisedStartTime, clockRate);

            EffectiveBPM = currentControlPoint.BPM * currentSliderVelocity;
        }

        private static double calculateSliderVelocity(ControlPointInfo controlPointInfo, double globalSliderVelocity, double startTime, double clockRate)
        {
            var activeEffectControlPoint = controlPointInfo.EffectPointAt(startTime);
            return globalSliderVelocity * (activeEffectControlPoint.ScrollSpeed) * clockRate;
        }

        public TaikoDifficultyHitObject? PreviousMono(int backwardsIndex) => monoDifficultyHitObjects?.ElementAtOrDefault(MonoIndex - (backwardsIndex + 1));
        public TaikoDifficultyHitObject? NextMono(int forwardsIndex) => monoDifficultyHitObjects?.ElementAtOrDefault(MonoIndex + (forwardsIndex + 1));
        public TaikoDifficultyHitObject? PreviousNote(int backwardsIndex) => noteDifficultyHitObjects.ElementAtOrDefault(NoteIndex - (backwardsIndex + 1));
        public TaikoDifficultyHitObject? NextNote(int forwardsIndex) => noteDifficultyHitObjects.ElementAtOrDefault(NoteIndex + (forwardsIndex + 1));
    }
}
