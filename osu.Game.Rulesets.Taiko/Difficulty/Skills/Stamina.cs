// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    class NoteIntervalManager
    {
        /// <summary>
        /// Maximum number of entries to keep in interval histories/>.
        /// </summary>
        private const int max_history_length = 2;

        /// <summary>
        /// We assume notes of the same color is always alternated, hence having the highest possible hit interval per finger.
        /// Finger hit delta time stored are d1, d2, k1, k2, in that order.
        ///
        /// TODO: Is there a prettier way to do this?
        /// </summary>
        private LimitedCapacityQueue<double>[] noteIntervalHistories = new[] {
            new LimitedCapacityQueue<double>(max_history_length),
            new LimitedCapacityQueue<double>(max_history_length),
            new LimitedCapacityQueue<double>(max_history_length),
            new LimitedCapacityQueue<double>(max_history_length)
        };
        private double[] previousHitTime = { 0, 0, 0, 0 };
        private int donIndex = 1;
        private int katIndex = 3;

        private LimitedCapacityQueue<double> hit(TaikoDifficultyHitObject current, int indexOffset)
        {
            noteIntervalHistories[indexOffset].Enqueue(previousHitTime[indexOffset] == 0 ? double.MaxValue : current.StartTime - previousHitTime[indexOffset]);
            previousHitTime[indexOffset] = current.StartTime;
            return noteIntervalHistories[indexOffset];
        }

        /// <summary>
        /// Calculates and returns note interval history for the current hitting finger
        /// </summary>
        public LimitedCapacityQueue<double> hit(TaikoDifficultyHitObject current)
        {
            if (current.HitType == HitType.Centre)
            {
                donIndex = donIndex == 0 ? 1 : 0;
                return hit(current, donIndex);
            }
            else
            {
                katIndex = katIndex == 2 ? 3 : 2;
                return hit(current, katIndex);
            }
        }
    }

    /// <summary>
    /// Calculates the stamina coefficient of taiko difficulty.
    /// </summary>
    /// <remarks>
    /// The reference play style chosen uses two hands, with full alternating (the hand changes after every hit).
    /// </remarks>
    public class Stamina : StrainDecaySkill
    {
        protected override double SkillMultiplier => 0.51;
        protected override double StrainDecayBase => 0.4;

        private NoteIntervalManager noteIntervalManager = new NoteIntervalManager();

        /// <summary>
        /// Creates a <see cref="Stamina"/> skill.
        /// </summary>
        /// <param name="mods">Mods for use in skill calculations.</param>
        public Stamina(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            if (!(current.BaseObject is Hit))
            {
                return 0.0;
            }

            double objectStrain = 1;

            TaikoDifficultyHitObject hitObject = (TaikoDifficultyHitObject)current;
            var durationHistory = noteIntervalManager.hit(hitObject);
            double shortestRecentNote = durationHistory.Average();
            objectStrain += speedBonus(shortestRecentNote);
            return objectStrain;
        }

        /// <summary>
        /// Applies a penalty for hit objects marked with <see cref="TaikoDifficultyHitObject.StaminaCheese"/>.
        /// </summary>
        /// <param name="notePairDuration">The duration between the current and previous note hit using the same finger.</param>
        private double cheesePenalty(double notePairDuration)
        {
            if (notePairDuration > 125) return 1;
            if (notePairDuration < 100) return 0.6;

            return 0.6 + (notePairDuration - 100) * 0.016;
        }

        // This is the same sigmoid function as the rhythm rework one, might want to find a way to unify them.
        private double sigmoid(double val, double center, double width)
        {
            return Math.Tanh(Math.E * -(val - center) / width);
        }


        /// <summary>
        /// Applies a speed bonus dependent on the time since the last hit performed using this hand.
        /// </summary>
        /// <param name="notePairDuration">The duration between the current and previous note hit using the same finger.</param>
        private double speedBonus(double notePairDuration)
        {
            return Math.Min(250 / notePairDuration, 4);
            // if (notePairDuration >= 200) return 0;

            // double bonus = 200 - notePairDuration;
            // bonus *= bonus;
            // return bonus / 100000;
        }
    }
}