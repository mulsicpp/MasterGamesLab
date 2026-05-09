using System.Collections.Generic;
using GeometryGeneration.Projections;
using UnityEngine;

namespace GeometryGeneration
{
    public class Map : MonoBehaviour
    {
        [SerializeField] private int radius = 10;
        [SerializeField] private int resolution = 5;
        [SerializeField] private float hexSize = 1;
        [SerializeField] private int maxSpawn = 10;

        private HexagonalSphere hexagonalSphere;
        private MeshFilter meshFilter;


        [SerializeField] private float fullSphereDistance = 2;
        [SerializeField] private float fullProjectionDistance = 1.5f;

        [SerializeField] private PlanetCameraController planetCamera;

        private Vector3 oldProjectionCenter;
        private float oldProjectionFactor;

        private void Start()
        {
            meshFilter = GetComponent<MeshFilter>();
            hexagonalSphere = new HexagonalSphere(radius, resolution, hexSize);
        }

        private void Update()
        {
            var projectionCenter = (planetCamera.transform.position - transform.position).normalized;
            var currentDistance = planetCamera.CurrentDistance;
            var projectionFactor = (currentDistance - fullSphereDistance) /
                                   (fullProjectionDistance - fullSphereDistance);

            projectionFactor = Mathf.Clamp01(projectionFactor);

            if (oldProjectionCenter == projectionCenter && Mathf.Approximately(oldProjectionFactor, projectionFactor))
            {
                return;
            }

            var projection = new BerghausStarProjection(projectionCenter.normalized, radius);

            hexagonalSphere.UpdateTiles(projection, projectionFactor);

            oldProjectionCenter = projectionCenter;
            oldProjectionFactor = projectionFactor;

            var mapMesh = hexagonalSphere.GenerateMesh();
            var mesh = new Mesh
            {
                vertices = mapMesh.Vertices.ToArray(),
                triangles = mapMesh.Triangles.ToArray()
            };
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
        }
    }
}