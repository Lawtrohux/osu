// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public static class ColourEvaluator
    {
        // TODO - Share this sigmoid
        private static double sigmoid(double val, double center, double width)
        {
            return Math.Tanh(Math.E * -(val - center) / width);
        }

        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            TaikoDifficultyHitObject taikoCurrent = (TaikoDifficultyHitObject)current;
            TaikoDifficultyHitObjectColour colour = taikoCurrent.Colour;
            if (colour == null) return 0;

            double objectStrain = 1.85;

            if (colour.Delta)
            {
                objectStrain *= sigmoid(colour.DeltaRunLength, 3, 3) * 0.5 + 0.5;
            }
            else
            {
                objectStrain *= sigmoid(colour.DeltaRunLength, 2, 2) * 0.5 + 0.5;
            }

            objectStrain *= -sigmoid(colour.RepetitionInterval, 1, 8);
            return objectStrain;
        }
    }
}
