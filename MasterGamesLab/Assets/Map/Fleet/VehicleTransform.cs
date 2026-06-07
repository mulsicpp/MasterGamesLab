using UnityEngine;

namespace Map.Fleet
{
    public class VehicleTransform
    {
        public Vector3 Position;
        public Vector3 Up;
        public Vector3 Forward;

        public VehicleTransform AdjustUpVector()
        {
            var sideways = Vector3.Cross(Forward, Up);

            Up = Vector3.Cross(sideways, Forward).normalized;

            return this;
        }

        public VehicleTransform AdjustForwardVector()
        {
            var sideways = Vector3.Cross(Up, Forward);

            Forward = Vector3.Cross(sideways, Up).normalized;

            return this;
        }

    }
}