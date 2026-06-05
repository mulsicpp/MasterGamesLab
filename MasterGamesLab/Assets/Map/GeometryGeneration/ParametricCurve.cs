using UnityEngine;

namespace Map.GeometryGeneration
{
    public class ParametricCurve
    {
        private Vector3 a;
        private Vector3 b;
        private Vector3 c;
        private Vector3 d;

        public static ParametricCurve FromBezierPoints(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var a = -p0 + 3 * p1 - 3 * p2 + p3;
            var b = 3 * p0 - 6 * p1 + 3 * p2;
            var c = -3 * p0 + 3 * p1;
            var d = p0;
            return new ParametricCurve(a, b, c, d);
        }

        private ParametricCurve(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public Vector3 Evaluate(float t) => ((a * t + b) * t + c) * t + d;

        public Vector3 Derivative(float t) => (3 * a * t + 2 * b) * t + c;
    }
}