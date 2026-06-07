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
    }

    public class SmoothDrivingNone : SmoothDriving
    {
        public override float VisualProgress => vehicle.RouteProgress;

        public SmoothDrivingNone(Vehicle vehicle) : base(vehicle) { }

        public override void AddProgressUpdate(Vehicle.VehicleProgressState state, double serverTime) {}
        public override void Reset() {}
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
    }
}