// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    /// <summary>
    /// Calculates the rhythm coefficient of taiko difficulty.
    /// </summary>
    public class Rhythm : StrainDecaySkill
    {
        protected override double SkillMultiplier => 3.1;
        protected override double StrainDecayBase => 0;

        /// <summary>
        /// The note-based decay for rhythm strain.
        /// </summary>
        /// <remarks>
        /// <see cref="StrainDecayBase"/> is not used here, as it's time- and not note-based.
        /// </remarks>
        private const double strain_decay = 0.96;

        /// <summary>
        /// Maximum number of entries in <see cref="rhythmHistory"/>.
        /// </summary>
        private const int rhythm_history_max_length = 8;

        /// <summary>
        /// Contains the last <see cref="rhythm_history_max_length"/> changes in note sequence rhythms.
        /// </summary>
        private readonly LimitedCapacityQueue<TaikoDifficultyHitObject> rhythmHistory = new LimitedCapacityQueue<TaikoDifficultyHitObject>(rhythm_history_max_length);

        /// <summary>
        /// Contains the rolling rhythm strain.
        /// Used to apply per-note decay.
        /// </summary>
        private double currentStrain;

        /// <summary>
        /// Number of notes since the last rhythm change has taken place.
        /// </summary>
        private int notesSinceRhythmChange;

        private double greatHitWindow;

        public Rhythm(Mod[] mods, double greatHitWindow)
            : base(mods)
        {
            this.greatHitWindow = greatHitWindow;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            // Drum rolls and Swells are exempt.
            if (!(current.BaseObject is Hit))
            {
                resetRhythmAndStrain();
                return 0.0;
            }

            currentStrain *= strain_decay;

            TaikoDifficultyHitObject hitObject = (TaikoDifficultyHitObject)current;
            notesSinceRhythmChange += 1;

            double objectStrain = hitObject.Rhythm.Difficulty;

            objectStrain *= speedPenalty(hitObject.DeltaTime);
            objectStrain *= leniencyPenalty(hitObject);

            currentStrain += objectStrain;
            return currentStrain;
        }

        /// <summary>
        /// Applies a penalty if they hit objects can be cheesed by being hit off-time.
        /// </summary>
        /// <remarks>
        /// Takes the maximum leniency between past to current and current to next hitobjects
        /// </remarks>
        private double leniencyPenalty(TaikoDifficultyHitObject hitObject)
        {
            double penalty = sigmoid(hitObject.Rhythm.Leniency, 0.5, 0.3) * 0.5 + 0.5;
            return penalty;
        }

        private double sigmoid(double val, double center, double width)
        {
            return Math.Tanh(Math.E * -(val - center) / width);
        }

        /// <summary>
        /// Determines whether the rhythm change pattern starting at <paramref name="start"/> is a repeat of any of the
        /// <paramref name="mostRecentPatternsToCompare"/>.
        /// </summary>
        private bool samePattern(int start, int mostRecentPatternsToCompare)
        {
            for (int i = 0; i < mostRecentPatternsToCompare; i++)
            {
                if (!rhythmHistory[start + i].Rhythm.Equals(rhythmHistory[rhythmHistory.Count - mostRecentPatternsToCompare + i].Rhythm))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates a single rhythm repetition penalty.
        /// </summary>
        /// <param name="notesSince">Number of notes since the last repetition of a rhythm change.</param>
        private static double repetitionPenalty(int notesSince) => Math.Min(1.0, 0.06 * notesSince);

        /// <summary>
        /// Calculates a penalty for objects that do not require alternating hands.
        /// </summary>
        /// <param name="deltaTime">Time (in milliseconds) since the last hit object.</param>
        private double speedPenalty(double deltaTime)
        {
            if(deltaTime > 300) {
                resetRhythmAndStrain();
            }

            return sigmoid(deltaTime, 160, 180) * 0.5 + 0.5;
        }

        /// <summary>
        /// Resets the rolling strain value and <see cref="notesSinceRhythmChange"/> counter.
        /// </summary>
        private void resetRhythmAndStrain()
        {
            currentStrain = 0.0;
            notesSinceRhythmChange = 0;
        }
    }
}
