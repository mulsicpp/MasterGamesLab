using NUnit.Framework.Constraints;
using UnityEngine;

namespace Map.Fleet
{
    public abstract class SmoothDriving
    {
        protected readonly Vehicle vehicle;
        public abstract float VisualProgress { get; }

        protected SmoothDriving(Vehicle vehicle) {
            this.vehicle = vehicle;
            Reset();
        }

        public abstract void AddProgressUpdate(Vehicle.VehicleProgressState state, double serverTime);
        public abstract void Reset();
        public abstract void Tick(float tickDuration);
    }

    public class SmoothDrivingPredictNewest : SmoothDriving
    {
        public override float VisualProgress => bestState.Progress + (float)(vehicle.BaseSpeedTPS * (Time.timeAsDouble - stateClientTime));
        private Vehicle.VehicleProgressState bestState;
        private double stateServerTime;
        private double stateClientTime;

        public SmoothDrivingPredictNewest(Vehicle vehicle) : base(vehicle) { }
        public override void AddProgressUpdate(Vehicle.VehicleProgressState state, double serverTime) {
            double deltaClientTime = Time.timeAsDouble - stateClientTime;
            double deltaServerTime = serverTime - stateServerTime;

            if (deltaServerTime > deltaClientTime)
            {
                bestState = state;
                stateServerTime = serverTime;
                stateClientTime = Time.timeAsDouble;
            }
        }

        public override void Reset()
        {
            bestState = vehicle.ProgressState;
            bestState.Progress = 0;
            stateServerTime = 0;
            stateClientTime = Time.timeAsDouble;
        }

        public override void Tick(float tickDuration) { }
    }

    public class SmoothDrivingLinearSimulationInterpolation : SmoothDriving
    {
        public override float VisualProgress
        {
            get
            {
                float oldVisualProgress = oldProgress + vehicle.SpeedAt(oldProgress) * (Time.time - Time.fixedTime);
                float newVisualProgress = newProgress + vehicle.SpeedAt(newProgress) * (Time.time - Time.fixedTime);
                return oldVisualProgress * (1 - NewProgressWeight) + newVisualProgress * NewProgressWeight;
            }
        }

        private float oldProgress, newProgress;
        private float lastUpdateTime;

        private const float INTERPOLATION_DURATION = 0.3f;

        private float NewProgressWeight => Mathf.Clamp((Time.time - lastUpdateTime) / INTERPOLATION_DURATION, 0, 1);

        public SmoothDrivingLinearSimulationInterpolation(Vehicle vehicle) : base(vehicle) { }

        public override void AddProgressUpdate(Vehicle.VehicleProgressState state, double serverTime)
        {
            oldProgress = oldProgress * (1 - NewProgressWeight) + newProgress * NewProgressWeight;
            newProgress = state.Progress;
            lastUpdateTime = Time.time;
        }

        public override void Reset()
        {
            oldProgress = 0;
            newProgress = 0;
            lastUpdateTime = Time.time;
        }

        public override void Tick(float tickDuration)
        {
            if(vehicle.Exists && vehicle.IsDriving)
            {
                oldProgress += tickDuration * vehicle.SpeedAt(oldProgress);
                newProgress += tickDuration * vehicle.SpeedAt(newProgress);
            }
        }
    }
}