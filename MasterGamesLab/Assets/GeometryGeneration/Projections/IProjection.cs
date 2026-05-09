using UnityEngine;

namespace GeometryGeneration.Projections
{
    public interface IProjection
    {
        public Vector3 Project(Vector3 point);
    }
}