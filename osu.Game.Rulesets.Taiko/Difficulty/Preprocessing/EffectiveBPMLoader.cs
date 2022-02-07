using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing
{
    public class EffectiveBPMLoader
    {
        private IBeatmap beatmap;
        private IList<TaikoDifficultyHitObject> hitObjects;
        private IReadOnlyList<TimingControlPoint> controlPoints;
        private double beatmapGlobalSv = 1.0;

        public EffectiveBPMLoader(IBeatmap beatmap, List<TaikoDifficultyHitObject> hitObjects)
        {
            this.beatmap = beatmap;
            this.hitObjects = hitObjects;
            this.controlPoints = beatmap.ControlPointInfo.TimingPoints;
            this.beatmapGlobalSv = beatmap.Difficulty.SliderMultiplier;
        }

        public void LoadEffectiveBPM()
        {
            IEnumerator<TimingControlPoint> controlPointEnumerator = controlPoints.GetEnumerator();
            controlPointEnumerator.MoveNext();
            TimingControlPoint currentControlPoint = controlPointEnumerator.Current;
            TimingControlPoint nextControlPoint = controlPointEnumerator.MoveNext() ? controlPointEnumerator.Current : null;

            IEnumerator<TaikoDifficultyHitObject> hitObjectEnumerator = hitObjects.GetEnumerator();
            while(hitObjectEnumerator.MoveNext())
            {
                TaikoDifficultyHitObject currentHitObject = hitObjectEnumerator.Current;

                if (nextControlPoint != null && currentHitObject.StartTime > nextControlPoint.Time)
                {
                    currentControlPoint = nextControlPoint;
                    nextControlPoint = controlPointEnumerator.MoveNext() ? controlPointEnumerator.Current : null;
                }

                currentHitObject.EffectiveBPM = currentControlPoint.BPM * beatmapGlobalSv;
            }
        }
    }
}