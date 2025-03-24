// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Pattern
{
    public class TaikoPatternFields
    {
        public TaikoRhythmicAlignmentField RhythmField;

        public TaikoRhythmicAlignmentField CentreField;

        public TaikoRhythmicAlignmentField RimField;

        public TaikoRhythmicAlignmentField ColourChangeField;

        public TaikoPatternFields()
        {
            CentreField = new TaikoRhythmicAlignmentField(4, 0.5, 0.7071);
            RimField = new TaikoRhythmicAlignmentField(4, 0.5, 0.7071);
            ColourChangeField = new TaikoRhythmicAlignmentField(4, 0.5, 0.7071);
        }
    }
}
