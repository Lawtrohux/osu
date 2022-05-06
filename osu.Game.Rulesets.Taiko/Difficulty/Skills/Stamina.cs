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
    class SingleKeyStamina
    {
        private const int max_history_length = 2;

        private LimitedCapacityQueue<double> IntervalHistory = new LimitedCapacityQueue<double>(max_history_length);
        private double PreviousHitTime = -1;
        // private double CurrentStrain = 0;
        private double StrainDecayBase = 0.2;

        private double StrainValueOf(DifficultyHitObject current)
        {
            if (PreviousHitTime == -1)
            {
                PreviousHitTime = current.StartTime;
                return 0;
            }
            else
            {
                double objectStrain = 0.3;
                IntervalHistory.Enqueue(current.StartTime - PreviousHitTime);
                PreviousHitTime = current.StartTime;
                objectStrain += speedBonus(IntervalHistory.Min());
                return objectStrain;
            }
        }

        public double StrainValueAt(DifficultyHitObject current)
        {
            // CurrentStrain *= strainDecay(current.StartTime - PreviousHitTime);
            // CurrentStrain += StrainValueOf(current);

            return StrainValueOf(current);
        }

        private double strainDecay(double ms) => Math.Pow(StrainDecayBase, ms / 1000);

        /// <summary>
        /// Applies a speed bonus dependent on the time since the last hit performed using this key.
        /// </summary>
        /// <param name="notePairDuration">The duration between the current and previous note hit using the same key.</param>
        private double speedBonus(double notePairDuration)
        {
            return 175 / Math.Pow(notePairDuration + 100, 1);
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
        protected override double SkillMultiplier => 1;
        protected override double StrainDecayBase => 0.4;

        private SingleKeyStamina[] keyStamina = new SingleKeyStamina[4] {
            new SingleKeyStamina(),
            new SingleKeyStamina(),
            new SingleKeyStamina(),
            new SingleKeyStamina()
        };
        private int donIndex = 1;
        private int katIndex = 3;

        /// <summary>
        /// Creates a <see cref="Stamina"/> skill.
        /// </summary>
        /// <param name="mods">Mods for use in skill calculations.</param>
        public Stamina(Mod[] mods)
            : base(mods)
        {
        }

        private SingleKeyStamina getNextSingleKeyStamina(TaikoDifficultyHitObject current)
        {
            if (current.HitType == HitType.Centre)
            {
                donIndex = donIndex == 0 ? 1 : 0;
                // Console.Write(donIndex + ",");
                return keyStamina[donIndex];
            }
            else
            {
                katIndex = katIndex == 2 ? 3 : 2;
                // Console.Write(katIndex + ",");
                return keyStamina[katIndex];
            }
        }

        // This is the same sigmoid as the one in Rhythm, might want to unify these
        private double sigmoid(double val, double center, double width)
        {
            return Math.Tanh(Math.E * -(val - center) / width);
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            if (!(current.BaseObject is Hit))
            {
                return 0.0;
            }


            // Console.Write(current.BaseObject.StartTime + ",");
            TaikoDifficultyHitObject hitObject = (TaikoDifficultyHitObject)current;
            double objectStrain = getNextSingleKeyStamina(hitObject).StrainValueAt(hitObject);
            // Console.WriteLine(objectStrain);

            // if (hitObject.StaminaCheese)
            //     objectStrain *= cheesePenalty(hitObject.DeltaTime);

            return objectStrain;
        }

        /// <summary>
        /// Applies a penalty for hit objects marked with <see cref="TaikoDifficultyHitObject.StaminaCheese"/>.
        /// </summary>
        /// <param name="notePairDuration">The duration between the current and previous note hit using the same finger.</param>
        private double cheesePenalty(double notePairDuration)
        {
            if (notePairDuration > 125) return 1;
            if (notePairDuration < 100) return 0.8;

            return 0.8 + (notePairDuration - 100) * 0.008;
        }
    }
}
