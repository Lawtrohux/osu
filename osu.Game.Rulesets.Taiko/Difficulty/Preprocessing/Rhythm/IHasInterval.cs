// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm
{
    /// <summary>
    /// Represents a hitobject that provides an interval value.
    /// The interval refers to the rhythmic timing the current hit object is placed on.
    /// </summary>
    public interface IHasInterval
    {
        // Gets the rhythmic interval associated with this object.
        double Interval { get; }
    }
}
