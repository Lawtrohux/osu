// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Rulesets.Taiko.Difficulty
{
    public class TaikoPerformanceAttributes : PerformanceAttributes
    {
        [JsonProperty("mechanical_difficulty")]
        public double MechanicalDifficulty { get; set; }

        [JsonProperty("rhythm_difficulty")]
        public double RhythmDifficulty { get; set; }

        [JsonProperty("reading_difficulty")]
        public double ReadingDifficulty { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("effective_miss_count")]
        public double EffectiveMissCount { get; set; }

        [JsonProperty("estimated_unstable_rate")]
        public double? EstimatedUnstableRate { get; set; }

        public override IEnumerable<PerformanceDisplayAttribute> GetAttributesForDisplay()
        {
            foreach (var attribute in base.GetAttributesForDisplay())
                yield return attribute;

            yield return new PerformanceDisplayAttribute(nameof(MechanicalDifficulty), "Mechanical Difficulty", MechanicalDifficulty);
            yield return new PerformanceDisplayAttribute(nameof(RhythmDifficulty), "Rhythm Difficulty", RhythmDifficulty);
            yield return new PerformanceDisplayAttribute(nameof(ReadingDifficulty), "Reading Difficulty", ReadingDifficulty);
            yield return new PerformanceDisplayAttribute(nameof(Accuracy), "Accuracy", Accuracy);
        }
    }
}
