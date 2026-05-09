using System;
using UnityEngine;

namespace GeometryGeneration.Projections
{
    public class BerghausStarProjection : IProjection
    {
        public Vector3 Center;
        public float Radius;
        public int Lobes;

        public BerghausStarProjection(Vector3 center, float radius, int lobes = 5)
        {
            Center = center;
            Radius = radius;
            Lobes = lobes;
        }

        /// <summary>
        /// Performs a Berghaus Star projection on a sphere, laying the result onto 
        /// the 3D tangent plane at the projection Center.
        /// </summary>
        /// <param name="point">The 3D point on the surface of the sphere to be projected.</param>
        /// <returns>A 3D Vector representing the projection on the tangent plane.</returns>
        public Vector3 Project(Vector3 point)
        {
            // 1. Normalize the center and the point to work strictly with directional geometry
            Vector3 cNorm = Center.normalized;
            Vector3 vNorm = point.normalized;

            // 2. Establish a local orthonormal basis (East, North, Center) at the projection center
            Vector3 NP = new Vector3(0, 1, 0); // Global North Pole
            Vector3 N; // Local North (Y-axis representation on the tangent plane)

            // Handle the singularity if the center is at or extremely close to the poles
            if (Mathf.Abs(cNorm.y) > 0.9999f)
            {
                // If the center is the North/South Pole, point N towards the Prime Meridian
                N = cNorm.y > 0 ? new Vector3(0, 0, -1) : new Vector3(0, 0, 1);
            }
            else
            {
                // Project the global North Pole onto the tangent plane at the center
                float dot = Vector3.Dot(NP, cNorm);
                N = (NP - (cNorm * dot)).normalized;
            }

            // Local East (X-axis representation on the tangent plane)
            Vector3 E = Vector3.Cross(N, cNorm).normalized;

            // 3. Project the input point onto the local basis (Use doubles to guarantee precision near bounds)
            double vx = Vector3.Dot(vNorm, E);
            double vy = Vector3.Dot(vNorm, N);
            double vz = Vector3.Dot(vNorm, cNorm);

            // Clamp vz to valid domain [-1, 1] to avoid NaNs in Math.Acos
            vz = Math.Max(-1.0, Math.Min(1.0, vz));

            // 4. Calculate polar coordinates based on Azimuthal Equidistant math
            double len = Math.Sqrt(vx * vx + vy * vy);
            double c = Math.Acos(vz); // Angular distance from center (in range[0, PI])

            double r = c; // Radial distance from the center on the resulting 2D map
            double theta;

            // Handle mathematical singularity at the projection center or its exact antipode
            if (len < 1e-6)
            {
                theta = Math.PI / 2.0; // Arbitrary valid angle (defaults pointing "Up/North")
            }
            else
            {
                theta = Math.Atan2(vy, vx);
            }

            // 5. Berghaus star modification exclusively for the back hemisphere (distance > PI/2 or vz < 0)
            if (vz < 0.0)
            {
                double pi = Math.PI;
                double halfPi = pi / 2.0;
                double k_lobe = 2.0 * pi / Math.Max(3, Lobes); // Safeguard minimum lobes

                // Identify which lobe this angle belongs to
                double lobeIndex = Math.Floor((theta - halfPi) / k_lobe + 0.5);
                double theta0 = k_lobe * lobeIndex + halfPi;

                // Get local angle within the selected lobe
                double dTheta = theta - theta0;

                // Apply Berghaus angular distortion constraint
                double alpha = Math.Atan2(Math.Sin(dTheta), 2.0 - Math.Cos(dTheta));
                theta = theta0 + Math.Asin((pi / r) * Math.Sin(alpha)) - alpha;
            }

            // 6. Convert back to pure 2D Cartesian coordinates (scaled by Radius)
            float mapX = (float)(r * Math.Cos(theta)) * Radius;
            float mapY = (float)(r * Math.Sin(theta)) * Radius;

            // 7. Place the 2D coordinates onto the 3D tangent plane at the Center point
            // The center of the tangent plane touching the sphere's surface
            Vector3 tangentOrigin = cNorm * Radius;

            // Extend outward from the tangent origin along the plane's local East and North axes
            return tangentOrigin + (E * mapX) + (N * mapY);
        }
    }
}