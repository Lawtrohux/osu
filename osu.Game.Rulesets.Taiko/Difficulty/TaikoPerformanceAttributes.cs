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

        [JsonProperty("stamina_performance")]
        public double StaminaPerformance { get; set; }

        [JsonProperty("rhythm_performance")]
        public double RhythmPerformance { get; set; }

        [JsonProperty("colour_performance")]
        public double ColourPerformance { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        public override IEnumerable<PerformanceDisplayAttribute> GetAttributesForDisplay()
        {
            foreach (var attribute in base.GetAttributesForDisplay())
                yield return attribute;

            yield return new PerformanceDisplayAttribute(nameof(TotalPerformance), "totalPerformance", TotalPerformance);
            yield return new PerformanceDisplayAttribute(nameof(StaminaPerformance), "StaminaPerformance", StaminaPerformance);
            yield return new PerformanceDisplayAttribute(nameof(RhythmPerformance), "RhythmPerformance", RhythmPerformance);
            yield return new PerformanceDisplayAttribute(nameof(ColourPerformance), "ColourPerformance", ColourPerformance);

            yield return new PerformanceDisplayAttribute(nameof(Accuracy), "Accuracy", Accuracy);
        }
    }
}
