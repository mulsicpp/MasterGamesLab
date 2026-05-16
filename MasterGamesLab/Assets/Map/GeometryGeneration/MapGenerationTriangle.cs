using UnityEngine;

namespace Map.GeometryGeneration
{
    public struct MapGenerationTriangle
    {
        public Vector3 PointA;
        public Vector3 PointB;
        public Vector3 PointC;
        public bool IncludePointA;
        public bool IncludePointB;
        public bool IncludePointC;
        public bool IncludeEdgeAb;
        public bool IncludeEdgeBc;
        public bool IncludeEdgeCa;
    }
}