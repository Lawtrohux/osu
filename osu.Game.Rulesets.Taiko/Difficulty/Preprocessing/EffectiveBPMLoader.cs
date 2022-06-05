using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing
{
    public class EffectiveBPMLoader
    {
        private IBeatmap beatmap;
        private readonly IList<TaikoDifficultyHitObject> hitObjects;
        private readonly IReadOnlyList<TimingControlPoint> controlPoints;
        private readonly double beatmapGlobalSv = 1.0;

        public EffectiveBPMLoader(IBeatmap beatmap, List<TaikoDifficultyHitObject> hitObjects)
        {
            controlPoints = beatmap.ControlPointInfo.TimingPoints;
            beatmapGlobalSv = beatmap.Difficulty.SliderMultiplier;
            this.beatmap = beatmap;
            this.hitObjects = hitObjects;
        }

        public void LoadEffectiveBPM()
        {
            using IEnumerator<TimingControlPoint> controlPointEnumerator = controlPoints.GetEnumerator();
            controlPointEnumerator.MoveNext();
            TimingControlPoint currentControlPoint = controlPointEnumerator.Current;
            TimingControlPoint nextControlPoint = controlPointEnumerator.MoveNext() ? controlPointEnumerator.Current : null;

            using IEnumerator<TaikoDifficultyHitObject> hitObjectEnumerator = hitObjects.GetEnumerator();

            while (hitObjectEnumerator.MoveNext())
            {
                TaikoDifficultyHitObject currentHitObject = hitObjectEnumerator.Current;

                if (nextControlPoint != null && currentHitObject.StartTime > nextControlPoint.Time)
                {
                    currentControlPoint = nextControlPoint;
                    nextControlPoint = controlPointEnumerator.MoveNext() ? controlPointEnumerator.Current : null;
                }

                currentHitObject.EffectiveBPM = currentControlPoint.BPM * beatmapGlobalSv * currentHitObject.BaseObject.DifficultyControlPoint.SliderVelocity;
            }
        }
    }
}
