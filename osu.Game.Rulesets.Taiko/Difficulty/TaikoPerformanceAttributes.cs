// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Rulesets.Taiko.Difficulty
{
    public class TaikoPerformanceAttributes : PerformanceAttributes
    {
        [JsonProperty("total_performance")]
        public double TotalPerformance { get; set; }

        [JsonProperty("RawDifficulty")]
        public double TotalDifficulty { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("stamina")]
        public double Stamina { get; set; }

        [JsonProperty("rhythm")]
        public double Rhythm { get; set; }

        [JsonProperty("colour")]
        public double Colour { get; set; }

        public override IEnumerable<PerformanceDisplayAttribute> GetAttributesForDisplay()
        {
            foreach (var attribute in base.GetAttributesForDisplay())
                yield return attribute;

            yield return new PerformanceDisplayAttribute(nameof(TotalPerformance), "totalPerformance", TotalPerformance);
            yield return new PerformanceDisplayAttribute(nameof(TotalDifficulty), "totalDifficulty", TotalDifficulty);
            yield return new PerformanceDisplayAttribute(nameof(Stamina), "Stamina", Stamina);
            yield return new PerformanceDisplayAttribute(nameof(Rhythm), "Rhythm", Rhythm);
            yield return new PerformanceDisplayAttribute(nameof(Colour), "Colour", Colour);

            yield return new PerformanceDisplayAttribute(nameof(Accuracy), "Accuracy", Accuracy);
        }
    }
}
