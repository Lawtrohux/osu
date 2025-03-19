// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Pattern
{
    public class TaikoDifficultyHitObjectPattern
    {
        public TaikoRhythmicAlignmentObject Rhythm;

        public TaikoRhythmicAlignmentObject? Mono;

        public TaikoRhythmicAlignmentObject? ColourChange;

        public TaikoDifficultyHitObjectPattern(
            TaikoDifficultyHitObject hitObject,
            TaikoPatternFields patternFields)
        {
            Rhythm = patternFields.RhythmField.Add(hitObject);

            if (hitObject.IsColourChange)
            {
                ColourChange = patternFields.ColourChangeField.Add(hitObject);
            }

            switch ((hitObject.BaseObject as Hit)?.Type)
            {
                case HitType.Centre:
                    Mono = patternFields.CentreField.Add(hitObject);
                    break;

                case HitType.Rim:
                    Mono = patternFields.RimField.Add(hitObject);
                    break;
            }
        }
    }
}

